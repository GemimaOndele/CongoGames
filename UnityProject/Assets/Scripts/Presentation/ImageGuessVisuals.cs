using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using CongoGames.Core;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Charge une image <b>réelle</b> depuis StreamingAssets/Theme/ImageGuess/ (png/jpg).
    /// S’il manque un fichier : drapeau tricolore uniquement (pas de paysage généré).
    /// </summary>
    public static class ImageGuessVisuals
    {
        private const string SubFolder = "Theme/ImageGuess";

        public static Texture2D ResolveTexture(string fileNameWithoutExt, int styleSeed)
        {
            if (!string.IsNullOrEmpty(fileNameWithoutExt) && !StreamingAssetsUrl.IsWebGlData)
            {
                foreach (string ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    string path = Path.Combine(Application.streamingAssetsPath, SubFolder, fileNameWithoutExt + ext);
                    if (!File.Exists(path)) continue;
                    byte[] raw = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (ImageConversion.LoadImage(tex, raw, false))
                    {
                        tex.Apply(false, false);
                        return tex;
                    }
                }
            }

            return BuildTricolorOnlyPlaceholder();
        }

        /// <summary>WebGL : charge via HTTP ; Editor/standalone : appelle <see cref="ResolveTexture"/>.</summary>
        public static IEnumerator CoResolveTexture(string fileNameWithoutExt, int styleSeed, System.Action<Texture2D> onDone)
        {
            if (onDone == null) yield break;
            if (StreamingAssetsUrl.IsWebGlData)
            {
                if (!string.IsNullOrEmpty(fileNameWithoutExt))
                {
                    foreach (string ext in new[] { ".png", ".jpg", ".jpeg" })
                    {
                        string u = StreamingAssetsUrl.UrlForRelativePath(SubFolder + "/" + fileNameWithoutExt + ext);
                        using (UnityWebRequest req = UnityWebRequest.Get(u))
                        {
                            req.timeout = 25;
                            yield return req.SendWebRequest();
                            if (req.result == UnityWebRequest.Result.Success && req.downloadHandler?.data != null
                                && req.downloadHandler.data.Length > 0)
                            {
                                byte[] raw = req.downloadHandler.data;
                                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                                if (ImageConversion.LoadImage(tex, raw, false))
                                {
                                    tex.Apply(false, false);
                                    onDone(tex);
                                    yield break;
                                }

                                UnityEngine.Object.Destroy(tex);
                            }
                        }
                    }
                }

                onDone(BuildTricolorOnlyPlaceholder());
                yield break;
            }

            onDone(ResolveTexture(fileNameWithoutExt, styleSeed));
        }

        /// <summary>Pas de scène générée : uniquement rappel visuel drapeau — ajoutez un fichier .jpg/.png du Congo dans Theme/ImageGuess/.</summary>
        private static Texture2D BuildTricolorOnlyPlaceholder()
        {
            const int w = 512;
            const int h = 384;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = x / (float)w;
                    Color c;
                    if (t < 0.33f)
                    {
                        c = new Color(0.10f, 0.50f, 0.24f, 1f);
                    }
                    else if (t < 0.66f)
                    {
                        c = new Color(0.94f, 0.80f, 0.10f, 1f);
                    }
                    else
                    {
                        c = new Color(0.80f, 0.10f, 0.16f, 1f);
                    }

                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, true);
            return tex;
        }
    }
}
