using System;
using System.IO;
using UnityEngine;

namespace CongoGames.Core
{
    /// <summary>
    /// En WebGL, <see cref="Application.streamingAssetsPath"/> est une URL HTTP(s) : pas de <c>file:///</c>
    /// ni d’énumération disque. Les chemins sous StreamingAssets se composent en URL.
    /// </summary>
    public static class StreamingAssetsUrl
    {
        public static bool IsWebGlData =>
#if UNITY_WEBGL && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public static string UrlForRelativePath(string pathUnderStreamingAssets)
        {
            if (string.IsNullOrEmpty(pathUnderStreamingAssets))
            {
                return pathUnderStreamingAssets;
            }

            string rel = pathUnderStreamingAssets.Replace("\\", "/").TrimStart('/');
            string root = (Application.streamingAssetsPath ?? "").Trim();
            if (string.IsNullOrEmpty(root))
            {
                return rel;
            }

            if (!root.EndsWith("/"))
            {
                root += "/";
            }

            return root + rel;
        }

        /// <summary>
        /// Convertit un chemin disque (Editor / standalone) contenant "StreamingAssets" en URL
        /// utilisable par UnityWebRequest. Si déjà http(s) ou file:, renvoie tel quel.
        /// </summary>
        public static string ToRequestUrl(string fileSystemPathOrUrl)
        {
            if (string.IsNullOrEmpty(fileSystemPathOrUrl))
            {
                return fileSystemPathOrUrl;
            }

            string t = fileSystemPathOrUrl.Trim();
            if (t.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                t.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                return t;
            }

            if (IsWebGlData)
            {
                string rel = ToRelativeFromStreamingPath(t, out bool ok);
                if (ok)
                {
                    return UrlForRelativePath(rel);
                }
            }

            // Chemins locaux : Uri encode espaces, &, #, etc. Sinon UnityWebRequest peut échouer
            // sur des noms du type « … & Wolverine … » alors que d’autres MP3 se chargent.
            try
            {
                string full = Path.GetFullPath(t);
                return new Uri(full).AbsoluteUri;
            }
            catch (Exception)
            {
                return "file:///" + Path.GetFullPath(t).Replace("\\", "/");
            }
        }

        public static string ToRelativeFromStreamingPath(string fileSystemPath, out bool ok)
        {
            ok = false;
            if (string.IsNullOrEmpty(fileSystemPath))
            {
                return null;
            }

            string norm = Path.GetFullPath(fileSystemPath).Replace("\\", "/");
            int i = norm.IndexOf("/StreamingAssets/", StringComparison.OrdinalIgnoreCase);
            if (i < 0)
            {
                return null;
            }

            int start = i + "/StreamingAssets/".Length;
            ok = true;
            return norm.Substring(start);
        }
    }
}
