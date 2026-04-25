import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");

const imagePath = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Datasets",
  "minigame_image_guess_extras.json"
);

const blindMetaPath = path.join(
  root,
  "UnityProject",
  "Assets",
  "Resources",
  "Datasets",
  "blind_playlist_meta.json"
);

function normalize(s) {
  return String(s || "").replace(/\s+/g, " ").trim();
}

function toUpper(s) {
  return normalize(s).toUpperCase();
}

function parseTrackNo(fileBase) {
  const m = String(fileBase || "").toLowerCase().match(/^track(\d+)$/);
  return m ? Number.parseInt(m[1], 10) : 0;
}

function questionType(item) {
  const h = String(item.hint || "").toLowerCase();
  const mod = Number(item.styleSeed || 0) % 10;
  if (mod === 1 || /interpr|artiste|chanteur|chanteuse|groupe/.test(h)) return "artist";
  if (mod === 2 || /titre/.test(h)) return "title";
  if (mod === 3 || /langue/.test(h)) return "lang";
  if (mod === 4 || /solo|collaboration|feat/.test(h)) return "collab";
  return "generic";
}

function mainArtist(artist) {
  const a = normalize(artist);
  const parts = a.split(/\s+(?:feat\.?|ft\.?|x|&|vs)\s+|,\s*/i).map(normalize).filter(Boolean);
  return parts.length > 0 ? parts[0] : a;
}

function inferLanguage(artist, title) {
  const s = `${artist} ${title}`.toLowerCase();
  if (/kilombo|bokoko|nzoungou|mbongo|ndombolo|ya nga|mentalité|makambo|moselebende/.test(s)) return "LINGALA";
  if (/bûcheron|journal|intime|contentieux/.test(s)) return "FRANCAIS";
  return "LINGALA";
}

function imageBaseFor(trackNo, type) {
  if (type === "lang") return "langues";
  if (type === "collab") return "sapeurs";
  if (type === "title") return trackNo % 2 === 0 ? "ndombolo" : "kintele";
  return trackNo % 3 === 0 ? "sapeurs" : "kintele";
}

async function main() {
  const imageRaw = await readFile(imagePath, "utf8");
  const imageJson = JSON.parse(imageRaw);
  const items = Array.isArray(imageJson.items) ? imageJson.items : [];

  const blindRaw = await readFile(blindMetaPath, "utf8");
  const blindJson = JSON.parse(blindRaw);
  const metaByTrack = new Map((Array.isArray(blindJson.items) ? blindJson.items : []).map((x) => [String(x.fileBase || "").toLowerCase(), x]));

  const musicItems = items
    .filter((it) => /\[category:music_related\]/i.test(String(it.trivia || "")))
    .sort((a, b) => Number(a.styleSeed || 0) - Number(b.styleSeed || 0))
    .slice(0, 40);

  let touched = 0;
  for (const it of musicItems) {
    const fb = String(it.audioFileBase || "").toLowerCase();
    const meta = metaByTrack.get(fb);
    if (!meta) continue;
    const t = questionType(it);
    const trackNo = parseTrackNo(fb);
    const artistFull = toUpper(meta.artist);
    const artistMain = toUpper(mainArtist(meta.artist));
    const title = toUpper(meta.title);
    const lang = inferLanguage(meta.artist, meta.title);

    if (t === "artist") {
      it.answerKey = artistMain || artistFull;
      it.altAnswerKey = artistFull || "";
    } else if (t === "title") {
      it.answerKey = title && title !== "—" ? title : "TITRE NON PRECISE";
      it.altAnswerKey = "";
    } else if (t === "lang") {
      it.answerKey = lang;
      it.altAnswerKey = lang === "LINGALA" ? "KITUBA" : "";
    } else if (t === "collab") {
      const isCollab = /\b(feat|ft| x |&|vs)\b/i.test(String(meta.artist || ""));
      it.answerKey = isCollab ? "COLLABORATION" : "SOLO";
      it.altAnswerKey = isCollab ? "FEAT" : "ARTISTE SEUL";
    }

    it.streamingFileBase = imageBaseFor(trackNo, t);
    const baseTrivia = String(it.trivia || "").replace(/\s*\[critical40:[^\]]+\]/gi, "").trim();
    it.trivia = `${baseTrivia} [critical40:locked]`.trim();
    touched++;
  }

  await writeFile(imagePath, JSON.stringify({ items }, null, 2), "utf8");
  console.log(`Top40 critical pass applied on ${touched} music rounds.`);
}

await main();
