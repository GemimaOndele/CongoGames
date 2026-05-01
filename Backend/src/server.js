import "dotenv/config";
import net from "node:net";
import path from "node:path";
import { fileURLToPath } from "node:url";
import http from "node:http";
import crypto from "node:crypto";
import express from "express";
import { WebSocketServer } from "ws";
import { GiftEngine } from "./services/giftEngine.js";
import { QuestionGenerator } from "./services/questionGenerator.js";
import { TikTokLiveBridge } from "./services/tiktokLiveBridge.js";
import {
  isTtsConfigured,
  synthesizeToAudioBase64,
  isElevenLabsReady
} from "./services/ttsService.js";
import { isEdgeTtsEnabled } from "./services/edgeTtsService.js";
import { isOpenAiSpeechReady } from "./services/openAiSpeechService.js";
import { MessageType, createMessage } from "./protocol/messages.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

// WebGL / navigateur : UnityWebRequest vers l’API déployée (HTTPS) exige CORS.
app.use((req, res, next) => {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
  if (req.method === "OPTIONS") {
    return res.status(204).end();
  }

  next();
});

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
const oauthProfiles = new Map();
const latestOAuthTokenByProvider = new Map();
const OAUTH_PROFILE_TTL_MS = Number(process.env.OAUTH_PROFILE_TTL_MS || 1000 * 60 * 60 * 12); // 12h

const oauthProviderUrls = {
  tiktok: (process.env.OAUTH_TIKTOK_URL || "").trim(),
  google: (process.env.OAUTH_GOOGLE_URL || "").trim(),
  facebook: (process.env.OAUTH_FACEBOOK_URL || "").trim()
};

function nowMs() {
  return Date.now();
}

function sanitizeProvider(raw) {
  const p = String(raw || "").trim().toLowerCase();
  if (p === "tiktok" || p === "google" || p === "facebook") return p;
  return "";
}

function parseBooleanFlag(raw) {
  if (raw === true || raw === 1) return true;
  const s = String(raw || "").trim().toLowerCase();
  return s === "1" || s === "true" || s === "yes" || s === "on";
}

function buildProfileFromRequest(req, providerHint = "") {
  const body = req.body || {};
  const query = req.query || {};
  const provider = sanitizeProvider(body.provider || query.provider || providerHint) || "invité";
  const displayName = String(body.displayName || query.displayName || body.user || query.user || "Joueur")
    .trim()
    .slice(0, 60) || "Joueur";
  const avatarUrl = String(body.avatarUrl || query.avatarUrl || body.profilePictureUrl || query.profilePictureUrl || "")
    .trim()
    .slice(0, 500);
  const isAdmin = parseBooleanFlag(body.isAdmin ?? query.isAdmin);
  return { provider, displayName, avatarUrl, isAdmin };
}

function saveOAuthProfile(profile) {
  const token = crypto.randomUUID();
  const provider = sanitizeProvider(profile?.provider) || "invité";
  oauthProfiles.set(token, {
    ...profile,
    provider,
    authToken: token,
    createdAt: nowMs(),
    expiresAt: nowMs() + OAUTH_PROFILE_TTL_MS
  });
  latestOAuthTokenByProvider.set(provider, token);
  return token;
}

function getOAuthProfile(token) {
  const key = String(token || "").trim();
  if (!key) return null;
  const p = oauthProfiles.get(key);
  if (!p) return null;
  if (p.expiresAt < nowMs()) {
    oauthProfiles.delete(key);
    return null;
  }
  return p;
}

function purgeExpiredOAuthProfiles() {
  const t = nowMs();
  for (const [token, p] of oauthProfiles.entries()) {
    if (!p || p.expiresAt < t) {
      oauthProfiles.delete(token);
      if (p?.provider && latestOAuthTokenByProvider.get(p.provider) === token) {
        latestOAuthTokenByProvider.delete(p.provider);
      }
    }
  }
}

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
<li><a href="/tts/status"><code>/tts/status</code></a> — TTS (Edge gratuit, OpenAI, ElevenLabs)</li>
<li><code>POST /tts</code> — formulaire <code>text=…</code> (Unity)</li>
<li><code>POST /events/chat</code>, <code>POST /events/gift</code>, <code>POST /question/generate</code></li>
</ul>
<p><strong>Ports actuels :</strong> HTTP <code>${boundHttpPort}</code>, WebSocket <code>${activeWsPort}</code></p>
</body></html>`);
});

app.get("/health", (_req, res) => {
  purgeExpiredOAuthProfiles();
  res.json({
    ok: true,
    service: "congogames-backend",
    httpPort: boundHttpPort,
    wsPort: activeWsPort,
    ttsEnabled: isTtsConfigured(),
    oauthProviders: {
      tiktok: Boolean(oauthProviderUrls.tiktok),
      google: Boolean(oauthProviderUrls.google),
      facebook: Boolean(oauthProviderUrls.facebook)
    },
    oauthProfilesActive: oauthProfiles.size
  });
});

app.get("/auth/providers", (_req, res) => {
  res.json({
    ok: true,
    providers: {
      tiktok: oauthProviderUrls.tiktok,
      google: oauthProviderUrls.google,
      facebook: oauthProviderUrls.facebook
    }
  });
});

app.get("/auth/:provider/start", (req, res) => {
  const provider = sanitizeProvider(req.params.provider);
  if (!provider) return res.status(400).json({ ok: false, error: "provider invalide" });
  const base = oauthProviderUrls[provider];
  if (!base) return res.status(501).json({ ok: false, error: `OAUTH_${provider.toUpperCase()}_URL manquant` });

  // On propage un state simple (optionnel) ; le callback d'identity provider doit revenir sur /auth/:provider/callback.
  const state = String(req.query.state || crypto.randomUUID());
  const join = base.includes("?") ? "&" : "?";
  const redirect = `${base}${join}state=${encodeURIComponent(state)}`;
  res.redirect(302, redirect);
});

app.get("/auth/:provider/callback", (req, res) => {
  const provider = sanitizeProvider(req.params.provider);
  if (!provider) return res.status(400).json({ ok: false, error: "provider invalide" });
  const profile = buildProfileFromRequest(req, provider);
  const authToken = saveOAuthProfile(profile);
  purgeExpiredOAuthProfiles();
  const payload = { ok: true, authToken, profile };

  // Mode navigateur: affiche un message simple prêt à copier/coller.
  if (String(req.query.format || "").toLowerCase() === "json") {
    return res.json(payload);
  }
  res.type("html").send(`<!DOCTYPE html><html lang="fr"><head><meta charset="utf-8"/><title>Connexion ${provider}</title>
<style>body{font-family:system-ui,sans-serif;max-width:44rem;margin:2rem auto;padding:0 1rem;line-height:1.5}code{background:#f0f0f0;padding:2px 6px;border-radius:4px}</style>
</head><body><h2>Connexion ${provider} réussie</h2>
<p>Copie ce token dans Unity si nécessaire :</p>
<p><code>${authToken}</code></p>
<pre>${JSON.stringify(payload, null, 2)}</pre>
</body></html>`);
});

app.get("/auth/:provider/latest", (req, res) => {
  const provider = sanitizeProvider(req.params.provider);
  if (!provider) return res.status(400).json({ ok: false, error: "provider invalide" });
  purgeExpiredOAuthProfiles();
  const token = latestOAuthTokenByProvider.get(provider);
  if (!token) return res.status(404).json({ ok: false, error: "aucun profil récent" });
  const p = getOAuthProfile(token);
  if (!p) return res.status(404).json({ ok: false, error: "profil expiré" });
  return res.json({
    ok: true,
    authToken: p.authToken,
    profile: {
      provider: p.provider,
      displayName: p.displayName,
      avatarUrl: p.avatarUrl,
      isAdmin: Boolean(p.isAdmin)
    },
    expiresAt: p.expiresAt
  });
});

app.post("/profile/sync", (req, res) => {
  const authToken = String(req.body?.authToken || req.query?.authToken || "").trim();
  if (authToken) {
    const p = getOAuthProfile(authToken);
    if (!p) return res.status(404).json({ ok: false, error: "authToken inconnu ou expiré" });
    return res.json({
      ok: true,
      authToken: p.authToken,
      profile: {
        provider: p.provider,
        displayName: p.displayName,
        avatarUrl: p.avatarUrl,
        isAdmin: Boolean(p.isAdmin)
      },
      expiresAt: p.expiresAt
    });
  }

  // Fallback: accepte aussi un push direct profil.
  const profile = buildProfileFromRequest(req);
  const token = saveOAuthProfile(profile);
  purgeExpiredOAuthProfiles();
  return res.json({ ok: true, authToken: token, profile });
});

app.get("/tts/status", (_req, res) => {
  res.json({
    ok: true,
    enabled: isTtsConfigured(),
    edge: isEdgeTtsEnabled(),
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
        "TTS non disponible : active TTS Edge (TTS_EDGE_ENABLED=1 par défaut) ou ajoute OPENAI_API_KEY / ElevenLabs dans .env"
    });
  }
  try {
    // Unity client: flux PCM uniquement (supprime les erreurs de décodage MP3 côté Windows).
    const audio = await synthesizeToAudioBase64(text, { preferPcm: true });
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
