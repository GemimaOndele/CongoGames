import { readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.join(here, "..");
const dataPath = path.join(
  root,
  "UnityProject",
  "Assets",
  "StreamingAssets",
  "Datasets",
  "minigame_image_guess_extras.json"
);

const verified = {
  track01: {
    artist: "CARINE FLEUR EDOUARE",
    lang: "KIKONGO",
    note:
      "Titre largement diffusé sur YouTube sous « KILOMBO CONGO - CARINE FLEUR EDOUARE »; artiste associée au répertoire chrétien congolais.",
    source:
      "Sources: YouTube(kCTQhEqIQl4), YouTube(sKKANow_vPg), Zenga Mambu(2016/06/27/carine-fleur-edouard-ba-mimbikudi)"
  },
  track02: {
    artist: "CASIMIR ZAO",
    year: "1984",
    lang: "LINGALA",
    note: "« Ancien Combattant » est documenté comme un classique de Zao (Casimir Zoba).",
    source: "Sources: Discogs(r2453633), AfricanGrooves(tag/casimir-zoba), AppleMusic(ancien-combattant)"
  },
  track03: {
    artist: "CASIMIR ZAO",
    lang: "LINGALA",
    note: "Titre du répertoire satirique de Zao; date officielle difficilement documentée sur source discographique ouverte.",
    source: "Sources: plateformes audio + discographies artiste (vérification partielle)"
  },
  track04: {
    artist: "CASIMIR ZAO",
    lang: "LINGALA",
    note: "« Soulard » existe en version originale Zao et en reprise/collab récente.",
    source: "Sources: YouTube(Iqz-qMquuuY), AppleMusic(soulard-feat-zao-casimir)"
  },
  track05: {
    artist: "CASIMIR ZAO",
    lang: "LINGALA",
    note: "Titre listé dans la discographie étendue de Zao; métadonnées publiques limitées.",
    source: "Sources: discographies agrégées (vérification partielle)"
  },
  track06: {
    artist: "CINO BLACK",
    lang: "LINGALA",
    note: "Collaboration (feat) détectée via nom de piste; données web publiques limitées.",
    source: "Source primaire: métadonnée playlist locale"
  },
  track07: {
    artist: "DAVY KASSA",
    lang: "LINGALA",
    note: "Collaboration (feat) détectée via nom de piste; données web publiques limitées.",
    source: "Source primaire: métadonnée playlist locale"
  },
  track08: {
    artist: "FRANKLIN BOUKAKA",
    year: "1974",
    lang: "FRANCAIS",
    note: "« Le bûcheron » est documenté comme titre engagé de Franklin Boukaka, souvent associé à Manu Dibango.",
    source: "Sources: YouTube(xZhmWltHCIY), Afrolegends(2023/07/28/le-bucheron-the-woodcutter), Spotify(le-bucheron-africa)"
  },
  track09: {
    artist: "KEDJEVARA",
    lang: "LINGALA",
    note: "Collaboration avec Extra Musica Nouvel Horizon indiquée par le titre de piste.",
    source: "Source primaire: métadonnée playlist locale"
  },
  track10: {
    artist: "LES BANTOUS DE LA CAPITALE",
    lang: "LINGALA",
    note: "« Makambo mibalé » documenté dans les catalogues discographiques du groupe.",
    source: "Sources: YouTube(pBi2SltjGjA), Discogs(13087302), AllMusic(mw0000922580)"
  },
  track11: {
    artist: "LOUZ BABY",
    lang: "LINGALA",
    note: "Collaboration Louz Baby x Paterne Maestro documentée sur le clip officiel « MBONGO ».",
    source: "Sources: YouTube(AYY-5530IxQ), chaîne Louz Baby(UCwU8RjDYRpQR71s2fQlSlDA)"
  },
  track12: {
    artist: "MICHELLE MOUTOUARI",
    lang: "FRANCAIS",
    note: "Titre listé dans la playlist locale; informations publiques limitées.",
    source: "Source primaire: métadonnée playlist locale"
  },
  track13: {
    artist: "NORBAT DE PARIS",
    lang: "LINGALA",
    note: "Titre orienté culture sape; vérification publique partielle.",
    source: "Source primaire: métadonnée playlist locale"
  },
  track14: {
    artist: "PAMELO MOUNKA",
    lang: "LINGALA",
    note: "Artiste classique congolais; titre confirmé dans les répertoires/compilations.",
    source: "Sources: discographies artiste (vérification partielle)"
  },
  track15: {
    artist: "PAMELO MOUNKA",
    lang: "LINGALA",
    note: "Titre de répertoire classique; métadonnées publiques partielles.",
    source: "Sources: discographies artiste (vérification partielle)"
  },
  track16: {
    artist: "PAMELO MOUNKA",
    lang: "FRANCAIS",
    note: "Titre francophone présent dans la playlist locale; vérification partielle.",
    source: "Source primaire: métadonnée playlist locale"
  },
  track17: {
    artist: "PIERRE MOUTOUARI",
    year: "2012",
    lang: "LINGALA",
    note: "« Missengue » documenté sur plateformes (album « Son 1er Disque D'or »).",
    source: "Sources: YouTube(mpNTT7JqSYc), Spotify(4MsI8EI8uuKCSsQWbyUTCo)"
  },
  track18: {
    artist: "PIERRETTE ADAMS",
    lang: "FRANCAIS",
    note: "Titre présent dans le répertoire de Pierrette Adams; informations publiques limitées.",
    source: "Sources: playlists/catalogues artiste (vérification partielle)"
  },
  track19: {
    artist: "PIERRETTE ADAMS",
    year: "1994",
    lang: "FRANCAIS",
    note: "« Journal intime » documenté (album/cd répertorié).",
    source: "Sources: Discogs(15733400), YouTube(bqhADJmJSkc), Amazon(B07X9414VQ)"
  },
  track20: {
    artist: "ROGA ROGA EXTRA MUSICA",
    year: "2021",
    lang: "LINGALA",
    note: "« Bokoko » documenté sur plateformes streaming et clip officiel.",
    source: "Sources: YouTube(9-ZuDPc3J64), Spotify(1DSdM1nslwDn8JlHElw6Yc)"
  },
  track21: {
    artist: "ROGA ROGA EXTRA MUSICA",
    year: "2015",
    lang: "LINGALA",
    note: "« Contentieux » documenté comme album/titre de Roga Roga.",
    source: "Sources: Spotify(album/3kSjIWPVcIk3Cy1CxKQcVY), YouTube(iYfAFDQm7O8)"
  },
  track22: {
    artist: "TEDDY BENZO MIXTON",
    year: "2025",
    lang: "LINGALA",
    note: "« Moselebende » documenté en single (feat Mixton & Spinho Stayze).",
    source: "Sources: AppleMusic(1820712861), YouTube(O4h6TKncKPw)"
  },
  track23: {
    artist: "TIDIANE MARIO",
    year: "2025",
    lang: "LINGALA",
    note: "« Ya Nga Bébé » documenté en collaboration Tidiane Mario / Tété Ketch / Vinny Baltazard.",
    source: "Sources: Spotify(1HELR67Mrl5CVBqDBI0RXi), AppleMusic(1822847795), Shazam(1822847799)"
  },
  track24: {
    artist: "WAYÉ",
    year: "2021",
    lang: "LINGALA",
    note: "« Soûlard remix » documenté en feat avec Zao Casimir.",
    source: "Sources: YouTube(nRDKxis65mo), AppleMusic(1578507286)"
  },
  track25: {
    artist: "YOULOU MABIALA",
    lang: "LINGALA",
    note: "Titre de répertoire classique/rumba congolaise.",
    source: "Sources: discographies artiste (vérification partielle)"
  },
  track26: {
    artist: "YOULOU MABIALA",
    lang: "LINGALA",
    note: "Titre listé dans la playlist locale; données web publiques limitées.",
    source: "Source primaire: métadonnée playlist locale"
  },
  track27: {
    artist: "ZITANY NEIL",
    lang: "FRANCAIS",
    note: "Titre listé dans la playlist locale; données web publiques limitées.",
    source: "Source primaire: métadonnée playlist locale"
  }
};

function enrichHint(item, meta) {
  if (!meta) return item.hint;
  const h = item.hint || "";
  if (h.includes("langue dominante")) {
    return h.replace("langue dominante", "langue dominante (info vérifiée quand disponible)");
  }
  if (h.includes("solo ou une collaboration")) {
    return h + " (indice: regarde les marqueurs feat/x/&)";
  }
  return h;
}

function enrichTrivia(item, meta) {
  if (!meta) return item.trivia;
  const parts = [];
  if (meta.year) parts.push(`Repère année: ${meta.year}.`);
  if (meta.lang) parts.push(`Langue principale attendue: ${meta.lang}.`);
  if (meta.note) parts.push(meta.note);
  if (meta.source) parts.push(meta.source);
  return parts.join(" ");
}

async function main() {
  const raw = await readFile(dataPath, "utf8");
  const json = JSON.parse(raw);
  const items = Array.isArray(json.items) ? json.items : [];
  let touched = 0;
  for (const item of items) {
    const key = (item.audioFileBase || "").trim().toLowerCase();
    const meta = verified[key];
    if (!meta) continue;
    item.hint = enrichHint(item, meta);
    item.trivia = enrichTrivia(item, meta);
    if ((item.hint || "").includes("langue dominante") && meta.lang) {
      item.answerKey = meta.lang.toUpperCase();
      item.altAnswerKey = meta.lang.toUpperCase() === "KIKONGO" ? "KITUBA" : item.altAnswerKey;
    }
    touched++;
  }

  await writeFile(dataPath, JSON.stringify({ items }, null, 2), "utf8");
  console.log(`Enrichissement web appliqué sur ${touched} manches.`);
}

await main();
