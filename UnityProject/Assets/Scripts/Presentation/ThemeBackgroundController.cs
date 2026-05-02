using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        [SerializeField] private bool muteVideoSound = false;
        [SerializeField] private int renderWidth = 1920;
        [SerializeField] private int renderHeight = 1080;
        [SerializeField] private float videoCycleSeconds = 14f;
        [SerializeField] private bool cycleVirtualThemes = true;
        [SerializeField] private float virtualThemeCycleSeconds = 14f;

        private RawImage raw;
        private VideoPlayer video;
        private RenderTexture rt;
        private Coroutine slideCo;
        private readonly List<Texture2D> slideTextures = new List<Texture2D>();
        private SyntheticVideoBackground synthetic;
        private VirtualShowStage3D virtual3D;
        private string activeModeId = "";
        private Coroutine videoCycleCo;
        private Coroutine virtualCycleCo;
        private Coroutine alternateMixCo;
        [SerializeField] private float minVideoSwitchSeconds = 6f;
        [SerializeField] private float minVirtualSwitchSeconds = 10f;
        private static readonly Dictionary<string, int> VideoCycleIndexByMode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private string debugVideoUrl = "—";
        private int debugVideoIndex;
        private int debugCandidateCount;
        private string debugPhase = "idle";

        public string DebugActiveModeId => activeModeId;
        public string DebugVideoUrl => debugVideoUrl;
        public int DebugVideoIndex => debugVideoIndex;
        public int DebugCandidateCount => debugCandidateCount;
        public string DebugPhase => debugPhase;

        public static void ResetRotationDebugState()
        {
            VideoCycleIndexByMode.Clear();
        }
        private static readonly string[] VirtualCycleModes =
        {
            "quiz", "quiz_alt",
            "semantic", "semantic_alt",
            "word-scramble", "word-scramble_alt",
            "crossword-lite", "crossword-lite_alt",
            "blind-test", "blind-test_alt",
            "mystery-word", "mystery-word_alt",
            "memory", "memory_alt",
            "speed-chrono", "speed-chrono_alt",
            "image-guess", "image-guess_alt"
        };

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

            var validCandidates = new List<string>();
            yield return CoGatherVideoCandidates(id, validCandidates);
            validCandidates = validCandidates
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            debugCandidateCount = validCandidates.Count;

            bool hasVideo = validCandidates.Count > 0;
            bool use3D = PresentationConfig.UseVirtual3DShowStage && virtual3D != null;
            // Défaut 1 : alterner vidéos Theme + plateau 3D (sinon les seuls MP4 masquent le 3D — plainte fréquente).
            bool mix3DWithVideo = PlayerPrefs.GetInt(PresentationConfig.PrefsMix3DWithVideo, 1) != 0;

            // Priorité aux vidéos Theme / remote : avec mix=1 on enchaîne aussi le 3D (voir CoAlternateVideoAndVirtual3D).
            if (hasVideo)
            {
                if (use3D && mix3DWithVideo)
                {
                    debugPhase = "mix-3d-video";
                    alternateMixCo = StartCoroutine(CoAlternateVideoAndVirtual3D(id, validCandidates));
                    yield break;
                }

                // Index de rotation géré uniquement dans CoCycleVideos (évite de sauter urls[0] au premier passage).
                int idx = 0;
                if (VideoCycleIndexByMode.TryGetValue(id, out int prev))
                {
                    idx = prev % validCandidates.Count;
                }

                debugVideoIndex = idx;
                if (validCandidates.Count == 1)
                {
                    VideoCycleIndexByMode[id] = (idx + 1) % validCandidates.Count;
                    debugPhase = "video-loop";
                    TryPlayVideoUrl(validCandidates[0], muteVideoSound, true);
                }
                else
                {
                    debugPhase = "video-cycle";
                    videoCycleCo = StartCoroutine(CoCycleVideos(id, validCandidates));
                }

                yield break;
            }

            if (use3D)
            {
                debugPhase = "3d-only";
                yield return CoStartSyntheticOrSlides(id);
                yield break;
            }

            yield return CoStartSyntheticOrSlides(id);
        }

        private IEnumerator CoGatherVideoCandidates(string id, List<string> validCandidates)
        {
            validCandidates.Clear();
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
                    validCandidates.Add(urlBg);
                }
            }

            IReadOnlyList<string> catalogPaths = ThemeModeCatalog.BuildLocalBackgroundCandidates(id);
            foreach (string path in catalogPaths)
            {
                if (StreamingAssetsUrl.IsWebGlData)
                {
                    string u = StreamingAssetsUrl.ToRequestUrl(path);
                    bool ok = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                    if (ok)
                    {
                        validCandidates.Add(u);
                    }
                }
                else
                {
                    if (File.Exists(path))
                    {
                        validCandidates.Add(StreamingAssetsUrl.ToRequestUrl(path));
                    }
                }
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // Beaucoup de serveurs de dev (npm, etc.) répondent mal à HEAD/Range : liste vide → on tente quand même les URLs du catalogue.
            if (StreamingAssetsUrl.IsWebGlData && validCandidates.Count == 0)
            {
                foreach (string path in catalogPaths)
                {
                    string u = StreamingAssetsUrl.ToRequestUrl(path);
                    if (!string.IsNullOrEmpty(u))
                    {
                        validCandidates.Add(u);
                    }
                }

                List<string> uq = validCandidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                validCandidates.Clear();
                validCandidates.AddRange(uq);
            }
#endif
        }

        /// <summary>
        /// Alterne plateau 3D (variantes <c>mode</c> / <c>mode_alt</c>) et chaque vidéo Theme, en boucle.
        /// </summary>
        private IEnumerator CoAlternateVideoAndVirtual3D(string modeId, List<string> urls)
        {
            if (urls == null || urls.Count == 0 || virtual3D == null || raw == null)
            {
                yield break;
            }

            TryGetVirtualThemeCycleIndices(modeId, out int self3dIdx, out int partner3dIdx);
            bool alternate3DVariant = false;

            bool phase3D = true;
            while (!string.IsNullOrEmpty(activeModeId) && string.Equals(activeModeId, modeId, StringComparison.Ordinal))
            {
                if (phase3D)
                {
                    debugPhase = "mix:3d";
                    ReleaseVideoPlayerAndRenderTexture();
                    synthetic?.Stop();
                    string mode3d = modeId;
                    if (self3dIdx >= 0 && partner3dIdx >= 0 && self3dIdx != partner3dIdx)
                    {
                        int pick = alternate3DVariant ? partner3dIdx : self3dIdx;
                        mode3d = VirtualCycleModes[pick];
                        alternate3DVariant = !alternate3DVariant;
                    }

                    virtual3D.RebuildForMode(mode3d, raw);
                    float wait3d = Mathf.Max(minVirtualSwitchSeconds, virtualThemeCycleSeconds);
                    yield return new WaitForSecondsRealtime(wait3d);
                    if (!string.Equals(activeModeId, modeId, StringComparison.Ordinal))
                    {
                        yield break;
                    }

                    virtual3D.Teardown();
                    phase3D = false;
                }
                else
                {
                    int idx = 0;
                    if (VideoCycleIndexByMode.TryGetValue(modeId, out int known))
                    {
                        idx = Mathf.Abs(known) % urls.Count;
                    }

                    TryPlayVideoUrl(urls[idx], muteVideoSound, true);
                    VideoCycleIndexByMode[modeId] = (idx + 1) % urls.Count;
                    debugVideoIndex = idx;
                    debugPhase = "mix:video";
                    float waitV = Mathf.Max(minVideoSwitchSeconds, videoCycleSeconds);
                    yield return new WaitForSecondsRealtime(waitV);
                    if (!string.Equals(activeModeId, modeId, StringComparison.Ordinal))
                    {
                        yield break;
                    }

                    ReleaseVideoPlayerAndRenderTexture();
                    phase3D = true;
                }
            }
        }

        private void ReleaseVideoPlayerAndRenderTexture()
        {
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
            debugVideoUrl = "—";
        }

        /// <summary>Sans fichier vidéo : plateau 3D (option) ; sinon slides ; sinon « clip » 2D procédural.</summary>
        private IEnumerator CoStartSyntheticOrSlides(string id)
        {
            if (PresentationConfig.UseVirtual3DShowStage && virtual3D != null)
            {
                debugPhase = "3d-cycle";
                synthetic?.Stop();
                virtual3D.RebuildForMode(id, raw);
                if (cycleVirtualThemes)
                {
                    virtualCycleCo = StartCoroutine(CoCycleVirtualThemes(id));
                }
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
            debugPhase = "synthetic";
        }

        private void StopEverything()
        {
            if (alternateMixCo != null)
            {
                StopCoroutine(alternateMixCo);
                alternateMixCo = null;
            }

            if (virtual3D != null)
            {
                virtual3D.Teardown();
            }

            if (slideCo != null)
            {
                StopCoroutine(slideCo);
                slideCo = null;
            }

            if (videoCycleCo != null)
            {
                StopCoroutine(videoCycleCo);
                videoCycleCo = null;
            }

            if (virtualCycleCo != null)
            {
                StopCoroutine(virtualCycleCo);
                virtualCycleCo = null;
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

            ReleaseVideoPlayerAndRenderTexture();
        }

        private void OnDestroy()
        {
            StopEverything();
        }

        private void TryPlayVideo(string fileUrl)
        {
            TryPlayVideoUrl(fileUrl, muteVideoSound, true);
        }

        private void TryPlayVideoUrl(string url, bool mute, bool loop)
        {
            debugVideoUrl = string.IsNullOrEmpty(url) ? "—" : Path.GetFileName(url);
            rt = new RenderTexture(renderWidth, renderHeight, 0, RenderTextureFormat.ARGB32);
            rt.Create();
            raw.texture = rt;
            raw.color = Color.white;
            raw.rectTransform.localScale = Vector3.one;

            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = loop;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = rt;
            video.url = url;
            // Stabilise le runtime (évite AudioSampleProvider buffer overflow en continu).
            // L'audio de fond est géré par ThemeMusicPlayer/GameSfxHub.
            video.audioOutputMode = VideoAudioOutputMode.None;

            video.prepareCompleted += OnVideoPrepared;
            video.errorReceived += OnVideoError;
            video.Prepare();
        }

        private IEnumerator CoCycleVideos(string modeId, List<string> urls)
        {
            if (urls == null || urls.Count == 0)
            {
                yield break;
            }

            while (!string.IsNullOrEmpty(activeModeId) && string.Equals(activeModeId, modeId, StringComparison.Ordinal))
            {
                int idx = 0;
                if (VideoCycleIndexByMode.TryGetValue(modeId, out int known))
                {
                    idx = Mathf.Abs(known) % urls.Count;
                }

                // Boucler chaque clip jusqu’au passage suivant (évite écran noir si la vidéo est plus courte que l’intervalle).
                TryPlayVideoUrl(urls[idx], muteVideoSound, true);
                VideoCycleIndexByMode[modeId] = (idx + 1) % urls.Count;
                debugVideoIndex = idx;
                debugPhase = "video-cycle";
                float wait = Mathf.Max(minVideoSwitchSeconds, videoCycleSeconds);
                yield return new WaitForSecondsRealtime(wait);
                if (!string.Equals(activeModeId, modeId, StringComparison.Ordinal))
                {
                    yield break;
                }

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
        }

        private IEnumerator CoCycleVirtualThemes(string modeId)
        {
            // Alterne uniquement ce mode et son voisin *_alt (pas toute la liste des mini-jeux).
            if (!TryGetVirtualThemeCycleIndices(modeId, out int selfIdx, out int partnerIdx))
            {
                yield break;
            }

            bool usePartner = false;
            while (!string.IsNullOrEmpty(activeModeId) && string.Equals(activeModeId, modeId, StringComparison.Ordinal))
            {
                yield return new WaitForSecondsRealtime(Mathf.Max(10f, virtualThemeCycleSeconds));
                if (virtual3D == null || raw == null || !string.Equals(activeModeId, modeId, StringComparison.Ordinal))
                {
                    yield break;
                }

                if (selfIdx == partnerIdx)
                {
                    continue;
                }

                usePartner = !usePartner;
                int pick = usePartner ? partnerIdx : selfIdx;
                virtual3D.RebuildForMode(VirtualCycleModes[pick], raw);
            }
        }

        /// <summary>Couple (mode, mode_alt) dans <see cref="VirtualCycleModes"/> ; sinon le même index deux fois.</summary>
        private static bool TryGetVirtualThemeCycleIndices(string modeId, out int selfIdx, out int partnerIdx)
        {
            selfIdx = -1;
            partnerIdx = -1;
            for (int i = 0; i < VirtualCycleModes.Length; i++)
            {
                if (!string.Equals(VirtualCycleModes[i], modeId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                selfIdx = i;
                if (i % 2 == 0 && i + 1 < VirtualCycleModes.Length)
                {
                    partnerIdx = i + 1;
                }
                else if (i % 2 == 1)
                {
                    partnerIdx = i - 1;
                }
                else
                {
                    partnerIdx = i;
                }

                return true;
            }

            return false;
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
