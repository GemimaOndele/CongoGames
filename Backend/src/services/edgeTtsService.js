/**
 * TTS gratuit via le service public « Read Aloud » (Microsoft Edge) — pas de clé API.
 * Décode le MP3 en PCM 16 bits mono (Unity) avec audio-decode.
 * Désactiver : TTS_EDGE_ENABLED=0 dans .env
 */

import { IsomorphicEdgeTTS } from "edge-tts-universal";
import decode from "audio-decode";

const MAX_CHARS = 2500;

function voice() {
  return (process.env.TTS_EDGE_VOICE || "fr-FR-DeniseNeural").trim();
}

export function isEdgeTtsEnabled() {
  const v = (process.env.TTS_EDGE_ENABLED ?? "1").trim().toLowerCase();
  if (v === "0" || v === "false" || v === "off") {
    return false;
  }
  return true;
}

/**
 * @returns {Promise<{format:string,sampleRate:number,channels:number,pcmBase64:string,mp3Base64:string,provider:string}>}
 */
export async function synthesizeEdgeToPcm(text) {
  const clean = String(text || "")
    .trim()
    .slice(0, MAX_CHARS);
  if (!clean) {
    throw new Error("texte vide");
  }
  const tts = new IsomorphicEdgeTTS(clean, voice());
  const { audio } = await tts.synthesize();
  const buf = Buffer.from(await audio.arrayBuffer());
  const ab = await decode(buf);
  const f32 = ab.channelData[0];
  if (!f32 || f32.length < 1) {
    throw new Error("edge-tts: audio vide après décodage");
  }
  const n = f32.length;
  const int16 = new Int16Array(n);
  for (let i = 0; i < n; i++) {
    const x = f32[i];
    const s = x > 1 ? 1 : x < -1 ? -1 : x;
    int16[i] = s < 0 ? s * 0x8000 : s * 0x7fff;
  }
  const pcmB64 = Buffer.from(int16.buffer, int16.byteOffset, int16.byteLength).toString("base64");
  return {
    format: "pcm",
    outputFormat: "pcm_s16le",
    sampleRate: ab.sampleRate,
    channels: 1,
    pcmBase64: pcmB64,
    mp3Base64: "",
    provider: "edge"
  };
}
