using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CongoGames.Core;

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
            var list = new List<string>(96);
            if (StreamingAssetsUrl.IsWebGlData)
            {
                foreach (string name in BackgroundVideoFileNames)
                {
                    list.Add(StreamingAssetsUrl.UrlForRelativePath("Theme/" + id + "/" + name));
                }

                // Même convention que l’éditeur : assets « gameplay » par mode (nouveaux fonds animés).
                foreach (string name in BackgroundVideoFileNames)
                {
                    list.Add(StreamingAssetsUrl.UrlForRelativePath("Theme/Gameplay/" + id + "/" + name));
                }

                foreach (string name in BackgroundVideoFileNames)
                {
                    list.Add(StreamingAssetsUrl.UrlForRelativePath("Theme/_dev_import/" + id + "/" + name));
                }

                foreach (string name in BackgroundVideoFileNames)
                {
                    list.Add(StreamingAssetsUrl.UrlForRelativePath("Theme/" + name));
                }

                foreach (string name in BackgroundVideoFileNames)
                {
                    list.Add(StreamingAssetsUrl.UrlForRelativePath("Theme/_global/" + name));
                }

                return list;
            }

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

            // Nouveau fallback explicite : Theme/_global/*
            string globalFolder = Path.Combine(root, "_global");
            foreach (string name in BackgroundVideoFileNames)
            {
                list.Add(Path.Combine(globalFolder, name));
            }

            // Fallback intelligent desktop: accepte aussi toute vidéo du dossier (pas seulement background/loop/show).
            AddDynamicFolderVideos(list, Path.Combine(root, id));
            AddDynamicFolderVideos(list, Path.Combine(root, "Gameplay", id));
            AddDynamicFolderVideos(list, devImport);
            AddDynamicFolderVideos(list, root);
            AddDynamicFolderVideos(list, globalFolder);
            AddDynamicFolderVideos(list, Path.Combine(root, "_dev_import", GlobalKey));

            return list;
        }

        private static void AddDynamicFolderVideos(List<string> list, string folder)
        {
            if (!Directory.Exists(folder))
            {
                return;
            }

            try
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    string ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".mp4" && ext != ".webm" && ext != ".mov" && ext != ".m4v")
                    {
                        continue;
                    }

                    list.Add(file);
                }
            }
            catch (IOException)
            {
                // Ignoré: on garde les candidats "nommés" déjà collectés.
            }
        }

        /// <summary>
        /// Ajoute toutes les vidéos découvertes sous les autres dossiers <c>Theme/&lt;mode&gt;</c>
        /// (rotation « globale » : pas seulement le mode actif).
        /// </summary>
        public static void AppendVideosFromOtherModeFolders(string currentModeId, List<string> destination)
        {
            if (destination == null || StreamingAssetsUrl.IsWebGlData)
            {
                return;
            }

            string root = Path.Combine(Application.streamingAssetsPath, "Theme");
            foreach (string mid in AllModeIds)
            {
                if (string.Equals(mid, currentModeId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AddDynamicFolderVideos(destination, Path.Combine(root, mid));
                AddDynamicFolderVideos(destination, Path.Combine(root, "Gameplay", mid));
            }
        }

        /// <summary>
        /// WebGL : URLs standards par autre mode (HEAD filtré dans ThemeBackgroundController).
        /// </summary>
        public static IEnumerable<string> EnumerateStandardVideoUrlsForOtherModes(string currentModeId)
        {
            foreach (string mid in AllModeIds)
            {
                if (string.Equals(mid, currentModeId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (string name in BackgroundVideoFileNames)
                {
                    yield return StreamingAssetsUrl.UrlForRelativePath("Theme/" + mid + "/" + name);
                }
            }
        }
    }
}
