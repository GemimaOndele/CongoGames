import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");

const blindMetaPath = path.join(
  root,
  "UnityProject",
  "Assets",
  "Resources",
  "Datasets",
  "blind_playlist_meta.json"
);

const imageGuessPath = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Datasets",
  "minigame_image_guess_extras.json"
);

const artistImageDir = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Theme",
  "ImageGuess"
);

function normalize(s) {
  return String(s || "").replace(/\s+/g, " ").trim();
}

function mainArtist(artistRaw) {
  const a = normalize(artistRaw);
  const parts = a
    .split(/\s+(?:feat\.?|ft\.?|x|&|vs)\s+|,\s*/i)
    .map((v) => normalize(v))
    .filter(Boolean);
  return parts.length > 0 ? parts[0] : a;
}

function categoryOf(item) {
  const m = String(item.trivia || "").toLowerCase().match(/\[category:([^\]]+)\]/);
  return m ? m[1] : "";
}

function trackNo(fileBase) {
  const m = String(fileBase || "").toLowerCase().match(/^track(\d+)$/);
  return m ? Number.parseInt(m[1], 10) : 0;
}

function detectType(item) {
  const h = String(item.hint || "").toLowerCase();
  const mod = Number(item.styleSeed || 0) % 10;
  if (mod === 1 || /interpr|artiste|chanteur|chanteuse|groupe/.test(h)) return "artist";
  if (mod === 2 || /titre/.test(h)) return "title";
  if (mod === 3 || /langue/.test(h)) return "lang";
  if (mod === 4 || /solo|collaboration|feat/.test(h)) return "collab";
  return "generic";
}

async function fetchJson(url) {
  const r = await fetch(url, { headers: { "user-agent": "CongoGames/1.0" } });
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  return r.json();
}

async function downloadBinary(url) {
  const r = await fetch(url, { headers: { "user-agent": "CongoGames/1.0" } });
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  const ab = await r.arrayBuffer();
  return Buffer.from(ab);
}

async function resolveArtistImageUrl(artistName) {
  const q = encodeURIComponent(artistName);
  const data = await fetchJson(`https://api.deezer.com/search/artist?q=${q}`);
  const list = Array.isArray(data?.data) ? data.data : [];
  if (list.length === 0) return "";
  const first = list[0];
  return String(first.picture_xl || first.picture_big || first.picture_medium || first.picture || "").trim();
}

async function main() {
  const blindMeta = JSON.parse(await readFile(blindMetaPath, "utf8"));
  const ig = JSON.parse(await readFile(imageGuessPath, "utf8"));
  const tracks = Array.isArray(blindMeta.items) ? blindMeta.items : [];
  const items = Array.isArray(ig.items) ? ig.items : [];

  await mkdir(artistImageDir, { recursive: true });

  const imageBaseByTrack = new Map();
  let downloaded = 0;
  let missingImage = 0;

  for (const t of tracks) {
    const fb = String(t.fileBase || "").toLowerCase();
    const no = trackNo(fb);
    if (no <= 0) continue;
    const artist = mainArtist(t.artist);
    const base = `artist_track${String(no).padStart(2, "0")}`;
    const out = path.join(artistImageDir, `${base}.jpg`);

    try {
      const imageUrl = await resolveArtistImageUrl(artist);
      if (!imageUrl) {
        missingImage++;
        continue;
      }
      const bin = await downloadBinary(imageUrl);
      if (!bin || bin.length < 1024) {
        missingImage++;
        continue;
      }
      await writeFile(out, bin);
      imageBaseByTrack.set(fb, base);
      downloaded++;
    } catch {
      missingImage++;
    }
  }

  let linked = 0;
  let muted = 0;
  for (const item of items) {
    const cat = categoryOf(item);
    if (cat !== "music_related") continue;
    const fb = String(item.audioFileBase || "").toLowerCase();
    const base = imageBaseByTrack.get(fb);
    if (base) {
      item.streamingFileBase = base;
      const kind = detectType(item);
      const cleaned = String(item.trivia || "").replace(/\s*\[artistImage:[^\]]+\]/gi, "").trim();
      item.trivia = `${cleaned} [artistImage:linked] [kind:${kind}]`.trim();
      linked++;
    } else {
      // Pas d'image artiste fiable => pas de musique pour éviter les incohérences.
      item.audioFileBase = "";
      item.audioUrl = "";
      const cleaned = String(item.trivia || "").replace(/\s*\[artistImage:[^\]]+\]/gi, "").trim();
      item.trivia = `${cleaned} [artistImage:missing_audio_muted]`.trim();
      muted++;
    }
  }

  await writeFile(imageGuessPath, JSON.stringify({ items }, null, 2), "utf8");
  console.log(`Artist image pass done.`);
  console.log(`- images downloaded: ${downloaded}`);
  console.log(`- tracks without artist image: ${missingImage}`);
  console.log(`- rounds linked image+music: ${linked}`);
  console.log(`- rounds muted (no linked image): ${muted}`);
}

await main();
