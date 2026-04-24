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
                "BRAZZA", "POINTE", "CUVETTE", "LIKOUALA", "SANGHA", "POOL", "LEFINI", "NIARI", "LOANGO", "OUESSO", "BETOU", "IMPFONDO", "BANIO", "MAYOMBE", "LESTUAIRE"
            }),
            new Theme("nature", "Forêts, fleuve, faune", new[]
            {
                "FLEUVE", "CONGO", "GORILLE", "SANGA", "FORET", "PARC", "EQUATEUR", "PLUIE", "RIVIERE", "OCEAN", "COCOTIER", "MANGROVE", "BASSIN", "FLORE", "FAUNE"
            }),
            new Theme("culture", "Culture, fête, drapeau", new[]
            {
                "BANTOU", "RUMBA", "NGOMA", "NDOMBO", "SEBEN", "DANSE", "CHANT", "BRAZZA", "MBOTE", "FETE", "AOUT", "CULTURE", "FOULE", "KERMES", "DANJAR"
            }),
            new Theme("lang", "Lingala & kituba (mots usités)", new[]
            {
                "MELESI", "MBOTE", "MALAMU", "MOTEMA", "MWANA", "KOKO", "BISO", "MANGI", "LIBOSO", "SIMBA", "MOSALA", "NDAKISA", "NZOTO", "MABOKO", "MPONDO"
            }),
            new Theme("sport", "Stade, foot, ferveur", new[]
            {
                "BUTEUR", "STADE", "EQUIPE", "BALLON", "VICTO", "COUPE", "FOULE", "BRAZZA", "BANC", "TALENT", "ESSAI", "JOUER", "MATCH", "GAGNE", "BUTS"
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
    }
}
