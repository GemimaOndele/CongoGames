import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");
const dataPath = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Datasets",
  "minigame_image_guess_extras.json"
);

function parseTrackIndex(audioFileBase) {
  const m = String(audioFileBase || "").trim().toLowerCase().match(/^track(\d+)$/);
  return m ? Number.parseInt(m[1], 10) : 0;
}

function difficultyLabel(trackIdx) {
  if (trackIdx <= 10) return "Niveau 1/3";
  if (trackIdx <= 20) return "Niveau 2/3";
  return "Niveau 3/3";
}

function cleanHintText(hint) {
  let h = String(hint || "").trim();
  h = h.replace(/\s+\(info vérifiée quand disponible\)\s+\(info vérifiée quand disponible\)/g, " (info vérifiée quand disponible)");
  h = h.replace(/\s+\(indice: regarde les marqueurs feat\/x\/&\)\s+\(indice: regarde les marqueurs feat\/x\/&\)/g, " (indice: regarde les marqueurs feat/x/&)");
  return h;
}

function detectQuestionType(item) {
  const hint = String(item.hint || "").toLowerCase();
  const mod = Number(item.styleSeed || 0) % 10;
  if (mod === 1 || hint.includes("qui interpr")) return "artist";
  if (mod === 2 || hint.includes("titre du morceau")) return "title";
  if (mod === 3 || hint.includes("langue dominante")) return "lang";
  if (mod === 4 || hint.includes("solo ou une collaboration")) return "collab";
  return "generic";
}

function buildTvHint(item, trackIdx, level) {
  const t = detectQuestionType(item);
  const rawTitle = String(item.answerKey || "ce titre");
  if (t === "artist") {
    return `🎬 ${level} • Manche piste ${trackIdx} — Qui est l'interprète principal entendu sur cet extrait ?`;
  }
  if (t === "title") {
    return `🎬 ${level} • Manche piste ${trackIdx} — Quel est le titre exact de ce morceau ?`;
  }
  if (t === "lang") {
    return `🎬 ${level} • Manche piste ${trackIdx} — Quelle langue est dominante dans cette chanson ?`;
  }
  if (t === "collab") {
    return `🎬 ${level} • Manche piste ${trackIdx} — Solo ou collaboration (feat/x/&), quel est le bon choix ?`;
  }
  return `🎬 ${level} • Manche piste ${trackIdx} — Observe l'image et écoute l'extrait, puis réponds précisément.`;
}

function appendEditorialTrivia(item, trackIdx, level) {
  const trivia = String(item.trivia || "").trim();
  const t = detectQuestionType(item);
  let trap = "";
  if (t === "artist") trap = "Piège subtil: ne confonds pas artiste principal et featuring.";
  else if (t === "title") trap = "Piège subtil: attention aux variantes live/remix dans les propositions.";
  else if (t === "lang") trap = "Piège subtil: distingue langue dominante et simples passages bilingues.";
  else if (t === "collab") trap = "Piège subtil: les séparateurs « feat », « x » et « & » indiquent souvent une collaboration.";
  const intro = `Édito TV: ${level}, focus piste ${trackIdx}.`;
  return `${intro} ${trap} ${trivia}`.trim();
}

async function main() {
  const raw = await readFile(dataPath, "utf8");
  const json = JSON.parse(raw);
  const items = Array.isArray(json.items) ? json.items : [];

  for (const item of items) {
    const trackIdx = parseTrackIndex(item.audioFileBase);
    const level = difficultyLabel(trackIdx);
    item.hint = buildTvHint(item, trackIdx, level);
    item.hint = cleanHintText(item.hint);
    item.trivia = appendEditorialTrivia(item, trackIdx, level);
  }

  await writeFile(dataPath, JSON.stringify({ items }, null, 2), "utf8");
  console.log(`Passe éditoriale appliquée sur ${items.length} manches.`);
}

await main();
