import "dotenv/config";
import net from "node:net";
import path from "node:path";
import { fileURLToPath } from "node:url";
import express from "express";
import { WebSocketServer } from "ws";
import { GiftEngine } from "./services/giftEngine.js";
import { QuestionGenerator } from "./services/questionGenerator.js";
import { TikTokLiveBridge } from "./services/tiktokLiveBridge.js";
import { MessageType, createMessage } from "./protocol/messages.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
app.use(express.json());

const PORT = Number(process.env.PORT || 3000);
const WS_PORT = Number(process.env.WS_PORT || 8080);

const giftEngine = new GiftEngine(path.join(__dirname, "config", "gift-balance.json"));
const questionGenerator = new QuestionGenerator(process.env.OPENAI_API_KEY);
const tiktokBridge = new TikTokLiveBridge(process.env.TIKTOK_USERNAME || "");
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

const { wsServer: wss, port: boundWsPort } = await startWebSocketServer(WS_PORT);
const boundHttpPort = await resolveAvailablePort(PORT);

function broadcast(type, payload) {
  const msg = createMessage(type, payload);
  wss.clients.forEach((client) => {
    if (client.readyState === 1) client.send(msg);
  });
}

wss.on("connection", (ws) => {
  ws.send(createMessage(MessageType.SYSTEM, { ok: true, text: "CongoGames WS connected" }));
});

app.get("/health", (_req, res) => {
  res.json({ ok: true, service: "congogames-backend" });
});

app.get("/metrics", (_req, res) => {
  res.json({
    wsClients: wss.clients.size,
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
    durationSec: resolved.durationSec || 0
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

app.listen(boundHttpPort, () => {
  console.log(`HTTP server on http://localhost:${boundHttpPort}`);
  console.log(`WebSocket server on ws://localhost:${boundWsPort}`);
  if (boundHttpPort !== PORT) {
    console.warn(`Requested PORT ${PORT} was busy, fallback to ${boundHttpPort}.`);
  }
  if (boundWsPort !== WS_PORT) {
    console.warn(`Requested WS_PORT ${WS_PORT} was busy, fallback to ${boundWsPort}.`);
  }
});

tiktokBridge
  .connect()
  .then((connected) => {
    if (connected) {
      console.log("TikTok bridge connected.");
    } else {
      console.log("TikTok bridge disabled.");
    }
  })
  .catch((error) => {
    console.warn("TikTok bridge not connected:", error.message);
  });

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
    durationSec: resolved.durationSec || 0
  };
  pushEvent(MessageType.GIFT, enriched);
  broadcast(MessageType.GIFT, enriched);
});
