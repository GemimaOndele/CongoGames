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

function normSpaces(s) {
  return String(s || "").replace(/\s+/g, " ").trim();
}

function upperClean(s) {
  return normSpaces(s).toUpperCase();
}

function categoryOf(item) {
  const t = String(item.trivia || "").toLowerCase();
  const m = t.match(/\[category:([^\]]+)\]/);
  return m ? m[1] : "";
}

function trackNo(fileBase) {
  const m = String(fileBase || "").toLowerCase().match(/^track(\d+)$/);
  return m ? Number.parseInt(m[1], 10) : 0;
}

function detectType(item) {
  const hint = String(item.hint || "").toLowerCase();
  const mod = Number(item.styleSeed || 0) % 10;
  if (mod === 1 || /interpr|artiste|chanteur|chanteuse|groupe/.test(hint)) return "artist";
  if (mod === 2 || /titre/.test(hint)) return "title";
  if (mod === 3 || /langue/.test(hint)) return "lang";
  if (mod === 4 || /collaboration|solo|feat/.test(hint)) return "collab";
  return "generic";
}

function splitMainArtist(artist) {
  const a = normSpaces(artist);
  const chunks = a.split(/\s+(?:feat\.?|ft\.?|x|&|vs)\s+|,\s*/i).map(normSpaces).filter(Boolean);
  return chunks.length > 0 ? chunks[0] : a;
}

function inferLang(meta) {
  const s = `${meta.artist || ""} ${meta.title || ""}`.toLowerCase();
  if (/kilombo|bokoko|mbongo|ngoma|nzoungou|ya nga|mentalité|moselebende|missengue/.test(s)) return "LINGALA";
  if (/bûcheron|journal|intime|contentieux/.test(s)) return "FRANCAIS";
  return "LINGALA";
}

function musicImageFor(type, track) {
  if (type === "lang") return "langues";
  if (type === "title") return track % 2 === 0 ? "ndombolo" : "sapeurs";
  if (type === "collab") return "sapeurs";
  // artist / generic
  return track % 3 === 0 ? "kintele" : "sapeurs";
}

function appendSyncTrivia(item, meta) {
  const base = String(item.trivia || "").replace(/\s*Sync meta:[^|]*\|\s*type:[^|]*\|\s*track:[^\s]+/i, "").trim();
  const sync = `Sync meta: ${meta.artist} - ${meta.title} | type:${detectType(item)} | track:${meta.fileBase}`;
  return `${base} ${sync}`.trim();
}

async function main() {
  const imageRaw = await readFile(imagePath, "utf8");
  const blindRaw = await readFile(blindMetaPath, "utf8");
  const imageJson = JSON.parse(imageRaw);
  const blindJson = JSON.parse(blindRaw);

  const items = Array.isArray(imageJson.items) ? imageJson.items : [];
  const metaMap = new Map((Array.isArray(blindJson.items) ? blindJson.items : []).map((m) => [String(m.fileBase || "").toLowerCase(), m]));

  let fixed = 0;
  for (const item of items) {
    const cat = categoryOf(item);
    if (cat !== "music_related") continue;

    const fb = String(item.audioFileBase || "").toLowerCase();
    const meta = metaMap.get(fb);
    if (!meta) continue;

    const t = detectType(item);
    const tn = trackNo(fb);
    const artistFull = upperClean(meta.artist);
    const artistMain = upperClean(splitMainArtist(meta.artist));
    const title = upperClean(meta.title || "");

    if (t === "artist") {
      item.answerKey = artistMain || artistFull;
      item.altAnswerKey = artistFull || "";
    } else if (t === "title") {
      item.answerKey = title && title !== "—" ? title : "TITRE NON PRECISE";
      item.altAnswerKey = "";
    } else if (t === "lang") {
      const lang = inferLang(meta);
      item.answerKey = lang;
      item.altAnswerKey = lang === "LINGALA" ? "KITUBA" : "";
    } else if (t === "collab") {
      const collab = /\b(feat|ft| x |&|vs)\b/i.test(meta.artist || "");
      item.answerKey = collab ? "COLLABORATION" : "SOLO";
      item.altAnswerKey = collab ? "FEAT" : "ARTISTE SEUL";
    }

    item.streamingFileBase = musicImageFor(t, tn);
    item.trivia = appendSyncTrivia(item, meta);
    fixed++;
  }

  await writeFile(imagePath, JSON.stringify({ items }, null, 2), "utf8");
  console.log(`Quality pass image-guess: ${fixed} manches music_related synchronisées.`);
}

await main();
