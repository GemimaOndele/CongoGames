import "dotenv/config";
import express from "express";
import { isTtsConfigured, synthesizeToAudioBase64 } from "../../src/services/ttsService.js";

/**
 * POST /api/tts (et /tts via rewrites) — formulaire x-www-form-urlencoded (Unity TtsClient).
 */
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

app.post("/", async (req, res) => {
  const text = req.body?.text ?? req.body?.message;
  if (!text || typeof text !== "string") {
    return res.status(400).json({ ok: false, error: "text (string) requis" });
  }
  if (!isTtsConfigured()) {
    return res.status(503).json({
      ok: false,
      error:
        "TTS non disponible : TTS Edge / OPENAI / ElevenLabs (variables d’environnement Vercel)"
    });
  }
  try {
    // Vercel/API: on force PCM-only pour Unity.
    const audio = await synthesizeToAudioBase64(text, { preferPcm: true });
    res.json({ ok: true, ...audio });
  } catch (err) {
    const status = Number(err?.status ?? err?.response?.status);
    const httpStatus = status === 429 || status === 401 || status === 403 ? status : 502;
    const message = err?.message || "tts_error";
    console.error("[tts] POST /api/tts:", message);
    res.status(httpStatus).json({
      ok: false,
      error: message,
      code: status === 429 ? "openai_quota" : undefined
    });
  }
});

export default app;
