using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CongoGames.Core
{
    /// <summary>
    /// Thèmes pour mots mélangés / mots croisés : listes de mots liés au Congo (pas RDC) — mélange et tirage 5–15 mots.
    /// </summary>
    public static class GridThemeBank
    {
        public readonly struct Theme
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string[] Words;

            public Theme(string id, string label, string[] words)
            {
                Id = id;
                Label = label;
                Words = words;
            }
        }

        private static readonly Theme[] Themes =
        {
            new Theme("geo", "Géo & lieux (Congo)", new[]
            {
                "KINTELE", "POINTE", "CUVETTE", "LIKOUALA", "SANGHA", "POOL", "LEFINI", "NIARI", "LOANGO", "OUESSO", "BETOU", "IMPFON", "BANIO", "MAYOMB", "ESTUA", "BASSA", "MFOUA", "LOUBO", "KINKA", "NOIRE"
            }),
            new Theme("nature", "Forêts, fleuve, faune", new[]
            {
                "FLEUVE", "CONGO", "GORIL", "SANGA", "FORET", "PARC", "EQUATE", "PLUIE", "OCEAN", "RIVIERE", "COCOTI", "MANGROV", "BASSIN", "FLORE", "FAUNE", "SAHBI", "HERBE", "BOISE", "PLAGE", "CRABE"
            }),
            new Theme("culture", "Culture, fête, drapeau", new[]
            {
                "BANTOU", "RUMBA", "NGOMA", "NDOMBO", "SEBEN", "DANSE", "CHANT", "CONGO", "MBOTE", "FETE", "AOUT", "CULTUR", "FOULE", "KERMES", "DANJAR", "FOYER", "GLOIRE", "HONOR", "SCENE", "HABIT"
            }),
            new Theme("lingala", "Lingala — vocabulaire (mots à retrouver)", new[]
            {
                "MELESI", "MBOTE", "MWANA", "MANGI", "TATA", "MAMA", "BISO", "KOKO", "MOTO", "MOKO", "PONA", "SUKA", "SIMBA", "BOKO", "BANDA", "KATI", "PEKO", "SUKI", "POTO", "MUNA", "MOKI", "BANA", "LISI", "NZOTO", "MOTI", "YAYA", "BUKA", "MELI", "MOSI", "LIBO"
            }),
            new Theme("kituba", "Kituba — vocabulaire (mots à retrouver)", new[]
            {
                "MALAMU", "SANTU", "MUNTU", "BANTU", "BOKO", "DIBU", "KUTA", "BUKA", "SUKA", "KUKU", "KISI", "MUKA", "TUKA", "DUKA", "ZONI", "TONI", "MUKO", "MBOA", "BUKI", "KONO", "TUKO", "BUKO", "KITA", "BUKU", "DUKO", "TATA", "MAMA", "MUNA", "SUKI", "MPASI"
            }),
            new Theme("sport", "Stade, foot, ferveur", new[]
            {
                "BUTEUR", "STADE", "EQUIPE", "BALLON", "VICTO", "COUPE", "FOULE", "CONGO", "BANC", "TALENT", "ESSAI", "JOUER", "MATCH", "GAGNE", "BUTS", "BLESS", "TIR", "BUT", "CARTO", "MEDAL"
            }),
            new Theme("histoire", "Indépendance, symboles", new[]
            {
                "AOUT", "ANNEE", "DROIT", "LIBRE", "PAIX", "UNION", "ORDRE", "TRAVAI", "PROGRE", "ECOLE", "VOTER", "LECONS", "TEMPO", "CONGO", "AVANT", "APRES", "EPOPEE", "PATRIO", "STATUE", "GLOIRE"
            }),
            new Theme("eco", "Commerce & vie quotidienne", new[]
            {
                "MARCHE", "VENTE", "POIDS", "PRIX", "KIOSK", "CAFE", "PAIN", "SABLE", "ROUTE", "GARES", "TAXES", "PRIME", "SALAIRE", "OFFRE", "ACHAT", "BUDGET", "CREDIT", "STOCKS", "BOUTIK", "CAISSE"
            }),
        };

        public static int MinWords => 5;
        public static int MaxWords => 12;

        /// <summary>Tirage d’un thème + N mots uniques, longueur 4–12, upper sans accents pour les grilles.</summary>
        public static void DrawSessionWords(out string themeLabel, out List<string> words, int? wordCount = null)
        {
            int n = wordCount ?? Random.Range(MinWords, MaxWords + 1);
            n = Mathf.Clamp(n, MinWords, MaxWords);
            const int maxLenFor7Grid = 7;
            Theme t = Themes[Random.Range(0, Themes.Length)];
            themeLabel = t.Label;
            var pool = new List<string>(t.Words.Length);
            foreach (string w in t.Words)
            {
                if (string.IsNullOrEmpty(w)) continue;
                string u = SanitizeForGrid(w);
                if (u.Length >= 4 && u.Length <= maxLenFor7Grid) pool.Add(u);
            }

            if (pool.Count < n)
            {
                for (int k = 0; k < 40 && pool.Count < n; k++)
                {
                    pool.Add("CONGO" + (char)('A' + (k % 26)));
                }
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var picked = new List<string>(n);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < pool.Count && picked.Count < n; i++)
            {
                if (seen.Add(pool[i])) picked.Add(pool[i]);
            }

            while (picked.Count < n)
            {
                string fallback = "PAYS" + picked.Count;
                if (seen.Add(fallback)) picked.Add(fallback);
            }

            for (int i = picked.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (picked[i], picked[j]) = (picked[j], picked[i]);
            }

            words = picked;
        }

        public static string SanitizeForGrid(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim().ToUpperInvariant();
            s = s.Replace("É", "E").Replace("È", "E").Replace("Ê", "E")
                .Replace("À", "A").Replace("Ô", "O")
                .Replace(" ", "")
                .Replace("'", "");
            if (s.Length > 12) s = s.Substring(0, 12);
            return s;
        }

        /// <summary>Glose FR optionnelle (thème langues / démo) — vide si inconnu.</summary>
        public static string TryGlossFr(string word)
        {
            string k = SanitizeForGrid(word ?? "");
            switch (k)
            {
                case "MELESI": return "merci";
                case "MBOTE": return "salut";
                case "MALAMU": return "bien";
                case "MOTEMA": return "cœur";
                case "MWANA": return "enfant";
                case "MAIE": return "eau";
                case "KOKO": return "grand-père";
                case "BISO": return "nous";
                case "MANGI": return "faim";
                case "LIBOSO": return "devant";
                case "SIMBA": return "lion";
                case "MOSALA": return "travail";
                case "NDAKISA": return "aider";
                case "NZOTO": return "corps";
                case "MABOKO": return "mains";
                case "PEKO": return "un peu";
                case "ZALA": return "être";
                case "SALUT": return "salut";
                case "NZOTO": return "corps";
                case "SANTU": return "saint / sainte (souvent)";
                case "MUNTU": return "personne / être humain (souvent)";
                case "BANTU": return "pluriel de muntu (souvent)";
                case "MPASI": return "souffrance / peine (souvent)";
                case "MOTI": return "mort (souvent)";
                case "BANDA": return "tribu / origine (souvent)";
                case "PONA": return "pour / afin de (souvent)";
                case "POTO": return "port (souvent)";
                case "MOKI": return "regard / œil (souvent)";
                case "BANA": return "enfants (souvent)";
                case "LISI": return "yeux (souvent)";
                case "LIBO": return "intestins (souvent)";
                case "YAYA": return "oncle (souvent)";
                case "MELI": return "poisson (souvent)";
                case "MOSI": return "fumée (souvent)";
                case "BUKA": return "ouvrir (souvent)";
                default: return "";
            }
        }
    }
}
