import "dotenv/config";
import net from "node:net";
import path from "node:path";
import { fileURLToPath } from "node:url";
import http from "node:http";
import express from "express";
import { WebSocketServer } from "ws";
import { GiftEngine } from "./services/giftEngine.js";
import { QuestionGenerator } from "./services/questionGenerator.js";
import { TikTokLiveBridge } from "./services/tiktokLiveBridge.js";
import { isTtsConfigured, synthesizeToAudioBase64, isElevenLabsReady } from "./services/ttsService.js";
import { isOpenAiSpeechReady } from "./services/openAiSpeechService.js";
import { MessageType, createMessage } from "./protocol/messages.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

const PORT = Number(process.env.PORT || 3000);
const WS_PORT = Number(process.env.WS_PORT || 8080);
const WS_SINGLE_PORT = String(process.env.WS_SINGLE_PORT || "false").toLowerCase() === "true";
const TIKTOK_BRIDGE_ENABLED = String(process.env.TIKTOK_BRIDGE_ENABLED ?? "true").toLowerCase() === "true";
const tiktokUsernames = [
  ...(process.env.TIKTOK_USERNAMES || "").split(","),
  process.env.TIKTOK_USERNAME || ""
]
  .map((u) => u.trim().replace(/^@/, ""))
  .filter(Boolean)
  .filter((u, i, arr) => arr.indexOf(u) === i);

const giftEngine = new GiftEngine(path.join(__dirname, "config", "gift-balance.json"));
const questionGenerator = new QuestionGenerator(process.env.OPENAI_API_KEY);
const tiktokBridge = new TikTokLiveBridge(tiktokUsernames);
const TIKTOK_RETRY_MS = Number(process.env.TIKTOK_RETRY_MS || 15000);
const recentEvents = [];
const RECENT_MAX = 200;

function pushEvent(type, payload) {
  recentEvents.push({ type, payload, ts: Date.now() });
  if (recentEvents.length > RECENT_MAX) recentEvents.shift();
}

function canListenOnPort(port) {
  return new Promise((resolve) => {
    const tester = net
      .createServer()
      .once("error", () => resolve(false))
      .once("listening", () => tester.close(() => resolve(true)))
      .listen(port);
  });
}

async function resolveAvailablePort(startPort, maxAttempts = 5) {
  for (let i = 0; i < maxAttempts; i++) {
    const tryPort = startPort + i;
    if (await canListenOnPort(tryPort)) return tryPort;
  }
  throw new Error(`Unable to resolve open port from ${startPort} to ${startPort + maxAttempts - 1}`);
}

function startHttpServer(expressApp, port) {
  const server = http.createServer(expressApp);
  return new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(port, () => resolve(server));
  });
}

async function startWebSocketServer(startPort, maxAttempts = 5) {
  for (let i = 0; i < maxAttempts; i++) {
    const tryPort = startPort + i;
    try {
      const wsServer = await new Promise((resolve, reject) => {
        const server = new WebSocketServer({ port: tryPort });
        server.once("listening", () => resolve(server));
        server.once("error", reject);
      });
      return { wsServer, port: tryPort };
    } catch (error) {
      if (error.code !== "EADDRINUSE") throw error;
    }
  }
  throw new Error(`Unable to bind WebSocket server from ${startPort} to ${startPort + maxAttempts - 1}`);
}

const boundHttpPort = await resolveAvailablePort(PORT);
const httpServer = await startHttpServer(app, boundHttpPort);

let activeWss;
let activeWsPort;
if (WS_SINGLE_PORT || WS_PORT === PORT || WS_PORT === boundHttpPort) {
  // Cloud-friendly mode: expose WS over the same public port as HTTP.
  activeWss = new WebSocketServer({ server: httpServer });
  activeWsPort = boundHttpPort;
} else {
  const { wsServer, port } = await startWebSocketServer(WS_PORT);
  activeWss = wsServer;
  activeWsPort = port;
}

function broadcast(type, payload) {
  const msg = createMessage(type, payload);
  activeWss.clients.forEach((client) => {
    if (client.readyState === 1) client.send(msg);
  });
}

activeWss.on("connection", (ws) => {
  ws.send(
    createMessage(MessageType.SYSTEM, {
      ok: true,
      text: "CongoGames WS connected",
      httpPort: boundHttpPort,
      httpApiBase: process.env.PUBLIC_HTTP_BASE || ""
    })
  );
});

app.get("/", (_req, res) => {
  res.type("html").send(`<!DOCTYPE html>
<html lang="fr"><head><meta charset="utf-8"/><title>CongoGames API</title>
<style>body{font-family:system-ui,sans-serif;max-width:42rem;margin:2rem auto;padding:0 1rem;line-height:1.5}
code{background:#f0f0f0;padding:2px 6px;border-radius:4px}a{color:#0a6}</style></head>
<body>
<h1>CongoGames — backend</h1>
<p>Le serveur tourne. Il n’y a pas de page web de jeu ici : c’est une <strong>API</strong> pour Unity et les scripts.</p>
<ul>
<li><a href="/health"><code>/health</code></a> — ports HTTP / WS + TTS</li>
<li><a href="/tts/status"><code>/tts/status</code></a> — TTS (OpenAI ou ElevenLabs) configuré ?</li>
<li><code>POST /tts</code> — formulaire <code>text=…</code> (Unity)</li>
<li><code>POST /events/chat</code>, <code>POST /events/gift</code>, <code>POST /question/generate</code></li>
</ul>
<p><strong>Ports actuels :</strong> HTTP <code>${boundHttpPort}</code>, WebSocket <code>${activeWsPort}</code></p>
</body></html>`);
});

app.get("/health", (_req, res) => {
  res.json({
    ok: true,
    service: "congogames-backend",
    httpPort: boundHttpPort,
    wsPort: activeWsPort,
    ttsEnabled: isTtsConfigured()
  });
});

app.get("/tts/status", (_req, res) => {
  res.json({
    ok: true,
    enabled: isTtsConfigured(),
    elevenLabs: isElevenLabsReady(),
    openAi: isOpenAiSpeechReady()
  });
});

app.post("/tts", async (req, res) => {
  const text = req.body?.text ?? req.body?.message;
  if (!text || typeof text !== "string") {
    return res.status(400).json({ ok: false, error: "text (string) requis" });
  }
  if (!isTtsConfigured()) {
    return res.status(503).json({
      ok: false,
      error:
        "TTS non configuré : ajoute OPENAI_API_KEY (recommandé) ou ELEVENLABS_API_KEY + ELEVENLABS_VOICE_ID dans .env"
    });
  }
  try {
    const preferPcm = String(req.body?.prefer_pcm ?? "1") !== "0";
    const audio = await synthesizeToAudioBase64(text, { preferPcm });
    res.json({ ok: true, ...audio });
  } catch (err) {
    const status = Number(err?.status ?? err?.response?.status);
    const httpStatus = status === 429 || status === 401 || status === 403 ? status : 502;
    const message = err?.message || "tts_error";
    console.error("[tts] POST /tts:", message);
    res.status(httpStatus).json({
      ok: false,
      error: message,
      code: status === 429 ? "openai_quota" : undefined
    });
  }
});

app.get("/metrics", (_req, res) => {
  res.json({
    wsClients: activeWss.clients.size,
    recentEvents: recentEvents.slice(-20)
  });
});

app.post("/events/chat", (req, res) => {
  const { user, message } = req.body;
  if (!user || !message) return res.status(400).json({ ok: false, error: "user and message required" });
  const payload = { user, message };
  pushEvent(MessageType.CHAT, payload);
  broadcast(MessageType.CHAT, payload);
  res.json({ ok: true });
});

app.post("/events/gift", (req, res) => {
  const { user, giftName } = req.body;
  if (!user || !giftName) return res.status(400).json({ ok: false, error: "user and giftName required" });

  const resolved = giftEngine.resolveGift(user, giftName);
  const payload = {
    user,
    giftName,
    accepted: Boolean(resolved.accepted),
    action: resolved.action || "",
    value: resolved.value || 0,
    durationSec: resolved.durationSec || 0,
    gameMode: resolved.gameMode || ""
  };
  pushEvent(MessageType.GIFT, payload);
  broadcast(MessageType.GIFT, payload);
  res.json({ ok: true, resolved });
});

app.post("/round/reset", (_req, res) => {
  giftEngine.resetRound();
  res.json({ ok: true });
});

app.post("/question/generate", async (req, res) => {
  const language = req.body?.language || "fr";
  const question = await questionGenerator.generateOne(language);
  pushEvent(MessageType.QUESTION, { language, question });
  broadcast(MessageType.QUESTION, question);
  res.json({ ok: true, question });
});

console.log(`HTTP server on http://localhost:${boundHttpPort}`);
console.log(`WebSocket server on ws://localhost:${activeWsPort}`);
if (boundHttpPort !== PORT) {
  console.warn(`Requested PORT ${PORT} was busy, fallback to ${boundHttpPort}.`);
}
if (activeWsPort !== WS_PORT) {
  console.warn(`Requested WS_PORT ${WS_PORT} was busy, fallback to ${activeWsPort}.`);
}

async function connectTikTokWithRetry() {
  const connected = await tiktokBridge.connect();
  if (connected) {
    const username = tiktokBridge.activeUsername || "unknown";
    console.log(`TikTok bridge connected on @${username}.`);
    broadcast(MessageType.SYSTEM, {
      ok: true,
      text: `TikTok bridge connected on @${username}`,
      source: "tiktok-bridge"
    });
    return;
  }
  if (tiktokUsernames.length === 0) {
    console.log("TikTok bridge disabled.");
    return;
  }
  broadcast(MessageType.SYSTEM, {
    ok: false,
    text: `TikTok bridge waiting for live on: ${tiktokUsernames.join(", ")}`,
    source: "tiktok-bridge"
  });
  console.log(`TikTok bridge retry in ${TIKTOK_RETRY_MS}ms...`);
  setTimeout(connectTikTokWithRetry, TIKTOK_RETRY_MS);
}

if (TIKTOK_BRIDGE_ENABLED) {
  connectTikTokWithRetry();
} else {
  console.log("TikTok bridge disabled by TIKTOK_BRIDGE_ENABLED=false.");
  broadcast(MessageType.SYSTEM, {
    ok: false,
    text: "TikTok bridge disabled (local test mode).",
    source: "tiktok-bridge"
  });
}

tiktokBridge.on("chat", (payload) => {
  pushEvent(MessageType.CHAT, payload);
  broadcast(MessageType.CHAT, payload);
});

tiktokBridge.on("gift", (payload) => {
  const resolved = giftEngine.resolveGift(payload.user, payload.giftName);
  const enriched = {
    ...payload,
    accepted: Boolean(resolved.accepted),
    action: resolved.action || "",
    value: resolved.value || 0,
    durationSec: resolved.durationSec || 0,
    gameMode: resolved.gameMode || ""
  };
  pushEvent(MessageType.GIFT, enriched);
  broadcast(MessageType.GIFT, enriched);
});
