/**
 * Génère blind_playlist_facts.json (année/langue/inspiration/verified) à partir
 * de minigame_image_guess_extras.json enrichi.
 */
import { readFile, writeFile, mkdir } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");
const inFile = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Datasets",
  "minigame_image_guess_extras.json"
);
const outDir = path.join(root, "UnityProject", "Assets", "Resources", "Datasets");
const outFile = path.join(outDir, "blind_playlist_facts.json");

function parseFactsFromTrivia(trivia) {
  const t = (trivia || "").trim();
  if (!t) return null;
  const yearMatch = t.match(/Repère année:\s*(\d{4})/i);
  const languageMatch = t.match(/Langue principale attendue:\s*([A-ZÀ-ÖØ-Ý\-]+)/i);
  const inspirationMatch = t.match(/(?:inspir[ée]e?|motif|raison)\s*:\s*([^.\n]{8,220})/i);
  const hasSources = /Sources\s*:/i.test(t);
  const partial = /vérification partielle/i.test(t);

  return {
    releaseYear: yearMatch ? Number.parseInt(yearMatch[1], 10) : 0,
    language: languageMatch ? languageMatch[1].trim().toUpperCase() : "",
    inspiration: inspirationMatch ? inspirationMatch[1].trim() : "",
    verified: hasSources && !partial,
  };
}

const raw = await readFile(inFile, "utf8");
const json = JSON.parse(raw);
const items = Array.isArray(json?.items) ? json.items : [];

const byTrack = new Map();
for (const it of items) {
  const fileBase = (it?.audioFileBase || "").trim();
  if (!fileBase) continue;
  const facts = parseFactsFromTrivia(it?.trivia || "");
  if (!facts) continue;

  const prev = byTrack.get(fileBase) || {
    fileBase,
    releaseYear: 0,
    language: "",
    inspiration: "",
    verified: false,
  };

  if (!prev.releaseYear && facts.releaseYear) prev.releaseYear = facts.releaseYear;
  if (!prev.language && facts.language) prev.language = facts.language;
  if (!prev.inspiration && facts.inspiration) prev.inspiration = facts.inspiration;
  prev.verified = Boolean(prev.verified || facts.verified);
  byTrack.set(fileBase, prev);
}

const out = { items: [...byTrack.values()].sort((a, b) => a.fileBase.localeCompare(b.fileBase, "fr")) };
await mkdir(outDir, { recursive: true });
await writeFile(outFile, JSON.stringify(out, null, 2) + "\n", "utf8");
console.log(`blind_playlist_facts.json généré: ${out.items.length} pistes -> ${outFile}`);
