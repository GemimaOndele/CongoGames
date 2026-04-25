/**
 * Télécharge un lot d’images Wikimedia Commons vers Theme/ImageGuess/ (noms alignés sur la banque du jeu)
 * et produit ATTRIBUTION.json + ATTRIBUTION.md (crédits, licences, liens).
 *
 * Usage:
 *   node scripts/import-image-guess-commons.mjs
 *   node scripts/import-image-guess-commons.mjs --manifest scripts/data/image_guess_commons.manifest.json
 *   node scripts/import-image-guess-commons.mjs --dry-run
 *
 * Prérequis : Node 18+ (fetch intégré). Réseau requis.
 */

import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(__dirname, "..");

const DEFAULT_MANIFEST = path.join(root, "scripts", "data", "image_guess_commons.manifest.json");
const DEFAULT_OUT = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Theme",
  "ImageGuess"
);

const UA = {
  "User-Agent": "CongoGamesCommonsImport/1.0 (local dataset; +https://github.com/GemimaOndele/CongoGames)"
};

let manifestPath = DEFAULT_MANIFEST;
let outDir = DEFAULT_OUT;
let dryRun = false;

const args = process.argv.slice(2);
for (let i = 0; i < args.length; i++) {
  if (args[i] === "--manifest" && args[i + 1]) manifestPath = path.resolve(args[++i]);
  if (args[i] === "--out" && args[i + 1]) outDir = path.resolve(args[++i]);
  if (args[i] === "--dry-run") dryRun = true;
}

function stripHtml(s) {
  if (s == null) return "";
  return String(s)
    .replace(/<br\s*\/?>/gi, " ")
    .replace(/<[^>]+>/g, "")
    .replace(/\s+/g, " ")
    .trim();
}

function extFromPath(u) {
  const p = new URL(u).pathname.toLowerCase();
  const e = path.extname(p).split("?")[0] || ".bin";
  if ([".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg"].includes(e)) return e;
  return ".bin";
}

async function queryImageInfo(fileTitle) {
  const u =
    "https://commons.wikimedia.org/w/api.php?action=query&format=json" +
    "&prop=imageinfo" +
    `&titles=${encodeURIComponent(fileTitle)}` +
    "&iiprop=url|mime|extmetadata|size" +
    "&iiurlwidth=1600";
  const res = await fetch(u, { headers: UA });
  if (!res.ok) throw new Error(`API ${res.status}`);
  const j = await res.json();
  const pages = j.query?.pages;
  if (!pages) return null;
  const p = Object.values(pages)[0];
  if (p.missing != null || p.invalid != null) return null;
  const ii = p.imageinfo?.[0];
  if (!ii) return null;
  return { pageid: p.pageid, title: p.title, ii };
}

function pickDownloadUrl(ii) {
  const mime = ii.mime || "";
  if (mime === "image/svg+xml" && ii.thumburl) {
    return { url: ii.thumburl, reason: "svg_raster_preview" };
  }
  if (ii.thumburl && (mime === "image/svg+xml" || !ii.url)) {
    return { url: ii.thumburl, reason: "thumb" };
  }
  return { url: ii.url, reason: "original" };
}

function buildCreditRecord(pageTitle, fileTitle, descriptionurl, ii, extMeta) {
  const lic =
    stripHtml(
      extMeta.LicenseShortName?.value || extMeta.License?.value || extMeta.UsageTerms?.value || "?"
    ) || "?";
  const artist = stripHtml(extMeta.Artist?.value || extMeta.Credit?.value || "") || "—";
  const attribution = stripHtml(
    extMeta.AttributionRequired?.value || extMeta.Credit?.value || ""
  );
  const licenseUrl = stripHtml(extMeta.LicenseUrl?.value || "");
  return {
    commonsPageTitle: pageTitle,
    fileTitle: fileTitle,
    filePageUrl: descriptionurl || ii.descriptionurl,
    fileUrl: ii.url,
    mime: ii.mime,
    license: lic,
    licenseUrl: licenseUrl || null,
    artist: artist,
    attributionNote: attribution || null
  };
}

function sleep(ms) {
  return new Promise((r) => setTimeout(r, ms));
}

async function main() {
  if (!fs.existsSync(manifestPath)) {
    console.error("Manifest introuvable :", manifestPath);
    process.exit(1);
  }

  const raw = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
  const items = raw.items;
  if (!Array.isArray(items) || items.length === 0) {
    console.error("Aucun item dans le manifeste.");
    process.exit(1);
  }

  if (!dryRun && !fs.existsSync(outDir)) {
    fs.mkdirSync(outDir, { recursive: true });
  }

  /** @type {Map<string, { buffer: Buffer, ext: string, credit: object }>} */
  const byTitle = new Map();
  const uniqueTitles = [...new Set(items.map((x) => x.fileTitle))];

  console.log("Fichiers Commons uniques :", uniqueTitles.length);
  for (const fileTitle of uniqueTitles) {
    if (byTitle.has(fileTitle)) continue;
    console.log("Résolution", fileTitle, "…");
    const info = await queryImageInfo(fileTitle);
    if (!info) {
      console.warn("  ! introuvable ou manquant :", fileTitle);
      byTitle.set(fileTitle, null);
      continue;
    }
    const { ii } = info;
    const extMeta = ii.extmetadata || {};
    const pick = pickDownloadUrl(ii);
    if (!pick?.url) {
      console.warn("  ! pas d’URL :", fileTitle);
      byTitle.set(fileTitle, null);
      continue;
    }
    if (dryRun) {
      console.log("  (dry-run) serait :", pick.url, pick.reason);
      byTitle.set(fileTitle, { buffer: null, ext: ".jpg", credit: { dryRun: true, fileTitle } });
      await sleep(200);
      continue;
    }

    const f = await fetch(pick.url, { headers: UA });
    if (!f.ok) {
      console.warn("  ! téléchargement", f.status, fileTitle);
      byTitle.set(fileTitle, null);
      continue;
    }
    const ab = await f.arrayBuffer();
    const buffer = Buffer.from(ab);
    let ext = extFromPath(pick.url);
    if (ext === ".php" || ext === ".bin") ext = ".png";

    const pageUrl = ii.descriptionurl ||
      `https://commons.wikimedia.org/wiki/${encodeURIComponent(fileTitle.replace(/ /g, "_"))}`;

    const baseCredit = buildCreditRecord(info.title, fileTitle, pageUrl, ii, extMeta);
    const credit = {
      ...baseCredit,
      downloadedFrom: pick.url,
      downloadMode: pick.reason
    };
    byTitle.set(fileTitle, { buffer, ext, credit });
    await sleep(350);
  }

  const flat = [];
  for (const row of items) {
    const e = byTitle.get(row.fileTitle);
    if (dryRun) {
      flat.push({
        id: row.id,
        fileTitle: row.fileTitle,
        status: "dry_run"
      });
      continue;
    }
    if (!e || !e.buffer) {
      flat.push({
        id: row.id,
        fileTitle: row.fileTitle,
        status: "error",
        note: "fichier introuvable ou téléchargement échoué"
      });
      continue;
    }
    const safeExt = e.ext === ".jpeg" ? ".jpg" : e.ext;
    const dest = path.join(outDir, `${row.id}${safeExt}`);
    fs.writeFileSync(dest, e.buffer);
    const c = e.credit;
    flat.push({
      id: row.id,
      localFile: `${row.id}${safeExt}`,
      fileTitle: row.fileTitle,
      filePageUrl: c.filePageUrl,
      fileUrl: c.fileUrl,
      license: c.license,
      licenseUrl: c.licenseUrl,
      artist: c.artist,
      downloadMode: c.downloadMode,
      downloadedFrom: c.downloadedFrom
    });
  }

  const jsonPath = path.join(outDir, "ATTRIBUTION.json");
  const mdPath = path.join(outDir, "ATTRIBUTION.md");
  if (!dryRun) {
    fs.writeFileSync(
      jsonPath,
      JSON.stringify(
        {
          generatedAt: new Date().toISOString(),
          source: "Wikimedia Commons",
          manifest: path.relative(root, manifestPath).replace(/\\/g, "/"),
          items: flat
        },
        null,
        2
      ),
      "utf8"
    );

    const md = [
      "# Crédits — images (Theme / ImageGuess)",
      "",
      "Ces fichiers proviennent de **Wikimedia Commons**. Chaque image reste sous la **licence indiquée sur la page du fichier** ; l’exemple ci-dessous est informatif — en cas de doute, se reporter à la page Commons et aux obligations de la licence (mention de l’auteur, partage identique, etc.).",
      "",
      "| Fichier local (jeu) | Fichier Commons | Auteur / crédit (indicatif) | Licence (indicatif) | Lien |",
      "|---------------------|-----------------|-----------------------------|------------------------|------|"
    ];
    for (const it of flat) {
      if (it.status === "error") {
        md.push(`| *${it.id}* | ${it.fileTitle} | — | — | (échec import) |`);
        continue;
      }
      if (it.status === "dry_run") {
        md.push(`| *${it.id}* | ${it.fileTitle} | — | — | (dry-run) |`);
        continue;
      }
      const link = (it.filePageUrl || "").replace(/\|/g, "\\|");
      md.push(
        `| \`${it.localFile}\` | \`${(it.fileTitle || "").replace(/\|/g, "\\|")}\` | ${(it.artist || "—").replace(/\|/g, "\\|")} | ${(it.license || "—").replace(/\|/g, "\\|")} | [Commons](${link}) |`
      );
    }
    md.push("");
    md.push("Fichier machine : `ATTRIBUTION.json`. Regénérer : `npm run dataset:image-guess`.");

    fs.writeFileSync(mdPath, md.join("\n"), "utf8");
  }

  console.log("");
  console.log(dryRun ? "(dry-run) Terminé." : "Terminé.");
  if (!dryRun) {
    console.log("Dossier :", outDir);
    console.log("  →", path.basename(jsonPath), path.basename(mdPath));
  }
  const nOk = flat.filter((x) => x.status !== "error").length;
  console.log("Lignes :", nOk, "/", items.length, dryRun ? "(dry-run, fichiers non écrits)" : "écrites");
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
