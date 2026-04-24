using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using CongoGames.Core;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Fond par mode : URL remote, puis <see cref="ThemeModeCatalog"/> (plusieurs noms
    /// <c>background/loop/theatre/show</c> .mp4/.webm par mode et global), sinon 3D <see cref="VirtualShowStage3D"/>, etc.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class ThemeBackgroundController : MonoBehaviour
    {
        [SerializeField] private bool muteVideoSound = true;
        [SerializeField] private int renderWidth = 1920;
        [SerializeField] private int renderHeight = 1080;

        private RawImage raw;
        private VideoPlayer video;
        private RenderTexture rt;
        private Coroutine slideCo;
        private readonly List<Texture2D> slideTextures = new List<Texture2D>();
        private SyntheticVideoBackground synthetic;
        private VirtualShowStage3D virtual3D;
        private string activeModeId = "";

        private void Awake()
        {
            raw = GetComponent<RawImage>();
            raw.raycastTarget = false;
            raw.texture = BuildSolidTexture(new Color(0.05f, 0.09f, 0.06f, 1f));
            raw.color = Color.white;
            synthetic = GetComponent<SyntheticVideoBackground>();
            if (synthetic == null)
            {
                synthetic = gameObject.AddComponent<SyntheticVideoBackground>();
            }

            virtual3D = GetComponent<VirtualShowStage3D>();
            if (virtual3D == null)
            {
                virtual3D = gameObject.AddComponent<VirtualShowStage3D>();
            }
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(activeModeId))
            {
                ApplyGameMode("quiz");
            }
        }

        public void ApplyGameMode(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId.Trim().ToLowerInvariant();
            StartCoroutine(CoApplyGameMode(id));
        }

        private IEnumerator CoApplyGameMode(string modeId)
        {
            yield return WebGlStreamingPrewarm.CoRunOnce();
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId.Trim().ToLowerInvariant();
            activeModeId = id;
            StopEverything();

            RemoteModeMediaEntry remote = RemoteThemeMediaConfig.Resolve(id);
            if (!string.IsNullOrWhiteSpace(remote.backgroundVideoUrl))
            {
                string urlBg = remote.backgroundVideoUrl.Trim();
                if (StreamingMediaUrlPolicy.IsNonStreamableContentPageUrl(urlBg))
                {
                    StreamingMediaUrlPolicy.LogOnceRejected("Fond (backgroundVideoUrl)", urlBg);
                }
                else
                {
                    TryPlayVideoUrl(urlBg, muteVideoSound);
                    yield break;
                }
            }

            IReadOnlyList<string> candidates = ThemeModeCatalog.BuildLocalBackgroundCandidates(id);
            foreach (string path in candidates)
            {
                if (StreamingAssetsUrl.IsWebGlData)
                {
                    string u = StreamingAssetsUrl.ToRequestUrl(path);
                    bool ok = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                    if (ok)
                    {
                        TryPlayVideoUrl(u, muteVideoSound);
                        yield break;
                    }
                }
                else
                {
                    if (File.Exists(path))
                    {
                        TryPlayVideo(StreamingAssetsUrl.ToRequestUrl(path));
                        yield break;
                    }
                }
            }

            yield return CoStartSyntheticOrSlides(id);
        }

        /// <summary>Sans fichier vidéo : plateau 3D (option) ; sinon slides ; sinon « clip » 2D procédural.</summary>
        private IEnumerator CoStartSyntheticOrSlides(string id)
        {
            if (PresentationConfig.UseVirtual3DShowStage && virtual3D != null)
            {
                synthetic?.Stop();
                virtual3D.RebuildForMode(id, raw);
                yield break;
            }

            virtual3D?.Teardown();
            string root = Path.Combine(Application.streamingAssetsPath, "Theme");
            string useSlidesFlag = Path.Combine(root, id, "use_slides.flag");
            string useSlidesGlobal = Path.Combine(root, "use_slides.flag");
            bool useSlides;
            if (StreamingAssetsUrl.IsWebGlData)
            {
                string u1 = StreamingAssetsUrl.UrlForRelativePath("Theme/" + id + "/use_slides.flag");
                string u2 = StreamingAssetsUrl.UrlForRelativePath("Theme/use_slides.flag");
                bool o1 = false;
                bool o2 = false;
                yield return WebGlStreamingPrewarm.CoHttpHeadOk(u1, b => o1 = b);
                yield return WebGlStreamingPrewarm.CoHttpHeadOk(u2, b => o2 = b);
                useSlides = o1 || o2;
            }
            else
            {
                useSlides = File.Exists(useSlidesFlag) || File.Exists(useSlidesGlobal);
            }

            if (useSlides)
            {
                if (StreamingAssetsUrl.IsWebGlData)
                {
                    var slidesW = new List<Texture2D>();
                    yield return CoBuildSlideTexturesWebGl(id, slidesW);
                    if (slidesW.Count > 0)
                    {
                        slideTextures.AddRange(slidesW);
                        raw.texture = slideTextures[0];
                        raw.color = Color.white;
                        raw.uvRect = new Rect(0f, 0f, 1f, 1f);
                        raw.rectTransform.localScale = Vector3.one;
                        slideCo = StartCoroutine(SlideShowLoop());
                        yield break;
                    }
                }
                else
                {
                    if (TryBuildSlideTextures(id, out List<Texture2D> slides))
                    {
                        slideTextures.AddRange(slides);
                        raw.texture = slideTextures[0];
                        raw.color = Color.white;
                        raw.uvRect = new Rect(0f, 0f, 1f, 1f);
                        raw.rectTransform.localScale = Vector3.one;
                        slideCo = StartCoroutine(SlideShowLoop());
                        yield break;
                    }
                }
            }

            synthetic?.Play(id, raw, renderWidth, renderHeight);
        }

        private void StopEverything()
        {
            if (virtual3D != null)
            {
                virtual3D.Teardown();
            }

            if (slideCo != null)
            {
                StopCoroutine(slideCo);
                slideCo = null;
            }

            foreach (Texture2D tex in slideTextures)
            {
                if (tex != null)
                {
                    Destroy(tex);
                }
            }

            slideTextures.Clear();

            synthetic?.Stop();

            if (video != null)
            {
                video.prepareCompleted -= OnVideoPrepared;
                video.errorReceived -= OnVideoError;
                Destroy(video);
                video = null;
            }

            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
                rt = null;
            }
        }

        private void OnDestroy()
        {
            StopEverything();
        }

        private void TryPlayVideo(string fileUrl)
        {
            TryPlayVideoUrl(fileUrl, muteVideoSound);
        }

        private void TryPlayVideoUrl(string url, bool mute)
        {
            rt = new RenderTexture(renderWidth, renderHeight, 0, RenderTextureFormat.ARGB32);
            rt.Create();
            raw.texture = rt;
            raw.color = Color.white;
            raw.rectTransform.localScale = Vector3.one;

            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = true;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = rt;
            video.url = url;
            if (mute)
            {
                video.audioOutputMode = VideoAudioOutputMode.None;
            }

            video.prepareCompleted += OnVideoPrepared;
            video.errorReceived += OnVideoError;
            video.Prepare();
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            raw.uvRect = new Rect(0f, 0f, 1f, 1f);
            source.Play();
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            string mode = activeModeId;
            StopEverything();
            StartCoroutine(CoStartSyntheticOrSlides(mode));
        }

        private bool TryBuildSlideTextures(string modeId, out List<Texture2D> textures)
        {
            textures = new List<Texture2D>();
            string root = Path.Combine(Application.streamingAssetsPath, "Theme");
            string folder = Path.Combine(root, modeId ?? "default");
            if (!Directory.Exists(folder))
            {
                return false;
            }

            List<string> paths = new List<string>();
            foreach (string f in Directory.GetFiles(folder))
            {
                string low = f.ToLowerInvariant();
                if (!low.EndsWith(".png", StringComparison.Ordinal) &&
                    !low.EndsWith(".jpg", StringComparison.Ordinal) &&
                    !low.EndsWith(".jpeg", StringComparison.Ordinal))
                {
                    continue;
                }

                string bn = Path.GetFileName(f).ToLowerInvariant();
                if (bn.StartsWith("bg_", StringComparison.Ordinal) || bn.StartsWith("slide", StringComparison.Ordinal))
                {
                    paths.Add(f);
                }
            }

            if (paths.Count == 0)
            {
                return false;
            }

            paths.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (string p in paths)
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (tex.LoadImage(bytes))
                    {
                        tex.Apply(false, true);
                        textures.Add(tex);
                    }
                    else
                    {
                        Destroy(tex);
                    }
                }
                catch (IOException)
                {
                    // ignoré
                }
            }

            return textures.Count > 0;
        }

        private IEnumerator CoBuildSlideTexturesWebGl(string modeId, List<Texture2D> textures)
        {
            string m = (modeId ?? "default").Trim();
            var names = new List<string>(96);
            for (int i = 1; i <= 32; i++)
            {
                names.Add("slide" + i + ".png");
                names.Add("slide" + i + ".jpg");
                names.Add("slide" + i + ".jpeg");
                names.Add("slide" + i.ToString("D2") + ".png");
                names.Add("slide" + i.ToString("D2") + ".jpg");
                names.Add("bg_" + i + ".png");
                names.Add("bg_" + i + ".jpg");
                names.Add("bg_" + i.ToString("D2") + ".png");
            }

            names.Sort(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string fileName in names)
            {
                if (!seen.Add(fileName))
                {
                    continue;
                }

                string rel = "Theme/" + m + "/" + fileName;
                string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                bool headOk = false;
                yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => headOk = b);
                if (!headOk)
                {
                    continue;
                }

                byte[] data = null;
                bool dOk = false;
                yield return WebGlStreamingPrewarm.CoHttpGetBytes(
                    u,
                    (b, ok) =>
                    {
                        data = b;
                        dOk = ok;
                    });
                if (!dOk || data == null || data.Length < 8)
                {
                    continue;
                }

                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (tex.LoadImage(data))
                {
                    tex.Apply(false, true);
                    textures.Add(tex);
                }
                else
                {
                    Destroy(tex);
                }
            }

            textures.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        }

        private IEnumerator SlideShowLoop()
        {
            int i = 0;
            while (enabled && slideTextures.Count > 0)
            {
                raw.texture = slideTextures[i % slideTextures.Count];
                raw.uvRect = new Rect(0f, 0f, 1f, 1f);
                float wait = 9f;
                float t = 0f;
                while (t < wait)
                {
                    t += Time.deltaTime;
                    float k = t / wait;
                    float scale = 1f + 0.018f * Mathf.Sin(k * Mathf.PI);
                    raw.rectTransform.localScale = Vector3.one * scale;
                    yield return null;
                }

                i++;
            }
        }

        private static Texture2D BuildSolidTexture(Color col)
        {
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.SetPixels(new[] { col, col, col, col });
            tex.Apply(false, true);
            return tex;
        }
    }
}
