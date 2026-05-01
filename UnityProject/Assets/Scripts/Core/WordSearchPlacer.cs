using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CongoGames.Core
{
    /// <summary>
    /// Une ligne de mot placée dans la grille (direction incluse : H, V, diagonales, avant/arrière).
    /// </summary>
    [Serializable]
    public struct WordLinePlacement
    {
        public string Word;
        public int StartR;
        public int StartC;
        public int Dr;
        public int Dc;
    }

    /// <summary>
    /// Remplit une grille n×n avec des mots en ligne droite (8 directions) ; cases restantes = lettres aléatoires.
    /// Les mots longs sont placés en premier pour favoriser croisements et densité (style mots mêlés classique).
    /// </summary>
    public static class WordSearchPlacer
    {
        private const string Fill = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static bool TryBuild(int size, IReadOnlyList<string> words, int maxAttempts, out char[,] grid, out List<WordLinePlacement> placements)
        {
            grid = null;
            placements = null;
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
                toPlace.Sort((a, b) =>
                {
                    int la = string.IsNullOrEmpty(a) ? 0 : a.Length;
                    int lb = string.IsNullOrEmpty(b) ? 0 : b.Length;
                    int cmp = lb.CompareTo(la);
                    if (cmp != 0) return cmp;
                    return string.CompareOrdinal(a, b);
                });

                var placedLines = new List<WordLinePlacement>(toPlace.Count);
                bool ok = true;
                foreach (string raw in toPlace)
                {
                    string w = (raw ?? "").Trim().ToUpperInvariant();
                    w = w.Replace("É", "E").Replace("È", "E");
                    if (w.Length < 2 || w.Length > size) { ok = false; break; }
                    if (!PlaceOneWord(g, size, w, placedLines))
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
                placements = placedLines;
                return true;
            }

            return false;
        }

        private static readonly int[,] Dirs =
        {
            { 0, 1 },
            { 0, -1 },
            { 1, 0 },
            { -1, 0 },
            { 1, 1 },
            { 1, -1 },
            { -1, 1 },
            { -1, -1 }
        };

        private static bool WordFitsBounds(int size, int len, int r, int c, int dr, int dc)
        {
            for (int i = 0; i < len; i++)
            {
                int rr = r + dr * i;
                int cc = c + dc * i;
                if (rr < 0 || cc < 0 || rr >= size || cc >= size)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool PlaceOneWord(char[,] g, int size, string w, List<WordLinePlacement> record)
        {
            int len = w.Length;
            if (len > size) return false;
            int nDir = Dirs.GetLength(0);
            for (int tries = 0; tries < 900; tries++)
            {
                int di = Random.Range(0, nDir);
                int dr = Dirs[di, 0];
                int dc = Dirs[di, 1];
                int r = Random.Range(0, size);
                int c = Random.Range(0, size);
                if (!WordFitsBounds(size, len, r, c, dr, dc))
                {
                    continue;
                }

                if (FitsLine(g, size, r, c, dr, dc, w))
                {
                    ApplyLine(g, r, c, dr, dc, w);
                    record?.Add(new WordLinePlacement { Word = w, StartR = r, StartC = c, Dr = dr, Dc = dc });
                    return true;
                }
            }

            for (int di = 0; di < nDir; di++)
            {
                int dr = Dirs[di, 0];
                int dc = Dirs[di, 1];
                for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    if (!WordFitsBounds(size, len, r, c, dr, dc))
                    {
                        continue;
                    }

                    if (FitsLine(g, size, r, c, dr, dc, w))
                    {
                        ApplyLine(g, r, c, dr, dc, w);
                        record?.Add(new WordLinePlacement { Word = w, StartR = r, StartC = c, Dr = dr, Dc = dc });
                        return true;
                    }
                }
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
