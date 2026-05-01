/**
 * Génère un dictionnaire TTS (find -> replace) depuis blind_playlist_meta.json.
 * Merge avec les overrides existants pour conserver les ajustements manuels.
 */
import { readFile, writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");
const datasetsDir = path.join(root, "UnityProject", "Assets", "Resources", "Datasets");
const blindMetaFile = path.join(datasetsDir, "blind_playlist_meta.json");
const outFile = path.join(datasetsDir, "tts_pronunciation_overrides.json");

function toTitleLike(text) {
  const s = (text || "").trim();
  if (!s) return "";
  return s
    .split(/\s+/)
    .map((w) => {
      if (/^AVB$/i.test(w)) return "AVB";
      if (/^[A-Z0-9]{2,}$/.test(w)) {
        const lowAcronyms = new Set(["DJ", "MC"]);
        if (lowAcronyms.has(w.toUpperCase())) return w.toUpperCase();
      }
      const low = w.toLowerCase();
      return low.charAt(0).toUpperCase() + low.slice(1);
    })
    .join(" ");
}

function normalizeForPronunciation(text) {
  let s = toTitleLike(text);
  if (!s) return "";

  s = s
    .replace(/\bAVB\b/gi, "A V B")
    .replace(/\bFt\.?\b/gi, "featuring")
    .replace(/\bFeat\.?\b/gi, "featuring")
    .replace(/\bVs\b/gi, "versus")
    .replace(/\s+x\s+/gi, " avec ")
    .replace(/\s*&\s*/g, " et ")
    .replace(/Extra Musica/gi, "Extra Moussica")
    .replace(/Mboshi/gi, "Mbochi")
    .replace(/Kituba/gi, "Kitouba")
    .replace(/Nzoungou/gi, "Ndzoungou")
    .replace(/Nzungou/gi, "Ndzoungou")
    .replace(/Soûlard/gi, "Soulard")
    .replace(/\s*[-_/\\]\s*/g, " ")
    .replace(/\s+/g, " ")
    .trim();

  return s;
}

function addRule(map, find, replace) {
  const f = (find || "").trim();
  const r = (replace || "").trim();
  if (!f || !r) return;
  const key = f.toLowerCase();
  if (!map.has(key)) {
    map.set(key, { find: f, replace: r });
  }
}

function upsertRule(map, find, replace) {
  const f = (find || "").trim();
  const r = (replace || "").trim();
  if (!f || !r) return;
  const key = f.toLowerCase();
  map.set(key, { find: f, replace: r });
}

async function readJsonSafe(file) {
  try {
    const raw = await readFile(file, "utf8");
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

const blindMeta = await readJsonSafe(blindMetaFile);
if (!blindMeta?.items || !Array.isArray(blindMeta.items)) {
  throw new Error("blind_playlist_meta.json invalide ou vide.");
}

const existing = await readJsonSafe(outFile);
const rulesMap = new Map();

if (existing?.items && Array.isArray(existing.items)) {
  for (const it of existing.items) {
    addRule(rulesMap, it?.find, it?.replace);
  }
}

for (const item of blindMeta.items) {
  const artist = (item?.artist || "").trim();
  const title = (item?.title || "").trim();

  if (artist) {
    upsertRule(rulesMap, artist, normalizeForPronunciation(artist));
  }

  if (title && title !== "—") {
    upsertRule(rulesMap, title, normalizeForPronunciation(title));
  }
}

const items = [...rulesMap.values()]
  .sort((a, b) => a.find.localeCompare(b.find, "fr", { sensitivity: "base" }));

await mkdir(datasetsDir, { recursive: true });
await writeFile(outFile, JSON.stringify({ items }, null, 2) + "\n", "utf8");
console.log("tts_pronunciation_overrides.json généré:", items.length, "règles");
