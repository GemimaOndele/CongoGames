#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CongoGames.EditorTools
{
    public static class CongoGamesBuild
    {
        private const string MainScene = "Assets/Scenes/Main.unity";
        private const string OutputPath = "../Builds/Windows/CongoGames.exe";
        private static readonly string[] AudioExtensions = { ".ogg", ".mp3", ".wav" };

        [MenuItem("CongoGames/Build/Windows 64-bit")]
        public static void BuildWindows64Menu()
        {
            BuildWindows64();
        }

        public static void BuildWindows64()
        {
            RunPlaylistCoherenceChecksOrThrow();

            string outputOverride = Environment.GetEnvironmentVariable("CONGOGAMES_BUILD_OUTPUT");
            string output = string.IsNullOrWhiteSpace(outputOverride)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, OutputPath))
                : Path.GetFullPath(outputOverride);
            string dir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes/Main.unity")))
            {
                throw new FileNotFoundException("Scene introuvable", MainScene);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { MainScene },
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "Build Windows echoue: " + summary.result + " (" + summary.totalErrors + " erreur(s), "
                    + summary.totalWarnings + " avertissement(s)).");
            }

            Debug.Log("[CongoGames] Build Windows OK: " + output + " (" + summary.totalSize + " bytes)");
        }

        private static void RunPlaylistCoherenceChecksOrThrow()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string themeRoot = Path.Combine(projectRoot, "Assets", "StreamingAssets", "Theme");
            string playlistDir = Path.Combine(themeRoot, "playlist");
            if (!Directory.Exists(playlistDir))
            {
                throw new DirectoryNotFoundException("[CongoGames] Playlist introuvable avant build: " + playlistDir);
            }

            string[] audioFiles = Directory.GetFiles(playlistDir)
                .Where(IsAudioPath)
                .ToArray();
            if (audioFiles.Length == 0)
            {
                throw new InvalidOperationException("[CongoGames] Aucune piste audio trouvée dans Theme/playlist. Build annulé.");
            }

            string metaPath = Path.Combine(projectRoot, "Assets", "StreamingAssets", "Datasets", "blind_playlist_meta.json");
            if (!File.Exists(metaPath))
            {
                Debug.LogWarning("[CongoGames] Check playlist: blind_playlist_meta.json (StreamingAssets) absent, contrôle partiel.");
                return;
            }

            BlindPlaylistMetaFile meta = null;
            try
            {
                meta = JsonUtility.FromJson<BlindPlaylistMetaFile>(File.ReadAllText(metaPath));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("[CongoGames] blind_playlist_meta.json invalide: " + ex.Message);
            }

            if (meta?.items == null || meta.items.Length == 0)
            {
                throw new InvalidOperationException("[CongoGames] blind_playlist_meta.json est vide. Build annulé.");
            }

            var stems = audioFiles
                .Select(Path.GetFileNameWithoutExtension)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            var stemsSet = new HashSet<string>(stems, StringComparer.OrdinalIgnoreCase);

            int exactMatches = 0;
            int fuzzyMatches = 0;
            int unresolved = 0;
            int trackKeyCount = 0;
            int maxTrackKey = 0;

            foreach (BlindMetaItem item in meta.items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.fileBase))
                {
                    continue;
                }

                string fileBase = item.fileBase.Trim();
                if (stemsSet.Contains(fileBase))
                {
                    exactMatches++;
                    continue;
                }

                if (TryParseTrackKey(fileBase, out int trackNo))
                {
                    trackKeyCount++;
                    if (trackNo > maxTrackKey) maxTrackKey = trackNo;
                    // Le runtime sait replier trackNN vers une piste disponible.
                    fuzzyMatches++;
                    continue;
                }

                if (TryResolveByArtistTitle(stems, item.artist, item.title))
                {
                    fuzzyMatches++;
                }
                else
                {
                    unresolved++;
                    Debug.LogWarning("[CongoGames] Playlist meta non résolue: " + fileBase);
                }
            }

            if (trackKeyCount > 0 && maxTrackKey > audioFiles.Length)
            {
                Debug.LogWarning(
                    "[CongoGames] Playlist check: trackNN max="
                    + maxTrackKey
                    + " mais seulement "
                    + audioFiles.Length
                    + " piste(s) audio. Le runtime fera un repli cyclique.");
            }

            if (unresolved > 0)
            {
                Debug.LogWarning(
                    "[CongoGames] Playlist check: "
                    + unresolved
                    + " entrée(s) meta non résolues (sur "
                    + meta.items.Length
                    + "). Build autorisé avec repli audio runtime.");
            }

            Debug.Log(
                "[CongoGames] Playlist check OK: "
                + audioFiles.Length
                + " piste(s), exact="
                + exactMatches
                + ", fallback="
                + fuzzyMatches
                + ", unresolved="
                + unresolved);
        }

        private static bool IsAudioPath(string path)
        {
            string ext = Path.GetExtension(path);
            return AudioExtensions.Any(x => string.Equals(x, ext, StringComparison.OrdinalIgnoreCase));
        }

        private static bool TryResolveByArtistTitle(List<string> stems, string artist, string title)
        {
            string artistN = NormalizeForMatch(artist);
            string titleN = NormalizeForMatch(title);
            if (artistN.Length == 0 && titleN.Length == 0)
            {
                return false;
            }

            foreach (string stem in stems)
            {
                string stemN = NormalizeForMatch(stem);
                bool artistOk = artistN.Length == 0 || stemN.Contains(artistN);
                bool titleOk = titleN.Length == 0 || stemN.Contains(titleN);
                if (artistOk && titleOk)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseTrackKey(string text, out int trackNo)
        {
            trackNo = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string t = text.Trim();
            if (!t.StartsWith("track", StringComparison.OrdinalIgnoreCase) || t.Length <= 5)
            {
                return false;
            }

            return int.TryParse(t.Substring(5), out trackNo) && trackNo > 0;
        }

        private static string NormalizeForMatch(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string lower = raw.ToLowerInvariant()
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ê", "e")
                .Replace("ë", "e")
                .Replace("à", "a")
                .Replace("â", "a")
                .Replace("ä", "a")
                .Replace("î", "i")
                .Replace("ï", "i")
                .Replace("ô", "o")
                .Replace("ö", "o")
                .Replace("ù", "u")
                .Replace("û", "u")
                .Replace("ü", "u")
                .Replace("ç", "c");

            var chars = lower.Where(char.IsLetterOrDigit).ToArray();
            return new string(chars);
        }

        [Serializable]
        private sealed class BlindPlaylistMetaFile
        {
            public BlindMetaItem[] items;
        }

        [Serializable]
        private sealed class BlindMetaItem
        {
            public string fileBase;
            public string artist;
            public string title;
        }
    }
}
#endif
