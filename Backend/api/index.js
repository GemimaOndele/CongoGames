import "dotenv/config";
import express from "express";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { GiftEngine } from "../src/services/giftEngine.js";
import { QuestionGenerator } from "../src/services/questionGenerator.js";
import {
  isTtsConfigured,
  synthesizeToAudioBase64,
  isElevenLabsReady
} from "../src/services/ttsService.js";
import { isEdgeTtsEnabled } from "../src/services/edgeTtsService.js";
import { isOpenAiSpeechReady } from "../src/services/openAiSpeechService.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
app.use(express.json());
app.use(express.urlencoded({ extended: true }));

app.use((req, res, next) => {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
  if (req.method === "OPTIONS") {
    return res.status(204).end();
  }

  next();
});

const giftEngine = new GiftEngine(path.join(__dirname, "..", "src", "config", "gift-balance.json"));
const questionGenerator = new QuestionGenerator(process.env.OPENAI_API_KEY);
const recentEvents = [];
const RECENT_MAX = 200;

function pushEvent(type, payload) {
  recentEvents.push({ type, payload, ts: Date.now() });
  if (recentEvents.length > RECENT_MAX) recentEvents.shift();
}

app.get("/health", (_req, res) => {
  res.json({
    ok: true,
    service: "congogames-backend-vercel",
    ttsEnabled: isTtsConfigured()
  });
});

// Unity WebGL (TtsClient.Probe + FetchClip) : exposé comme sur src/server.js
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
        "TTS non disponible : active TTS Edge (TTS_EDGE_ENABLED=1 par défaut) ou ajoute OPENAI_API_KEY / ElevenLabs sur Vercel (Variables d’environnement)"
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
    runtime: "vercel",
    recentEvents: recentEvents.slice(-20)
  });
});

app.post("/events/chat", (req, res) => {
  const { user, message } = req.body;
  if (!user || !message) return res.status(400).json({ ok: false, error: "user and message required" });
  pushEvent("chat", { user, message });
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
  pushEvent("gift", payload);
  res.json({ ok: true, resolved: payload });
});

app.post("/round/reset", (_req, res) => {
  giftEngine.resetRound();
  res.json({ ok: true });
});

app.post("/question/generate", async (req, res) => {
  const language = req.body?.language || "fr";
  const question = await questionGenerator.generateOne(language);
  pushEvent("question", { language, question });
  res.json({ ok: true, question });
});

export default app;
