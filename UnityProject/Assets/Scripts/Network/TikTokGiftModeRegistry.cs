using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using CongoGames.Core;

namespace CongoGames.Network
{
    /// <summary>
    /// Mapping exact TikTok giftName → modeId. Complète le champ <see cref="LiveMessage.gameMode"/> envoyé par le backend.
    /// Fichier : StreamingAssets/Theme/tiktok_gift_modes.json
    /// </summary>
    public static class TikTokGiftModeRegistry
    {
        [Serializable]
        private class FileDto
        {
            public GiftRow[] mappings;
        }

        [Serializable]
        private class GiftRow
        {
            public string tiktokGiftName;
            public string modeId;
        }

        private static Dictionary<string, string> map;
        private static bool loadAttempted;
        private static bool webPrewarmDone;

        public static string ResolveGameSwitchMode(LiveMessage msg)
        {
            if (msg == null) return null;

            string act = (msg.action ?? "").Trim().ToLowerInvariant();
            if (act == "mode" || act == "gamemode")
            {
                return ModeIdCatalog.NormalizeOrNull(msg.message ?? msg.text ?? "");
            }

            string fromServer = ModeIdCatalog.NormalizeOrNull(msg.gameMode);
            if (!string.IsNullOrEmpty(fromServer))
            {
                return fromServer;
            }

            EnsureLoaded();
            string gift = (msg.giftName ?? "").Trim();
            if (string.IsNullOrEmpty(gift))
            {
                return HeuristicFallback(msg);
            }

            if (map != null)
            {
                if (map.TryGetValue(gift, out string direct))
                {
                    return ModeIdCatalog.NormalizeOrNull(direct);
                }

                string lower = gift.ToLowerInvariant();
                foreach (KeyValuePair<string, string> kv in map)
                {
                    if (kv.Key != null && kv.Key.ToLowerInvariant() == lower)
                    {
                        return ModeIdCatalog.NormalizeOrNull(kv.Value);
                    }
                }
            }

            return HeuristicFallback(msg);
        }

        /// <summary>WebGL : JSON chargé par HTTP avant le premier <see cref="EnsureLoaded"/> fichier.</summary>
        public static void IngestPrewarmJson(string json)
        {
            map = new Dictionary<string, string>(StringComparer.Ordinal);
            loadAttempted = true;
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            try
            {
                FileDto dto = JsonUtility.FromJson<FileDto>(json);
                if (dto?.mappings == null)
                {
                    return;
                }

                foreach (GiftRow row in dto.mappings)
                {
                    if (row == null || string.IsNullOrWhiteSpace(row.tiktokGiftName) || string.IsNullOrWhiteSpace(row.modeId))
                    {
                        continue;
                    }

                    map[row.tiktokGiftName.Trim()] = row.modeId.Trim();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("TikTokGiftModeRegistry (Web) : " + ex.Message);
            }
        }

        public static IEnumerator CoPrewarmIfWebGl()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (webPrewarmDone)
            {
                yield break;
            }

            string url = StreamingAssetsUrl.UrlForRelativePath("Theme/tiktok_gift_modes.json");
            string text = null;
            bool ok = false;
            using (UnityWebRequest u = UnityWebRequest.Get(url))
            {
                u.timeout = 15;
                yield return u.SendWebRequest();
                ok = u.result == UnityWebRequest.Result.Success;
                text = u.downloadHandler?.text;
            }

            IngestPrewarmJson(ok ? text : null);
            webPrewarmDone = true;
#else
            yield break;
#endif
        }

        private static void EnsureLoaded()
        {
            if (loadAttempted) return;
#if UNITY_WEBGL && !UNITY_EDITOR
            loadAttempted = true;
            map = new Dictionary<string, string>(StringComparer.Ordinal);
            return;
#endif
            loadAttempted = true;
            map = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                string path = Path.Combine(Application.streamingAssetsPath, "Theme", "tiktok_gift_modes.json");
                if (!File.Exists(path))
                {
                    return;
                }

                string json = File.ReadAllText(path);
                FileDto dto = JsonUtility.FromJson<FileDto>(json);
                if (dto?.mappings == null) return;
                foreach (GiftRow row in dto.mappings)
                {
                    if (row == null || string.IsNullOrWhiteSpace(row.tiktokGiftName) || string.IsNullOrWhiteSpace(row.modeId))
                    {
                        continue;
                    }

                    map[row.tiktokGiftName.Trim()] = row.modeId.Trim();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("TikTokGiftModeRegistry: " + ex.Message);
            }
        }

        private static string HeuristicFallback(LiveMessage msg)
        {
            string g = (msg.giftName ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(g)) return null;

            if (g.Contains("quiz")) return "quiz";
            if (g.Contains("semantic") || g.Contains("assoc")) return "semantic";
            if (g.Contains("scramble") || g.Contains("melang")) return "word-scramble";
            if (g.Contains("cross") || g.Contains("crois")) return "crossword-lite";
            if (g.Contains("blind")) return "blind-test";
            if (g.Contains("mystery") || g.Contains("myst")) return "mystery-word";
            if (g.Contains("memory") || g.Contains("memo")) return "memory";
            if (g.Contains("chrono") || g.Contains("speed")) return "speed-chrono";
            if (g.Contains("image") || g.Contains("devine")) return "image-guess";
            return null;
        }
    }
}
