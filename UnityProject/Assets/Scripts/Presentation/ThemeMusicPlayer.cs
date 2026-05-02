using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using CongoGames.Audio;
using CongoGames.Core;
using CongoGames.Network;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Musique par mode : playlist track01/track02… dans Theme/&lt;mode&gt;/, sinon music.*, puis thème global, Resources, pad procédural.
    /// </summary>
    public class ThemeMusicPlayer : MonoBehaviour
    {
        public static ThemeMusicPlayer Instance { get; private set; }
        private const string AmbiencePresetPrefKey = "cg_audio_ambience_preset"; // 0=soft, 1=aggressive

        /// <summary>Phase 0–1 sur la piste en cours (pour synchroniser légèrement l’UI avec le rythme).</summary>
        public static float NormalizedPhase01 { get; private set; }

        [SerializeField] private float volume = 0.62f;
        [Tooltip("Optionnel : bus Audio Mixer (ex. Bus_Music) pour le mix broadcast.")]
        [SerializeField] private AudioMixerGroup musicOutputGroup;
        private float duckMultiplier = 1f;
        private float chronoDuckMultiplier = 1f;
        private float blindDuckMultiplier = 1f;

        private AudioSource music;
        private Coroutine playlistRoutine;
        private readonly List<AudioClip> playlistClips = new List<AudioClip>();
        private static readonly Dictionary<string, int> PlaylistStartIndexByMode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private string debugModeId = "";
        private string debugNowPlaying = "—";
        private int debugPlaylistCount;
        private int debugRotationIndex;
        private string debugSource = "none";
        private string debugObservedNowPlaying = "";
        private float debugNowPlayingSinceUnscaled;

        public string DebugModeId => debugModeId;
        public string DebugNowPlaying => debugNowPlaying;
        public int DebugPlaylistCount => debugPlaylistCount;
        public int DebugRotationIndex => debugRotationIndex;
        public string DebugSource => debugSource;
        public float DebugNowPlayingStableSeconds => Mathf.Max(0f, Time.unscaledTime - debugNowPlayingSinceUnscaled);

        public static void ResetRotationDebugState()
        {
            PlaylistStartIndexByMode.Clear();
        }

        private enum AmbiencePreset
        {
            Soft = 0,
            Aggressive = 1
        }

        private void Awake()
        {
            Instance = this;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0f;
            if (musicOutputGroup != null)
            {
                music.outputAudioMixerGroup = musicOutputGroup;
            }

            RefreshMusicVolume();
        }

        /// <summary>Atténuation live (ex. 0,45 quand l’hôte TTS parle). 1 = niveau nominal.</summary>
        public void SetBroadcastDuckMultiplier(float linear01)
        {
            duckMultiplier = Mathf.Clamp01(linear01);
            RefreshMusicVolume();
        }

        /// <summary>Atténuation temporaire pendant le compte à rebours visuel/sonore.</summary>
        public void SetChronoDuckMultiplier(float linear01)
        {
            chronoDuckMultiplier = Mathf.Clamp01(linear01);
            RefreshMusicVolume();
        }

        /// <summary>Atténuation forte pendant l'écoute du blind test (0 = mute).</summary>
        public void SetBlindDuckMultiplier(float linear01)
        {
            blindDuckMultiplier = Mathf.Clamp01(linear01);
            RefreshMusicVolume();
        }

        private void RefreshMusicVolume()
        {
            if (music != null)
            {
                music.volume = volume * duckMultiplier;
                music.volume *= chronoDuckMultiplier;
                music.volume *= blindDuckMultiplier;
            }
        }

        private void OnDestroy()
        {
            ClearPlaylistClips();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (!string.Equals(debugObservedNowPlaying, debugNowPlaying, StringComparison.Ordinal))
            {
                debugObservedNowPlaying = debugNowPlaying ?? "";
                debugNowPlayingSinceUnscaled = Time.unscaledTime;
            }

            if (music != null && music.isPlaying && music.clip != null && music.clip.length > 0.05f)
            {
                NormalizedPhase01 = Mathf.Repeat(music.time / music.clip.length, 1f);
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            // Les navigateurs bloquent l’autoplay audio tant qu’il n’y a pas de geste : on relance au 1er clic / toucher.
            if (music == null || music.isPlaying || music.clip == null)
            {
                return;
            }

            if (GameInput.AnyPrimaryPointerDown())
            {
                music.Play();
            }
#endif
        }

        public void ApplyGameMode(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId.Trim().ToLowerInvariant();
            debugModeId = id;
            debugNowPlaying = "chargement…";
            debugPlaylistCount = 0;
            debugRotationIndex = 0;
            debugSource = "mode-switch";
            playlistRoutine = null;
            StopAllCoroutines();
            ClearPlaylistClips();
            if (music.isPlaying)
            {
                music.Stop();
            }

            music.clip = null;
            music.loop = true;
            StartCoroutine(LoadMusicForMode(id));
        }

        /// <summary>
        /// Arrête toute BGM chargée depuis StreamingAssets quand <see cref="CongoGames.Audio.GameAudioManager"/> joue des clips dédiés.
        /// </summary>
        public void SuppressStreamingBgmForExternalManager()
        {
            playlistRoutine = null;
            StopAllCoroutines();
            ClearPlaylistClips();
            if (music != null)
            {
                if (music.isPlaying)
                {
                    music.Stop();
                }

                music.clip = null;
            }

            debugNowPlaying = "externe (GameAudioManager)";
            debugSource = "external-bgm";
        }

        public void SetAmbiencePreset(bool aggressive)
        {
            PlayerPrefs.SetInt(AmbiencePresetPrefKey, aggressive ? 1 : 0);
            PlayerPrefs.Save();
            string currentMode = GameModeManager.Instance != null ? GameModeManager.Instance.ActiveModeId : "quiz";
            ApplyGameMode(currentMode);
        }

        private void ClearPlaylistClips()
        {
            foreach (AudioClip c in playlistClips)
            {
                if (c != null)
                {
                    Destroy(c);
                }
            }

            playlistClips.Clear();
        }

        private IEnumerator LoadMusicForMode(string modeId)
        {
            yield return WebGlStreamingPrewarm.CoRunOnce();
            yield return TikTokGiftModeRegistry.CoPrewarmIfWebGl();
            yield return WebAudioGestureGate.CoWaitForUnlock();
            // Blind Test / Image Guess: la musique de manche est pilotée
            // par MiniGamePanelContent (indices audio contextualisés).
            // On évite toute musique de fond concurrente dans ces modes.
            if (string.Equals(modeId, "image-guess", StringComparison.OrdinalIgnoreCase)
                || string.Equals(modeId, "blind-test", StringComparison.OrdinalIgnoreCase))
            {
                debugNowPlaying = "piloté par GameSfxHub";
                debugSource = "mode-excluded";
                yield break;
            }
            RemoteModeMediaEntry remote = RemoteThemeMediaConfig.Resolve(modeId);
            string bottomU = (remote.bottomVideoUrl ?? "").Trim();
            bool bottomProvidesAudio = false; // Vidéo sans audio pour stabilité; ambiance pilotée par ThemeMusicPlayer.
            AmbiencePreset preset = GetAmbiencePresetFromPrefs();
            string[] preferredPrefixes = GetPreferredAmbiencePrefixes(preset);

            if (!bottomProvidesAudio && !string.IsNullOrWhiteSpace(remote.musicUrl))
            {
                string mUrl = remote.musicUrl.Trim();
                if (StreamingMediaUrlPolicy.IsNonStreamableContentPageUrl(mUrl))
                {
                    StreamingMediaUrlPolicy.LogOnceRejected("Thème (musicUrl)", mUrl);
                }
                else
                {
                    AudioClip httpClip = null;
                    yield return DownloadClipFromHttp(mUrl, c => httpClip = c);
                    if (httpClip != null)
                    {
                        debugNowPlaying = httpClip.name;
                        debugSource = "remote-music-url";
                        yield return CoPlayWithIntro(httpClip, true);
                        yield break;
                    }
                }
            }

            if (bottomProvidesAudio)
            {
                yield break;
            }

            string root = Path.Combine(Application.streamingAssetsPath, "Theme");
            string folder = Path.Combine(root, modeId);
            List<string> trackPaths = new List<string>();
            if (StreamingAssetsUrl.IsWebGlData)
            {
                yield return CoProbeTracksForWebGl(modeId, trackPaths, preferredPrefixes);
            }
            else
            {
                // Priorité 1: ambiances gameplay réelles dédiées (ambient_*, loop_*, bgm_*).
                TryAppendLocalGameplayAmbience(folder, trackPaths, preferredPrefixes);
                TryAppendLocalGameplayAmbience(Path.Combine(root, "Gameplay", modeId), trackPaths, preferredPrefixes);

                if (Directory.Exists(folder))
                {
                    foreach (string ext in new[] { "ogg", "wav", "mp3" })
                    {
                        try
                        {
                            trackPaths.AddRange(Directory.GetFiles(folder, "track*." + ext, SearchOption.TopDirectoryOnly));
                        }
                        catch (IOException)
                        {
                            // ignoré
                        }
                    }
                }

                if (string.Equals(modeId, "blind-test", StringComparison.OrdinalIgnoreCase))
                {
                    string legacyBlind = Path.Combine(root, "BlindTest");
                    if (Directory.Exists(legacyBlind))
                    {
                        foreach (string ext in new[] { "ogg", "wav", "mp3" })
                        {
                            try
                            {
                                trackPaths.AddRange(Directory.GetFiles(legacyBlind, "track*." + ext, SearchOption.TopDirectoryOnly));
                            }
                            catch (IOException)
                            {
                            }
                        }
                    }
                }

                if ((string.Equals(modeId, "blind-test", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(modeId, "image-guess", StringComparison.OrdinalIgnoreCase))
                    && trackPaths.Count < 2)
                {
                    TryAppendCongoleseRepoPlaylistFolder(trackPaths);
                }

                trackPaths = trackPaths
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                RotateStringListForMode(modeId, trackPaths);

                if ((string.Equals(modeId, "blind-test", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(modeId, "image-guess", StringComparison.OrdinalIgnoreCase))
                    && trackPaths.Count == 0 && Directory.Exists(root))
                {
                    foreach (string ext in new[] { "ogg", "wav", "mp3" })
                    {
                        try
                        {
                            trackPaths.AddRange(Directory.GetFiles(root, "track*." + ext, SearchOption.TopDirectoryOnly));
                        }
                        catch (IOException)
                        {
                            // ignoré
                        }
                    }

                    trackPaths = trackPaths
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    RotateStringListForMode(modeId, trackPaths);
                }
            }

            if (trackPaths.Count >= 2)
            {
                foreach (string path in trackPaths)
                {
                    AudioClip clip = null;
                    yield return DownloadClip(path, Path.GetFileName(path), c => clip = c);
                    if (clip != null)
                    {
                        playlistClips.Add(clip);
                    }
                }

                RotateClipListForMode(modeId, playlistClips);

                if (playlistClips.Count >= 2)
                {
                    debugPlaylistCount = playlistClips.Count;
                    debugSource = "playlist";
                    playlistRoutine = StartCoroutine(PlayPlaylistLoop());
                    yield break;
                }
            }

            if (trackPaths.Count == 1)
            {
                AudioClip one = null;
                yield return DownloadClip(trackPaths[0], Path.GetFileName(trackPaths[0]), c => one = c);
                if (one != null)
                {
                    debugNowPlaying = one.name;
                    debugPlaylistCount = 1;
                    debugSource = "single-track";
                    yield return CoPlayWithIntro(one, true);
                    yield break;
                }
            }

            string[] names = { "music.ogg", "music.wav", "music.mp3", "theme.ogg", "theme.wav" };
            foreach (string file in names)
            {
                string sub = Path.Combine(folder, file);
                if (StreamingAssetsUrl.IsWebGlData)
                {
                    string rel = "Theme/" + modeId + "/" + file;
                    string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                    bool headOk = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => headOk = b);
                    if (!headOk)
                    {
                        continue;
                    }

                    AudioClip subClip = null;
                    yield return DownloadClip(u, file, c => subClip = c);
                    if (subClip != null)
                    {
                        debugNowPlaying = subClip.name;
                        debugPlaylistCount = 1;
                        debugSource = "mode-music-file";
                        yield return CoPlayWithIntro(subClip, true);
                        yield break;
                    }
                }
                else
                {
                    if (!File.Exists(sub))
                    {
                        continue;
                    }

                    AudioClip subClip = null;
                    yield return DownloadClip(sub, file, c => subClip = c);
                    if (subClip != null)
                    {
                        debugNowPlaying = subClip.name;
                        debugPlaylistCount = 1;
                        debugSource = "mode-music-file";
                        yield return CoPlayWithIntro(subClip, true);
                        yield break;
                    }
                }
            }

            string resPath = "Audio/theme_" + SanitizeForResources(modeId);
            AudioClip res = Resources.Load<AudioClip>(resPath);
            if (res != null)
            {
                debugNowPlaying = res.name;
                debugPlaylistCount = 1;
                debugSource = "resources-mode";
                yield return CoPlayWithIntro(res, true);
                yield break;
            }

            if (modeId == "quiz" || modeId == "default")
            {
                foreach (string file in names)
                {
                    string globalPath = Path.Combine(root, file);
                    if (StreamingAssetsUrl.IsWebGlData)
                    {
                        string u = StreamingAssetsUrl.UrlForRelativePath("Theme/" + file);
                        bool headOk = false;
                        yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => headOk = b);
                        if (!headOk)
                        {
                            continue;
                        }

                        AudioClip gClip = null;
                        yield return DownloadClip(u, file, c => gClip = c);
                        if (gClip != null)
                        {
                            debugNowPlaying = gClip.name;
                            debugPlaylistCount = 1;
                            debugSource = "global-theme-file";
                            yield return CoPlayWithIntro(gClip, true);
                            yield break;
                        }
                    }
                    else
                    {
                        if (!File.Exists(globalPath))
                        {
                            continue;
                        }

                        AudioClip gClip = null;
                        yield return DownloadClip(globalPath, file, c => gClip = c);
                        if (gClip != null)
                        {
                            debugNowPlaying = gClip.name;
                            debugPlaylistCount = 1;
                            debugSource = "global-theme-file";
                            yield return CoPlayWithIntro(gClip, true);
                            yield break;
                        }
                    }
                }

                res = Resources.Load<AudioClip>("Audio/theme");
                if (res != null)
                {
                    debugNowPlaying = res.name;
                    debugPlaylistCount = 1;
                    debugSource = "resources-global";
                    yield return CoPlayWithIntro(res, true);
                    yield break;
                }
            }

            debugSource = "procedural";
            yield return CoProceduralModeLoop(modeId);
        }

        private static AmbiencePreset GetAmbiencePresetFromPrefs()
        {
            int v = PlayerPrefs.GetInt(AmbiencePresetPrefKey, 0);
            return v == 1 ? AmbiencePreset.Aggressive : AmbiencePreset.Soft;
        }

        private static string[] GetPreferredAmbiencePrefixes(AmbiencePreset preset)
        {
            if (preset == AmbiencePreset.Aggressive)
            {
                return new[]
                {
                    "ambient_live_aggressive_01",
                    "ambient_gameplay_02",
                    "ambient_gameplay_01",
                    "ambient_01",
                    "loop_01",
                    "bgm_01",
                    "music",
                    "theme"
                };
            }

            return new[]
            {
                "ambient_live_soft_01",
                "ambient_gameplay_01",
                "ambient_gameplay_02",
                "ambient_01",
                "loop_01",
                "bgm_01",
                "music",
                "theme"
            };
        }

        private static void TryAppendLocalGameplayAmbience(string folder, List<string> trackPaths, string[] preferredPrefixes)
        {
            if (trackPaths == null || string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                return;
            }

            string[] patterns = preferredPrefixes != null && preferredPrefixes.Length > 0
                ? preferredPrefixes
                : new[] { "ambient_01", "loop_01", "bgm_01", "music", "theme" };
            foreach (string ext in new[] { "ogg", "wav", "mp3" })
            {
                foreach (string p in patterns)
                {
                    try
                    {
                        string exact = Path.Combine(folder, p + "." + ext);
                        if (File.Exists(exact))
                        {
                            trackPaths.Add(exact);
                        }
                    }
                    catch (IOException)
                    {
                        // ignoré
                    }
                }
            }

            // Fallback large: toute ambiance gameplay du dossier (évite "toujours le même son").
            foreach (string ext in new[] { ".ogg", ".wav", ".mp3" })
            {
                try
                {
                    foreach (string f in Directory.GetFiles(folder, "*" + ext, SearchOption.TopDirectoryOnly))
                    {
                        string n = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                        if (n.Contains("ambient") || n.Contains("loop") || n.Contains("bgm") || n.Contains("music") || n.Contains("theme"))
                        {
                            trackPaths.Add(f);
                        }
                    }
                }
                catch (IOException)
                {
                    // ignoré
                }
            }
        }

        private static void RotateStringListForMode(string modeId, List<string> list)
        {
            if (list == null || list.Count < 2) return;
            int idx = 0;
            if (PlaylistStartIndexByMode.TryGetValue(modeId ?? "", out int known))
            {
                idx = Mathf.Abs(known) % list.Count;
            }

            if (idx > 0)
            {
                var copy = new List<string>(list);
                list.Clear();
                for (int i = 0; i < copy.Count; i++)
                {
                    list.Add(copy[(idx + i) % copy.Count]);
                }
            }

            PlaylistStartIndexByMode[modeId ?? "default"] = (idx + 1) % list.Count;
        }

        private static void RotateClipListForMode(string modeId, List<AudioClip> list)
        {
            if (list == null || list.Count < 2) return;
            int idx = 0;
            if (PlaylistStartIndexByMode.TryGetValue(modeId ?? "", out int known))
            {
                idx = Mathf.Abs(known) % list.Count;
            }

            if (idx > 0)
            {
                var copy = new List<AudioClip>(list);
                list.Clear();
                for (int i = 0; i < copy.Count; i++)
                {
                    list.Add(copy[(idx + i) % copy.Count]);
                }
            }

            PlaylistStartIndexByMode[modeId ?? "default"] = (idx + 1) % list.Count;
        }

        private IEnumerator CoPlayWithIntro(AudioClip main, bool loop)
        {
            if (main == null) yield break;
            AudioClip intro = ProceduralClips.BuildTamTamIntro();
            music.clip = intro;
            music.loop = false;
            RefreshMusicVolume();
            music.Play();
            yield return new WaitWhile(() => music.isPlaying);
            Destroy(intro);
            music.clip = main;
            music.loop = loop;
            music.Play();
            debugNowPlaying = main.name;
        }

        private IEnumerator CoProceduralModeLoop(string modeId)
        {
            AudioClip intro = ProceduralClips.BuildTamTamIntro();
            music.clip = intro;
            music.loop = false;
            RefreshMusicVolume();
            music.Play();
            yield return new WaitWhile(() => music.isPlaying);
            Destroy(intro);
            int phase = 0;
            while (enabled)
            {
                AudioClip pad = ProceduralClips.BuildAmbientPadForMode((modeId ?? "x") + "|p" + phase);
                music.clip = pad;
                music.loop = true;
                music.Play();
                debugNowPlaying = "procedural:" + (modeId ?? "default") + "#" + phase;
                debugRotationIndex = phase;
                float len = pad != null && pad.length > 0.2f ? pad.length : 6f;
                len = Mathf.Clamp(len, 5f, 12f);
                yield return new WaitForSeconds(len * 0.96f);
                music.Stop();
                Destroy(pad);
                phase++;
            }
        }

        /// <summary>Durée max d’écoute par piste en boucle (le morceau complet peut être plus long sur disque).</summary>
        private const float PlaylistSegmentMaxSeconds = 60f;

        private IEnumerator PlayPlaylistLoop()
        {
            bool needIntro = true;
            int i = 0;
            while (enabled && playlistClips.Count > 0)
            {
                AudioClip c = playlistClips[i % playlistClips.Count];
                debugPlaylistCount = playlistClips.Count;
                debugRotationIndex = i % playlistClips.Count;
                debugNowPlaying = c != null ? c.name : "—";
                if (needIntro)
                {
                    needIntro = false;
                    yield return CoPlayWithIntro(c, playlistClips.Count == 1);
                }
                else
                {
                    music.clip = c;
                    music.loop = playlistClips.Count == 1;
                    music.Play();
                }

                float full = c != null && c.length > 0.1f ? c.length : 120f;
                float wait = Mathf.Min(full, PlaylistSegmentMaxSeconds);
                yield return new WaitForSeconds(wait * 0.995f);
                i++;
            }
        }

        private IEnumerator DownloadClip(string fullPathOrUrl, string fileName, Action<AudioClip> assign)
        {
            string uri = (fullPathOrUrl != null && fullPathOrUrl.IndexOf("://", StringComparison.Ordinal) >= 0)
                ? fullPathOrUrl
                : StreamingAssetsUrl.ToRequestUrl(fullPathOrUrl);
            AudioType audioType = GuessAudioType(fileName);
            yield return StartCoroutine(DownloadClipUriCo(uri, audioType, assign));
        }

        private IEnumerator DownloadClipFromHttp(string absoluteUrl, Action<AudioClip> assign)
        {
            AudioType audioType = GuessAudioType(absoluteUrl);
            yield return StartCoroutine(DownloadClipUriCo(absoluteUrl, audioType, assign));
        }

        private IEnumerator DownloadClipUriCo(string uri, AudioType audioType, Action<AudioClip> assign)
        {
            if (string.IsNullOrEmpty(uri))
            {
                yield break;
            }

            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            req.timeout = 90;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            long code = req.responseCode;
            if (code > 0L && (code < 200L || code > 299L))
            {
                yield break;
            }

            if (req.downloadedBytes < 32u)
            {
                yield break;
            }

            if (req.downloadHandler is not DownloadHandlerAudioClip dha)
            {
                yield break;
            }

            byte[] raw = dha.data;
            if (raw == null || !LooksLikeAudioPayload(raw, audioType))
            {
                // Souvent page HTML/JSON d’erreur (HEAD ok, GET réinitialisé, mauvais type MIME…).
                yield break;
            }

            AudioClip clip;
            try
            {
                clip = DownloadHandlerAudioClip.GetContent(req);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("ThemeMusicPlayer: GetContent a échoué (" + uri + "): " + ex.Message);
                yield break;
            }

            if (clip == null || clip.length < 0.02f)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }

                yield break;
            }

            assign?.Invoke(clip);
        }

        private static bool LooksLikeAudioPayload(byte[] data, AudioType t)
        {
            if (data == null || data.Length < 4)
            {
                return false;
            }

            // Réponses HTML (reverse proxy, 404, etc.)
            if (data[0] == (byte)'<' && data.Length > 4)
            {
                if (data[1] == (byte)'!' || (data[1] == (byte)'h' && data[2] == (byte)'t' && data[3] == (byte)'m'))
                {
                    return false;
                }
            }

            switch (t)
            {
                case AudioType.MPEG:
                    if (data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
                    {
                        return true; // ID3v2
                    }

                    for (int i = 0; i <= data.Length - 2 && i < 12; i++)
                    {
                        if (data[i] == 0xFF && (data[i + 1] & 0xE0) == 0xE0)
                        {
                            return true; // trame MP3
                        }
                    }

                    return false;
                case AudioType.OGGVORBIS:
                    return data[0] == 0x4F && data[1] == 0x67 && data[2] == 0x67 && data[3] == 0x53;
                case AudioType.WAV:
                    return data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46;
                default:
                    return data.Length > 32;
            }
        }

        private static AudioType GuessAudioType(string pathOrUrl)
        {
            string p = pathOrUrl ?? "";
            if (p.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) || p.Contains(".ogg?", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.OGGVORBIS;
            }

            if (p.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) || p.Contains(".mp3?", StringComparison.OrdinalIgnoreCase))
            {
                return AudioType.MPEG;
            }

            return AudioType.WAV;
        }

        private IEnumerator CoProbeTracksForWebGl(string modeId, List<string> trackPaths, string[] preferredPrefixes)
        {
            string m = (modeId ?? "default").Trim();
            string[] gameplayPrefixes = preferredPrefixes != null && preferredPrefixes.Length > 0
                ? preferredPrefixes
                : new[] { "ambient_gameplay_01", "ambient_gameplay_02", "ambient_01", "loop_01", "bgm_01", "music", "theme" };
            foreach (string ext in new[] { "mp3", "ogg", "wav" })
            {
                for (int i = 0; i < gameplayPrefixes.Length; i++)
                {
                    string relA = "Theme/" + m + "/" + gameplayPrefixes[i] + "." + ext;
                    string relB = "Theme/Gameplay/" + m + "/" + gameplayPrefixes[i] + "." + ext;
                    string ua = StreamingAssetsUrl.UrlForRelativePath(relA);
                    string ub = StreamingAssetsUrl.UrlForRelativePath(relB);
                    bool oka = false;
                    bool okb = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(ua, b => oka = b);
                    if (oka) trackPaths.Add(ua);
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(ub, b => okb = b);
                    if (okb) trackPaths.Add(ub);
                }
            }

            for (int n = 1; n <= 36; n++)
            {
                string prefix = "track" + n.ToString("D2");
                foreach (string ext in new[] { "mp3", "ogg", "wav" })
                {
                    string rel = "Theme/" + m + "/" + prefix + "." + ext;
                    string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                    bool ok = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                    if (ok)
                    {
                        trackPaths.Add(u);
                    }
                }
            }

            for (int n = 1; n <= 36; n++)
            {
                string prefix = "track" + n.ToString("D2");
                foreach (string ext in new[] { "mp3", "ogg", "wav" })
                {
                    string rel = "Theme/Gameplay/" + m + "/" + prefix + "." + ext;
                    string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                    bool ok = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                    if (ok)
                    {
                        trackPaths.Add(u);
                    }
                }
            }

            if (string.Equals(m, "blind-test", StringComparison.OrdinalIgnoreCase))
            {
                for (int n = 1; n <= 36; n++)
                {
                    string prefix = "track" + n.ToString("D2");
                    foreach (string ext in new[] { "mp3", "ogg", "wav" })
                    {
                        string rel = "Theme/BlindTest/" + prefix + "." + ext;
                        string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                        bool ok = false;
                        yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                        if (ok)
                        {
                            trackPaths.Add(u);
                        }
                    }
                }
            }

            // Theme/playlist/ : réservé blind-test + image-guess uniquement (pas de fond général mini-jeux).
            if (string.Equals(m, "blind-test", StringComparison.OrdinalIgnoreCase)
                || string.Equals(m, "image-guess", StringComparison.OrdinalIgnoreCase))
            {
                for (int n = 1; n <= 36; n++)
                {
                    string prefix = "track" + n.ToString("D2");
                    foreach (string ext in new[] { "mp3", "ogg", "wav" })
                    {
                        string rel = "Theme/playlist/" + prefix + "." + ext;
                        string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                        bool ok = false;
                        yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                        if (ok)
                        {
                            trackPaths.Add(u);
                        }
                    }
                }
            }

            if (trackPaths.Count == 0)
            {
                AppendOptimisticWebGlThemeAudioUrls(m, trackPaths);
            }

            DeduplicateAndRotateWebGlTrackList(m, trackPaths);
            if (trackPaths.Count > 0)
            {
                yield break;
            }

            for (int n = 1; n <= 36; n++)
            {
                string prefix = "track" + n.ToString("D2");
                foreach (string ext in new[] { "mp3", "ogg", "wav" })
                {
                    string rel = "Theme/" + prefix + "." + ext;
                    string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                    bool ok = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                    if (ok)
                    {
                        trackPaths.Add(u);
                    }
                }
            }

            if (trackPaths.Count == 0)
            {
                AppendOptimisticWebGlThemeAudioUrls(m, trackPaths);
            }

            DeduplicateAndRotateWebGlTrackList(m, trackPaths);
        }

        /// <summary>
        /// WebGL : si HEAD a tout échoué, on injecte des chemins « canoniques » (même logique que l’éditeur) pour tenter le GET audio au chargement.
        /// </summary>
        private static void AppendOptimisticWebGlThemeAudioUrls(string modeId, List<string> trackPaths)
        {
            if (trackPaths == null)
            {
                return;
            }

            string m = (modeId ?? "quiz").Trim();
            string[] rels =
            {
                "Theme/" + m + "/ambient_gameplay_02.wav",
                "Theme/" + m + "/ambient_gameplay_01.wav",
                "Theme/Gameplay/" + m + "/ambient_gameplay_02.wav",
                "Theme/Gameplay/" + m + "/ambient_gameplay_01.wav",
                "Theme/" + m + "/track01.ogg",
                "Theme/" + m + "/track01.mp3",
                "Theme/" + m + "/track02.ogg",
                "Theme/" + m + "/track02.mp3",
                "Theme/" + m + "/music.ogg",
                "Theme/" + m + "/music.mp3"
            };

            for (int i = 0; i < rels.Length; i++)
            {
                trackPaths.Add(StreamingAssetsUrl.UrlForRelativePath(rels[i]));
            }
        }

        /// <summary>
        /// WebGL : pas de mélange aléatoire (casualise la détection Éditeur vs navigateur) ; rotation stable par mode.
        /// </summary>
        private static void DeduplicateAndRotateWebGlTrackList(string modeId, List<string> trackPaths)
        {
            if (trackPaths == null || trackPaths.Count == 0)
            {
                return;
            }

            List<string> uq = trackPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            RotateStringListForMode(modeId, uq);
            trackPaths.Clear();
            trackPaths.AddRange(uq);
        }

        /// <summary>
        /// Dossier <c>Congogame/playlist</c> à la racine du dépôt (hors UnityProject), éditeur / binaires seulement.
        /// WebGL : mêmes fichiers attendus sous <c>StreamingAssets/Theme/playlist/</c> (track01.mp3…), voir <see cref="CoProbeTracksForWebGl"/>.
        /// </summary>
        private static void TryAppendCongoleseRepoPlaylistFolder(List<string> trackPaths)
        {
            if (StreamingAssetsUrl.IsWebGlData)
            {
                return;
            }

            string congo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "playlist"));
            if (!Directory.Exists(congo))
            {
                return;
            }

            foreach (string ext in new[] { "ogg", "wav", "mp3" })
            {
                try
                {
                    trackPaths.AddRange(Directory.GetFiles(congo, "*." + ext, SearchOption.TopDirectoryOnly));
                }
                catch (IOException)
                {
                }
            }
        }

        private static string SanitizeForResources(string modeId)
        {
            string s = "";
            foreach (char c in modeId)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    s += c;
                }
            }

            return s.Length > 0 ? s : "default";
        }

    }
}
