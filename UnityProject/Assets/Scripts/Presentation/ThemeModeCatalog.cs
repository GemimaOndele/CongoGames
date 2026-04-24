using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>Modes de mini-jeu reconnus par le thème (fond vidéo / 3D / remote).</summary>
    public static class ThemeModeCatalog
    {
        public const string GlobalKey = "_global";

        public static readonly string[] AllModeIds =
        {
            "quiz",
            "semantic",
            "word-scramble",
            "crossword-lite",
            "blind-test",
            "mystery-word",
            "memory",
            "speed-chrono",
            "image-guess"
        };

        public static IReadOnlyList<string> BackgroundVideoFileNames { get; } = new[]
        {
            "background.mp4",
            "background.webm",
            "loop.mp4",
            "loop.webm",
            "theatre.mp4",
            "theatre.webm",
            "show.mp4",
            "show.webm"
        };

        public static IReadOnlyList<string> BuildLocalBackgroundCandidates(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId.Trim();
            var list = new List<string>(24);
            string root = Path.Combine(Application.streamingAssetsPath, "Theme");
            foreach (string name in BackgroundVideoFileNames)
            {
                list.Add(Path.Combine(root, id, name));
            }

            // Imports dev (hors dépôt) : outils comme tools/fetch-youtube-theme.ps1
            string devImport = Path.Combine(root, "_dev_import", id);
            foreach (string name in BackgroundVideoFileNames)
            {
                list.Add(Path.Combine(devImport, name));
            }

            foreach (string name in BackgroundVideoFileNames)
            {
                list.Add(Path.Combine(root, name));
            }

            return list;
        }
    }
}
