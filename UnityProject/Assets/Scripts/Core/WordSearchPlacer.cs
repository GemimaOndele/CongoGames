using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CongoGames.Core
{
    /// <summary>
    /// Remplit une grille 7×7 (ou n×n) avec des mots en ligne droite (H, V) ; cases restantes = lettres aléatoires.
    /// </summary>
    public static class WordSearchPlacer
    {
        private const string Fill = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static bool TryBuild(int size, IReadOnlyList<string> words, int maxAttempts, out char[,] grid)
        {
            grid = null;
            if (words == null || words.Count == 0 || size < 3)
            {
                return false;
            }

            for (int att = 0; att < maxAttempts; att++)
            {
                char[,] g = new char[size, size];
                for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    g[r, c] = ' ';
                }

                var toPlace = new List<string>(words);
                for (int i = toPlace.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (toPlace[i], toPlace[j]) = (toPlace[j], toPlace[i]);
                }

                bool ok = true;
                foreach (string raw in toPlace)
                {
                    string w = (raw ?? "").Trim().ToUpperInvariant();
                    w = w.Replace("É", "E").Replace("È", "E");
                    if (w.Length < 2 || w.Length > size) { ok = false; break; }
                    if (!PlaceOneWord(g, size, w))
                    {
                        ok = false;
                        break;
                    }
                }

                if (!ok) continue;
                for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    if (g[r, c] == ' ')
                    {
                        g[r, c] = Fill[Random.Range(0, Fill.Length)];
                    }
                }

                grid = g;
                return true;
            }

            return false;
        }

        private static bool PlaceOneWord(char[,] g, int size, string w)
        {
            int len = w.Length;
            if (len > size) return false;
            int tries = 500;
            while (tries-- > 0)
            {
                // droite (0,1) ou bas (1,0)
                int di = Random.Range(0, 2);
                int r;
                int c;
                if (di == 0)
                {
                    r = Random.Range(0, size);
                    c = Random.Range(0, size - len + 1);
                    if (FitsLine(g, size, r, c, 0, 1, w)) { ApplyLine(g, r, c, 0, 1, w); return true; }
                }
                else
                {
                    r = Random.Range(0, size - len + 1);
                    c = Random.Range(0, size);
                    if (FitsLine(g, size, r, c, 1, 0, w)) { ApplyLine(g, r, c, 1, 0, w); return true; }
                }
            }

            for (int r = 0; r < size; r++)
            for (int c = 0; c <= size - len; c++)
            {
                if (FitsLine(g, size, r, c, 0, 1, w)) { ApplyLine(g, r, c, 0, 1, w); return true; }
            }

            for (int r = 0; r <= size - len; r++)
            for (int c = 0; c < size; c++)
            {
                if (FitsLine(g, size, r, c, 1, 0, w)) { ApplyLine(g, r, c, 1, 0, w); return true; }
            }

            return false;
        }

        private static bool FitsLine(char[,] g, int size, int r, int c, int dr, int dc, string w)
        {
            for (int i = 0; i < w.Length; i++)
            {
                int rr = r + dr * i;
                int cc = c + dc * i;
                if (rr < 0 || cc < 0 || rr >= size || cc >= size) return false;
                char ch = g[rr, cc];
                if (ch != ' ' && ch != w[i]) return false;
            }

            return true;
        }

        private static void ApplyLine(char[,] g, int r, int c, int dr, int dc, string w)
        {
            for (int i = 0; i < w.Length; i++)
            {
                g[r + dr * i, c + dc * i] = w[i];
            }
        }
    }
}
