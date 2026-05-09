using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        private const string JeuxFirstClipSessionPrefKey = "cg_jeux_first_clip_session";

        /// <summary>Phase 0–1 sur la piste en cours (pour synchroniser légèrement l’UI avec le rythme).</summary>
        public static float NormalizedPhase01 { get; private set; }

        [SerializeField] private float volume = 0.88f;
        [Tooltip("Optionnel : bus Audio Mixer (ex. Bus_Music) pour le mix broadcast.")]
        [SerializeField] private AudioMixerGroup musicOutputGroup;
        private float duckMultiplier = 1f;
        private float chronoDuckMultiplier = 1f;
        private float blindDuckMultiplier = 1f;

        private AudioSource music;
        private Coroutine playlistRoutine;
        private readonly List<AudioClip> playlistClips = new List<AudioClip>();
        /// <summary>Dossier Jeux enfant : segments d’1 min, ordre mélangé, décalage à chaque grand tour.</summary>
        private bool jeuxSegmentScheduleActive;
        private float activeSegmentStartTime;
        private float activeSegmentDuration;
        private AudioClip activeSegmentClip;
        private readonly Dictionary<string, float> jeuxPlaybackCursorByClip = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private static JeuxStartRule[] jeuxStartRules;
        private static bool jeuxStartRulesLoaded;
        private static float jeuxDefaultStartPercent = 0.5f;
        /// <summary>UnityEngine.Random est souvent peu « mélangé » au 1er tirage ; on utilise System.Random re-grainé.</summary>
        private System.Random playlistRng = new System.Random();
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

        /// <summary>
        /// Si le dossier Jeux_musique_playlist contient des pistes, la BGM Theme segmentée joue en priorité :
        /// <see cref="GameAudioManager"/> ne remplace pas par les clips Resources.
        /// </summary>
        public static bool ShouldDeferDedicatedBgmForJeuxPlaylist()
        {
            return HasJeuxMusiqueAudioOnDisk();
        }

        /// <summary>
        /// Racines possibles pour <c>Jeux_musique_playlist</c> : StreamingAssets, racine dépôt (éditeur),
        /// et à côté du <c>.exe</c> en build (même disposition que si tu copies le dossier depuis le dépôt).
        /// </summary>
        private static void CollectJeuxPlaylistRootFolders(List<string> sink)
        {
            if (sink == null)
            {
                return;
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddCandidate(string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                try
                {
                    string full = Path.GetFullPath(path);
                    if (seen.Add(full))
                    {
                        sink.Add(full);
                    }
                }
                catch (IOException)
                {
                    if (seen.Add(path))
                    {
                        sink.Add(path);
                    }
                }
            }

            AddCandidate(Path.Combine(Application.streamingAssetsPath, "Theme", "Jeux_musique_playlist"));

#if UNITY_EDITOR
            try
            {
                AddCandidate(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Jeux_musique_playlist")));
            }
            catch (IOException)
            {
                // ignoré
            }
#endif

            try
            {
                string gameFolder = Directory.GetParent(Application.dataPath)?.FullName;
                if (!string.IsNullOrEmpty(gameFolder))
                {
                    AddCandidate(Path.Combine(gameFolder, "Jeux_musique_playlist"));
                }
            }
            catch (IOException)
            {
                // ignoré
            }
        }

        private static bool HasJeuxMusiqueAudioOnDisk()
        {
            var roots = new List<string>();
            CollectJeuxPlaylistRootFolders(roots);
            foreach (string dir in roots)
            {
                if (Directory.Exists(dir) && HasAudioFilesInFolder(dir))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAudioFilesInFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return false;
            }

            foreach (string ext in new[] { "mp3", "ogg", "wav" })
            {
                try
                {
                    if (Directory.GetFiles(folder, "*." + ext, SearchOption.TopDirectoryOnly).Length > 0)
                    {
                        return true;
                    }
                }
                catch (IOException)
                {
                    // ignoré
                }
            }

            return false;
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
            ReseedPlaylistRandom();
        }

        private void ReseedPlaylistRandom()
        {
            unchecked
            {
                int s = Environment.TickCount
                        ^ Guid.NewGuid().GetHashCode()
                        ^ (GetEntityId().GetHashCode() * 397)
                        ^ (int)(DateTime.UtcNow.Ticks & int.MaxValue);
                if (s == 0)
                {
                    s = 0x6EFE9E77;
                }

                playlistRng = new System.Random(s);
                UnityEngine.Random.InitState(s);
            }
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

            if (activeSegmentDuration > 0.05f && music != null && music.isPlaying)
            {
                NormalizedPhase01 = Mathf.Clamp01((music.time - activeSegmentStartTime) / activeSegmentDuration);
            }
            else if (music != null && music.isPlaying && music.clip != null && music.clip.length > 0.05f)
            {
                NormalizedPhase01 = Mathf.Repeat(music.time / music.clip.length, 1f);
            }
            else
            {
                NormalizedPhase01 = 0f;
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
            jeuxSegmentScheduleActive = false;
            activeSegmentStartTime = 0f;
            activeSegmentDuration = 0f;
            SaveCurrentJeuxCursorIfAny();
            ReseedPlaylistRandom();
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
            SaveCurrentJeuxCursorIfAny();
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
                // Ambiances Theme/<mode> + Gameplay + track01… en continu.
                // Theme/playlist (congolais) : uniquement via TryAppendCongoleseRepoPlaylistFolder pour blind-test / image-guess
                // (ces modes court-circuitent plus haut — ce bloc reste pour cohérence si la logique évolue).
                TryAppendLocalGameplayAmbience(folder, trackPaths, preferredPrefixes);
                TryAppendLocalGameplayAmbience(Path.Combine(root, "Gameplay", modeId), trackPaths, preferredPrefixes);
                TryAppendRootThemeTracks(root, trackPaths);
                TryAppendAllThemeAudioExceptReservedPlaylist(root, trackPaths);

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

                // Jeux_musique_playlist : s’ajoute aux pistes Theme (ne remplace plus tout le pool).
                if (HasJeuxMusiqueAudioOnDisk())
                {
                    var jeuxRoots = new List<string>();
                    CollectJeuxPlaylistRootFolders(jeuxRoots);
                    foreach (string dir in jeuxRoots)
                    {
                        TryCollectAudioFiles(dir, trackPaths);
                    }

                    jeuxSegmentScheduleActive = true;
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

            if (StreamingAssetsUrl.IsWebGlData)
            {
                yield return CoProbeJeuxMusiqueFolderForWebGl(trackPaths);
            }

            trackPaths = trackPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (jeuxSegmentScheduleActive && trackPaths.Count > 1)
            {
                ShuffleStringListInPlace(trackPaths);
            }

            bool loadPlaylist =
                trackPaths.Count >= 2
                || (jeuxSegmentScheduleActive && trackPaths.Count >= 1);
            if (loadPlaylist && trackPaths.Count > 0)
            {
                var loadedPairs = new List<(string path, AudioClip clip)>(trackPaths.Count);
                foreach (string path in trackPaths)
                {
                    AudioClip clip = null;
                    yield return DownloadClip(path, Path.GetFileName(path), c => clip = c);
                    if (clip != null)
                    {
                        loadedPairs.Add((path, clip));
                    }
                }

                const string jeuxMarker = "Jeux_musique_playlist";
                if (jeuxSegmentScheduleActive && loadedPairs.Count > 0)
                {
                    var themeClips = new List<AudioClip>();
                    var jeuxClips = new List<AudioClip>();
                    foreach ((string path, AudioClip clip) in loadedPairs)
                    {
                        if (path.IndexOf(jeuxMarker, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            jeuxClips.Add(clip);
                        }
                        else
                        {
                            themeClips.Add(clip);
                        }
                    }

                    if (themeClips.Count > 0 && jeuxClips.Count > 0)
                    {
                        foreach (AudioClip c in MergeJeuxAndThemePlayOrder(themeClips, jeuxClips))
                        {
                            playlistClips.Add(c);
                        }
                    }
                    else
                    {
                        foreach ((_, AudioClip clip) in loadedPairs)
                        {
                            playlistClips.Add(clip);
                        }
                    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    int pathsHorsJeux = trackPaths.Count(p => p.IndexOf(jeuxMarker, StringComparison.OrdinalIgnoreCase) < 0);
                    if (pathsHorsJeux > 0 && themeClips.Count == 0 && jeuxClips.Count > 0)
                    {
                        Debug.LogWarning(
                            "ThemeMusicPlayer : aucune ambiance Theme chargée ("
                            + pathsHorsJeux
                            + " fichiers hors Jeux ont échoué au décodage). Sur l’EXE Windows, place ffmpeg sur le PATH système ou à côté du jeu ; les .wav/.ogg Theme passent souvent par ffmpeg si FMOD refuse le fichier.");
                    }
#endif
                }
                else
                {
                    foreach ((_, AudioClip clip) in loadedPairs)
                    {
                        playlistClips.Add(clip);
                    }
                }

                if (jeuxSegmentScheduleActive && playlistClips.Count >= 1)
                {
                    DeduplicateJeuxPlaylistClipReferences();
                    if (playlistClips.Count >= 2)
                    {
                        ShuffleAudioClipListCryptographic(playlistClips);
                        RotateJeuxFirstClipVsPreviousLaunch(playlistClips);
                    }

#if UNITY_EDITOR
                    if (trackPaths.Count > playlistClips.Count)
                    {
                        Debug.LogWarning(
                            "ThemeMusicPlayer Jeux : seules "
                            + playlistClips.Count
                            + " / "
                            + trackPaths.Count
                            + " pistes ont pu être décodées (vérifie encodage MP3, caractères spéciaux dans les noms, Console pour les échecs GET). "
                            + "Avec une seule piste valide, le « premier morceau » ne peut pas varier.");
                    }
#endif

                    debugPlaylistCount = playlistClips.Count;
                    debugSource = "jeux-segment-playlist";
                    playlistRoutine = StartCoroutine(PlayJeuxStyleSegmentLoop());
                    yield break;
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

            if (trackPaths.Count == 1 && !jeuxSegmentScheduleActive)
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

        /// <summary>Alias : segments 1 min + ordre mélangé à chaque tour (même logique que Jeux).</summary>
        private IEnumerator PlayPlaylistLoop()
        {
            yield return StartCoroutine(PlayJeuxStyleSegmentLoop());
        }

        private void ShuffleStringListInPlace(List<string> list)
        {
            if (list == null || list.Count < 2)
            {
                return;
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = playlistRng.Next(0, i + 1);
                string tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        private void ShuffleAudioClipListInPlace(List<AudioClip> list)
        {
            if (list == null || list.Count < 2)
            {
                return;
            }

            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = playlistRng.Next(0, i + 1);
                AudioClip tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }

        /// <summary>Évite que la première piste du nouveau tour soit la même que la dernière du tour précédent (cas fréquent avec 2–3 titres).</summary>
        private void ShuffleAudioClipListInPlaceAvoidImmediateRepeat(List<AudioClip> list, AudioClip lastPlayedClip)
        {
            ShuffleAudioClipListInPlace(list);
            if (list == null || list.Count < 2 || lastPlayedClip == null)
            {
                return;
            }

            if (list[0] == lastPlayedClip)
            {
                AudioClip t = list[0];
                list[0] = list[1];
                list[1] = t;
            }
        }

        /// <summary>Décale circulairement la liste pour que la 1ʳᵉ piste jouée après intro ne soit pas toujours la même position disque.</summary>
        private void RotateAudioClipListRandomStart(List<AudioClip> list)
        {
            if (list == null || list.Count < 2)
            {
                return;
            }

            int k = playlistRng.Next(0, list.Count);
            if (k <= 0)
            {
                return;
            }

            List<AudioClip> head = list.GetRange(0, k);
            list.RemoveRange(0, k);
            list.AddRange(head);
        }

        private void DeduplicateJeuxPlaylistClipReferences()
        {
            if (playlistClips == null || playlistClips.Count < 2)
            {
                return;
            }

            var seen = new HashSet<AudioClip>();
            for (int i = playlistClips.Count - 1; i >= 0; i--)
            {
                AudioClip c = playlistClips[i];
                if (c == null || !seen.Add(c))
                {
                    playlistClips.RemoveAt(i);
                }
            }
        }

        private static void ShuffleAudioClipListCryptographic(List<AudioClip> list)
        {
            if (list == null || list.Count < 2)
            {
                return;
            }

            byte[] buf = new byte[4];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    rng.GetBytes(buf);
                    uint u = BitConverter.ToUInt32(buf, 0);
                    int j = (int)(u % (uint)(i + 1));
                    AudioClip tmp = list[i];
                    list[i] = list[j];
                    list[j] = tmp;
                }
            }
        }

        /// <summary>
        /// Mélange Theme et Jeux puis entrelace (évite d’enchaîner surtout des titres Jeux si les deux pools sont chargés).
        /// </summary>
        private static List<AudioClip> MergeJeuxAndThemePlayOrder(List<AudioClip> themeClips, List<AudioClip> jeuxClips)
        {
            var result = new List<AudioClip>();
            if (themeClips == null || jeuxClips == null)
            {
                return result;
            }

            var ta = new List<AudioClip>(themeClips);
            var jb = new List<AudioClip>(jeuxClips);
            ShuffleAudioClipListCryptographic(ta);
            ShuffleAudioClipListCryptographic(jb);
            int i = 0;
            int j = 0;
            bool takeTheme = UnityEngine.Random.Range(0, 2) == 0;
            while (i < ta.Count || j < jb.Count)
            {
                if (takeTheme && i < ta.Count)
                {
                    result.Add(ta[i++]);
                }
                else if (!takeTheme && j < jb.Count)
                {
                    result.Add(jb[j++]);
                }
                else if (i < ta.Count)
                {
                    result.Add(ta[i++]);
                }
                else if (j < jb.Count)
                {
                    result.Add(jb[j++]);
                }

                takeTheme = !takeTheme;
            }

            return result;
        }

        /// <summary>Évite de redémarrer sur la même 1ʳᵉ piste qu’au lancement précédent (PlayerPrefs).</summary>
        private void RotateJeuxFirstClipVsPreviousLaunch(List<AudioClip> clips)
        {
            if (clips == null || clips.Count < 2 || clips[0] == null)
            {
                return;
            }

            string prev = PlayerPrefs.GetString(JeuxFirstClipSessionPrefKey, "");
            if (!string.IsNullOrEmpty(prev))
            {
                int guard = 0;
                while (string.Equals(clips[0].name, prev, StringComparison.Ordinal)
                    && guard < clips.Count)
                {
                    AudioClip move = clips[0];
                    clips.RemoveAt(0);
                    clips.Add(move);
                    guard++;
                }
            }

            PlayerPrefs.SetString(JeuxFirstClipSessionPrefKey, clips[0].name);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Conservé pour compatibilité avec d'anciens appels : les pistes Jeux ne doivent plus
        /// remplacer les ambiances Theme. Le dossier Theme/playlist reste le seul dossier exclu
        /// des BGM générales, car il est réservé aux modes blind-test / image-guess.
        /// </summary>
        private void FilterTrackPathsToJeuxPlaylistIfAny(List<string> trackPaths)
        {
        }

        private static void TryCollectAudioFiles(string folder, List<string> trackPaths)
        {
            if (trackPaths == null || string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                return;
            }

            foreach (string ext in new[] { "mp3", "ogg", "wav" })
            {
                try
                {
                    foreach (string f in Directory.GetFiles(folder, "*." + ext, SearchOption.TopDirectoryOnly))
                    {
                        trackPaths.Add(f);
                    }
                }
                catch (IOException)
                {
                    // ignoré
                }
            }
        }

        private static void TryAppendRootThemeTracks(string root, List<string> trackPaths)
        {
            if (trackPaths == null || string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return;
            }

            foreach (string ext in new[] { "ogg", "wav", "mp3" })
            {
                try
                {
                    trackPaths.AddRange(Directory.GetFiles(root, "track*." + ext, SearchOption.TopDirectoryOnly));
                }
                catch (IOException)
                {
                    // ignore
                }
            }
        }

        private static void TryAppendAllThemeAudioExceptReservedPlaylist(string root, List<string> trackPaths)
        {
            if (trackPaths == null || string.IsNullOrEmpty(root) || !Directory.Exists(root))
            {
                return;
            }

            try
            {
                foreach (string file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
                {
                    string ext = Path.GetExtension(file);
                    if (!ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (IsInReservedThemePlaylistFolder(root, file))
                    {
                        continue;
                    }

                    trackPaths.Add(file);
                }
            }
            catch (IOException)
            {
                // ignore
            }
            catch (UnauthorizedAccessException)
            {
                // ignore
            }
        }

        private static bool IsInReservedThemePlaylistFolder(string themeRoot, string file)
        {
            if (string.IsNullOrEmpty(themeRoot) || string.IsNullOrEmpty(file))
            {
                return false;
            }

            try
            {
                string root = Path.GetFullPath(themeRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string full = Path.GetFullPath(file);
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                string rel = full.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string[] parts = rel.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Any(p => string.Equals(p, "playlist", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception)
            {
                return file.IndexOf(Path.DirectorySeparatorChar + "playlist" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0
                    || file.IndexOf(Path.AltDirectorySeparatorChar + "playlist" + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        private IEnumerator CoProbeJeuxMusiqueFolderForWebGl(List<string> trackPaths)
        {
            if (trackPaths == null)
            {
                yield break;
            }

            int before = trackPaths.Count;
            for (int n = 1; n <= 36; n++)
            {
                string prefix = "track" + n.ToString("D2");
                foreach (string ext in new[] { "mp3", "ogg", "wav" })
                {
                    string rel = "Theme/Jeux_musique_playlist/" + prefix + "." + ext;
                    string u = StreamingAssetsUrl.UrlForRelativePath(rel);
                    bool ok = false;
                    yield return WebGlStreamingPrewarm.CoHttpHeadOk(u, b => ok = b);
                    if (ok)
                    {
                        trackPaths.Add(u);
                    }
                }
            }

            if (trackPaths.Count > before)
            {
                jeuxSegmentScheduleActive = true;
            }
        }

        private IEnumerator PlayJeuxStyleSegmentLoop()
        {
            music.loop = false;
            ReseedPlaylistRandom();
            bool needIntro = true;
            AudioClip lastPlayedInPreviousScan = null;
            bool applyRandomStartOffsetOnce = true;
            while (enabled && playlistClips.Count > 0)
            {
                // Nouvel ordre aléatoire à chaque « tour de rôle » (toutes les pistes pour la fenêtre 1 min en cours).
                if (playlistClips.Count >= 2)
                {
                    ShuffleAudioClipListInPlaceAvoidImmediateRepeat(playlistClips, lastPlayedInPreviousScan);
                    if (applyRandomStartOffsetOnce)
                    {
                        applyRandomStartOffsetOnce = false;
                        RotateAudioClipListRandomStart(playlistClips);
                    }
                }

                bool playedAnyThisRound = false;
                AudioClip lastPlayedThisScan = null;
                for (int i = 0; i < playlistClips.Count; i++)
                {
                    AudioClip c = playlistClips[i];
                    if (c == null)
                    {
                        continue;
                    }

                    float total = c.length > 0.05f ? c.length : 0f;
                    if (total < 0.05f)
                    {
                        continue;
                    }

                    float startTime = ResolveJeuxClipStartTime(c, total);
                    float dur = Mathf.Min(PlaylistSegmentMaxSeconds, total - startTime);
                    if (dur < 0.05f)
                    {
                        float fallbackStart = ResolveDefaultStartForJeuxClip(c, total);
                        startTime = Mathf.Clamp(fallbackStart, 0f, Mathf.Max(0f, total - 0.02f));
                        dur = Mathf.Min(PlaylistSegmentMaxSeconds, total - startTime);
                    }

                    if (dur < 0.05f)
                    {
                        continue;
                    }

                    playedAnyThisRound = true;
                    lastPlayedThisScan = c;
                    debugPlaylistCount = playlistClips.Count;
                    debugRotationIndex = i;
                    debugNowPlaying = c.name + " @" + Mathf.FloorToInt(startTime) + "s";
                    if (needIntro)
                    {
                        needIntro = false;
                        yield return CoPlaySegmentWithIntro(c, startTime, dur);
                    }
                    else
                    {
                        yield return CoPlaySegment(c, startTime, dur);
                    }

                    float next = Mathf.Repeat(startTime + dur, total);
                    if (next < 0.05f && total > 0.1f)
                    {
                        next = Mathf.Clamp(ResolveDefaultStartForJeuxClip(c, total), 0f, Mathf.Max(0f, total - 0.02f));
                    }
                    jeuxPlaybackCursorByClip[c.name ?? ""] = next;
                }

                lastPlayedInPreviousScan = lastPlayedThisScan;

                if (!playedAnyThisRound)
                {
                    yield return null;
                }
            }
        }

        private IEnumerator CoPlaySegment(AudioClip c, float startTime, float duration)
        {
            if (c == null)
            {
                yield break;
            }

            music.clip = c;
            music.loop = false;
            float st = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, c.length - 0.01f));
            music.time = st;
            activeSegmentStartTime = st;
            activeSegmentDuration = duration;
            activeSegmentClip = c;
            RefreshMusicVolume();
            music.Play();
            yield return new WaitForSeconds(duration * 0.995f);
            music.Stop();
            activeSegmentDuration = 0f;
            activeSegmentClip = null;
        }

        private IEnumerator CoPlaySegmentWithIntro(AudioClip c, float startTime, float duration)
        {
            if (c == null)
            {
                yield break;
            }

            AudioClip intro = ProceduralClips.BuildTamTamIntro();
            music.clip = intro;
            music.loop = false;
            activeSegmentDuration = 0f;
            RefreshMusicVolume();
            music.Play();
            yield return new WaitWhile(() => music.isPlaying);
            Destroy(intro);
            yield return CoPlaySegment(c, startTime, duration);
        }

        private void SaveCurrentJeuxCursorIfAny()
        {
            if (!jeuxSegmentScheduleActive || music == null || activeSegmentClip == null || activeSegmentClip.length <= 0.1f)
            {
                return;
            }

            float pos = Mathf.Clamp(music.time, 0f, Mathf.Max(0f, activeSegmentClip.length - 0.01f));
            if (pos < 0.05f)
            {
                return;
            }

            string key = activeSegmentClip.name ?? "";
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            jeuxPlaybackCursorByClip[key] = pos;
        }

        private float ResolveJeuxClipStartTime(AudioClip clip, float totalSeconds)
        {
            if (clip == null || totalSeconds <= 0.1f)
            {
                return 0f;
            }

            string key = clip.name ?? "";
            if (!string.IsNullOrWhiteSpace(key)
                && jeuxPlaybackCursorByClip.TryGetValue(key, out float saved)
                && saved >= 0f
                && saved < totalSeconds - 0.01f)
            {
                return saved;
            }

            float fromPolicy = ResolveDefaultStartForJeuxClip(clip, totalSeconds);
            return Mathf.Clamp(fromPolicy, 0f, Mathf.Max(0f, totalSeconds - 0.02f));
        }

        private static float ResolveDefaultStartForJeuxClip(AudioClip clip, float totalSeconds)
        {
            if (clip == null || totalSeconds <= 0.1f)
            {
                return 0f;
            }

            string n = NormalizeTrackName(clip.name);
            if (TryResolveJeuxStartFromOverrides(n, totalSeconds, out float fromOverrides))
            {
                return fromOverrides;
            }

            // Par défaut: éviter le début pour ne pas exposer nom artiste/titre.
            return Mathf.Clamp(totalSeconds * 0.5f, 0f, Mathf.Max(0f, totalSeconds - 0.2f));
        }

        private static bool TryResolveJeuxStartFromOverrides(string normalizedTrackName, float clipLength, out float startSec)
        {
            startSec = 0f;
            LoadJeuxStartRulesOnce();
            if (jeuxStartRules == null || jeuxStartRules.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < jeuxStartRules.Length; i++)
            {
                JeuxStartRule rule = jeuxStartRules[i];
                if (rule == null || string.IsNullOrWhiteSpace(rule.match))
                {
                    continue;
                }

                if (!IsLooseRuleMatch(normalizedTrackName, NormalizeTrackName(rule.match)))
                {
                    continue;
                }

                startSec = ComputeStartFromRule(rule.mode, rule.value, clipLength);
                return true;
            }

            startSec = Mathf.Clamp(clipLength * Mathf.Clamp01(jeuxDefaultStartPercent), 0f, Mathf.Max(0f, clipLength - 0.2f));
            return true;
        }

        private static void LoadJeuxStartRulesOnce()
        {
            if (jeuxStartRulesLoaded)
            {
                return;
            }

            jeuxStartRulesLoaded = true;
            try
            {
                TextAsset ta = Resources.Load<TextAsset>("Datasets/audio_playback_start_overrides");
                if (ta == null || string.IsNullOrWhiteSpace(ta.text))
                {
                    jeuxStartRules = Array.Empty<JeuxStartRule>();
                    return;
                }

                AudioPlaybackStartOverridesFile parsed = JsonUtility.FromJson<AudioPlaybackStartOverridesFile>(ta.text);
                jeuxStartRules = parsed?.jeuxRules ?? Array.Empty<JeuxStartRule>();
                if (parsed != null)
                {
                    jeuxDefaultStartPercent = Mathf.Clamp01(parsed.jeuxDefaultStartPercent);
                }
            }
            catch
            {
                jeuxStartRules = Array.Empty<JeuxStartRule>();
            }
        }

        private static bool IsLooseRuleMatch(string normalizedText, string normalizedRule)
        {
            if (string.IsNullOrWhiteSpace(normalizedText) || string.IsNullOrWhiteSpace(normalizedRule))
            {
                return false;
            }

            string[] tokens = normalizedRule.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < tokens.Length; i++)
            {
                string t = tokens[i];
                if (t.Length <= 1) continue;
                if (normalizedText.IndexOf(t, StringComparison.Ordinal) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private static float ComputeStartFromRule(string mode, float value, float clipLength)
        {
            string m = (mode ?? "").Trim().ToLowerInvariant();
            switch (m)
            {
                case "start":
                    return 0f;
                case "seconds":
                    return Mathf.Clamp(value, 0f, Mathf.Max(0f, clipLength - 0.2f));
                case "percent":
                    return Mathf.Clamp(clipLength * Mathf.Clamp01(value), 0f, Mathf.Max(0f, clipLength - 0.2f));
                default:
                    return Mathf.Clamp(clipLength * 0.5f, 0f, Mathf.Max(0f, clipLength - 0.2f));
            }
        }

        private static string NormalizeTrackName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "";
            }

            string src = raw.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(src.Length);
            for (int i = 0; i < src.Length; i++)
            {
                char c = src[i];
                if (char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        [Serializable]
        private sealed class AudioPlaybackStartOverridesFile
        {
            public float jeuxDefaultStartPercent = 0.5f;
            public JeuxStartRule[] jeuxRules;
        }

        [Serializable]
        private sealed class JeuxStartRule
        {
            public string match;
            public string mode;
            public float value;
        }

        private IEnumerator DownloadClip(string fullPathOrUrl, string fileName, Action<AudioClip> assign)
        {
            bool isHttp = fullPathOrUrl != null && fullPathOrUrl.IndexOf("://", StringComparison.Ordinal) >= 0;
            if (!isHttp
                && !string.IsNullOrEmpty(fullPathOrUrl)
                && File.Exists(fullPathOrUrl))
            {
                yield return CoDownloadLocalAudioWithFallback(fullPathOrUrl, fileName, assign);
                yield break;
            }

            string uri = isHttp
                ? fullPathOrUrl
                : StreamingAssetsUrl.ToRequestUrl(fullPathOrUrl);
            AudioType audioType = GuessAudioType(fileName);
            yield return StartCoroutine(DownloadClipUriCo(uri, audioType, assign));
        }

        /// <summary>
        /// Fichier disque : essai direct puis copie vers un chemin court uniquement ASCII (évite &amp;, #, emojis, chemins &gt; 260, etc.).
        /// </summary>
        private IEnumerator CoDownloadLocalAudioWithFallback(string fullPath, string fileName, Action<AudioClip> assign)
        {
            AudioType audioType = GuessAudioType(fileName);
            string uri1 = StreamingAssetsUrl.ToRequestUrl(fullPath);
            bool received = false;
            Action<AudioClip> once = c =>
            {
                received = true;
                assign?.Invoke(c);
            };
            yield return StartCoroutine(DownloadClipUriCo(uri1, audioType, once, false, fullPath));
            if (received)
            {
                yield break;
            }

            string temp = TryBuildTempCopyForLocalAudioLoad(fullPath, fileName);
            if (string.IsNullOrEmpty(temp) || !File.Exists(temp))
            {
                yield break;
            }

            string uri2 = StreamingAssetsUrl.ToRequestUrl(temp);
            received = false;
            yield return StartCoroutine(DownloadClipUriCo(uri2, audioType, once, true, temp));
        }

        private static string TryBuildTempCopyForLocalAudioLoad(string sourcePath, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                {
                    return null;
                }

                string ext = Path.GetExtension(fileName);
                if (string.IsNullOrEmpty(ext))
                {
                    ext = Path.GetExtension(sourcePath);
                }

                if (string.IsNullOrEmpty(ext))
                {
                    ext = ".mp3";
                }

                string full = Path.GetFullPath(sourcePath);
                long len = new FileInfo(full).Length;
                int h = (full.GetHashCode() * 31)
                    ^ (int)(len & 0x7FFFFFFF)
                    ^ (int)((len >> 32) & 0x7FFFFFFF)
                    ^ ((fileName ?? Path.GetFileName(full)) ?? "").GetHashCode(StringComparison.Ordinal);
                string safe = "jeux_bgm_" + h.ToString("X8") + ext.ToLowerInvariant();
                string dir = Path.Combine(Application.temporaryCachePath, "CongoGames_JeuxAudio");
                Directory.CreateDirectory(dir);
                string dest = Path.Combine(dir, safe);
                if (File.Exists(dest))
                {
                    long s1 = new FileInfo(full).Length;
                    long s2 = new FileInfo(dest).Length;
                    if (s1 == s2)
                    {
                        return dest;
                    }
                }

                File.Copy(full, dest, true);
                return dest;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private IEnumerator DownloadClipFromHttp(string absoluteUrl, Action<AudioClip> assign)
        {
            AudioType audioType = GuessAudioType(absoluteUrl);
            yield return StartCoroutine(DownloadClipUriCo(absoluteUrl, audioType, assign));
        }

        /// <param name="optionalLocalPathForTranscode">Si renseigné (fichier disque), dernier recours : ffmpeg → WAV PCM pour contenus que FMOD ne décode pas.</param>
        private IEnumerator DownloadClipUriCo(
            string uri,
            AudioType audioType,
            Action<AudioClip> assign,
            bool skipBinaryGuard = false,
            string optionalLocalPathForTranscode = null)
        {
            if (string.IsNullOrEmpty(uri))
            {
                yield break;
            }

            bool done = false;
            void Got(AudioClip c)
            {
                done = true;
                assign?.Invoke(c);
            }

            // Fichier disque (Jeux, etc.) : éviter de passer par FMOD/MP3 en premier — GetContent loggue une erreur à chaque échec.
            // On tente d’abord WAV natif si déjà .wav, puis ffmpeg → PCM → AudioClip.Create, puis seulement UnityWebRequest sur l’URI d’origine.
            if (!string.IsNullOrEmpty(optionalLocalPathForTranscode) && File.Exists(optionalLocalPathForTranscode))
            {
                string ext = Path.GetExtension(optionalLocalPathForTranscode);
                if (!string.IsNullOrEmpty(ext) && ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryLoadPcmWavFileIntoClip(optionalLocalPathForTranscode, out AudioClip wavDirect) && wavDirect != null)
                    {
                        Got(wavDirect);
                    }

                    if (done)
                    {
                        yield break;
                    }
                }

                string wavFromFf = TryTranscodeLocalAudioWithFfmpeg(optionalLocalPathForTranscode);
                if (!string.IsNullOrEmpty(wavFromFf) && File.Exists(wavFromFf))
                {
                    if (TryLoadPcmWavFileIntoClip(wavFromFf, out AudioClip pcmClip) && pcmClip != null)
                    {
                        Got(pcmClip);
                    }

                    if (done)
                    {
                        yield break;
                    }
                }
            }

            yield return DownloadClipUriSingleAttempt(uri, audioType, Got, skipBinaryGuard);
            if (done)
            {
                yield break;
            }

            // Beaucoup de « MP3 » issus du web sont des formats que le décodeur FMOD d’Unity refuse ; UNKNOWN peut aider.
            if (audioType == AudioType.MPEG)
            {
                yield return DownloadClipUriSingleAttempt(uri, AudioType.UNKNOWN, Got, skipBinaryGuard);
                if (done)
                {
                    yield break;
                }
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!done)
            {
                Debug.LogWarning(
                    "ThemeMusicPlayer: impossible de décoder ce fichier avec Unity/FMOD (extension ou codec incompatible). "
                    + "Installe ffmpeg sur le PATH pour conversion automatique en WAV, ou exporte les morceaux en .ogg / .wav. URI : "
                    + uri);
            }
#endif
        }

        private IEnumerator DownloadClipUriSingleAttempt(string uri, AudioType audioType, Action<AudioClip> assign, bool skipBinaryGuard)
        {
            if (string.IsNullOrEmpty(uri))
            {
                yield break;
            }

            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            req.timeout = 120;
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

            if (req.downloadHandler is not DownloadHandlerAudioClip)
            {
                yield break;
            }

            byte[] raw = req.downloadHandler.data;
            AudioType guardType = audioType == AudioType.UNKNOWN ? AudioType.UNKNOWN : audioType;
            if (!skipBinaryGuard && (raw == null || !LooksLikeAudioPayload(raw, guardType)))
            {
                yield break;
            }

            AudioClip clip;
            try
            {
                clip = DownloadHandlerAudioClip.GetContent(req);
            }
            catch (Exception)
            {
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

        /// <summary>
        /// ffmpeg doit être disponible sur le PATH (Windows : winget install ffmpeg / choco install ffmpeg).
        /// </summary>
        /// <summary>Utilisable par <see cref="GameSfxHub"/> (blind / image guess) pour le même repli que la BGM Theme.</summary>
        public static string TryTranscodeLocalAudioWithFfmpeg(string sourcePath)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                {
                    return null;
                }

                string ffmpeg = ResolveFfmpegExecutable();
                if (string.IsNullOrEmpty(ffmpeg))
                {
                    return null;
                }

                string dir = Path.Combine(Application.temporaryCachePath, "CongoGames_JeuxFfmpeg");
                Directory.CreateDirectory(dir);
                string full = Path.GetFullPath(sourcePath);
                long len = new FileInfo(full).Length;
                string tag = (full.GetHashCode() ^ len.GetHashCode()).ToString("X8");
                string dest = Path.Combine(dir, "ff_pcm_" + tag + ".wav");

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = "-nostdin -hide_banner -loglevel error -y -i \"" + full + "\" -acodec pcm_s16le -ar 44100 -ac 2 \"" + dest + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null)
                    {
                        return null;
                    }

                    if (!p.WaitForExit(180000))
                    {
                        try
                        {
                            p.Kill();
                        }
                        catch (Exception)
                        {
                            // ignoré
                        }

                        return null;
                    }

                    if (p.ExitCode != 0 || !File.Exists(dest) || new FileInfo(dest).Length < 256)
                    {
                        try
                        {
                            if (File.Exists(dest))
                            {
                                File.Delete(dest);
                            }
                        }
                        catch (Exception)
                        {
                            // ignoré
                        }

                        return null;
                    }

                    return dest;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Charge un WAV PCM 16 bits little-endian sans passer par UnityWebRequest / FMOD (évite les erreurs GetContent sur certains flux).
        /// </summary>
        /// <summary>Utilisable par <see cref="GameSfxHub"/> pour lire un WAV sans FMOD.</summary>
        public static bool TryLoadPcmWavFileIntoClip(string wavPath, out AudioClip clip)
        {
            clip = null;

            try
            {
                if (string.IsNullOrEmpty(wavPath) || !File.Exists(wavPath))
                {
                    return false;
                }

                byte[] bytes = File.ReadAllBytes(wavPath);
                if (bytes.Length < 44)
                {
                    return false;
                }

                if (bytes[0] != (byte)'R' || bytes[1] != (byte)'I' || bytes[2] != (byte)'F' || bytes[3] != (byte)'F')
                {
                    return false;
                }

                if (bytes[8] != (byte)'W' || bytes[9] != (byte)'A' || bytes[10] != (byte)'V' || bytes[11] != (byte)'E')
                {
                    return false;
                }

                int offset = 12;
                ushort audioFormat = 0;
                ushort numChannels = 0;
                int sampleRate = 0;
                ushort bitsPerSample = 0;
                int dataOffset = -1;
                int dataSize = 0;
                int fmtChunkDataStart = -1;
                int fmtChunkDeclaredSize = 0;

                while (offset + 8 <= bytes.Length)
                {
                    string id = Encoding.ASCII.GetString(bytes, offset, 4);
                    uint chunkSizeU = BitConverter.ToUInt32(bytes, offset + 4);
                    if (chunkSizeU > (uint)(bytes.Length - offset - 8))
                    {
                        return false;
                    }

                    int chunkSize = (int)chunkSizeU;
                    int chunkDataStart = offset + 8;
                    int paddedSize = chunkSize + (chunkSize % 2);

                    if (id == "fmt ")
                    {
                        if (chunkSize < 16 || chunkDataStart + 16 > bytes.Length)
                        {
                            return false;
                        }

                        fmtChunkDataStart = chunkDataStart;
                        fmtChunkDeclaredSize = chunkSize;
                        audioFormat = BitConverter.ToUInt16(bytes, chunkDataStart);
                        numChannels = BitConverter.ToUInt16(bytes, chunkDataStart + 2);
                        sampleRate = BitConverter.ToInt32(bytes, chunkDataStart + 4);
                        bitsPerSample = BitConverter.ToUInt16(bytes, chunkDataStart + 14);
                    }
                    else if (id == "data")
                    {
                        dataOffset = chunkDataStart;
                        dataSize = chunkSize;
                        break;
                    }

                    offset = chunkDataStart + paddedSize;
                }

                if (dataOffset < 0 || dataSize <= 0)
                {
                    return false;
                }

                // PCM 16 bit entrelacé : WAVE_FORMAT_PCM (1) ou WAVE_FORMAT_EXTENSIBLE (0xFFFE) avec sous-type PCM.
                bool pcm16 = audioFormat == 1 && bitsPerSample == 16;
                if (!pcm16
                    && audioFormat == 0xFFFE
                    && bitsPerSample == 16
                    && fmtChunkDeclaredSize >= 40
                    && fmtChunkDataStart >= 0
                    && fmtChunkDataStart + 40 <= bytes.Length)
                {
                    int pcmMarker = BitConverter.ToInt32(bytes, fmtChunkDataStart + 24);
                    pcm16 = pcmMarker == 1;
                }

                if (!pcm16)
                {
                    return false;
                }

                if (numChannels < 1 || numChannels > 8)
                {
                    return false;
                }

                if (sampleRate <= 0 || sampleRate > 480000)
                {
                    return false;
                }

                int bytesPerFrame = numChannels * (bitsPerSample / 8);
                int frameCount = dataSize / bytesPerFrame;
                if (frameCount <= 0 || dataOffset + dataSize > bytes.Length)
                {
                    return false;
                }

                var interleaved = new float[frameCount * numChannels];
                int bi = dataOffset;
                for (int f = 0; f < frameCount; f++)
                {
                    for (int ch = 0; ch < numChannels; ch++)
                    {
                        if (bi + 1 >= bytes.Length)
                        {
                            return false;
                        }

                        short s = (short)(bytes[bi] | (bytes[bi + 1] << 8));
                        bi += 2;
                        interleaved[f * numChannels + ch] = s / 32768f;
                    }
                }

                string name = "JeuxWav_" + (wavPath.GetHashCode() & 0x7FFFFFFF).ToString("X");
                clip = AudioClip.Create(name, frameCount, numChannels, sampleRate, false);
                clip.SetData(interleaved, 0);
                return true;
            }
            catch (Exception)
            {
                clip = null;
                return false;
            }
        }

        /// <summary>
        /// Unity (surtout lancé depuis le raccourci) n’a souvent pas le même PATH que PowerShell après winget.
        /// On teste le PATH fusionné, puis des emplacements WinGet / Gyan habituels sous Windows.
        /// </summary>
        private static string ResolveFfmpegExecutable()
        {
            string[] names = { "ffmpeg.exe", "ffmpeg" };

            void AppendPathParts(HashSet<string> seen, string block)
            {
                if (string.IsNullOrEmpty(block))
                {
                    return;
                }

                foreach (string part in block.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string d = part.Trim().Trim('"');
                    if (d.Length > 0)
                    {
                        seen.Add(d);
                    }
                }
            }

            var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            AppendPathParts(dirs, Environment.GetEnvironmentVariable("PATH") ?? "");
            AppendPathParts(dirs, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "");
            AppendPathParts(dirs, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine) ?? "");

            foreach (string dir in dirs)
            {
                foreach (string n in names)
                {
                    try
                    {
                        string full = Path.Combine(dir, n);
                        if (File.Exists(full) && RunFfmpegVersionOk(full))
                        {
                            return full;
                        }
                    }
                    catch (Exception)
                    {
                        // ignoré
                    }
                }
            }

            if (Application.platform == RuntimePlatform.WindowsEditor
                || Application.platform == RuntimePlatform.WindowsPlayer)
            {
                try
                {
                    string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string wingetPkgs = Path.Combine(local, "Microsoft", "WinGet", "Packages");
                    if (Directory.Exists(wingetPkgs))
                    {
                        foreach (string pkgDir in Directory.EnumerateDirectories(wingetPkgs))
                        {
                            string low = Path.GetFileName(pkgDir) ?? "";
                            if (low.IndexOf("ffmpeg", StringComparison.OrdinalIgnoreCase) < 0
                                && low.IndexOf("Gyan", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                continue;
                            }

                            foreach (string probe in new[]
                                     {
                                         Path.Combine(pkgDir, "ffmpeg.exe"),
                                         Path.Combine(pkgDir, "bin", "ffmpeg.exe"),
                                         Path.Combine(pkgDir, "ffmpeg-full_build", "bin", "ffmpeg.exe"),
                                     })
                            {
                                if (File.Exists(probe) && RunFfmpegVersionOk(probe))
                                {
                                    return probe;
                                }
                            }

                            foreach (string sub in Directory.EnumerateDirectories(pkgDir))
                            {
                                foreach (string probe in new[]
                                         {
                                             Path.Combine(sub, "bin", "ffmpeg.exe"),
                                             Path.Combine(sub, "ffmpeg.exe"),
                                         })
                                {
                                    if (File.Exists(probe) && RunFfmpegVersionOk(probe))
                                    {
                                        return probe;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // ignoré
                }

                foreach (string hint in new[]
                         {
                             @"C:\ffmpeg\bin\ffmpeg.exe",
                             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
                             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ffmpeg", "bin", "ffmpeg.exe"),
                         })
                {
                    try
                    {
                        if (File.Exists(hint) && RunFfmpegVersionOk(hint))
                        {
                            return hint;
                        }
                    }
                    catch (Exception)
                    {
                        // ignoré
                    }
                }
            }

            foreach (string name in names)
            {
                try
                {
                    if (RunFfmpegVersionOk(name))
                    {
                        return name;
                    }
                }
                catch (Exception)
                {
                    // ignoré
                }
            }

            return null;
        }

        private static bool RunFfmpegVersionOk(string executablePathOrName)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = executablePathOrName,
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi))
                {
                    if (p == null)
                    {
                        return false;
                    }

                    return p.WaitForExit(8000) && p.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
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
                    if (data.Length >= 3 && data[0] == 0x49 && data[1] == 0x44 && data[2] == 0x33)
                    {
                        return true; // ID3v2
                    }

                    // Les tags ID3v2 (souvent + image) dépassent facilement 10–12 o : l’ancien plafond
                    // rejetait des MP3 valides (seul un morceau « court entête » passait, ex. Billie Jean).
                    int scan = Mathf.Min(data.Length - 2, 256 * 1024);
                    for (int i = 0; i <= scan; i++)
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

            var dirs = new List<string>();
#if UNITY_EDITOR
            try
            {
                string repo = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "playlist"));
                if (!string.IsNullOrEmpty(repo))
                {
                    dirs.Add(repo);
                }
            }
            catch (IOException)
            {
                // ignoré
            }
#endif

            try
            {
                string gameFolder = Directory.GetParent(Application.dataPath)?.FullName;
                if (!string.IsNullOrEmpty(gameFolder))
                {
                    dirs.Add(Path.Combine(gameFolder, "playlist"));
                }
            }
            catch (IOException)
            {
                // ignoré
            }

            var seenDir = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string congo in dirs)
            {
                if (string.IsNullOrEmpty(congo) || !seenDir.Add(Path.GetFullPath(congo)))
                {
                    continue;
                }

                if (!Directory.Exists(congo))
                {
                    continue;
                }

                foreach (string ext in new[] { "ogg", "wav", "mp3" })
                {
                    try
                    {
                        trackPaths.AddRange(Directory.GetFiles(congo, "*." + ext, SearchOption.TopDirectoryOnly));
                    }
                    catch (IOException)
                    {
                        // ignoré
                    }
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
