/**
 * Synthèse vocale : ElevenLabs (voix pro) → OpenAI (PCM) → Edge TTS (gratuit, repli fiable).
 * Si crédits / erreur sur les moteurs payants, le repli Edge reste disponible (navigateur / API).
 */

import { isOpenAiSpeechReady, synthesizeOpenAiSpeechToPcm } from "./openAiSpeechService.js";
import { isEdgeTtsEnabled, synthesizeEdgeToPcm } from "./edgeTtsService.js";
import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const DEFAULT_MODEL = "eleven_multilingual_v2";
const MAX_CHARS = 2500;
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DEFAULT_PRONUNCIATION_OVERRIDES = [
  ["chat", "tchat"],
  ["chater", "tchater"],
  ["chatter", "tchatter"],
  ["te", "té"],
  ["mbote", "mboté"],
  ["nzele", "nzèlè"],
  ["mwasi", "mwassi"],
  ["moasi", "mwassi"],
  ["lingala", "lingála"],
  ["kituba", "kitouba"],
  ["nana", "nana"],
  ["momi", "momí"],
  ["momí", "momí"],
  ["momie", "momie"]
];

function escapeRegex(input) {
  return String(input || "").replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

function loadPronunciationOverrides() {
  const fromConfig = [];
  try {
    const cfgPath = path.resolve(__dirname, "../config/tts-pronunciation-overrides.json");
    const raw = readFileSync(cfgPath, "utf8");
    const json = JSON.parse(raw);
    const list = Array.isArray(json?.replacements) ? json.replacements : [];
    for (const item of list) {
      const from = String(item?.from || "").trim();
      const to = String(item?.to || "").trim();
      if (!from || !to) continue;
      fromConfig.push([from, to]);
    }
  } catch {
    // Fallback silencieux: on garde les règles codées en dur.
  }

  const source = fromConfig.length > 0 ? fromConfig : DEFAULT_PRONUNCIATION_OVERRIDES;
  return source.map(([from, to]) => [new RegExp(`\\b${escapeRegex(from)}\\b`, "gi"), to]);
}

const PRONUNCIATION_OVERRIDES = loadPronunciationOverrides();

function applyFrenchPronunciationOverrides(text) {
  const src = String(text || "");
  if (!src) return src;

  let out = src;
  for (const [rx, repl] of PRONUNCIATION_OVERRIDES) {
    out = out.replace(rx, repl);
  }
  return out;
}

export function isElevenLabsReady() {
  const k = process.env.ELEVENLABS_API_KEY?.trim();
  const v = process.env.ELEVENLABS_VOICE_ID?.trim();
  return Boolean(k && v);
}

export function isTtsConfigured() {
  if (isEdgeTtsEnabled()) {
    return true;
  }
  return isElevenLabsReady() || isOpenAiSpeechReady();
}

function parseSampleRateFromFormat(outputFormat) {
  const m = String(outputFormat).match(/(?:pcm|mp3|wav)_(\d+)/i);
  if (m) return Number(m[1]) || 22050;
  return 22050;
}

async function synthesizeElevenLabsOnly(text, opts = {}) {
  const preferPcm = Boolean(opts.preferPcm);
  const apiKey = process.env.ELEVENLABS_API_KEY?.trim();
  const voiceId = process.env.ELEVENLABS_VOICE_ID?.trim();
  if (!apiKey || !voiceId) {
    throw new Error("ElevenLabs: définir ELEVENLABS_API_KEY et ELEVENLABS_VOICE_ID (voix instant clone ou voix autorisée sur ton compte)");
  }

  const modelId = (process.env.ELEVENLABS_MODEL_ID || DEFAULT_MODEL).trim();
  const preferred = (process.env.ELEVENLABS_OUTPUT_FORMAT || "pcm_22050").trim();
  /** Unity décode mal certains MP3 sous Windows (« Unable to read data ») : le client envoie prefer_pcm=1. */
  const attemptFormats = preferPcm
    ? ["pcm_22050", "pcm_16000"]
    : [...new Set([preferred, "pcm_22050", "pcm_16000", "mp3_22050_32"])];

  let lastErr = "";

  for (const outputFormat of attemptFormats) {
    try {
      const url = `https://api.elevenlabs.io/v1/text-to-speech/${encodeURIComponent(voiceId)}?output_format=${encodeURIComponent(outputFormat)}`;

      const response = await fetch(url, {
        method: "POST",
        headers: {
          "xi-api-key": apiKey,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          text,
          model_id: modelId
        })
      });

      if (!response.ok) {
        const errText = await response.text();
        lastErr = `ElevenLabs ${response.status} (${outputFormat}): ${errText.slice(0, 280)}`;
        console.warn("[tts]", lastErr);
        if (response.status === 402 || errText.includes("paid_plan_required")) {
          break;
        }
        continue;
      }

      const buf = Buffer.from(await response.arrayBuffer());
      if (!buf.length) {
        lastErr = "Réponse audio vide";
        continue;
      }

      const sampleRate = parseSampleRateFromFormat(outputFormat);

      if (outputFormat.startsWith("mp3")) {
        return {
          format: "mp3",
          outputFormat,
          sampleRate,
          channels: 1,
          mp3Base64: buf.toString("base64"),
          pcmBase64: "",
          provider: "elevenlabs"
        };
      }

      return {
        format: "pcm",
        outputFormat,
        sampleRate,
        channels: 1,
        pcmBase64: buf.toString("base64"),
        mp3Base64: "",
        provider: "elevenlabs"
      };
    } catch (e) {
      lastErr = e?.message || String(e);
      console.warn("[tts] ElevenLabs", outputFormat, lastErr);
    }
  }

  throw new Error(lastErr || "ElevenLabs: échec");
}

export async function synthesizeToAudioBase64(text, opts = {}) {
  const clean = applyFrenchPronunciationOverrides(String(text || "").trim()).slice(0, MAX_CHARS);
  if (!clean) {
    throw new Error("texte vide");
  }

  const errors = [];

  if (isElevenLabsReady()) {
    try {
      return await synthesizeElevenLabsOnly(clean, opts);
    } catch (e) {
      const msg = e?.message || String(e);
      errors.push("ElevenLabs: " + msg);
      console.warn("[tts] ElevenLabs indisponible, repli suivant:", msg.slice(0, 160));
    }
  }

  if (isOpenAiSpeechReady()) {
    try {
      return await synthesizeOpenAiSpeechToPcm(clean);
    } catch (e) {
      const msg = e?.message || String(e);
      errors.push("OpenAI: " + msg);
      console.warn("[tts] OpenAI (PCM) indisponible, repli Edge:", msg.slice(0, 120));
    }
  }

  if (isEdgeTtsEnabled()) {
    try {
      return await synthesizeEdgeToPcm(clean);
    } catch (e) {
      const msg = e?.message || String(e);
      errors.push("Edge: " + msg);
      console.warn("[tts] Edge (gratuit) indisponible:", msg.slice(0, 160));
    }
  }

  throw new Error(
    errors.join(" | ") ||
      "Aucun TTS disponible : configurez ElevenLabs, OPENAI_API_KEY, ou laissez Edge actif (TTS_EDGE_ENABLED=1)."
  );
}
