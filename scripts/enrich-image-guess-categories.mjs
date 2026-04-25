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

function normalizeTriviaCategory(item) {
  const hint = String(item.hint || "").toLowerCase();
  const hasAudio =
    String(item.audioFileBase || "").trim().length > 0 ||
    String(item.audioUrl || "").trim().length > 0;

  let category = "culture";
  if (hasAudio || /chanson|musique|artiste|interpr|titre du morceau|feat|collaboration/.test(hint)) {
    category = "music_related";
  } else if (/département|fleuve|océan|ville|capitale|pays voisin|frontière|géograph/.test(hint)) {
    category = "geo";
  } else if (/président|résistant|combattant|histoire|règne/.test(hint)) {
    category = "histoire";
  } else if (/personnalité|écrivain|scientifique|artiste|politique/.test(hint)) {
    category = "personnalite";
  } else if (/plat|cuisine/.test(hint)) {
    category = "gastronomie";
  } else if (/livre|roman|ouvrage/.test(hint)) {
    category = "litterature";
  } else if (/site touristique|parc|réserve/.test(hint)) {
    category = "tourisme";
  } else if (/habit|tenue|traditionnel/.test(hint)) {
    category = "tenues_traditionnelles";
  }

  const trivia = String(item.trivia || "").trim();
  const noOldTag = trivia.replace(/^\[category:[^\]]+\]\s*/i, "");
  item.trivia = `[category:${category}] ${noOldTag}`.trim();
  return category;
}

const curatedRounds = [
  {
    hint: "Image géographie : ce fleuve majeur est-il le fleuve Congo ?",
    answerKey: "CONGO",
    styleSeed: 9501,
    streamingFileBase: "fleuve_congo",
    altAnswerKey: "FLEUVE CONGO",
    trivia: "[category:geo] Le fleuve Congo traverse l'Afrique centrale et borde la capitale du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image géographie : quel département du Congo est montré ici ?",
    answerKey: "POOL",
    styleSeed: 9502,
    streamingFileBase: "departement_pool",
    altAnswerKey: "",
    trivia: "[category:geo] Le Pool est un département du sud du Congo, autour de la capitale.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image ville : reconnais-tu la capitale du Congo ?",
    answerKey: "BRAZZAVILLE",
    styleSeed: 9503,
    streamingFileBase: "capitale",
    altAnswerKey: "CAPITALE",
    trivia: "[category:geo] Brazzaville est la capitale politique du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image ville côtière : quel port du Congo vois-tu ?",
    answerKey: "POINTE NOIRE",
    styleSeed: 9504,
    streamingFileBase: "pointe_noire",
    altAnswerKey: "POINTENOIRE",
    trivia: "[category:geo] Pointe-Noire est la grande ville portuaire du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image nature : ce parc national est-il Odzala ?",
    answerKey: "ODZALA",
    styleSeed: 9510,
    streamingFileBase: "odzala",
    altAnswerKey: "ODZALA KOKOUA",
    trivia: "[category:tourisme] Odzala-Kokoua est un parc emblématique du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image faune : quel grand primate emblématique du Congo vois-tu ?",
    answerKey: "GORILLE",
    styleSeed: 9511,
    streamingFileBase: "gorille",
    altAnswerKey: "",
    trivia: "[category:tourisme] Le gorille est une espèce iconique des forêts d'Afrique centrale.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image côte : cet espace maritime borde-t-il le Congo ?",
    answerKey: "OCEAN ATLANTIQUE",
    styleSeed: 9512,
    streamingFileBase: "ocean",
    altAnswerKey: "ATLANTIQUE",
    trivia: "[category:geo] Le Congo possède une façade sur l'océan Atlantique.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image drapeau : de quel pays s'agit-il ?",
    answerKey: "CONGO",
    styleSeed: 9513,
    streamingFileBase: "drapeau",
    altAnswerKey: "REPUBLIQUE DU CONGO",
    trivia: "[category:histoire] Drapeau national du Congo, adopté avec ses couleurs vert-jaune-rouge.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image culturelle : ce style vestimentaire est associé à quel mouvement congolais ?",
    answerKey: "SAPE",
    styleSeed: 9514,
    streamingFileBase: "sapeurs",
    altAnswerKey: "SAPEURS",
    trivia: "[category:tenues_traditionnelles] La SAPE est un mouvement culturel de l'élégance vestimentaire.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image langue : parmi ces langues, laquelle est nationale au Congo ?",
    answerKey: "LINGALA",
    styleSeed: 9515,
    streamingFileBase: "langues",
    altAnswerKey: "KITUBA",
    trivia: "[category:culture] Le lingala et le kituba sont deux langues nationales du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image histoire : quel président du Congo est souvent associé à la longue période contemporaine ?",
    answerKey: "DENIS SASSOU NGUESSO",
    styleSeed: 9520,
    streamingFileBase: "capitale",
    altAnswerKey: "SASSOU NGUESSO",
    trivia: "[category:histoire] Question d'histoire politique du Congo contemporain.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image histoire : qui fut le premier président du Congo indépendant ?",
    answerKey: "FULBERT YOULOU",
    styleSeed: 9521,
    streamingFileBase: "drapeau",
    altAnswerKey: "YOULOU",
    trivia: "[category:histoire] Fulbert Youlou est une figure de l'indépendance politique du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image personnalité : cet auteur congolais est aussi connu comme professeur et essayiste. Qui est-ce ?",
    answerKey: "ALAIN MABANCKOU",
    styleSeed: 9523,
    streamingFileBase: "capitale",
    altAnswerKey: "MABANCKOU",
    trivia: "[category:personnalite] Personnalité culturelle congolaise reconnue internationalement.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image personnalité politique : quel nom est associé aux grandes institutions de l'époque post-indépendance ?",
    answerKey: "MARIEN NGOUABI",
    styleSeed: 9524,
    streamingFileBase: "drapeau",
    altAnswerKey: "NGOUABI",
    trivia: "[category:personnalite] Personnalité politique majeure de l'histoire du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image histoire : quel personnage est lié à l'hymne et à l'engagement culturel congolais ?",
    answerKey: "JACQUES OPANGAULT",
    styleSeed: 9522,
    streamingFileBase: "capitale",
    altAnswerKey: "",
    trivia: "[category:histoire] Manche de culture politique congolaise.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image littérature : ce livre est d'un auteur congolais. Donne un nom d'écrivain majeur.",
    answerKey: "ALAIN MABANCKOU",
    styleSeed: 9530,
    streamingFileBase: "kintele",
    altAnswerKey: "MABANCKOU",
    trivia: "[category:litterature] Alain Mabanckou est un écrivain congolais reconnu à l'international.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image littérature : quel écrivain congolais est connu pour une oeuvre historique et romanesque ?",
    answerKey: "EMMANUEL DONGALA",
    styleSeed: 9531,
    streamingFileBase: "capitale",
    altAnswerKey: "DONGALA",
    trivia: "[category:litterature] Emmanuel Dongala est un auteur important du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image gastronomie : ce plat traditionnel à base de manioc est appelé comment ?",
    answerKey: "FOUFOU",
    styleSeed: 9540,
    streamingFileBase: "parc",
    altAnswerKey: "FUFU",
    trivia: "[category:gastronomie] Le foufou est un aliment central en Afrique centrale.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image gastronomie : quelle préparation de feuilles est très connue au Congo ?",
    answerKey: "PONDU",
    styleSeed: 9541,
    streamingFileBase: "likouala",
    altAnswerKey: "SAKA SAKA",
    trivia: "[category:gastronomie] Le pondu est un classique de la cuisine congolaise.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image tourisme : ce lieu naturel du nord du Congo appartient à quelle zone ?",
    answerKey: "SANGHA",
    styleSeed: 9550,
    streamingFileBase: "sangha",
    altAnswerKey: "",
    trivia: "[category:tourisme] La Sangha est une zone forestière et écologique majeure.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image histoire : ce personnage est évoqué parmi les figures de résistance et d'engagement national. Quel nom proposes-tu ?",
    answerKey: "ANDRE GRENIER",
    styleSeed: 9551,
    streamingFileBase: "drapeau",
    altAnswerKey: "",
    trivia: "[category:resistance] Manche de culture générale sur les figures de résistance et d'engagement au Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image résistance : quel concept décrit le mieux ces figures historiques congolaises ?",
    answerKey: "RESISTANCE",
    styleSeed: 9552,
    streamingFileBase: "capitale",
    altAnswerKey: "COMBATTANTS",
    trivia: "[category:resistance] Les combattants et résistants appartiennent à la mémoire historique du Congo.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image voisins : parmi ces pays, lequel est voisin du Congo à l'ouest ?",
    answerKey: "GABON",
    styleSeed: 9560,
    streamingFileBase: "drapeau",
    altAnswerKey: "",
    trivia: "[category:geo] Le Congo partage une frontière avec le Gabon à l'ouest.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image voisins : quel pays est voisin du Congo au nord-est ?",
    answerKey: "CENTRAFRIQUE",
    styleSeed: 9561,
    streamingFileBase: "departement_pool",
    altAnswerKey: "REPUBLIQUE CENTRAFRICAINE",
    trivia: "[category:geo] Le Congo est frontalier de la République centrafricaine.",
    audioFileBase: "",
    audioUrl: ""
  },
  {
    hint: "Image culture : ce style musical urbain populaire au Congo, c'est quoi ?",
    answerKey: "NDOMBOLO",
    styleSeed: 9570,
    streamingFileBase: "ndombolo",
    altAnswerKey: "",
    trivia: "[category:music_related] Le ndombolo est un style majeur de la scène congolaise.",
    audioFileBase: "track09",
    audioUrl: ""
  },
  {
    hint: "Image artiste : reconnais-tu cet univers rumba/congolais ? Donne l'artiste principal.",
    answerKey: "ROGA ROGA",
    styleSeed: 9571,
    streamingFileBase: "sapeurs",
    altAnswerKey: "ROGA ROGA EXTRA MUSICA",
    trivia: "[category:music_related] Manche artiste/musique liée au blind.",
    audioFileBase: "track22",
    audioUrl: ""
  }
];

async function main() {
  const raw = await readFile(dataPath, "utf8");
  const json = JSON.parse(raw);
  const items = Array.isArray(json.items) ? json.items : [];

  let musicTagged = 0;
  for (const item of items) {
    const cat = normalizeTriviaCategory(item);
    if (cat === "music_related") musicTagged++;
    if (cat !== "music_related") {
      // Garantit absence de musique sur les manches non musicales.
      item.audioFileBase = "";
      item.audioUrl = "";
    }
  }

  const existingKeys = new Set(items.map((i) => `${i.hint}__${i.answerKey}`));
  let added = 0;
  for (const round of curatedRounds) {
    const key = `${round.hint}__${round.answerKey}`;
    if (existingKeys.has(key)) continue;
    items.push(round);
    existingKeys.add(key);
    added++;
  }

  await writeFile(dataPath, JSON.stringify({ items }, null, 2), "utf8");
  console.log(`Image-guess categories: ${items.length} manches total.`);
  console.log(`- music_related taggées: ${musicTagged}`);
  console.log(`- nouvelles manches ajoutées: ${added}`);
}

await main();
