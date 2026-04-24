using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using CongoGames.Core;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Charge une image depuis StreamingAssets/Theme/ImageGuess/ ou génère une scène illustrative.
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

            return BuildProceduralScene(styleSeed);
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

                onDone(BuildProceduralScene(styleSeed));
                yield break;
            }

            onDone(ResolveTexture(fileNameWithoutExt, styleSeed));
        }

        private static Texture2D BuildProceduralScene(int seed)
        {
            const int w = 512;
            const int h = 384;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            System.Random rng = new System.Random(seed == 0 ? 42 : seed);
            Color skyTop = new Color(0.12f + (float)rng.NextDouble() * 0.08f, 0.28f, 0.52f + (float)rng.NextDouble() * 0.1f, 1f);
            Color skyBot = new Color(0.45f + (float)rng.NextDouble() * 0.15f, 0.62f, 0.85f, 1f);
            Color ground = new Color(0.08f + (float)rng.NextDouble() * 0.06f, 0.38f + (float)rng.NextDouble() * 0.12f, 0.14f, 1f);
            Color sun = new Color(1f, 0.92f, 0.35f, 1f);
            Color monument = new Color(0.06f, 0.07f, 0.09f, 1f);

            float sunX = 0.62f + (float)(rng.NextDouble() * 0.2 - 0.1f);
            float sunY = 0.72f + (float)(rng.NextDouble() * 0.12);
            float sunR = w * (0.06f + (float)rng.NextDouble() * 0.03f);
            int groundLine = (int)(h * (0.42f + (seed % 5) * 0.02f));

            for (int y = 0; y < h; y++)
            {
                float v = y / (float)h;
                Color rowSky = Color.Lerp(skyBot, skyTop, Mathf.SmoothStep(0f, 1f, v));
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w;
                    if (y < groundLine)
                    {
                        float gnoise = Mathf.PerlinNoise(u * 5.7f + seed * 0.01f, v * 4.3f) * 0.08f;
                        tex.SetPixel(x, y, ground + new Color(gnoise, gnoise * 0.6f, gnoise * 0.4f, 0f));
                    }
                    else
                    {
                        Color c = rowSky;
                        float dx = (x - sunX * w);
                        float dy = (y - sunY * h);
                        if (dx * dx + dy * dy < sunR * sunR)
                        {
                            float k = 1f - Mathf.Sqrt(dx * dx + dy * dy) / sunR;
                            c = Color.Lerp(c, sun, Mathf.Clamp01(k * 1.2f));
                        }

                        float band = Mathf.PerlinNoise(u * 3f + seed * 0.02f, v * 2f);
                        c = Color.Lerp(c, new Color(0.75f, 0.82f, 0.95f, 1f), band * 0.12f * (1f - v));
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            DrawMonumentSilhouette(tex, w, h, groundLine, monument, rng);
            DrawCongoBrandingStrip(tex, w, h, groundLine, seed);
            tex.Apply(false, true);
            return tex;
        }

        private static void DrawCongoBrandingStrip(Texture2D tex, int w, int h, int groundLine, int seed)
        {
            // Bande drapeau (déco) si l’utilisateur n’a pas encore placé d’image réelle dans Theme/ImageGuess/
            for (int y = groundLine - 3; y < Mathf.Min(groundLine + 5, h); y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = x / (float)w;
                    Color c;
                    if (t < 0.33f)
                    {
                        c = new Color(0.12f, 0.52f, 0.24f, 1f);
                    }
                    else if (t < 0.66f)
                    {
                        c = new Color(0.95f, 0.8f, 0.1f, 1f);
                    }
                    else
                    {
                        c = new Color(0.82f, 0.12f, 0.15f, 1f);
                    }

                    float n = Mathf.PerlinNoise(x * 0.03f + seed * 0.01f, y * 0.08f) * 0.06f;
                    tex.SetPixel(x, y, c + new Color(n, n * 0.5f, n * 0.4f, 0f));
                }
            }
        }

        private static void DrawMonumentSilhouette(Texture2D tex, int w, int h, int groundLine, Color fill, System.Random rng)
        {
            int apexX = w / 2 + rng.Next(-w / 8, w / 8);
            int baseW = w / 5 + rng.Next(-20, 40);
            int height = Mathf.Clamp(h - groundLine - 20, 80, h - groundLine - 5);
            int topY = groundLine - height;
            for (int y = topY; y <= groundLine; y++)
            {
                float t = (y - topY) / (float)Mathf.Max(1, height);
                int half = Mathf.RoundToInt(baseW * 0.5f * (0.35f + t * 0.65f));
                for (int x = apexX - half; x <= apexX + half; x++)
                {
                    if (x < 0 || x >= w || y < 0 || y >= h) continue;
                    tex.SetPixel(x, y, fill);
                }
            }

            int archTop = groundLine - height / 3;
            int archHalf = baseW / 4;
            for (int y = archTop; y <= groundLine; y++)
            for (int x = apexX - archHalf; x <= apexX + archHalf; x++)
            {
                float nx = (x - apexX) / (float)Mathf.Max(1, archHalf);
                float ny = (y - archTop) / (float)Mathf.Max(1, groundLine - archTop);
                if (nx * nx + ny * ny * 0.85f < 0.85f && x >= 0 && x < w && y >= 0 && y < h)
                {
                    Color below = tex.GetPixel(x, y);
                    Color punch = new Color(0.48f, 0.64f, 0.9f, 1f);
                    tex.SetPixel(x, y, Color.Lerp(below, punch, 0.92f));
                }
            }
        }
    }
}
