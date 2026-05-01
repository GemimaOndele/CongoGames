/**
 * Parse Congogame/playlist/*.mp3 (noms réels) -> blind_playlist_meta.json.
 * Le blind test se base sur ces noms de fichiers (artiste/groupe/titre/style).
 */
import { readdir, writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");
const playlistDir = path.join(root, "playlist");
const outDir = path.join(root, "UnityProject", "Assets", "Resources", "Datasets");
const outFile = path.join(outDir, "blind_playlist_meta.json");

function normalizeSpaces(s) {
  return (s || "").replace(/\s+/g, " ").trim();
}

function parseArtistAndTitle(baseRaw) {
  let base = normalizeSpaces(baseRaw);
  let artist = base;
  let title = "—";

  const spacedSep = base.indexOf(" - ");
  const genericSep = spacedSep < 0 ? base.indexOf("-") : -1;
  const cut = spacedSep >= 0 ? spacedSep : genericSep;
  if (cut > 0 && cut < base.length - 1) {
    const sepLen = spacedSep >= 0 ? 3 : 1;
    artist = normalizeSpaces(base.slice(0, cut));
    title = normalizeSpaces(base.slice(cut + sepLen)) || "—";
  }

  // Préfixe explicite groupe.
  artist = artist.replace(/^Le groupe_/i, "").trim();
  artist = normalizeSpaces(artist);
  title = normalizeSpaces(title);

  return {
    artist: artist || "?",
    title: title || "—",
  };
}

function extractTagValues(base) {
  const tags = [];
  const re = /(\[[^\]]+\]|\([^)]+\))/g;
  let m;
  while ((m = re.exec(base)) !== null) {
    const t = m[0].slice(1, -1).trim();
    if (t) tags.push(t);
  }
  return tags;
}

const files = (await readdir(playlistDir).catch(() => []))
  .filter((f) => /\.mp3$/i.test(f))
  .sort((a, b) => a.localeCompare(b, "fr", { sensitivity: "base" }));

const items = [];
let n = 1;
function postProcessMetaItem(item) {
  const fb = item.fileBase || "";
  // Fichier renommé côté utilisateur : "Mvila" fait partie du nom de l'invité (Marvy Mvila), titre affiché = Tala Mbasse.
  if (/Dj Bookson feat Marvy.*Mvila Tala mbasse/i.test(fb)) {
    return {
      ...item,
      artist: "Dj Bookson feat Marvy Mvila",
      title: "Tala Mbasse",
    };
  }
  return item;
}

for (const f of files) {
  const base = f.replace(/\.mp3$/i, "");
  const { artist, title } = parseArtistAndTitle(base);
  const tags = extractTagValues(base);
  items.push(
    postProcessMetaItem({
      fileBase: base,
      fileName: f,
      artist,
      title,
      tags,
    })
  );
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
