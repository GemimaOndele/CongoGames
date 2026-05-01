using System;
using System.Collections.Generic;
using UnityEngine;

namespace CongoGames.Core
{
    public readonly struct CrosswordClueEntry
    {
        public readonly int Number;
        public readonly string Word;
        public readonly bool Horizontal;
        public readonly int StartR;
        public readonly int StartC;

        public CrosswordClueEntry(int number, string word, bool horizontal, int startR, int startC)
        {
            Number = number;
            Word = word;
            Horizontal = horizontal;
            StartR = startR;
            StartC = startC;
        }
    }

    /// <summary>
    /// Génère une grille de mots croisés simple (horizontal/vertical uniquement) avec croisements.
    /// </summary>
    public static class CrosswordClassicPlacer
    {
        private readonly struct Slot
        {
            public readonly string Word;
            public readonly int StartR;
            public readonly int StartC;
            public readonly bool Horizontal;

            public Slot(string word, int startR, int startC, bool horizontal)
            {
                Word = word;
                StartR = startR;
                StartC = startC;
                Horizontal = horizontal;
            }
        }

        private struct Candidate
        {
            public int R;
            public int C;
            public bool Horizontal;
            public int Intersections;
        }

        public static bool TryBuild(
            int size,
            IReadOnlyList<string> words,
            int maxAttempts,
            out char[,] grid,
            out List<WordLinePlacement> placements,
            out List<CrosswordClueEntry> clues)
        {
            grid = null;
            placements = null;
            clues = null;
            if (size < 5 || words == null || words.Count == 0)
            {
                return false;
            }

            List<string> pool = BuildPool(words, size);
            if (pool.Count < 2)
            {
                return false;
            }

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                char[,] g = new char[size, size];
                var slots = new List<Slot>(pool.Count);
                if (!TryPlaceAll(g, size, pool, slots))
                {
                    continue;
                }

                if (slots.Count < Mathf.Min(5, pool.Count))
                {
                    continue;
                }

                BuildOutputs(g, slots, out placements, out clues);
                grid = g;
                return true;
            }

            return false;
        }

        private static List<string> BuildPool(IReadOnlyList<string> words, int size)
        {
            var pool = new List<string>(words.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < words.Count; i++)
            {
                string w = GridThemeBank.SanitizeForGrid(words[i] ?? "");
                if (w.Length < 3 || w.Length > size) continue;
                if (seen.Add(w)) pool.Add(w);
            }

            pool.Sort((a, b) => b.Length.CompareTo(a.Length));
            return pool;
        }

        private static bool TryPlaceAll(char[,] grid, int size, List<string> words, List<Slot> slots)
        {
            string first = words[0];
            int row = size / 2;
            int col = Mathf.Max(0, (size - first.Length) / 2);
            if (!CanPlace(grid, size, first, row, col, true, requireCross: false, out _))
            {
                return false;
            }

            PlaceWord(grid, first, row, col, true);
            slots.Add(new Slot(first, row, col, true));

            for (int wi = 1; wi < words.Count; wi++)
            {
                string w = words[wi];
                var candidates = BuildCandidates(grid, size, w);
                if (candidates.Count == 0)
                {
                    continue;
                }

                int pick = UnityEngine.Random.Range(0, candidates.Count);
                Candidate c = candidates[pick];
                PlaceWord(grid, w, c.R, c.C, c.Horizontal);
                slots.Add(new Slot(w, c.R, c.C, c.Horizontal));
            }

            return true;
        }

        private static List<Candidate> BuildCandidates(char[,] grid, int size, string word)
        {
            var list = new List<Candidate>(64);
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    char ch = grid[r, c];
                    if (ch == '\0') continue;
                    for (int i = 0; i < word.Length; i++)
                    {
                        if (word[i] != ch) continue;
                        int startHc = c - i;
                        if (CanPlace(grid, size, word, r, startHc, true, requireCross: true, out int crossH))
                        {
                            list.Add(new Candidate { R = r, C = startHc, Horizontal = true, Intersections = crossH });
                        }

                        int startVr = r - i;
                        if (CanPlace(grid, size, word, startVr, c, false, requireCross: true, out int crossV))
                        {
                            list.Add(new Candidate { R = startVr, C = c, Horizontal = false, Intersections = crossV });
                        }
                    }
                }
            }

            list.Sort((a, b) => b.Intersections.CompareTo(a.Intersections));
            if (list.Count > 16)
            {
                list.RemoveRange(16, list.Count - 16);
            }

            return list;
        }

        private static bool CanPlace(
            char[,] grid,
            int size,
            string word,
            int startR,
            int startC,
            bool horizontal,
            bool requireCross,
            out int intersections)
        {
            intersections = 0;
            if (horizontal)
            {
                if (startR < 0 || startR >= size || startC < 0 || startC + word.Length > size) return false;
                if (HasLetter(grid, startR, startC - 1) || HasLetter(grid, startR, startC + word.Length)) return false;
            }
            else
            {
                if (startC < 0 || startC >= size || startR < 0 || startR + word.Length > size) return false;
                if (HasLetter(grid, startR - 1, startC) || HasLetter(grid, startR + word.Length, startC)) return false;
            }

            for (int i = 0; i < word.Length; i++)
            {
                int rr = horizontal ? startR : startR + i;
                int cc = horizontal ? startC + i : startC;
                char existing = grid[rr, cc];
                if (existing != '\0' && existing != word[i]) return false;
                if (existing == word[i]) intersections++;

                if (existing == '\0')
                {
                    if (horizontal)
                    {
                        if (HasLetter(grid, rr - 1, cc) || HasLetter(grid, rr + 1, cc)) return false;
                    }
                    else
                    {
                        if (HasLetter(grid, rr, cc - 1) || HasLetter(grid, rr, cc + 1)) return false;
                    }
                }
            }

            if (requireCross && intersections == 0) return false;
            return true;
        }

        private static bool HasLetter(char[,] g, int r, int c)
        {
            int h = g.GetLength(0);
            int w = g.GetLength(1);
            if (r < 0 || c < 0 || r >= h || c >= w) return false;
            return g[r, c] != '\0';
        }

        private static void PlaceWord(char[,] grid, string word, int startR, int startC, bool horizontal)
        {
            for (int i = 0; i < word.Length; i++)
            {
                int rr = horizontal ? startR : startR + i;
                int cc = horizontal ? startC + i : startC;
                grid[rr, cc] = word[i];
            }
        }

        private static void BuildOutputs(
            char[,] grid,
            List<Slot> slots,
            out List<WordLinePlacement> placements,
            out List<CrosswordClueEntry> clues)
        {
            placements = new List<WordLinePlacement>(slots.Count);
            clues = new List<CrosswordClueEntry>(slots.Count);
            var numbers = BuildNumbers(grid);
            for (int i = 0; i < slots.Count; i++)
            {
                Slot s = slots[i];
                placements.Add(new WordLinePlacement
                {
                    Word = s.Word,
                    StartR = s.StartR,
                    StartC = s.StartC,
                    Dr = s.Horizontal ? 0 : 1,
                    Dc = s.Horizontal ? 1 : 0
                });

                int num = numbers[s.StartR, s.StartC];
                clues.Add(new CrosswordClueEntry(num, s.Word, s.Horizontal, s.StartR, s.StartC));
            }
        }

        private static int[,] BuildNumbers(char[,] grid)
        {
            int rows = grid.GetLength(0);
            int cols = grid.GetLength(1);
            int[,] numbers = new int[rows, cols];
            int n = 1;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (grid[r, c] == '\0') continue;
                    bool startH = c == 0 || grid[r, c - 1] == '\0';
                    bool startV = r == 0 || grid[r - 1, c] == '\0';
                    if (!startH && !startV) continue;
                    numbers[r, c] = n++;
                }
            }

            return numbers;
        }
    }
}
