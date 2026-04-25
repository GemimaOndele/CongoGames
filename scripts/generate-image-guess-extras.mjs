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

const outStreaming = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Datasets",
  "minigame_image_guess_extras.json"
);

const imageBases = [
  "sapeurs",
  "kintele",
  "pointe_noire",
  "capitale",
  "ndombolo",
  "langues",
  "gorille",
  "fleuve_congo",
  "ocean",
  "parc",
  "kouilou",
  "likouala",
  "sangha",
  "aeroport",
  "odzala",
  "departement_pool",
  "drapeau"
];

const legacyArtists = new Set([
  "casimir zao",
  "franklin boukaka",
  "les bantous de la capitale",
  "pamelo mounka",
  "pierre moutouari",
  "pierrette adams",
  "youlou mabiala"
]);

function normalize(s) {
  return (s || "")
    .trim()
    .replace(/\s+/g, " ")
    .replace(/\u2019/g, "'")
    .replace(/\u2018/g, "'");
}

function splitArtists(rawArtist) {
  const artist = normalize(rawArtist);
  const chunks = artist
    .split(/\s+(?:feat\.?|ft\.?|x|&|vs)\s+|,\s*/i)
    .map((v) => normalize(v))
    .filter(Boolean);
  if (chunks.length === 0) return [artist];
  return chunks;
}

function inferLanguage(title, artist) {
  const s = (title + " " + artist).toLowerCase();
  if (/kilombo|makambo|ngoma|mbongo|bokoko|ya nga|missengue|loufoulakari/.test(s)) {
    return "LINGALA";
  }
  if (/congo|heritage|journal|amour|contentieux/.test(s)) {
    return "FRANCAIS";
  }
  return "LINGALA";
}

function inferStyle(artist) {
  const a = artist.toLowerCase();
  if (legacyArtists.has(a)) return "CLASSIQUE";
  if (a.includes("extra musica") || a.includes("feat") || a.includes(" x ")) return "MODERNE";
  return "FUSION";
}

function buildVariants(track, i) {
  const artist = normalize(track.artist);
  const title = normalize(track.title);
  const parts = splitArtists(artist);
  const mainArtist = parts[0] || artist;
  const isCollab = parts.length > 1 || /feat|ft| x |&|vs/i.test(artist);
  const lang = inferLanguage(title, artist);
  const style = inferStyle(artist);
  const fileBase = track.fileBase;
  const img = imageBases[i % imageBases.length];
  const commonTrivia =
    `Piste ${fileBase} — ${artist} - ${title}. ` +
    `Style estimé: ${style}. Réponse basée sur les métadonnées playlist.`;

  return [
    {
      hint: `Image d'artiste liée à l'extrait: qui interprète « ${title} » ?`,
      answerKey: mainArtist.toUpperCase(),
      styleSeed: 6000 + i * 10 + 1,
      streamingFileBase: img,
      altAnswerKey: artist.toUpperCase(),
      trivia: `${commonTrivia} Question: artiste principal.`,
      audioFileBase: fileBase,
      audioUrl: ""
    },
    {
      hint: `Avec cette image et l'extrait audio, quel est le titre du morceau ?`,
      answerKey: title.toUpperCase(),
      styleSeed: 6000 + i * 10 + 2,
      streamingFileBase: img,
      altAnswerKey: "",
      trivia: `${commonTrivia} Question: titre exact.`,
      audioFileBase: fileBase,
      audioUrl: ""
    },
    {
      hint: `D'après l'extrait, dans quelle langue dominante est chanté ce titre ?`,
      answerKey: lang,
      styleSeed: 6000 + i * 10 + 3,
      streamingFileBase: "langues",
      altAnswerKey: lang === "LINGALA" ? "KITUBA" : "",
      trivia: `${commonTrivia} Langue dominante estimée automatiquement.`,
      audioFileBase: fileBase,
      audioUrl: ""
    },
    {
      hint: `Ce titre est-il un solo ou une collaboration (feat/x/&)?`,
      answerKey: isCollab ? "COLLABORATION" : "SOLO",
      styleSeed: 6000 + i * 10 + 4,
      streamingFileBase: img,
      altAnswerKey: isCollab ? "FEAT" : "ARTISTE SEUL",
      trivia: `${commonTrivia} Détection via nom d'artiste (${artist}).`,
      audioFileBase: fileBase,
      audioUrl: ""
    }
  ];
}

async function main() {
  const raw = await readFile(blindMetaPath, "utf8");
  const parsed = JSON.parse(raw);
  const tracks = Array.isArray(parsed.items) ? parsed.items : [];
  if (tracks.length === 0) {
    throw new Error("blind_playlist_meta.json vide");
  }

  const args = new Set(process.argv.slice(2));
  const hasAll = args.has("--all");
  const countArg = process.argv.find((a) => a.startsWith("--count="));
  const parsedCount = countArg ? Number.parseInt(countArg.split("=")[1], 10) : Number.NaN;
  const targetCount = hasAll
    ? tracks.length * 4
    : (Number.isFinite(parsedCount) && parsedCount > 0 ? parsedCount : 40);

  const items = [];
  let i = 0;
  while (items.length < targetCount) {
    const track = tracks[i % tracks.length];
    const variants = buildVariants(track, i);
    for (const v of variants) {
      items.push(v);
      if (items.length >= targetCount) break;
    }
    i++;
  }

  await mkdir(path.dirname(outStreaming), { recursive: true });
  await writeFile(outStreaming, JSON.stringify({ items }, null, 2), "utf8");
  console.log(`minigame_image_guess_extras.json: ${items.length} manches -> ${outStreaming}`);
}

await main();
