using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CongoGames.Core
{
    /// <summary>
    /// Contenu de démo non répétitif pour blind test, mot mystère, etc.
    /// </summary>
    public static class MiniGameDemoBanks
    {
        private static string s_blindJsonWeb;
        private static string s_imageJsonWeb;

        /// <summary>WebGL : JSON récupérés par HTTP (préchauffage), avant ouverture des fichiers disque.</summary>
        public static void IngestWebGlDatasetsJson(string blindJson, string imageJson)
        {
            s_blindJsonWeb = string.IsNullOrWhiteSpace(blindJson) ? null : blindJson;
            s_imageJsonWeb = string.IsNullOrWhiteSpace(imageJson) ? null : imageJson;
            blindRoundsRuntime = null;
            imageGuessRoundsRuntime = null;
        }
        public readonly struct BlindRound
        {
            public readonly string Prompt;
            public readonly string[] Choices;
            public readonly int CorrectIndex;
            public readonly string SubLine;
            /// <summary>Nom de fichier sans extension : cherché dans StreamingAssets/Theme/BlindTest/ puis Theme/ (.ogg, .mp3, .wav).</summary>
            public readonly string AudioFileBase;
            /// <summary>Si non vide (http/https), extrait joué à la place du fichier local.</summary>
            public readonly string AudioUrl;
            /// <summary>Libellé court pour l’UI (ex. « Artiste / groupe », « Titre / thème »).</summary>
            public readonly string CategoryLabel;

            public BlindRound(
                string prompt,
                string[] choices,
                int correct,
                string sub,
                string audioFileBase = null,
                string audioUrl = null,
                string categoryLabel = "Blind test")
            {
                Prompt = prompt;
                Choices = choices;
                CorrectIndex = correct;
                SubLine = sub;
                AudioFileBase = audioFileBase;
                AudioUrl = audioUrl;
                CategoryLabel = string.IsNullOrEmpty(categoryLabel) ? "Blind test" : categoryLabel;
            }
        }

        private static readonly BlindRound[] BlindRoundsDefault =
        {
            new BlindRound(
                "D’après l’extrait : quel style est le plus proche ?",
                new[] { "Rumba congolaise", "Reggae jamaïcain", "Metal symphonique", "Country US" },
                0,
                "Indice : musique urbaine et danses du Congo.",
                "track01",
                null),
            new BlindRound(
                "Cet air évoque surtout quelle ville du Congo ?",
                new[] { "Pointe-Noire (côte)", "Lisbonne", "Tokyo", "Mexico" },
                0,
                "Indice : port et culture musicale côtière.",
                "track02",
                null),
            new BlindRound(
                "On entend souvent ce type de percussion en fête : comment l’appelle-t-on familièrement ?",
                new[] { "Tam-tam / ngoma", "Violon", "Flûte à bec", "Harpe" },
                0,
                "Indice : peaux et rythme.",
                "track01",
                null),
            new BlindRound(
                "Dans le pays « Congo », le ndombolo vient le plus souvent d’un mix …",
                new[] { "Guitare, seben, rythme club / ndombolo", "Hipp-hop US pur", "Opéra wagnérien", "Jazz new-orléans" },
                0,
                "Indice : danse de hanches + section rythmique. Ne pas confondre le Congo et la RDC (ex-Zaïre).",
                "track02",
                null),
            new BlindRound(
                "Le ndombolo est surtout lié à …",
                new[] { "Danse et musique pop récente au Congo", "Opéra classique", "Jazz manouche", "Musique celtique" },
                0,
                "Indice : mouvements rapides des hanches.",
                "track01",
                null),
            new BlindRound(
                "Quel instrument à lames (sanza / likembe) est typique de musiques d’Afrique centrale ?",
                new[] { "Sanza / mbira", "Tuba", "Trombone", "Cymbales orchestre" },
                0,
                "Indice : petit instrument tenu en main.",
                "track02",
                null),
            new BlindRound(
                "La capitale du Congo est connue pour son lien avec quel fleuve ?",
                new[] { "Le fleuve Congo", "Le Nil", "Le Danube", "Le Mississippi" },
                0,
                "Indice : capitale sur la rive nord.",
                "track01",
                null),
            new BlindRound(
                "Dans un blind test « tradition », on cherche souvent à reconnaître …",
                new[] { "Le titre ou l’artiste", "La marque du micro", "Le prix du billet", "La température" },
                0,
                "Indice : culture musicale.",
                "track02",
                null),
            new BlindRound(
                "Type d’épreuve : on écoute un extrait court puis on devine plutôt …",
                new[] { "Qui chante ou le nom de la chanson", "La température du studio", "Le prix de l’instrument", "La taille du public" },
                0,
                "Indice : émission radio / soirée.",
                "track01",
                null),
            new BlindRound(
                "En rumba congolaise, les textes chantés racontent souvent …",
                new[] { "La vie, l’amour, la société", "Des recettes de cuisine française", "Le code routier", "La météo en Antarctique" },
                0,
                "Indice : chanson narrative.",
                "track02",
                null),
            new BlindRound(
                "Un « générique » de fin d’émission TV ressemble plutôt à …",
                new[] { "Une musique courte et mémorable", "Un silence de 10 minutes", "Un cours de maths", "Un bulletin d’info sans son" },
                0,
                "Indice : habillage sonore.",
                "track01",
                null),
            new BlindRound(
                "Pour animer une soirée dansante au Congo, on entend souvent …",
                new[] { "Rumba, ndombolo, afrobeat local", "Un opéra wagnerien seul", "Du heavy metal viking", "De la musique de film muette" },
                0,
                "Indice : piste DJ.",
                "track02",
                null),
            new BlindRound(
                "Le likembe / sanza se joue surtout en …",
                new[] { "Frappant ou pinçant les lamelles", "Soufflant dans un tuyau de plomb", "Grattant une corde de violon", "Tapant sur une enclume" },
                0,
                "Indice : instrument à lames.",
                "track01",
                null,
                "Style / interprétation"),
            // — Rounds musique : piste track01 / track02 + réponses plausibles (titre, artiste, style)
            new BlindRound(
                "D’après l’extrait, quel intitulé ou thème de titre collerait le mieux ici (démo) ?",
                new[] { "Congo mawa / mélodie urbaine (style blind)", "Berceuse nordique 1850", "Hymne à la pizza", "Générique météo Alaska" },
                0,
                "Écoute l’intro : pense « titre thématique » plus que technique.",
                "track01", null, "Titre / thème (démo)"),
            new BlindRound(
                "Dans l’imaginaire d’une fête au Congo, une « tête d’affiche » c’est plutôt … (démo) ?",
                new[] { "Chanteur star devant, chœurs, guitares et mambos en fond", "Un quatuor à cordes classique seul", "DJ techno Berlin sans chœurs", "Fanfare militaire sèche" },
                0,
                "Indice : cliché fête, pas le nom d’un morceau précis.",
                "track02", null, "Artiste / icône"),
            new BlindRound(
                "Quel groupe a marqué longtemps la scène des danses congolaises avec orchestre et danseurs (démo) ?",
                new[] { "Extra Musica (réf. culture) / soukous, danses d’avant", "The Beatles (Liverpool)", "BTS (Séoul)", "Metallica (Bay Area)" },
                0,
                "Indice : orchestre et danses, pas rock métal.",
                "track01", null, "Groupe / orchestre"),
            new BlindRound(
                "Quel type de thème de morceau est le plus proche d’un air « ndombolo / club » au Congo (démo) ?",
                new[] { "Basse lourde + refrain chanté, club / ndombolo", "Messe grégorienne a cappella", "Symphonie classique 4 mouvements", "Jingle pub dentifrice" },
                0,
                "Pense rythme et ambiance live TikTok, pas l’orchestration symphonique.",
                "track02", null, "Style / genre"),
            new BlindRound(
                "Si l’extrait rappelait un « classique congolais » côté mélodies douces, on pencherait plutôt pour…",
                new[] { "Rumba mélodique (ballade) type souvenirs congolais", "Techno hardcore 200 BPM", "Polka bavaroise seule", "Cours de boucherie (ASMR)" },
                0,
                "Indice : mélopée et guitare, plutôt que boucherie.",
                "track01", null, "Ambiance / timbre"),
            new BlindRound(
                "Quel type de propos les textes de rumba mettent le plus en avant (thème) ?",
                new[] { "Histoire, amour, société, fierté locale (thème) ", "Recettes d’hôtel 5 étoiles", "Code fiscal français", "Tableau périodique (chimie)" },
                0,
                "Indice : on écoute le sens, pas l’heure d’enregistrement exacte (démo).",
                "track02", null, "Thème / lyrisme (texte)"),
            new BlindRound(
                "Dans l’esprit « fête congolaise », l’orchestrateur est souvent devant, avec …",
                new[] { "Guitaristes, atalaku, seku / animation", "Hockey sur glace seul", "Cours d’alpinisme", "Conférence sur les taux" },
                0,
                "Indice : place du concert public (démo).",
                "track01", null, "Cliché fête (place orchestre)"),
            new BlindRound(
                "Pour cadrer l’exercice, un blind test c’est surtout : on écoute, puis on répond sur … (plusieurs piste)",
                new[] { "Rythme, mélodie, type de voix, parfois époque / style (indicatif) ", "La marque du câble USB seul", "N° de série de la chambre d’enregistrement", "L’heure d’ouverture du stade" },
                0,
                "Avec tes propres pistes licence OK dans Theme/blind-test/, les réponses colleraient au morceau.",
                "track02", null, "Jeu pédago / cadrage")
        };

        private static readonly Queue<int> BlindOrder = new Queue<int>();
        private static int lastBlind = -1;
        /// <summary>Indices contigus générés depuis blind_playlist_meta (paires titre/artiste par piste).</summary>
        private static int s_blindMetaBlockStart = -1;
        private static int s_blindMetaBlockLength;
        private static BlindRound[] blindRoundsRuntime;

        private static readonly string[] ScrambleWords =
        {
            "CONGO", "RUMBA", "KINTELE", "NGOMA", "MBOTE", "SANZA", "KONGO", "DANSE", "LIKOUALA", "POINTE", "FORET", "FLEUVE",
            "POOL", "LION", "VENT", "CIRE", "OCEAN", "CHANT", "BRUME", "TAMTAM", "RYTHME", "CULTURE", "MUSIQUE", "FESTIVAL",
            "NDOMBOLO", "EQUATEUR", "CUVETTE", "LINGALA", "KITUBA", "MAKOUA", "OUESSO", "IMPOKO", "CONGOLAIS", "LIBREVILLE",
            "POINTENOIRE", "INDEPENDANCE", "LOANGO", "BASCONGO", "LESTUAIRE", "SANGHA", "LEKOLO", "MAYOMBE", "NIARI", "LEFINI"
        };
        private static readonly Queue<int> ScrambleOrder = new Queue<int>();
        private static int lastScramble = -1;

        private static readonly string[] MysteryWords = { "CONGO", "RUMBA", "KINTELE", "NGOMA", "MBOTE", "SANZA", "LIKOUALA", "NDOMBOLO" };
        private static readonly Queue<int> MysteryOrder = new Queue<int>();
        private static int lastMystery = -1;

        public readonly struct ImageGuessRound
        {
            public readonly string Hint;
            public readonly string AnswerKey;
            public readonly int StyleSeed;
            public readonly string StreamingFileBase;
            public readonly string AltAnswerKey;
            public readonly string Trivia;
            public readonly string AudioFileBase;
            public readonly string AudioUrl;

            public ImageGuessRound(
                string hint,
                string answerKey,
                int styleSeed,
                string streamingFileBase = null,
                string altAnswerKey = null,
                string trivia = null,
                string audioFileBase = null,
                string audioUrl = null)
            {
                Hint = hint;
                AnswerKey = answerKey.Trim().ToUpperInvariant();
                StyleSeed = styleSeed;
                StreamingFileBase = streamingFileBase;
                AltAnswerKey = string.IsNullOrWhiteSpace(altAnswerKey) ? null : altAnswerKey.Trim().ToUpperInvariant();
                Trivia = trivia;
                AudioFileBase = string.IsNullOrWhiteSpace(audioFileBase) ? null : audioFileBase.Trim();
                AudioUrl = string.IsNullOrWhiteSpace(audioUrl) ? null : audioUrl.Trim();
            }
        }

        private static readonly ImageGuessRound[] ImageGuessRoundsDefault =
        {
            new ImageGuessRound(
                "Photo d’une capitale sur le fleuve : de quelle ville s’agit-il ? (réponse courte, capitale du Congo)",
                "BRAZZAVILLE",
                1101,
                "capitale",
                "CAPIT",
                "Fête nationale : 15 août. Image : placez un fichier réel capitale.png (ou .jpg) dans Theme/ImageGuess/ (photo libre de droits, ex. vue urbaine reconnue)."),
            new ImageGuessRound(
                "Image d’une grande ville portuaire et pétrolière côté ouest : quel nom (ville côtière du Congo) ?",
                "POINTE NOIRE",
                1202,
                "pointe_noire",
                "POINTENOIRE",
                "Ne pas confondre le Congo (pays) et la RDC (ex-Zaïre) : ce sont deux pays voisins du fleuve."),
            new ImageGuessRound(
                "Fleuve majeur visible sur la photo (baigne aussi la capitale) : quel est son nom ?",
                "CONGO", 1303, "fleuve_congo", "FLEUVE",
                "Image : fleuve_congo.png — documentaire / touristique, fleuve au Congo."),
            new ImageGuessRound(
                "Faune : primate ou mammifère de forêt tropicale du nord (photo) — lequel (un mot) ?",
                "GORILLE", 1404, "gorille", "ELEPHANT",
                "Image : gorille.png (parc ou zone protégée)."),
            new ImageGuessRound(
                "Drapeau du pays sur la photo : quelle est la couleur de la bande **centrale** (vertical) ?",
                "JAUNE", 1505, "drapeau", "JAUNE",
                "Trois bandes vert, jaune, rouge drapeau du Congo."),
            new ImageGuessRound(
                "Aire naturelle du nord, gorilles et forêts : un mot du nom de ce parc (réputé) ?",
                "NOUABALE", 1606, "parc", "NOUABALENDOKI",
                "Image : parc.png (Nouabalé-Ndoki ou scène forêt équatoriale congolaise, licence autorisée)."),
            new ImageGuessRound(
                "Océan visible à l’ouest du pays sur la carte côtière : lequel (un mot) ?",
                "ATLANTIQUE",
                1707,
                "ocean",
                null,
                "Image optionnelle ocean.png (côte, barrière de corail, ou cartographie)."),
            new ImageGuessRound(
                "Carte / relief : département au sud autour de la région de la capitale (un mot) ?",
                "POOL", 1808, "departement_pool", "DEPARTEMENT",
                "Image carte ou symbole administratif, ou pool.png (carte)."),
            new ImageGuessRound(
                "Département du nord, marécages, réputé pour l’eau (un mot) ?",
                "LIKOUALA", 1909, "likouala", "LIWILI", null),
            new ImageGuessRound(
                "Département nord, bassin de la Sanga, forêts (un mot) ?",
                "SANGHA", 2010, "sangha", "OUANZA", null),
            new ImageGuessRound(
                "Scène de fête : danse de hanches très présente (nom court) ?",
                "NDOMBOLO", 2212, "ndombolo", "NDOMBO",
                "Image fête / danse, ou ndombolo.png (ambiance, droits d’auteur respectés)."),
            new ImageGuessRound(
                "Infrastructures : grand stade africain, jeux, près de la capitale (toponyme en un mot) ?",
                "KINTELE", 2313, "kintele", "STADE",
                "Image : kintele.png (stade, photo touristique / presse autorisée)."),
            new ImageGuessRound(
                "Transport : code IATA (3 lettres) de l’aéroport international Maya-Maya (Congo) ?",
                "BZV", 2414, "aeroport", "BRA",
                "Image aéroport : aeroport.png ou bzv.png (Wikimedia / licence compatible).")
        };

        private static readonly Queue<int> ImageGuessOrder = new Queue<int>();
        private static int lastImageGuess = -1;
        private static ImageGuessRound[] imageGuessRoundsRuntime;

        [Serializable] private class BlindExtrasFile
        {
            public BlindJsonItem[] items;
        }

        [Serializable] private class BlindJsonItem
        {
            public string prompt;
            public string a;
            public string b;
            public string c;
            public string d;
            public int correctIndex;
            public string subLine;
            public string audioFileBase;
            public string audioUrl;
            public string categoryLabel;
        }

        [Serializable] private class ImageExtrasFile
        {
            public ImageJsonItem[] items;
        }

        [Serializable] private class ImageJsonItem
        {
            public string hint;
            public string answerKey;
            public int styleSeed;
            public string streamingFileBase;
            public string altAnswerKey;
            public string trivia;
            public string audioFileBase;
            public string audioUrl;
        }

        private static BlindRound[] GetBlindRoundsMerged()
        {
            if (blindRoundsRuntime == null) BuildBlindRounds();
            return blindRoundsRuntime;
        }

        private static void BuildBlindRounds()
        {
            s_blindMetaBlockStart = -1;
            s_blindMetaBlockLength = 0;
            var list = new List<BlindRound>(BlindRoundsDefault.Length + 4);
            var playlistOnly = new List<BlindRound>(64);
            foreach (BlindRound r in BlindRoundsDefault)
            {
                list.Add(r);
            }

            if (!string.IsNullOrWhiteSpace(s_blindJsonWeb))
            {
                try
                {
                    AppendBlindExtrasFromJsonString(list, s_blindJsonWeb);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("minigame_blind_extras (Web) : " + e.Message);
                }
            }

            string path = Path.Combine(Application.streamingAssetsPath, "Datasets", "minigame_blind_extras.json");
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                    AppendBlindExtrasFromJsonString(list, json);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("minigame_blind_extras.json : " + e.Message);
                }
            }

            int beforePlaylistMeta = list.Count;
            int playlistAdded = AppendBlindRoundsFromPlaylistMeta(list);
            if (list.Count > beforePlaylistMeta)
            {
                s_blindMetaBlockStart = beforePlaylistMeta;
                s_blindMetaBlockLength = list.Count - beforePlaylistMeta;
            }

            // Construction dédiée "playlist-only" pour verrouiller la cohérence
            // question <-> propositions <-> morceau joué.
            int playlistOnlyAdded = AppendBlindRoundsFromPlaylistMeta(playlistOnly);

            // Si la playlist est disponible, on force un blind test 100% corrélé
            // aux noms de fichiers de la playlist (artiste/titre) et à leur audio.
            if (playlistAdded > 0 && playlistOnlyAdded > 0)
            {
                list = playlistOnly;
                s_blindMetaBlockStart = 0;
                s_blindMetaBlockLength = list.Count;
            }

            blindRoundsRuntime = list.ToArray();
        }

        [Serializable] private class BlindPlaylistMetaFile
        {
            public BlindMetaItem[] items;
        }

        [Serializable] private class BlindMetaItem
        {
            public string fileBase;
            public string artist;
            public string title;
        }

        [Serializable] private class BlindFactsFile
        {
            public BlindFactItem[] items;
        }

        [Serializable] private class BlindFactItem
        {
            public string fileBase;
            public int releaseYear;
            public string language;
            public string inspiration;
            public bool verified;
        }

        private static readonly string[] ArtistTrapChoices =
        {
            "Daft Punk",
            "Taylor Swift",
            "Eminem",
            "Ariana Grande",
            "Bad Bunny",
            "The Weeknd"
        };

        private static readonly string[] TitleTrapChoices =
        {
            "Smells Like Teen Spirit",
            "Shape of You",
            "Bohemian Rhapsody",
            "Billie Jean",
            "Gangnam Style",
            "Despacito"
        };

        private static readonly string[] ArtistTrapClassique =
        {
            "Franco Luambo",
            "Tabu Ley Rochereau",
            "Miriam Makeba",
            "Salif Keita"
        };

        private static readonly string[] ArtistTrapModerne =
        {
            "Fally Ipupa",
            "Davido",
            "Burna Boy",
            "Aya Nakamura"
        };

        private static readonly string[] TitleTrapClassique =
        {
            "Independance Cha Cha",
            "Mario",
            "Sodade",
            "Yeke Yeke"
        };

        private static readonly string[] TitleTrapModerne =
        {
            "Djadja",
            "Calm Down",
            "Mon Soleil",
            "Jerusalema"
        };
        private static readonly Regex CollaborationSplitRegex = new Regex(@"\s*(?:feat\.?|ft\.?|versus|vs|,|&| x )\s*", RegexOptions.IgnoreCase);

        private static int AppendBlindRoundsFromPlaylistMeta(List<BlindRound> list)
        {
            if (list == null) return 0;
            int added = 0;
            TextAsset ta = Resources.Load<TextAsset>("Datasets/blind_playlist_meta");
            if (ta == null) return 0;
            try
            {
                BlindPlaylistMetaFile f = JsonUtility.FromJson<BlindPlaylistMetaFile>(ta.text);
                if (f?.items == null || f.items.Length == 0) return 0;
                var normalized = f.items
                    .Where(x => x != null && !string.IsNullOrEmpty(x.fileBase))
                    .Select(NormalizeBlindMetaItem)
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .ToList();
                if (normalized.Count == 0) return 0;
                var titles = normalized
                    .Select(x => x.title)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var artists = normalized
                    .Select(x => x.artist)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var collaboratorPool = normalized
                    .SelectMany(x => ExtractCollaborationArtists(NormalizeArtistDisplay(x.artist)))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (titles.Count < 1 || artists.Count < 1) return 0;
                Dictionary<string, BlindFactItem> factsByTrack = LoadBlindFactsByTrack();
                foreach (var it in normalized)
                {
                    string t = NormalizeTitleDisplay(it.title);
                    string a = NormalizeArtistDisplay(it.artist);
                    if (t.Length < 1 || a.Length < 1) continue;
                    factsByTrack.TryGetValue(it.fileBase, out BlindFactItem fact);
                    bool classic = IsClassicArtist(a);
                    string[] tChoices = BuildFourUniques(t, titles, MergeTrapChoices(
                        TitleTrapChoices,
                        classic ? TitleTrapClassique : TitleTrapModerne));
                    int ti = System.Array.IndexOf(tChoices, t);
                    if (ti < 0) ti = 0;
                    list.Add(new BlindRound(
                        "Après l’extrait (~1 min), quel est le titre de ce morceau ?",
                        tChoices,
                        ti,
                        "Indice : concentre-toi sur la voix, le refrain et l'ambiance.",
                        it.fileBase,
                        null,
                        classic ? "Titre (playlist • classique)" : "Titre (playlist • moderne)"));
                    added++;
                    string[] aChoices = BuildFourUniques(a, artists, MergeTrapChoices(
                        ArtistTrapChoices,
                        classic ? ArtistTrapClassique : ArtistTrapModerne));
                    int ai = System.Array.IndexOf(aChoices, a);
                    if (ai < 0) ai = 0;
                    list.Add(new BlindRound(
                        "Après l’extrait, qui interprète ce morceau (artiste ou groupe) ?",
                        aChoices,
                        ai,
                        "Indice : écoute le timbre de voix, le style et l'identité du groupe.",
                        it.fileBase,
                        null,
                        classic ? "Artiste (playlist • classique)" : "Artiste (playlist • moderne)"));
                    added++;

                    List<string> collabs = ExtractCollaborationArtists(a);
                    if (collabs.Count >= 2)
                    {
                        string collabTarget = collabs[Random.Range(1, collabs.Count)];
                        string[] cChoices = BuildFourUniques(
                            collabTarget,
                            collaboratorPool.Where(x => !string.Equals(x, collabTarget, StringComparison.Ordinal)).ToList(),
                            MergeTrapChoices(ArtistTrapChoices, classic ? ArtistTrapClassique : ArtistTrapModerne));
                        int ci = System.Array.IndexOf(cChoices, collabTarget);
                        if (ci < 0) ci = 0;
                        list.Add(new BlindRound(
                            "Sur cette collaboration, quel nom apparaît parmi les artistes invités ?",
                            cChoices,
                            ci,
                            BuildCollaborationHint(it.fileBase, t, a, fact),
                            it.fileBase,
                            null,
                            "Collaboration (playlist)"));
                        added++;
                    }

                    if (fact != null && fact.verified)
                    {
                        if (fact.releaseYear >= 1900 && fact.releaseYear <= 2100)
                        {
                            string[] yearChoices = BuildYearChoices(fact.releaseYear, factsByTrack.Values);
                            int yi = System.Array.IndexOf(yearChoices, fact.releaseYear.ToString());
                            if (yi < 0) yi = 0;
                            list.Add(new BlindRound(
                                "Selon les sources vérifiées, en quelle année ce morceau est-il sorti ?",
                                yearChoices,
                                yi,
                                "Indice : concentre-toi sur l'époque musicale du morceau.",
                                it.fileBase,
                                null,
                                "Année (vérifiée)"));
                            added++;
                        }

                        string lang = (fact.language ?? "").Trim().ToUpperInvariant();
                        if (lang.Length >= 3)
                        {
                            string[] langChoices = BuildLanguageChoices(lang);
                            int li = System.Array.IndexOf(langChoices, lang);
                            if (li < 0) li = 0;
                            list.Add(new BlindRound(
                                "D'après les sources vérifiées, quelle est la langue principale de ce titre ?",
                                langChoices,
                                li,
                                "Indice : écoute la diction dominante du morceau.",
                                it.fileBase,
                                null,
                                "Langue (vérifiée)"));
                            added++;
                        }

                        string insp = (fact.inspiration ?? "").Trim();
                        if (insp.Length >= 8)
                        {
                            string[] inspChoices = BuildInspirationChoices(insp, factsByTrack.Values);
                            int ii = System.Array.IndexOf(inspChoices, insp);
                            if (ii < 0) ii = 0;
                            list.Add(new BlindRound(
                                "Selon les sources vérifiées, ce morceau rend surtout hommage ou s'inspire de quoi ?",
                                inspChoices,
                                ii,
                                "Indice : écoute le contexte annoncé autour du titre.",
                                it.fileBase,
                                null,
                                "Inspiration (vérifiée)"));
                            added++;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("blind_playlist_meta : " + e.Message);
            }
            return added;
        }

        private static Dictionary<string, BlindFactItem> LoadBlindFactsByTrack()
        {
            var map = new Dictionary<string, BlindFactItem>(StringComparer.OrdinalIgnoreCase);
            LoadBlindFactsIntoMap(map, "Datasets/blind_playlist_facts", "blind_playlist_facts");
            // Overrides manuels prioritaires (corrections validées avec l'utilisateur).
            LoadBlindFactsIntoMap(map, "Datasets/blind_playlist_facts_overrides", "blind_playlist_facts_overrides");

            return map;
        }

        private static void LoadBlindFactsIntoMap(
            Dictionary<string, BlindFactItem> map,
            string resourcePath,
            string logPrefix)
        {
            if (map == null) return;
            TextAsset ta = Resources.Load<TextAsset>(resourcePath);
            if (ta == null || string.IsNullOrWhiteSpace(ta.text))
            {
                return;
            }

            try
            {
                BlindFactsFile f = JsonUtility.FromJson<BlindFactsFile>(ta.text);
                if (f?.items == null) return;
                foreach (BlindFactItem it in f.items)
                {
                    if (it == null || string.IsNullOrWhiteSpace(it.fileBase)) continue;
                    map[it.fileBase.Trim()] = it;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(logPrefix + " : " + e.Message);
            }
        }

        private static string[] BuildYearChoices(int year, IEnumerable<BlindFactItem> facts)
        {
            var pool = new List<string>();
            if (facts != null)
            {
                pool.AddRange(facts
                    .Where(x => x != null && x.releaseYear >= 1900 && x.releaseYear <= 2100 && x.releaseYear != year)
                    .Select(x => x.releaseYear.ToString()));
            }

            pool.Add((year - 1).ToString());
            pool.Add((year + 1).ToString());
            pool.Add((year - 2).ToString());
            pool.Add((year + 2).ToString());
            pool.Add((year - 5).ToString());
            pool.Add((year + 5).ToString());

            return BuildFourUniques(year.ToString(), pool.Distinct().ToList(), null);
        }

        private static string[] BuildLanguageChoices(string lang)
        {
            var pool = new List<string>
            {
                "LINGALA",
                "KITOUBA",
                "FRANÇAIS",
                "MBOCHI",
                "LARI",
                "ANGLAIS"
            };
            return BuildFourUniques(lang, pool, null);
        }

        private static string[] BuildInspirationChoices(string inspiration, IEnumerable<BlindFactItem> facts)
        {
            var pool = new List<string>();
            if (facts != null)
            {
                pool.AddRange(facts
                    .Where(x => x != null && !string.IsNullOrWhiteSpace(x.inspiration))
                    .Select(x => x.inspiration.Trim())
                    .Where(x => !string.Equals(x, inspiration, StringComparison.Ordinal)));
            }

            pool.Add("Une pure improvisation sans référence précise.");
            pool.Add("Une commande publicitaire sans lien à un artiste.");
            pool.Add("Un jingle de studio sans histoire particulière.");
            pool.Add("Un exercice vocal sans dédicace ni hommage.");

            return BuildFourUniques(inspiration, pool.Distinct().ToList(), null);
        }

        private static List<string> ExtractCollaborationArtists(string artistRaw)
        {
            string s = NormalizeArtistDisplay(artistRaw);
            if (string.IsNullOrWhiteSpace(s))
            {
                return new List<string>();
            }

            string[] parts = CollaborationSplitRegex.Split(s);
            var names = parts
                .Select(x => NormalizeContributorToken(x))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();
            return names;
        }

        private static string NormalizeContributorToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return "";
            string t = token.Trim();
            t = t.Replace("Le groupe_", "", StringComparison.OrdinalIgnoreCase).Trim();
            t = t.Replace("le groupe_", "", StringComparison.OrdinalIgnoreCase).Trim();
            while (t.Contains("  ")) t = t.Replace("  ", " ");
            return t;
        }

        private static bool IsClassicArtist(string artist)
        {
            string a = (artist ?? "").ToLowerInvariant();
            return a.Contains("casimir zao")
                || a.Contains("franklin boukaka")
                || a.Contains("bantous")
                || a.Contains("pamelo")
                || a.Contains("pierrette adams")
                || a.Contains("pierre moutouari")
                || a.Contains("youlou mabiala");
        }

        private static IReadOnlyList<string> MergeTrapChoices(IReadOnlyList<string> a, IReadOnlyList<string> b)
        {
            var list = new List<string>();
            if (a != null) list.AddRange(a);
            if (b != null) list.AddRange(b);
            return list;
        }

        private static (string fileBase, string artist, string title)? NormalizeBlindMetaItem(BlindMetaItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.fileBase))
            {
                return null;
            }

            string fileBase = item.fileBase.Trim();
            string artist = string.IsNullOrWhiteSpace(item.artist) ? null : item.artist.Trim();
            string title = string.IsNullOrWhiteSpace(item.title) ? null : item.title.Trim();

            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title))
            {
                ParseArtistTitleFromFileBase(fileBase, out string parsedArtist, out string parsedTitle);
                if (string.IsNullOrEmpty(artist)) artist = parsedArtist;
                if (string.IsNullOrEmpty(title)) title = parsedTitle;
            }

            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title))
            {
                return null;
            }

            artist = NormalizeArtistDisplay(artist);
            title = NormalizeTitleDisplay(title);
            return (fileBase, artist, title);
        }

        private static void ParseArtistTitleFromFileBase(string fileBase, out string artist, out string title)
        {
            artist = null;
            title = null;
            if (string.IsNullOrWhiteSpace(fileBase))
            {
                return;
            }

            string cleaned = fileBase.Trim().Replace('_', ' ');
            int sep = cleaned.IndexOf(" - ", StringComparison.Ordinal);
            if (sep < 0) sep = cleaned.IndexOf('-', StringComparison.Ordinal);
            if (sep <= 0 || sep >= cleaned.Length - 1)
            {
                return;
            }

            artist = cleaned.Substring(0, sep).Trim();
            title = cleaned.Substring(sep + (cleaned[sep] == '-' ? 1 : 3)).Trim();
            if (string.IsNullOrEmpty(artist) || string.IsNullOrEmpty(title))
            {
                artist = null;
                title = null;
            }
        }

        private static string[] BuildFourUniques(string correct, List<string> pool, IReadOnlyList<string> traps)
        {
            if (string.IsNullOrEmpty(correct)) correct = "?";
            string correctKey = CanonicalChoiceKey(correct);
            var wrong = new List<string>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            used.Add(correctKey);

            if (pool != null)
            {
                foreach (string s in pool)
                {
                    if (string.IsNullOrWhiteSpace(s)) continue;
                    string val = s.Trim();
                    string key = CanonicalChoiceKey(val);
                    if (used.Contains(key)) continue;
                    used.Add(key);
                    wrong.Add(val);
                }
            }
            if (traps != null)
            {
                foreach (string trap in traps)
                {
                    if (string.IsNullOrWhiteSpace(trap)) continue;
                    string val = trap.Trim();
                    string key = CanonicalChoiceKey(val);
                    if (used.Contains(key)) continue;
                    used.Add(key);
                    wrong.Add(val);
                }
            }
            for (int i = wrong.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (wrong[i], wrong[j]) = (wrong[j], wrong[i]);
            }

            var pick = new List<string> { correct };
            for (int i = 0; i < 3 && i < wrong.Count; i++) pick.Add(wrong[i]);
            int pad = 0;
            while (pick.Count < 4) pick.Add("— (autre morceau) " + (++pad));
            return pick.Take(4).ToArray();
        }

        private static string CanonicalChoiceKey(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "";
            }

            string s = raw.Trim();
            s = s.ToLowerInvariant();
            s = RemoveDiacritics(s);
            s = s.Replace("_", " ");
            s = Regex.Replace(s, @"\s+", " ");
            return s.Trim();
        }

        private static string RemoveDiacritics(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "";
            }

            string norm = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            for (int i = 0; i < norm.Length; i++)
            {
                char c = norm[i];
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string BuildCollaborationHint(string fileBase, string title, string artist, BlindFactItem fact)
        {
            if (fact != null && !string.IsNullOrWhiteSpace(fact.inspiration))
            {
                return "Indice : " + fact.inspiration.Trim();
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                return "Indice : pense au morceau \"" + title.Trim() + "\" et a ses artistes invites.";
            }

            if (!string.IsNullOrWhiteSpace(artist))
            {
                return "Indice : reste sur les artistes associes a \"" + artist.Trim() + "\".";
            }

            if (!string.IsNullOrWhiteSpace(fileBase))
            {
                return "Indice : cherche le nom invite relie a ce morceau.";
            }

            return "Indice : concentre-toi sur l'invite associe au titre entendu.";
        }

        private static string NormalizeArtistDisplay(string raw)
        {
            string s = (raw ?? "").Trim();
            if (s.Length == 0) return "";
            s = s.Replace("Le groupe_", "", StringComparison.OrdinalIgnoreCase).Trim();
            s = s.Replace("le groupe_", "", StringComparison.OrdinalIgnoreCase).Trim();
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            // On conserve les collaborations (feat/x/&/vs) pour rester fidèle au nom de fichier.
            if (s.Length > 92) s = s.Substring(0, 91).TrimEnd() + "…";
            return s;
        }

        private static string NormalizeTitleDisplay(string raw)
        {
            string s = (raw ?? "").Trim();
            if (s.Length == 0) return "";
            int p = s.IndexOf('(');
            if (p > 0) s = s.Substring(0, p).Trim();
            int b = s.IndexOf('[');
            if (b > 0) s = s.Substring(0, b).Trim();
            if (s.Length > 44) s = s.Substring(0, 43).TrimEnd() + "…";
            return s;
        }

        private static void AppendBlindExtrasFromJsonString(List<BlindRound> list, string json)
        {
            if (string.IsNullOrWhiteSpace(json) || list == null)
            {
                return;
            }

            BlindExtrasFile f = JsonUtility.FromJson<BlindExtrasFile>(json);
            if (f?.items == null)
            {
                return;
            }

            foreach (BlindJsonItem row in f.items)
            {
                if (row == null || string.IsNullOrEmpty(row.prompt)) continue;
                string[] c = { row.a, row.b, row.c, row.d };
                if (c[0] == null) c[0] = "";
                if (c[1] == null) c[1] = "";
                if (c[2] == null) c[2] = "";
                if (c[3] == null) c[3] = "";
                int cor = Mathf.Clamp(row.correctIndex, 0, 3);
                list.Add(new BlindRound(
                    row.prompt,
                    c,
                    cor,
                    row.subLine ?? "",
                    row.audioFileBase,
                    row.audioUrl,
                    row.categoryLabel));
            }
        }

        private static ImageGuessRound[] GetImageGuessRoundsMerged()
        {
            if (imageGuessRoundsRuntime == null) BuildImageGuessRounds();
            return imageGuessRoundsRuntime;
        }

        private static void BuildImageGuessRounds()
        {
            var list = new List<ImageGuessRound>(ImageGuessRoundsDefault.Length + 4);
            foreach (ImageGuessRound r in ImageGuessRoundsDefault)
            {
                list.Add(r);
            }

            if (!string.IsNullOrWhiteSpace(s_imageJsonWeb))
            {
                try
                {
                    AppendImageExtrasFromJsonString(list, s_imageJsonWeb);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("minigame_image_guess_extras (Web) : " + e.Message);
                }
            }

            string pathI = Path.Combine(Application.streamingAssetsPath, "Datasets", "minigame_image_guess_extras.json");
            if (File.Exists(pathI))
            {
                try
                {
                    string json = File.ReadAllText(pathI);
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                    AppendImageExtrasFromJsonString(list, json);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning("minigame_image_guess_extras.json : " + e.Message);
                }
            }

            imageGuessRoundsRuntime = list.ToArray();
        }

        private static void AppendImageExtrasFromJsonString(List<ImageGuessRound> list, string json)
        {
            if (string.IsNullOrWhiteSpace(json) || list == null)
            {
                return;
            }

            ImageExtrasFile f = JsonUtility.FromJson<ImageExtrasFile>(json);
            if (f?.items == null)
            {
                return;
            }

            foreach (ImageJsonItem row in f.items)
            {
                if (row == null || string.IsNullOrEmpty(row.hint) || string.IsNullOrEmpty(row.answerKey)) continue;
                list.Add(new ImageGuessRound(
                    row.hint,
                    row.answerKey,
                    row.styleSeed,
                    row.streamingFileBase,
                    row.altAnswerKey,
                    row.trivia,
                    row.audioFileBase,
                    row.audioUrl));
            }
        }

        public static ImageGuessRound NextImageGuessRound()
        {
            RefillImageGuess();
            int ix = ImageGuessOrder.Dequeue();
            lastImageGuess = ix;
            return GetImageGuessRoundsMerged()[ix];
        }

        public static bool ImageGuessMatches(ImageGuessRound r, string userInput)
        {
            string u = NormalizeGuess(userInput);
            if (u.Length < 2) return false;
            if (KeyMatches(r.AnswerKey, u)) return true;
            if (!string.IsNullOrEmpty(r.AltAnswerKey) && KeyMatches(r.AltAnswerKey, u)) return true;
            return false;
        }

        private static bool KeyMatches(string keyRaw, string u)
        {
            string key = NormalizeGuess(keyRaw);
            if (u == key) return true;
            if (u.Length >= 4 && (u.Contains(key) || key.Contains(u))) return true;
            return false;
        }

        private static string NormalizeGuess(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim().ToUpperInvariant();
            s = s.Replace(" ", "").Replace("-", "").Replace("'", "").Replace("É", "E").Replace("È", "E").Replace("Ê", "E");
            return s;
        }

        private static void RefillImageGuess()
        {
            if (ImageGuessOrder.Count > 0) return;
            int len = GetImageGuessRoundsMerged().Length;
            List<int> idx = new List<int>(len);
            for (int i = 0; i < len; i++) idx.Add(i);
            for (int i = idx.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            if (idx.Count > 1 && lastImageGuess >= 0 && idx[0] == lastImageGuess)
            {
                (idx[0], idx[1]) = (idx[1], idx[0]);
            }

            ImageGuessOrder.Clear();
            foreach (int v in idx) ImageGuessOrder.Enqueue(v);
        }

        public static BlindRound NextBlindRound()
        {
            RefillBlind();
            int ix = BlindOrder.Dequeue();
            lastBlind = ix;
            return GetBlindRoundsMerged()[ix];
        }

        /// <summary>Mélange l’ordre des choix pour l’affichage A–D (bonne réponse suit).</summary>
        public static BlindRound ToShuffledDisplay(BlindRound r)
        {
            int n = r.Choices != null ? r.Choices.Length : 0;
            if (n <= 1) return r;
            int[] order = new int[n];
            for (int i = 0; i < n; i++) order[i] = i;
            for (int i = n - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            string[] o = new string[n];
            int newCorrect = 0;
            for (int k = 0; k < n; k++)
            {
                o[k] = r.Choices[order[k]];
                if (order[k] == r.CorrectIndex) newCorrect = k;
            }

            return new BlindRound(
                r.Prompt,
                o,
                newCorrect,
                r.SubLine,
                r.AudioFileBase,
                r.AudioUrl,
                r.CategoryLabel);
        }

        private static void RefillBlind()
        {
            if (BlindOrder.Count > 0) return;
            int len = GetBlindRoundsMerged().Length;
            List<int> idx;
            if (s_blindMetaBlockStart >= 0 && s_blindMetaBlockLength >= 2)
            {
                int m0 = s_blindMetaBlockStart;
                int mlen = s_blindMetaBlockLength;
                if (m0 + mlen <= len && mlen % 2 == 0)
                {
                    int pairCount = mlen / 2;
                    idx = new List<int>(len);

                    var pairStarts = new List<int>(pairCount);
                    for (int p = 0; p < pairCount; p++)
                    {
                        pairStarts.Add(m0 + p * 2);
                    }

                    for (int i = pairStarts.Count - 1; i > 0; i--)
                    {
                        int j = Random.Range(0, i + 1);
                        (pairStarts[i], pairStarts[j]) = (pairStarts[j], pairStarts[i]);
                    }

                    // Passe 1 : une question par piste (titre OU artiste), ordre aléatoire.
                    var deferred = new List<int>(pairCount);
                    foreach (int start in pairStarts)
                    {
                        bool askTitleFirst = Random.value < 0.5f;
                        int first = askTitleFirst ? start : start + 1;
                        int second = askTitleFirst ? start + 1 : start;
                        idx.Add(first);
                        deferred.Add(second);
                    }

                    // Passe 2 : seconde question de chaque piste, encore mélangée.
                    for (int i = deferred.Count - 1; i > 0; i--)
                    {
                        int j = Random.Range(0, i + 1);
                        (deferred[i], deferred[j]) = (deferred[j], deferred[i]);
                    }

                    foreach (int d in deferred)
                    {
                        idx.Add(d);
                    }

                    for (int i = 0; i < len; i++)
                    {
                        if (i < m0 || i >= m0 + mlen)
                        {
                            idx.Add(i);
                        }
                    }

                    for (int a = mlen; a < idx.Count; a++)
                    {
                        int b = Random.Range(a, idx.Count);
                        (idx[a], idx[b]) = (idx[b], idx[a]);
                    }
                }
                else
                {
                    idx = BuildFullRandomOrder(len);
                }
            }
            else
            {
                idx = BuildFullRandomOrder(len);
            }

            if (idx.Count > 1 && lastBlind >= 0 && idx[0] == lastBlind)
            {
                (idx[0], idx[1]) = (idx[1], idx[0]);
            }

            BlindOrder.Clear();
            foreach (int v in idx) BlindOrder.Enqueue(v);
        }

        private static List<int> BuildFullRandomOrder(int len)
        {
            var idx = new List<int>(len);
            for (int i = 0; i < len; i++) idx.Add(i);
            for (int i = idx.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            return idx;
        }

        private static string RandomPseudoWord(int len)
        {
            len = Mathf.Clamp(len, 4, 12);
            const string vow = "AEIOUY";
            const string con = "BCDFGHJKLMNPQRSTVWXZ";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(len);
            for (int i = 0; i < len; i++)
            {
                string pool = (i % 2 == 0) ? con : vow;
                sb.Append(pool[Random.Range(0, pool.Length)]);
            }

            return sb.ToString();
        }

        public static string NextScrambleWord()
        {
            if (Random.value < 0.4f)
            {
                return RandomPseudoWord(Random.Range(4, 13));
            }

            RefillScramble();
            int ix = ScrambleOrder.Dequeue();
            lastScramble = ix;
            return ScrambleWords[ix];
        }

        private static void RefillScramble()
        {
            if (ScrambleOrder.Count > 0) return;
            List<int> idx = new List<int>(ScrambleWords.Length);
            for (int i = 0; i < ScrambleWords.Length; i++)
            {
                int L = ScrambleWords[i] != null ? ScrambleWords[i].Length : 0;
                if (L >= 4 && L <= 12)
                {
                    idx.Add(i);
                }
            }

            if (idx.Count == 0)
            {
                for (int i = 0; i < ScrambleWords.Length; i++) idx.Add(i);
            }
            for (int i = idx.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            if (idx.Count > 1 && lastScramble >= 0 && idx[0] == lastScramble)
            {
                (idx[0], idx[1]) = (idx[1], idx[0]);
            }

            ScrambleOrder.Clear();
            foreach (int v in idx) ScrambleOrder.Enqueue(v);
        }

        public static string NextMysteryWord()
        {
            RefillMystery();
            int ix = MysteryOrder.Dequeue();
            lastMystery = ix;
            return MysteryWords[ix];
        }

        private static void RefillMystery()
        {
            if (MysteryOrder.Count > 0) return;
            List<int> idx = new List<int>(MysteryWords.Length);
            for (int i = 0; i < MysteryWords.Length; i++) idx.Add(i);
            for (int i = idx.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            if (idx.Count > 1 && lastMystery >= 0 && idx[0] == lastMystery)
            {
                (idx[0], idx[1]) = (idx[1], idx[0]);
            }

            MysteryOrder.Clear();
            foreach (int v in idx) MysteryOrder.Enqueue(v);
        }

        public static string MysteryMaskFor(string word)
        {
            if (string.IsNullOrEmpty(word)) return "";
            word = word.Trim().ToUpperInvariant();
            int reveal = Mathf.Max(1, word.Length / 3);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < word.Length; i++)
            {
                if (i > 0) sb.Append(' ');
                bool show = (i + word.Length) % (reveal + 2) == 0 || i == word.Length - 1;
                sb.Append(show ? word[i] : '_');
            }

            return sb.ToString();
        }

        /// <summary>Affichage compact : lettres connues + « _ » pour le reste (une seule espace entre positions).</summary>
        public static string MysteryDisplayLine(string word)
        {
            if (string.IsNullOrEmpty(word)) return "";
            word = word.Trim().ToUpperInvariant();
            int n = word.Length;
            if (n == 0) return "";
            bool[] show = new bool[n];
            show[0] = true;
            show[n - 1] = true;
            if (n > 5)
            {
                show[n / 2] = true;
            }

            if (n > 7)
            {
                show[1 + (n / 3)] = true;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder(n * 2);
            for (int i = 0; i < n; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(show[i] ? word[i] : '_');
            }

            return sb.ToString();
        }
    }
}
