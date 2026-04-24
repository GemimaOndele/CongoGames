using System;
using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Filtre les URLs de « page » (YouTube, Spotify web…) que Unity <see cref="UnityEngine.Video.VideoPlayer"/>
    /// ne peut pas lire : il faut un fichier <c>.mp4</c> / <c>.webm</c> (HTTPS direct) ou un fichier
    /// dans <see cref="UnityEngine.Application.streamingAssetsPath"/>. Aucun gros binaire n’est exigé dans le dépôt.
    /// </summary>
    public static class StreamingMediaUrlPolicy
    {
        /// <summary>True si l’URL pointe typiquement vers une page applicative, pas un flux de fichier audio/vidéo unique.</summary>
        public static bool IsNonStreamableContentPageUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            string t = url.Trim();
            if (!t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !t.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!Uri.TryCreate(t, UriKind.Absolute, out Uri uri))
            {
                return true;
            }

            string host = uri.Host;
            if (string.IsNullOrEmpty(host)) return true;
            host = host.ToLowerInvariant();
            if (host == "youtu.be" || host == "m.youtube.com" || host == "music.youtube.com") return true;
            if (host == "www.youtube.com" || host == "youtube.com" || host.EndsWith(".youtube.com", StringComparison.Ordinal)) return true;
            if (host.Contains("open.spotify.com", StringComparison.Ordinal) || host == "spotify.com" || host.EndsWith(".spotify.com", StringComparison.Ordinal)) return true;
            if (host.Contains("tiktok.com", StringComparison.Ordinal)) return true;
            if (host.Contains("facebook.com", StringComparison.Ordinal) && t.ToLowerInvariant().Contains("/watch")) return true;
            if (host.Contains("instagram.com", StringComparison.Ordinal) || host.Contains("twitter.com", StringComparison.Ordinal) || host == "x.com" || host.EndsWith(".x.com", StringComparison.Ordinal)) return true;
            if (host.Contains("soundcloud.com", StringComparison.Ordinal) && t.IndexOf(".mp3", StringComparison.OrdinalIgnoreCase) < 0) return true;
            return false;
        }

        public static void LogOnceRejected(string fieldLabel, string url, string helpHint = null)
        {
            string h = string.IsNullOrEmpty(helpHint)
                ? "Utilisez une URL HTTPS directe (fichier .mp4/.webm) dans remote_media.json, ou un fichier local dans StreamingAssets/Theme/. Voir docs/THEME_YOUTUBE_AND_STREAMING.md"
                : helpHint;
            Debug.LogWarning("[Média] " + fieldLabel + " non lisible (page web / plateforme) : « " + url + " ». " + h);
        }
    }
}
