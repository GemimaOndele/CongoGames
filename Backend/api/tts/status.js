import "dotenv/config";
import express from "express";
import { isTtsConfigured, isElevenLabsReady } from "../../src/services/ttsService.js";
import { isEdgeTtsEnabled } from "../../src/services/edgeTtsService.js";
import { isOpenAiSpeechReady } from "../../src/services/openAiSpeechService.js";

/**
 * GET /api/tts/status (et /tts/status via rewrites) — propre Vercel, sans s’appuyer sur api/index.js.
 */
const app = express();
app.use((req, res, next) => {
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, PUT, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");
  if (req.method === "OPTIONS") {
    return res.status(204).end();
  }
  next();
});

app.get("/", (req, res) => {
  res.json({
    ok: true,
    enabled: isTtsConfigured(),
    edge: isEdgeTtsEnabled(),
    elevenLabs: isElevenLabsReady(),
    openAi: isOpenAiSpeechReady()
  });
});

export default app;
