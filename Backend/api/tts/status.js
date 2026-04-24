import "dotenv/config";
import express from "express";
import { isTtsConfigured, isElevenLabsReady } from "../../src/services/ttsService.js";
import { isEdgeTtsEnabled } from "../../src/services/edgeTtsService.js";
import { isOpenAiSpeechReady } from "../../src/services/openAiSpeechService.js";

/**
 * GET /api/tts/status (et /tts/status via rewrites) — propre Vercel, sans s’appuyer sur api/index.js.
 * Navigateur (Accept text/html) : page explicative. Unity / ?format=json : JSON uniquement.
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

function ttsStatusPayload() {
  return {
    ok: true,
    enabled: isTtsConfigured(),
    edge: isEdgeTtsEnabled(),
    elevenLabs: isElevenLabsReady(),
    openAi: isOpenAiSpeechReady()
  };
}

/**
 * @param {{ hostLabel: "congogames" | "backend" }} o
 */
function ttsStatusHtmlRow(label, on, help) {
  const icon = on ? "✅" : "⬜";
  return `<li class=row><span class=ic>${icon}</span><div><strong>${label}</strong><p class=hi>${help}</p></div></li>`;
}

function renderTtsStatusPage(payload, o) {
  const j = JSON.stringify(payload, null, 2);
  const baseNote =
    o.hostLabel === "congogames"
      ? "Tu es sur <strong>congogames.vercel.app</strong> (site WebGL + proxy vers l’API). C’est l’adresse <strong>à privilégier</strong> pour le jeu : même origine, chemins <code>/tts</code>, <code>/health</code>, etc."
      : "Tu es sur l’<strong>API seule</strong> (déploiement direct, ex. <code>…-nine.vercel.app</code> ou, une fois l’alias correct, l’<strong>hôte court</strong> <code>congogames-backend-cg.vercel.app</code> — c’est le nom de projet Vercel sans le suffixe d’équipe).<br><br>Le jeu, lui, se lance sur <a href=\"https://congogames.vercel.app/\">congogames.vercel.app</a> (build WebGL), pas sur l’hôte de l’API.";

  return (
    "<!DOCTYPE html><html lang=fr><head><meta charset=utf-8><meta name=viewport content=\"width=device-width, initial-scale=1\">" +
    "<title>Statut TTS — CongoGames</title>" +
    "<style>" +
    ":root{--bg1:#0f0c29;--bg2:#302b63;--bg3:#24243e;--card:#ffffff0d;--txt:#e8e6f3;--acc:#6ee7a8;--muted:#a8a3c0}" +
    "body{margin:0;min-height:100vh;font-family:system-ui,Segoe UI,sans-serif;background:linear-gradient(160deg,var(--bg1) 0%,var(--bg2) 50%,var(--bg3) 100%);color:var(--txt);overflow-x:hidden}" +
    ".wrap{max-width:32rem;margin:0 auto;padding:1.5rem 1rem 2.5rem;perspective:900px}" +
    ".card{background:var(--card);border-radius:1.25rem;padding:1.5rem;backdrop-filter:blur(12px);border:1px solid #fff1;box-shadow:0 20px 50px #0004;transform:rotateX(4deg);transition:transform .3s}" +
    ".card:hover{transform:rotateX(0deg) translateY(-2px)}" +
    "h1{font-size:1.25rem;font-weight:700;letter-spacing:.02em;margin:0 0 .5rem;display:flex;align-items:center;gap:.5rem}" +
    "h1 span{font-size:1.5rem} .sub{font-size:.9rem;color:var(--muted);line-height:1.5;margin:0 0 1.25rem}" +
    "ul{list-style:none;padding:0;margin:0} .row{display:flex;gap:.75rem;align-items:flex-start;padding:.65rem 0;border-bottom:1px solid #fff1}" +
    ".ic{font-size:1.25rem} .row strong{color:var(--acc);font-size:.95rem} .hi{margin:.25rem 0 0;font-size:.8rem;color:var(--muted);line-height:1.45}" +
    "pre{white-space:pre-wrap;font-size:.72rem;background:#0003;padding:1rem;border-radius:.6rem;overflow:auto;margin:1rem 0 0}" +
    ".foot{margin-top:1.5rem;font-size:.82rem;color:var(--muted)}" +
    "a{color:#93c5fd} code{font-size:.78rem;background:#0002;padding:2px 6px;border-radius:4px}" +
    "</style></head><body><div class=wrap><div class=card><h1><span>🎮🔊</span> Statut TTS</h1>" +
    "<p class=sub>⬜ indique <strong>optionnel / non branché</strong> — le jeu parle dès qu’<strong>un</strong> moteur est prêt (souvent Edge seul, ✅).</p>" +
    "<p class=sub>" +
    baseNote +
    "</p><ul>" +
    ttsStatusHtmlRow("TTS prêt (enabled)", payload.enabled, "Au moins un moteur (Edge / OpenAI / ElevenLabs) est configuré côté Vercel (projet congogames-backend-cg).") +
    ttsStatusHtmlRow("edge-tts (gratuit, démo)", payload.edge, "TTS en ligne sans clé lourde ; c’est souvent le seul nécessaire pour des démos. ⬆️ Si c’est seul : les cases ElevenLabs / OpenAI peuvent rester vides, ce n’est pas un bug.") +
    ttsStatusHtmlRow(
      "ElevenLabs",
      payload.elevenLabs,
      payload.elevenLabs
        ? "Moteur prêt : <code>ELEVENLABS_API_KEY</code> et <code>ELEVENLABS_VOICE_ID</code> sont reconnus sur congogames-backend-cg."
        : "⬜ = non configuré. Depuis <code>Backend/.env</code> : <code>npm run backend:vercel:env</code> puis <code>npm run backend:vercel</code>. À la main : Vercel → congogames-backend-cg → <code>ELEVENLABS_API_KEY</code> + <code>ELEVENLABS_VOICE_ID</code> (les deux requis), puis <code>npm run backend:vercel</code> ou redeploy Git."
    ) +
    ttsStatusHtmlRow(
      "OpenAI Speech",
      payload.openAi,
      payload.openAi
        ? "Moteur prêt : <code>OPENAI_API_KEY</code> reconnue sur congogames-backend-cg. Optionnel : <code>OPENAI_TTS_MODEL</code> (défaut tts-1), <code>OPENAI_TTS_VOICE</code> (défaut alloy)."
        : "⬜ = pas de clé. Même principe : <code>npm run backend:vercel:env</code> (pousse aussi <code>OPENAI_API_KEY</code> depuis <code>Backend/.env</code>) puis <code>npm run backend:vercel</code>. À la main : <code>OPENAI_API_KEY</code> sur le même projet. Optionnel : <code>OPENAI_TTS_MODEL</code> (défaut tts-1), <code>OPENAI_TTS_VOICE</code> (défaut alloy)."
    ) +
    "</ul><p class=sub>⬜ indique <strong>optionnel / non branché</strong> — le jeu parle dès qu’<strong>un</strong> moteur est prêt (souvent Edge seul, ✅).</p><p class=sub>📋 JSON : <a href=\"?format=json\">?format=json</a></p><pre>" +
    j.replace(/</g, "\\u003c") +
    "</pre>" +
    '<p class=foot>👉 <strong>Pour tester le jeu en ligne</strong> : ouvre <a href="https://congogames.vercel.app/">congogames.vercel.app</a> — c’est le build WebGL, pas l’écran d’API.</p></div></div></body></html>'
  );
}

app.get("/", (req, res) => {
  const payload = ttsStatusPayload();
  const acc = (req.get("Accept") || "").toLowerCase();
  const ua = (req.get("User-Agent") || "");
  const isUnity = /unityplayer|unity\//i.test(ua);
  if (isUnity || req.query.format === "json" || req.query.pretty === "0") {
    return res.json(payload);
  }
  if (req.query.pretty === "1" || req.query.html === "1") {
    const h = (req.get("X-Forwarded-Host") || req.get("Host") || "").toLowerCase();
    return res
      .type("html")
      .send(
        renderTtsStatusPage(payload, {
          hostLabel: h.includes("congogames.vercel.app") ? "congogames" : "backend"
        })
      );
  }
  if (acc.includes("text/html") && (acc.indexOf("text/html") < acc.indexOf("application/json") || acc.indexOf("application/json") < 0)) {
    const h2 = (req.get("X-Forwarded-Host") || req.get("Host") || "").toLowerCase();
    return res
      .type("html")
      .send(
        renderTtsStatusPage(payload, {
          hostLabel: h2.includes("congogames.vercel.app") ? "congogames" : "backend"
        })
      );
  }
  return res.json(payload);
});

export default app;
