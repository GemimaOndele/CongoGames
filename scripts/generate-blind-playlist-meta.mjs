/**
 * Parse Congogame/playlist/*.mp3 "Artiste - Titre.mp3" -> StreamingAssets Datasets JSON pour le blind test.
 * Lancé après copy-playlist (même ordre de tri = track01, track02…).
 */
import { readdir, writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");
const playlistDir = path.join(root, "playlist");
const outDir = path.join(root, "UnityProject", "Assets", "Resources", "Datasets");
const outFile = path.join(outDir, "blind_playlist_meta.json");

function parseName(base) {
  const sep = " - ";
  const i = base.indexOf(sep);
  if (i <= 0) {
    return { artist: base.trim() || "?", title: "—" };
  }
  return {
    artist: base.slice(0, i).trim() || "?",
    title: base.slice(i + sep.length).trim() || "—",
  };
}

const files = (await readdir(playlistDir).catch(() => []))
  .filter((f) => /\.mp3$/i.test(f))
  .sort((a, b) => a.localeCompare(b, "fr", { sensitivity: "base" }));

const items = [];
let n = 1;
for (const f of files) {
  const base = f.replace(/\.mp3$/i, "");
  const { artist, title } = parseName(base);
  items.push({
    fileBase: "track" + n.toString().padStart(2, "0"),
    artist,
    title,
  });
  n++;
}

const json = JSON.stringify(
  { items },
  null,
  2
);
await mkdir(outDir, { recursive: true });
await writeFile(outFile, json, "utf8");
console.log("blind_playlist_meta.json : " + items.length + " pistes -> " + outFile);
