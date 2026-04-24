using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CongoGames.Core;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Lecture de StreamingAssets/Theme/remote_media.json — URLs HTTPS vers fichiers médias directs (.mp4, .webm, .ogg, .mp3…).
    /// Les liens « page » (YouTube, Spotify) ne sont pas des flux vidéo/audio exploitables par Unity : il faut une URL qui pointe vers le fichier.
    /// </summary>
    [Serializable]
    public class RemoteThemeMediaFile
    {
        public RemoteModeMediaEntry[] entries;
    }

    [Serializable]
    public class RemoteModeMediaEntry
    {
        public string modeId;
        public string musicUrl;
        public string bottomVideoUrl;
        public string backgroundVideoUrl;
    }

    public static class RemoteThemeMediaConfig
    {
        private static RemoteThemeMediaFile cached;
        private static bool loadAttempted;

        /// <summary>Surcharge session (champ debug) — prioritaire sur le JSON.</summary>
        public static string RuntimeMusicUrl { get; set; }

        public static string RuntimeBottomVideoUrl { get; set; }

        public static string RuntimeBackgroundVideoUrl { get; set; }

        public static void SetRuntimeOverrides(string musicUrl, string bottomVideoUrl, string backgroundVideoUrl)
        {
            RuntimeMusicUrl = string.IsNullOrWhiteSpace(musicUrl) ? null : musicUrl.Trim();
            RuntimeBottomVideoUrl = string.IsNullOrWhiteSpace(bottomVideoUrl) ? null : bottomVideoUrl.Trim();
            RuntimeBackgroundVideoUrl = string.IsNullOrWhiteSpace(backgroundVideoUrl) ? null : backgroundVideoUrl.Trim();
        }

        public static void ClearCache()
        {
            cached = null;
            loadAttempted = false;
        }

        /// <summary>WebGL : charge <c>Theme/remote_media.json</c> par HTTP. Editor : appelle <see cref="EnsureLoaded"/> classique.</summary>
        public static IEnumerator CoLoadFromWeb()
        {
#if UNITY_EDITOR
            EnsureLoaded();
            yield break;
#elif UNITY_WEBGL
            if (loadAttempted)
            {
                yield break;
            }

            string url = StreamingAssetsUrl.UrlForRelativePath("Theme/remote_media.json");
            string text = null;
            bool netOk = false;
            yield return WebGlStreamingPrewarm.CoHttpGetText(
                url,
                (t, ok) =>
                {
                    text = t;
                    netOk = ok;
                });
            loadAttempted = true;
            if (netOk && !string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    cached = JsonUtility.FromJson<RemoteThemeMediaFile>(WrapArray(text));
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("remote_media.json (WebGL) : " + ex.Message);
                    cached = null;
                }
            }
            else
            {
                cached = null;
            }
#else
            EnsureLoaded();
            yield break;
#endif
        }

        public static RemoteModeMediaEntry Resolve(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId.Trim().ToLowerInvariant();
            EnsureLoaded();
            RemoteModeMediaEntry merged;
            if (cached?.entries == null || cached.entries.Length == 0)
            {
                merged = new RemoteModeMediaEntry { modeId = id };
            }
            else
            {
                RemoteModeMediaEntry global = null;
                RemoteModeMediaEntry match = null;
                foreach (RemoteModeMediaEntry e in cached.entries)
                {
                    if (e == null) continue;
                    string mid = (e.modeId ?? "").Trim().ToLowerInvariant();
                    if (mid == "_global" || mid == "global" || mid == "" || mid == "*")
                    {
                        global = e;
                    }

                    if (mid == id)
                    {
                        match = e;
                        break;
                    }
                }

                merged = Merge(global, match ?? new RemoteModeMediaEntry { modeId = id });
            }

            if (RuntimeMusicUrl != null) merged.musicUrl = RuntimeMusicUrl;
            if (RuntimeBottomVideoUrl != null) merged.bottomVideoUrl = RuntimeBottomVideoUrl;
            if (RuntimeBackgroundVideoUrl != null) merged.backgroundVideoUrl = RuntimeBackgroundVideoUrl;
            return merged;
        }

        private static RemoteModeMediaEntry Merge(RemoteModeMediaEntry global, RemoteModeMediaEntry specific)
        {
            if (global == null) return specific ?? new RemoteModeMediaEntry();
            if (specific == null) return Clone(global);

            return new RemoteModeMediaEntry
            {
                modeId = specific.modeId,
                musicUrl = Pick(specific.musicUrl, global.musicUrl),
                bottomVideoUrl = Pick(specific.bottomVideoUrl, global.bottomVideoUrl),
                backgroundVideoUrl = Pick(specific.backgroundVideoUrl, global.backgroundVideoUrl)
            };
        }

        private static RemoteModeMediaEntry Clone(RemoteModeMediaEntry g)
        {
            return new RemoteModeMediaEntry
            {
                modeId = g.modeId,
                musicUrl = g.musicUrl,
                bottomVideoUrl = g.bottomVideoUrl,
                backgroundVideoUrl = g.backgroundVideoUrl
            };
        }

        private static string Pick(string a, string b)
        {
            return !string.IsNullOrWhiteSpace(a) ? a.Trim() : (b ?? "").Trim();
        }

        private static void EnsureLoaded()
        {
            if (loadAttempted) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            return;
#endif
            loadAttempted = true;
            string path = Path.Combine(Application.streamingAssetsPath, "Theme", "remote_media.json");
            if (!File.Exists(path))
            {
                cached = null;
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                cached = JsonUtility.FromJson<RemoteThemeMediaFile>(WrapArray(json));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("remote_media.json : " + ex.Message);
                cached = null;
            }
        }

        /// <summary>JsonUtility attend un objet racine ; on accepte un tableau brut [...] dans le fichier.</summary>
        private static string WrapArray(string raw)
        {
            string t = raw.Trim();
            if (t.StartsWith("[", StringComparison.Ordinal))
            {
                return "{\"entries\":" + t + "}";
            }

            return t;
        }
    }
}
