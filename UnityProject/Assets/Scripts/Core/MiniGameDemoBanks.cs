using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CongoGames.Core
{
    /// <summary>
    /// Contenu de démo non répétitif pour blind test, mot mystère, etc.
    /// </summary>
    public static class MiniGameDemoBanks
    {
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

            public BlindRound(string prompt, string[] choices, int correct, string sub, string audioFileBase = null, string audioUrl = null)
            {
                Prompt = prompt;
                Choices = choices;
                CorrectIndex = correct;
                SubLine = sub;
                AudioFileBase = audioFileBase;
                AudioUrl = audioUrl;
            }
        }

        private static readonly BlindRound[] BlindRounds =
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
                "Quel chanteur est associé à la rumba et aux scènes congolaises (réf. culture populaire) ?",
                new[] { "Pépé Kallé (rumba)", "Elvis Presley", "Freddie Mercury", "Céline Dion" },
                0,
                "Indice : Zaïko / rumba.",
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
                "Brazzaville est connue pour son lien avec quel fleuve ?",
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
                null,
                null)
        };

        private static readonly Queue<int> BlindOrder = new Queue<int>();
        private static int lastBlind = -1;

        private static readonly string[] ScrambleWords =
        {
            "CONGO", "RUMBA", "BRAZZA", "NGOMA", "MBOTE", "SANZA", "KONGO", "DANSE", "LIKOUALA", "POINTE", "FORET", "FLEUVE",
            "POOL", "LION", "VENT", "CIRE", "OCEAN", "CHANT", "BRUME", "TAMTAM", "RYTHME", "CULTURE", "MUSIQUE", "FESTIVAL",
            "NDOMBOLO", "EQUATEUR", "CUVETTE", "LINGALA", "BRAZZAVILLE", "MAKOUA", "OUESSO", "IMPOKO", "CONGOLAIS", "LIBREVILLE",
            "POINTENOIRE", "INDEPENDANCE", "LOANGO", "BASCONGO", "LESTUAIRE", "SANGHA", "LEKOLO", "MAYOMBE", "NIARI", "LEFINI"
        };
        private static readonly Queue<int> ScrambleOrder = new Queue<int>();
        private static int lastScramble = -1;

        private static readonly string[] MysteryWords = { "CONGO", "RUMBA", "BRAZZA", "NGOMA", "MBOTE", "SANZA", "LIKOUALA", "NDOMBOLO" };
        private static readonly Queue<int> MysteryOrder = new Queue<int>();
        private static int lastMystery = -1;

        public readonly struct ImageGuessRound
        {
            public readonly string Hint;
            public readonly string AnswerKey;
            public readonly int StyleSeed;
            public readonly string StreamingFileBase;

            public ImageGuessRound(string hint, string answerKey, int styleSeed, string streamingFileBase = null)
            {
                Hint = hint;
                AnswerKey = answerKey.Trim().ToUpperInvariant();
                StyleSeed = styleSeed;
                StreamingFileBase = streamingFileBase;
            }
        }

        private static readonly ImageGuessRound[] ImageGuessRounds =
        {
            new ImageGuessRound("Capitale sur le fleuve — grande ville au bord de l’eau ?", "BRAZZAVILLE", 1101, "brazzaville"),
            new ImageGuessRound("Ville côtière, pétrole et port sur l’océan ?", "POINTE NOIRE", 1202, "pointe_noire"),
            new ImageGuessRound("Grand fleuve qui traverse le pays ?", "CONGO", 1303, "fleuve_congo"),
            new ImageGuessRound("Grand mammifère des forêts du nord ?", "GORILLE", 1404, "gorille"),
            new ImageGuessRound("Couleur au centre du drapeau (entre vert et rouge) ?", "JAUNE", 1505, "drapeau"),
            new ImageGuessRound("Parc du nord connu pour la forêt et les gorilles (un mot) ?", "NOUABALE", 1606, "parc"),
            new ImageGuessRound("Océan à l’ouest du pays ?", "ATLANTIQUE", 1707, null),
            new ImageGuessRound("Région sèche au sud du pays (nom court) ?", "POOL", 1808, null)
        };

        private static readonly Queue<int> ImageGuessOrder = new Queue<int>();
        private static int lastImageGuess = -1;

        public static ImageGuessRound NextImageGuessRound()
        {
            RefillImageGuess();
            int ix = ImageGuessOrder.Dequeue();
            lastImageGuess = ix;
            return ImageGuessRounds[ix];
        }

        public static bool ImageGuessMatches(ImageGuessRound r, string userInput)
        {
            string u = NormalizeGuess(userInput);
            if (u.Length < 2) return false;
            string key = NormalizeGuess(r.AnswerKey);
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
            List<int> idx = new List<int>(ImageGuessRounds.Length);
            for (int i = 0; i < ImageGuessRounds.Length; i++) idx.Add(i);
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
            return BlindRounds[ix];
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

            return new BlindRound(r.Prompt, o, newCorrect, r.SubLine, r.AudioFileBase, r.AudioUrl);
        }

        private static void RefillBlind()
        {
            if (BlindOrder.Count > 0) return;
            List<int> idx = new List<int>(BlindRounds.Length);
            for (int i = 0; i < BlindRounds.Length; i++) idx.Add(i);
            for (int i = idx.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (idx[i], idx[j]) = (idx[j], idx[i]);
            }

            if (idx.Count > 1 && lastBlind >= 0 && idx[0] == lastBlind)
            {
                (idx[0], idx[1]) = (idx[1], idx[0]);
            }

            BlindOrder.Clear();
            foreach (int v in idx) BlindOrder.Enqueue(v);
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
