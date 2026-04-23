import OpenAI from "openai";

const MAX_CHARS = 4096;

export function isOpenAiSpeechReady() {
  return Boolean(process.env.OPENAI_API_KEY?.trim());
}

/**
 * TTS OpenAI en PCM 24 kHz mono 16-bit (évite MP3 côté Unity / fichiers temporaires).
 */
export async function synthesizeOpenAiSpeechToPcm(text) {
  const key = process.env.OPENAI_API_KEY?.trim();
  if (!key) {
    throw new Error("OPENAI_API_KEY manquant pour la synthèse vocale");
  }

  const clean = String(text || "").trim().slice(0, MAX_CHARS);
  if (!clean) {
    throw new Error("texte vide");
  }

  const client = new OpenAI({ apiKey: key });
  const model = (process.env.OPENAI_TTS_MODEL || "tts-1").trim();
  const voice = (process.env.OPENAI_TTS_VOICE || "alloy").trim();

  let response;
  try {
    response = await client.audio.speech.create({
      model,
      voice,
      input: clean,
      response_format: "pcm"
    });
  } catch (err) {
    const status = err?.status ?? err?.response?.status;
    const apiMsg = err?.error?.message || err?.message || String(err);
    const wrapped = new Error(
      status === 429
        ? "OpenAI : quota ou limite atteinte (429). Vérifie crédits et facturation sur https://platform.openai.com"
        : `OpenAI TTS : ${apiMsg}`
    );
    wrapped.status = status;
    throw wrapped;
  }

  let buf;
  if (typeof response.arrayBuffer === "function") {
    buf = Buffer.from(await response.arrayBuffer());
  } else if (typeof response.blob === "function") {
    const blob = await response.blob();
    buf = Buffer.from(await blob.arrayBuffer());
  } else {
    throw new Error("OpenAI TTS: réponse binaire non supportée par ce SDK");
  }

  if (!buf.length) {
    throw new Error("OpenAI TTS: réponse vide");
  }

  return {
    format: "pcm",
    outputFormat: `${model}_pcm_24000`,
    sampleRate: 24000,
    channels: 1,
    pcmBase64: buf.toString("base64"),
    mp3Base64: "",
    provider: "openai"
  };
}
