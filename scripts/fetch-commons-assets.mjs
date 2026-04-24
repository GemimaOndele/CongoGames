/**
 * Télécharge des fichiers listés par l'API Wikimedia Commons (recherche) + manifeste JSON.
 *
 * Usage:
 *   node scripts/fetch-commons-assets.mjs --query "Brazzaville" --limit 5
 *
 * Sortie : Datasets/harvest/staging/ (voir .gitignore : staging ignoré si besoin)
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(__dirname, "..");
const STAGING = path.join(root, "Datasets", "harvest", "staging");

let query = "Brazzaville";
let limit = 3;
const args = process.argv.slice(2);
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--query" && args[i + 1]) query = args[++i];
  if (args[i] === "--limit" && args[i + 1]) limit = Math.min(50, Math.max(1, parseInt(args[++i], 10) || 3));
}

if (!fs.existsSync(STAGING)) fs.mkdirSync(STAGING, { recursive: true });

const ua = { "User-Agent": "CongoGamesDatasetBot/1.0 (local; https://github.com/GemimaOndele/CongoGames)" };

const searchUrl =
  "https://commons.wikimedia.org/w/api.php?action=query&format=json" +
  "&list=search&srnamespace=6" +
  `&srlimit=${limit}` +
  `&srsearch=${encodeURIComponent(query)}`;

const sRes = await fetch(searchUrl, { headers: ua });
if (!sRes.ok) {
  console.error("API search erreur:", sRes.status);
  process.exit(1);
}
const sData = await sRes.json();
const hits = sData.query?.search || [];
if (hits.length === 0) {
  console.log("Aucun résultat pour :", query);
  process.exit(0);
}

const titles = hits.map((h) => h.title).join("|");
const infoUrl =
  "https://commons.wikimedia.org/w/api.php?action=query&format=json" +
  `&titles=${encodeURIComponent(titles)}` +
  "&prop=imageinfo&iiprop=url|extmetadata&iiurlwidth=200";

const iRes = await fetch(infoUrl, { headers: ua });
if (!iRes.ok) {
  console.error("API imageinfo erreur:", iRes.status);
  process.exit(1);
}
const iData = await iRes.json();
const pages = iData.query?.pages || {};

const manifest = {
  query,
  limit,
  fetchedAt: new Date().toISOString(),
  items: []
};

for (const pid of Object.keys(pages)) {
  const p = pages[pid];
  if (p.missing) continue;
  const ii = p.imageinfo?.[0];
  if (!ii?.url) continue;

  const url = ii.url;
  const meta = ii.extmetadata || {};
  const license = (meta.LicenseShortName?.value || meta.License?.value || "?")
    .toString()
    .replace(/<[^>]+>/g, "");
  const artist = (meta.Artist?.value || "?").toString().replace(/<[^>]+>/g, " ").slice(0, 500);

  let ext = path.extname(new URL(url).pathname).toLowerCase();
  if (![".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".ogg", ".mp3", ".opus", ".webm"].includes(ext)) {
    ext = ".bin";
  }

  const safeName = `commons_${pid}${ext}`;
  const dest = path.join(STAGING, safeName);

  try {
    const f = await fetch(url, { headers: ua });
    if (!f.ok) throw new Error(String(f.status));
    fs.writeFileSync(dest, Buffer.from(await f.arrayBuffer()));
  } catch (e) {
    console.warn("Skip", p.title, e.message);
    continue;
  }

  manifest.items.push({
    file: safeName,
    pageTitle: p.title,
    pageUrl: `https://commons.wikimedia.org/?curid=${pid}`,
    fileUrl: url,
    licenseShort: license,
    artist,
    sizeBytes: fs.statSync(dest).size
  });
}

const manifestPath = path.join(
  STAGING,
  `manifest-${query.replace(/[^a-z0-9_-]/gi, "_")}-${Date.now()}.json`
);
fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2), "utf8");
console.log("Dossier :", STAGING);
console.log("Manifeste :", manifestPath);
console.log("Téléchargés :", manifest.items.length, "/", hits.length);
