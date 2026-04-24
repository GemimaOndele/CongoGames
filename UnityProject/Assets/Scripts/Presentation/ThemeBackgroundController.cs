using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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
                    return;
                }
            }

            IReadOnlyList<string> candidates = ThemeModeCatalog.BuildLocalBackgroundCandidates(id);
            foreach (string path in candidates)
            {
                if (File.Exists(path))
                {
                    TryPlayVideo(path);
                    return;
                }
            }

            StartSyntheticOrSlides(id);
        }

        /// <summary>Sans fichier vidéo : plateau 3D (option) ; sinon slides ; sinon « clip » 2D procédural.</summary>
        private void StartSyntheticOrSlides(string id)
        {
            if (PresentationConfig.UseVirtual3DShowStage && virtual3D != null)
            {
                synthetic?.Stop();
                virtual3D.RebuildForMode(id, raw);
                return;
            }

            virtual3D?.Teardown();
            string root = Path.Combine(Application.streamingAssetsPath, "Theme");
            string useSlidesFlag = Path.Combine(root, id, "use_slides.flag");
            string useSlidesGlobal = Path.Combine(root, "use_slides.flag");
            if (File.Exists(useSlidesFlag) || File.Exists(useSlidesGlobal))
            {
                if (TryBuildSlideTextures(id, out List<Texture2D> slides))
                {
                    slideTextures.AddRange(slides);
                    raw.texture = slideTextures[0];
                    raw.color = Color.white;
                    raw.uvRect = new Rect(0f, 0f, 1f, 1f);
                    raw.rectTransform.localScale = Vector3.one;
                    slideCo = StartCoroutine(SlideShowLoop());
                    return;
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

        private void TryPlayVideo(string path)
        {
            TryPlayVideoUrl(FileUrl(path), muteVideoSound);
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
            StartSyntheticOrSlides(mode);
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

        private static string FileUrl(string path)
        {
            return "file:///" + Path.GetFullPath(path).Replace("\\", "/");
        }
    }
}
