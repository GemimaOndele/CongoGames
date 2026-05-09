using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using CongoGames.AI;
using CongoGames.Audio;
using CongoGames.Core;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Effets sonores UI (clic, bonne/mauvaise réponse). Charge Resources/Audio/sfx_* si présents.
    /// Blind test : fichier dans StreamingAssets/Theme/BlindTest/ ou Theme/ (.ogg/.mp3/.wav), ou URL http(s), sinon stub procédural.
    /// </summary>
    public class GameSfxHub : MonoBehaviour
    {
        public static GameSfxHub Instance { get; private set; }

        [SerializeField] private float volume = 0.82f;
        [Tooltip("Bus mixer SFX one-shots (optionnel).")]
        [SerializeField] private AudioMixerGroup sfxOutputGroup;
        [Tooltip("Bus mixer boucle blind test (optionnel, sinon sfxOutputGroup).")]
        [SerializeField] private AudioMixerGroup blindOutputGroup;
        private float duckMultiplier = 1f;

        private AudioSource source;
        private AudioSource crowdSource;
        private AudioSource popSource;
        private AudioSource blindLoop;
        private AudioClip blindLoopClip;
        private Coroutine blindMusicCo;
        private AudioClip tapClip;
        private AudioClip okClip;
        private AudioClip badClip;
        private AudioClip cheerClip;
        private AudioClip chronoTickClip;
        public bool IsBlindMusicPlaying => blindLoop != null && blindLoop.isPlaying && blindLoop.clip != null;
        public bool IsBlindMusicLoading => blindMusicCo != null;
        private static Dictionary<string, string> blindTrackAliasCache;
        private static Dictionary<string, string> blindLooseBaseCache;
        private static BlindStartRule[] blindStartRules;
        private static bool blindStartRulesLoaded;
        private static float blindDefaultStartPercent = 0.5f;
        private readonly Dictionary<string, float> blindPlaybackCursorByKey = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private string blindCurrentPlaybackKey;
        private string blindPendingQuestionContext;

        private void Awake()
        {
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            if (sfxOutputGroup != null)
            {
                source.outputAudioMixerGroup = sfxOutputGroup;
            }

            crowdSource = gameObject.AddComponent<AudioSource>();
            crowdSource.playOnAwake = false;
            crowdSource.spatialBlend = 0f;
            if (sfxOutputGroup != null)
            {
                crowdSource.outputAudioMixerGroup = sfxOutputGroup;
            }

            popSource = gameObject.AddComponent<AudioSource>();
            popSource.playOnAwake = false;
            popSource.spatialBlend = 0f;
            popSource.priority = 128;
            if (sfxOutputGroup != null)
            {
                popSource.outputAudioMixerGroup = sfxOutputGroup;
            }

            blindLoop = gameObject.AddComponent<AudioSource>();
            blindLoop.playOnAwake = false;
            blindLoop.loop = true;
            blindLoop.spatialBlend = 0f;
            AudioMixerGroup blindBus = blindOutputGroup != null ? blindOutputGroup : sfxOutputGroup;
            if (blindBus != null)
            {
                blindLoop.outputAudioMixerGroup = blindBus;
            }

            RefreshSfxVolumes();

            tapClip = Resources.Load<AudioClip>("Audio/sfx_tap") ?? ProceduralClips.BuildUiTap();
            okClip = Resources.Load<AudioClip>("Audio/sfx_correct") ?? ProceduralClips.BuildCorrectChime();
            badClip = Resources.Load<AudioClip>("Audio/sfx_wrong") ?? ProceduralClips.BuildWrongBuzz();
            cheerClip = Resources.Load<AudioClip>("Audio/sfx_cheer") ?? ProceduralClips.BuildCrowdCheer();
            chronoTickClip = Resources.Load<AudioClip>("Audio/sfx_chrono_tick") ?? ProceduralClips.BuildChronoTick();
        }

        /// <summary>Tic une fois par seconde (phases écoute blind, révélation image, etc.).</summary>
        public void PlayChronoTick(float linearVolume = 0.62f)
        {
            if (source == null || chronoTickClip == null) return;
            source.PlayOneShot(chronoTickClip, Mathf.Clamp01(linearVolume));
        }

        public void SetBroadcastDuckMultiplier(float linear01)
        {
            duckMultiplier = Mathf.Clamp01(linear01);
            RefreshSfxVolumes();
        }

        private void RefreshSfxVolumes()
        {
            if (source != null)
            {
                source.volume = volume * duckMultiplier;
            }

            if (crowdSource != null)
            {
                crowdSource.volume = volume * duckMultiplier;
            }

            if (popSource != null)
            {
                popSource.volume = volume * duckMultiplier;
            }

            ApplyBlindLoopVolume();
        }

        /// <summary>
        /// L'extrait blind ne suit pas le duck broadcast (même bus que les SFX) : sinon la voix ou un état « speaking »
        /// peut laisser le volume à ~5–72 % ou masquer l'écoute alors que le fond Theme est déjà coupé.
        /// </summary>
        private void ApplyBlindLoopVolume()
        {
            if (blindLoop == null)
            {
                return;
            }

            if (blindLoop.clip == null)
            {
                blindLoop.volume = 0f;
                return;
            }

            blindLoop.volume = Mathf.Clamp01(volume) * 0.88f;
        }

        public void PlayTap()
        {
            if (source != null && tapClip != null)
            {
                source.PlayOneShot(tapClip);
            }
        }

        /// <summary>Petit "pop" UI pour les overlays live (mot trouvé, bonus, etc.).</summary>
        public void PlayUiPop(float linearVolume = 0.22f)
        {
            if (tapClip == null) return;
            if (popSource != null)
            {
                popSource.pitch = UnityEngine.Random.Range(0.94f, 1.07f);
                popSource.PlayOneShot(tapClip, Mathf.Clamp01(linearVolume));
                return;
            }

            if (source == null) return;
            float prev = source.pitch;
            source.pitch = UnityEngine.Random.Range(0.94f, 1.07f);
            source.PlayOneShot(tapClip, Mathf.Clamp01(linearVolume));
            source.pitch = prev;
        }

        /// <summary>Cue blind test (tam-tam court, sans attendre la musique de fond).</summary>
        public void PlayBlindDrumCue()
        {
            if (source == null) return;
            AudioClip drum = ProceduralClips.BuildTamTamIntro();
            source.PlayOneShot(drum, 0.88f);
            Destroy(drum, 2f);
        }

        public void PlayBlindDemoMusic(int seed, string streamingFileBase = null, string remoteUrl = null, string questionContext = null)
        {
            blindPendingQuestionContext = questionContext ?? "";
            StopBlindDemoMusic();
            if (blindLoop == null) return;
            blindLoop.loop = true;
            blindMusicCo = StartCoroutine(CoLoadAndPlayBlindMusic(seed, streamingFileBase, remoteUrl));
        }

        public void StopBlindDemoMusic()
        {
            SaveBlindPlaybackCursor();
            if (blindMusicCo != null)
            {
                StopCoroutine(blindMusicCo);
                blindMusicCo = null;
            }

            if (blindLoop != null)
            {
                blindLoop.Stop();
                blindLoop.clip = null;
            }

            if (blindLoopClip != null)
            {
                Destroy(blindLoopClip);
                blindLoopClip = null;
            }
            blindCurrentPlaybackKey = null;

            RefreshSfxVolumes();
        }

        private IEnumerator CoLoadAndPlayBlindMusic(int seed, string streamingFileBase, string remoteUrl)
        {
            // Ne pas bloquer sur IsSpeakingNow (reste souvent vrai : file TTS, flags). La phase écoute démarre après CoWaitHostSilence.
            float cap = Time.unscaledTime + 0.45f;
            while (AIHostManager.Instance != null && AIHostManager.Instance.IsSpeakingNow && Time.unscaledTime < cap)
            {
                yield return null;
            }

            AudioClip loaded = null;
            string lastRejectedSource = "";
            string lastRejectedReason = "";
            string url = (remoteUrl ?? "").Trim();
            if (url.Length > 10
                && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                && StreamingMediaUrlPolicy.IsNonStreamableContentPageUrl(url))
            {
                StreamingMediaUrlPolicy.LogOnceRejected("Blind (audioUrl)", url, "Importez l’audio en .mp3 local (StreamingAssets) ou un lien direct vers un .mp3/.ogg si vous avez hébergé le fichier.");
                lastRejectedSource = url;
                lastRejectedReason = "URL non streamable (page web, pas un fichier audio direct)";
            }
            else if (url.Length > 10
                && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                && !StreamingMediaUrlPolicy.IsNonStreamableContentPageUrl(url))
            {
                AudioType t = GuessAudioTypeFromUrl(url);
                using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, t))
                {
                    yield return uwr.SendWebRequest();
                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        if (TryExtractDownloadedClip(uwr, out AudioClip c, out string reason))
                        {
                            loaded = c;
                        }
                        else
                        {
                            lastRejectedSource = url;
                            lastRejectedReason = reason;
                            Debug.LogWarning("[BlindTest] Audio URL rejeté: " + url + " | " + reason);
                        }
                    }
                    else
                    {
                        lastRejectedSource = url;
                        lastRejectedReason = "Erreur réseau: " + uwr.error;
                        Debug.LogWarning("[BlindTest] Erreur réseau audio URL: " + url + " | " + uwr.error);
                    }
                }
            }

            if (loaded == null && !string.IsNullOrWhiteSpace(streamingFileBase))
            {
                string[] baseCandidates = BuildBlindFileBaseCandidates(streamingFileBase);
                string[] exts = { ".ogg", ".mp3", ".wav" };
                if (StreamingAssetsUrl.IsWebGlData)
                {
                    string[] relRoots = { "Theme/BlindTest", "Theme/playlist", "Theme" };
                    foreach (string r in relRoots)
                    {
                        foreach (string baseClean in baseCandidates)
                        {
                            foreach (string ext in exts)
                            {
                                string u = StreamingAssetsUrl.UrlForRelativePath(r + "/" + baseClean + ext);
                                AudioType at = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".mp3" ? AudioType.MPEG : AudioType.WAV;
                                using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(u, at))
                                {
                                    yield return uwr.SendWebRequest();
                                    if (uwr.result == UnityWebRequest.Result.Success)
                                    {
                                        if (TryExtractDownloadedClip(uwr, out AudioClip c, out string reason))
                                        {
                                            loaded = c;
                                            break;
                                        }
                                        lastRejectedSource = u;
                                        lastRejectedReason = reason;
                                        Debug.LogWarning("[BlindTest] Fichier webgl rejeté: " + u + " | " + reason);
                                    }
                                    else
                                    {
                                        lastRejectedSource = u;
                                        lastRejectedReason = "Erreur réseau: " + uwr.error;
                                    }
                                }
                            }

                            if (loaded != null)
                            {
                                break;
                            }
                        }

                        if (loaded != null)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    string[] rootFolders = CollectBlindAudioRootFolders();
                    foreach (string root in rootFolders)
                    {
                        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                        {
                            continue;
                        }

                        foreach (string baseClean in baseCandidates)
                        {
                            foreach (string ext in exts)
                            {
                                string full = Path.Combine(root, baseClean + ext);
                                if (!File.Exists(full))
                                {
                                    continue;
                                }

                                AudioType at = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".mp3" ? AudioType.MPEG : AudioType.WAV;
                                string uri = StreamingAssetsUrl.ToRequestUrl(full);
                                using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uri, at))
                                {
                                    yield return uwr.SendWebRequest();
                                    if (uwr.result == UnityWebRequest.Result.Success)
                                    {
                                        if (TryExtractDownloadedClip(uwr, out AudioClip c, out string reason))
                                        {
                                            loaded = c;
                                            break;
                                        }
                                        lastRejectedSource = full;
                                        lastRejectedReason = reason;
                                        Debug.LogWarning("[BlindTest] Fichier local rejeté (FMOD): " + full + " | " + reason);
                                    }
                                    else
                                    {
                                        lastRejectedSource = full;
                                        lastRejectedReason = "Erreur réseau locale: " + uwr.error;
                                    }
                                }

                                // Repli identique ThemeMusicPlayer : FMOD refuse souvent MP3/OGG sous Windows — WAV natif ou ffmpeg.
                                if (loaded == null && File.Exists(full))
                                {
                                    AudioClip bypass = TryLoadBlindTrackBypassingFmod(full);
                                    if (bypass != null)
                                    {
                                        loaded = bypass;
                                        break;
                                    }
                                }
                            }

                            if (loaded != null)
                            {
                                break;
                            }
                        }

                        if (loaded != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (loaded == null)
            {
                if (!string.IsNullOrWhiteSpace(lastRejectedSource))
                {
                    Debug.LogWarning("[BlindTest] Fallback audio procédural utilisé. Dernière source rejetée: " + lastRejectedSource + " | " + lastRejectedReason);
                }
                loaded = ProceduralClips.BuildBlindMusicStub(seed);
            }

            if (loaded != null && string.IsNullOrWhiteSpace(loaded.name))
            {
                loaded.name = "blind_fallback_or_loaded";
            }

            blindLoopClip = loaded;
            string playbackKey = BuildBlindPlaybackKey(streamingFileBase, remoteUrl, loaded);
            blindCurrentPlaybackKey = playbackKey;
            blindLoop.clip = blindLoopClip;
            float startAt = ResolveBlindPlaybackStartSeconds(playbackKey, loaded.length, blindPendingQuestionContext);
            if (!string.IsNullOrWhiteSpace(playbackKey)
                && blindPlaybackCursorByKey.TryGetValue(playbackKey, out float saved)
                && saved > 0.05f
                && saved < Mathf.Max(0.1f, loaded.length - 0.1f))
            {
                startAt = saved;
            }

            if (loaded.length > 0.1f)
            {
                blindLoop.time = Mathf.Clamp(startAt, 0f, Mathf.Max(0f, loaded.length - 0.02f));
            }
            blindLoop.Play();
            RefreshSfxVolumes();
            blindMusicCo = null;
        }

        private static string[] CollectBlindAudioRootFolders()
        {
            var roots = new List<string>(8);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void add(string p)
            {
                if (string.IsNullOrWhiteSpace(p)) return;
                try
                {
                    string full = Path.GetFullPath(p);
                    if (seen.Add(full)) roots.Add(full);
                }
                catch
                {
                    if (seen.Add(p)) roots.Add(p);
                }
            }

            // Sources standard du projet Unity.
            add(Path.Combine(Application.streamingAssetsPath, "Theme", "BlindTest"));
            add(Path.Combine(Application.streamingAssetsPath, "Theme", "playlist"));
            add(Path.Combine(Application.streamingAssetsPath, "Theme"));

#if UNITY_EDITOR
            // En éditeur : supporte aussi la playlist racine du dépôt (C:/Congogame/playlist).
            try
            {
                add(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "playlist")));
            }
            catch { }
#endif

            // En build : supporte un dossier "playlist" à côté du .exe.
            try
            {
                string gameFolder = Directory.GetParent(Application.dataPath)?.FullName;
                if (!string.IsNullOrWhiteSpace(gameFolder))
                {
                    add(Path.Combine(gameFolder, "playlist"));
                }
            }
            catch { }

            return roots.ToArray();
        }

        private void SaveBlindPlaybackCursor()
        {
            if (blindLoop == null || blindLoop.clip == null || string.IsNullOrWhiteSpace(blindCurrentPlaybackKey))
            {
                return;
            }

            float len = blindLoop.clip.length;
            if (len <= 0.1f)
            {
                return;
            }

            float pos = Mathf.Clamp(blindLoop.time, 0f, len - 0.01f);
            if (pos < 0.05f)
            {
                return;
            }

            blindPlaybackCursorByKey[blindCurrentPlaybackKey] = pos;
        }

        private static string BuildBlindPlaybackKey(string streamingFileBase, string remoteUrl, AudioClip clip)
        {
            string url = (remoteUrl ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(url))
            {
                return "url:" + url.ToLowerInvariant();
            }

            string baseName = (streamingFileBase ?? "").Trim();
            if (!string.IsNullOrWhiteSpace(baseName))
            {
                if (TryResolveTrackAlias(baseName, out string alias) && !string.IsNullOrWhiteSpace(alias))
                {
                    return "base:" + NormalizeForMatch(alias);
                }
                return "base:" + NormalizeForMatch(baseName);
            }

            if (clip != null && !string.IsNullOrWhiteSpace(clip.name))
            {
                return "clip:" + NormalizeForMatch(clip.name);
            }

            return "";
        }

        private static float ResolveBlindPlaybackStartSeconds(string playbackKey, float clipLength, string questionContext)
        {
            if (clipLength <= 0.2f)
            {
                return 0f;
            }

            string keyN = NormalizeForMatch(playbackKey);
            string ctxN = NormalizeForMatch(questionContext);

            if (TryResolveBlindStartFromOverrides(keyN, ctxN, clipLength, out float fromOverrides))
            {
                return fromOverrides;
            }

            bool isBlemixDelinquant = keyN.Contains("blemix") && keyN.Contains("delinquant");
            if (isBlemixDelinquant)
            {
                return Mathf.Clamp(10f, 0f, Mathf.Max(0f, clipLength - 0.2f));
            }

            bool isSiboyMobali = keyN.Contains("siboy") && keyN.Contains("mobali");
            bool artistQuestion = IsArtistQuestionContext(ctxN);
            bool titleQuestion = IsTitleQuestionContext(ctxN);
            if (isSiboyMobali)
            {
                if (artistQuestion)
                {
                    return 0f;
                }

                if (titleQuestion)
                {
                    return Mathf.Clamp(clipLength * 0.72f, 0f, Mathf.Max(0f, clipLength - 0.2f));
                }

                return Mathf.Clamp(clipLength * 0.58f, 0f, Mathf.Max(0f, clipLength - 0.2f));
            }

            // Règle générale demandée: éviter les toutes premières secondes.
            return Mathf.Clamp(clipLength * 0.5f, 0f, Mathf.Max(0f, clipLength - 0.2f));
        }

        private static bool TryResolveBlindStartFromOverrides(string normalizedPlaybackKey, string normalizedQuestionContext, float clipLength, out float startSec)
        {
            startSec = 0f;
            LoadBlindStartRulesOnce();
            if (blindStartRules == null || blindStartRules.Length == 0)
            {
                return false;
            }

            bool artistQuestion = IsArtistQuestionContext(normalizedQuestionContext);
            bool titleQuestion = IsTitleQuestionContext(normalizedQuestionContext);
            foreach (BlindStartRule rule in blindStartRules)
            {
                if (rule == null || string.IsNullOrWhiteSpace(rule.match))
                {
                    continue;
                }

                if (!IsLooseRuleMatch(normalizedPlaybackKey, NormalizeForMatch(rule.match)))
                {
                    continue;
                }

                string scope = (rule.questionScope ?? "any").Trim().ToLowerInvariant();
                if (scope == "artist" && !artistQuestion) continue;
                if (scope == "title" && !titleQuestion) continue;

                startSec = ComputeStartFromRule(rule.mode, rule.value, clipLength);
                return true;
            }

            // Même sans règle matchée: fallback configurable JSON.
            startSec = Mathf.Clamp(clipLength * Mathf.Clamp01(blindDefaultStartPercent), 0f, Mathf.Max(0f, clipLength - 0.2f));
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

        private static void LoadBlindStartRulesOnce()
        {
            if (blindStartRulesLoaded)
            {
                return;
            }

            blindStartRulesLoaded = true;
            try
            {
                TextAsset ta = Resources.Load<TextAsset>("Datasets/audio_playback_start_overrides");
                if (ta == null || string.IsNullOrWhiteSpace(ta.text))
                {
                    blindStartRules = Array.Empty<BlindStartRule>();
                    return;
                }

                AudioPlaybackStartOverridesFile parsed = JsonUtility.FromJson<AudioPlaybackStartOverridesFile>(ta.text);
                blindStartRules = parsed?.blindRules ?? Array.Empty<BlindStartRule>();
                if (parsed != null)
                {
                    blindDefaultStartPercent = Mathf.Clamp01(parsed.blindDefaultStartPercent);
                }
            }
            catch
            {
                blindStartRules = Array.Empty<BlindStartRule>();
            }
        }

        private static bool IsArtistQuestionContext(string normalizedContext)
        {
            if (string.IsNullOrWhiteSpace(normalizedContext))
            {
                return false;
            }

            return normalizedContext.Contains("artiste")
                || normalizedContext.Contains("chanteur")
                || normalizedContext.Contains("interprete")
                || normalizedContext.Contains("type artist")
                || normalizedContext.Contains("qui interprete");
        }

        private static bool IsTitleQuestionContext(string normalizedContext)
        {
            if (string.IsNullOrWhiteSpace(normalizedContext))
            {
                return false;
            }

            return normalizedContext.Contains("titre")
                || normalizedContext.Contains("type title")
                || normalizedContext.Contains("nom du titre");
        }

        private static AudioType GuessAudioTypeFromUrl(string urlLower)
        {
            string u = urlLower.ToLowerInvariant();
            if (u.Contains(".ogg")) return AudioType.OGGVORBIS;
            if (u.Contains(".wav")) return AudioType.WAV;
            if (u.Contains(".mp3")) return AudioType.MPEG;
            return AudioType.UNKNOWN;
        }

        private static string[] BuildBlindFileBaseCandidates(string streamingFileBase)
        {
            string baseClean = (streamingFileBase ?? "").Trim();
            if (string.IsNullOrWhiteSpace(baseClean))
            {
                return Array.Empty<string>();
            }

            var candidates = new List<string>(4);
            AddUniqueCandidate(candidates, baseClean);

            if (TryResolveTrackAlias(baseClean, out string alias))
            {
                AddUniqueCandidate(candidates, alias);
            }

            if (!IsTrackKey(baseClean) && TryResolveTrackKeyFromNamedBase(baseClean, out string trackKey))
            {
                AddUniqueCandidate(candidates, trackKey);
                if (TryResolveTrackAlias(trackKey, out string aliasFromTrack))
                {
                    AddUniqueCandidate(candidates, aliasFromTrack);
                }
            }

            if (TryResolveLoosePlaylistBase(baseClean, out string bestLoose))
            {
                AddUniqueCandidate(candidates, bestLoose);
            }

            return candidates.ToArray();
        }

        private static void AddUniqueCandidate(List<string> candidates, string candidate)
        {
            if (candidates == null || string.IsNullOrWhiteSpace(candidate))
            {
                return;
            }

            string c = candidate.Trim();
            for (int i = 0; i < candidates.Count; i++)
            {
                if (string.Equals(candidates[i], c, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            candidates.Add(c);
        }

        private static bool TryResolveTrackAlias(string trackBase, out string resolvedBase)
        {
            resolvedBase = null;
            if (string.IsNullOrWhiteSpace(trackBase))
            {
                return false;
            }

            string key = trackBase.Trim();
            if (!IsTrackKey(key))
            {
                return false;
            }

            if (blindTrackAliasCache == null)
            {
                blindTrackAliasCache = BuildBlindTrackAliasCache();
            }

            if (blindTrackAliasCache != null && blindTrackAliasCache.TryGetValue(key, out string alias) && !string.IsNullOrWhiteSpace(alias))
            {
                resolvedBase = alias;
                return true;
            }

            // Dernier repli: garantir une piste jouable même si la métadonnée
            // ne peut pas être associée exactement au fichier.
            string byIndex = ResolveTrackAliasByIndex(key);
            if (!string.IsNullOrWhiteSpace(byIndex))
            {
                resolvedBase = byIndex;
                return true;
            }

            return false;
        }

        private static bool TryResolveTrackKeyFromNamedBase(string namedBase, out string trackKey)
        {
            trackKey = null;
            if (string.IsNullOrWhiteSpace(namedBase))
            {
                return false;
            }

            string normalizedInput = NormalizeForMatch(namedBase);
            if (string.IsNullOrWhiteSpace(normalizedInput))
            {
                return false;
            }

            try
            {
                string metaPath = Path.Combine(Application.streamingAssetsPath, "Datasets", "blind_playlist_meta.json");
                if (!File.Exists(metaPath))
                {
                    return false;
                }

                string json = File.ReadAllText(metaPath);
                BlindMetaFileForAlias meta = JsonUtility.FromJson<BlindMetaFileForAlias>(json);
                if (meta?.items == null || meta.items.Length == 0)
                {
                    return false;
                }

                foreach (BlindMetaItemForAlias item in meta.items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.fileBase))
                    {
                        continue;
                    }

                    if (!IsTrackKey(item.fileBase))
                    {
                        continue;
                    }

                    string joined = (item.artist ?? "") + " - " + (item.title ?? "");
                    string joinedN = NormalizeForMatch(joined);
                    if (!string.IsNullOrWhiteSpace(joinedN) && string.Equals(joinedN, normalizedInput, StringComparison.Ordinal))
                    {
                        trackKey = item.fileBase.Trim();
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // On reste permissif : le blind test continue même si ce mapping échoue.
            }

            return false;
        }

        private static bool TryResolveLoosePlaylistBase(string rawBase, out string resolvedBase)
        {
            resolvedBase = null;
            if (string.IsNullOrWhiteSpace(rawBase))
            {
                return false;
            }

            if (StreamingAssetsUrl.IsWebGlData)
            {
                return false;
            }

            string key = rawBase.Trim();
            if (blindLooseBaseCache == null)
            {
                blindLooseBaseCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            else if (blindLooseBaseCache.TryGetValue(key, out string cached) && !string.IsNullOrWhiteSpace(cached))
            {
                resolvedBase = cached;
                return true;
            }

            try
            {
                string playlistDir = Path.Combine(Application.streamingAssetsPath, "Theme", "playlist");
                if (!Directory.Exists(playlistDir))
                {
                    return false;
                }

                string target = NormalizeForMatch(key);
                if (string.IsNullOrWhiteSpace(target))
                {
                    return false;
                }

                string[] files = Directory.GetFiles(playlistDir);
                int bestScore = -1;
                string best = null;
                foreach (string f in files)
                {
                    string ext = Path.GetExtension(f);
                    if (!ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                        && !ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string baseName = Path.GetFileNameWithoutExtension(f);
                    string norm = NormalizeForMatch(baseName);
                    if (string.IsNullOrWhiteSpace(norm))
                    {
                        continue;
                    }

                    int score = 0;
                    if (string.Equals(norm, target, StringComparison.Ordinal))
                    {
                        score += 1000;
                    }
                    if (norm.Contains(target) || target.Contains(norm))
                    {
                        score += 200;
                    }

                    string[] toksA = norm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    string[] toksB = target.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int overlap = 0;
                    for (int i = 0; i < toksA.Length; i++)
                    {
                        for (int j = 0; j < toksB.Length; j++)
                        {
                            if (toksA[i] == toksB[j])
                            {
                                overlap++;
                                break;
                            }
                        }
                    }
                    score += overlap * 7;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = baseName;
                    }
                }

                if (!string.IsNullOrWhiteSpace(best) && bestScore >= 12)
                {
                    blindLooseBaseCache[key] = best;
                    resolvedBase = best;
                    return true;
                }
            }
            catch (Exception)
            {
                // Pas bloquant.
            }

            return false;
        }

        private static bool IsTrackKey(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string t = text.Trim().ToLowerInvariant();
            if (!t.StartsWith("track", StringComparison.Ordinal) || t.Length <= 5)
            {
                return false;
            }

            for (int i = 5; i < t.Length; i++)
            {
                if (!char.IsDigit(t[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseTrackNumber(string trackKey, out int trackNumber)
        {
            trackNumber = 0;
            if (!IsTrackKey(trackKey))
            {
                return false;
            }

            string t = trackKey.Trim().Substring(5);
            return int.TryParse(t, out trackNumber) && trackNumber > 0;
        }

        private static Dictionary<string, string> BuildBlindTrackAliasCache()
        {
            var cache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string metaPath = Path.Combine(Application.streamingAssetsPath, "Datasets", "blind_playlist_meta.json");
                string playlistDir = Path.Combine(Application.streamingAssetsPath, "Theme", "playlist");
                if (!File.Exists(metaPath) || !Directory.Exists(playlistDir))
                {
                    return cache;
                }

                string json = File.ReadAllText(metaPath);
                BlindMetaFileForAlias meta = JsonUtility.FromJson<BlindMetaFileForAlias>(json);
                if (meta?.items == null || meta.items.Length == 0)
                {
                    return cache;
                }

                string[] files = Directory.GetFiles(playlistDir);
                foreach (BlindMetaItemForAlias item in meta.items)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.fileBase) || !IsTrackKey(item.fileBase))
                    {
                        continue;
                    }

                    string alias = FindBestPlaylistFileBase(files, item.artist, item.title);
                    if (!string.IsNullOrWhiteSpace(alias))
                    {
                        cache[item.fileBase.Trim()] = alias;
                    }
                }
            }
            catch (Exception)
            {
                // Ne pas casser le flux blind test si la résolution d'alias échoue.
            }

            return cache;
        }

        private static string ResolveTrackAliasByIndex(string trackKey)
        {
            if (!TryParseTrackNumber(trackKey, out int trackNumber))
            {
                return null;
            }

            try
            {
                string playlistDir = Path.Combine(Application.streamingAssetsPath, "Theme", "playlist");
                if (!Directory.Exists(playlistDir))
                {
                    return null;
                }

                string[] files = Directory.GetFiles(playlistDir);
                var audio = new List<string>();
                for (int i = 0; i < files.Length; i++)
                {
                    string ext = Path.GetExtension(files[i]);
                    if (ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                        || ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                        || ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {
                        audio.Add(files[i]);
                    }
                }

                if (audio.Count == 0)
                {
                    return null;
                }

                audio.Sort(StringComparer.OrdinalIgnoreCase);
                int idx = (trackNumber - 1) % audio.Count;
                if (idx < 0 || idx >= audio.Count)
                {
                    return null;
                }

                return Path.GetFileNameWithoutExtension(audio[idx]);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string FindBestPlaylistFileBase(string[] files, string artist, string title)
        {
            if (files == null || files.Length == 0)
            {
                return null;
            }

            string artistN = NormalizeForMatch(artist);
            string titleN = NormalizeForMatch(title);
            int bestScore = -1;
            string bestBase = null;

            foreach (string f in files)
            {
                string ext = Path.GetExtension(f);
                if (!ext.Equals(".ogg", StringComparison.OrdinalIgnoreCase)
                    && !ext.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
                    && !ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string stem = Path.GetFileNameWithoutExtension(f);
                string stemN = NormalizeForMatch(stem);
                int score = 0;
                if (!string.IsNullOrWhiteSpace(titleN) && stemN.Contains(titleN))
                {
                    score += 3;
                }

                if (!string.IsNullOrWhiteSpace(artistN) && stemN.Contains(artistN))
                {
                    score += 2;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestBase = stem;
                }
            }

            return bestScore >= 2 ? bestBase : null;
        }

        private static string NormalizeForMatch(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string decomposed = raw.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);
            for (int i = 0; i < decomposed.Length; i++)
            {
                char c = decomposed[i];
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(c);
                if (cat == UnicodeCategory.NonSpacingMark)
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

            return sb.ToString().Trim();
        }

        [Serializable]
        private sealed class BlindMetaFileForAlias
        {
            public BlindMetaItemForAlias[] items;
        }

        [Serializable]
        private sealed class BlindMetaItemForAlias
        {
            public string fileBase;
            public string artist;
            public string title;
        }

        [Serializable]
        private sealed class AudioPlaybackStartOverridesFile
        {
            public float blindDefaultStartPercent = 0.5f;
            public BlindStartRule[] blindRules;
        }

        [Serializable]
        private sealed class BlindStartRule
        {
            public string match;
            public string mode;
            public float value;
            public string questionScope;
        }

        private static bool TryExtractDownloadedClip(UnityWebRequest uwr, out AudioClip clip, out string reason)
        {
            clip = null;
            reason = "";
            if (uwr == null || uwr.downloadHandler == null || uwr.downloadedBytes <= 0)
            {
                reason = "download vide ou null";
                return false;
            }

            // Evite de tenter un décodage audio si le serveur renvoie une page HTML/JSON d'erreur.
            string contentType = uwr.GetResponseHeader("Content-Type");
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                string ct = contentType.ToLowerInvariant();
                if (ct.Contains("text/") || ct.Contains("application/json") || ct.Contains("application/xml") || ct.Contains("html"))
                {
                    reason = "content-type non audio: " + contentType;
                    return false;
                }
            }

            try
            {
                clip = DownloadHandlerAudioClip.GetContent(uwr);
                if (clip == null || clip.loadState == AudioDataLoadState.Failed)
                {
                    reason = clip == null ? "clip null après décodage" : "loadState failed";
                    clip = null;
                    return false;
                }

                reason = "ok";
                return true;
            }
            catch (Exception ex)
            {
                reason = "exception décodage: " + ex.Message;
                clip = null;
                return false;
            }
        }

        /// <summary>Stop acclamations / rires (appelé avant la question suivante pour ne pas chevaucher le son).</summary>
        public void StopFeedbackOneShots()
        {
            if (source != null) source.Stop();
            if (crowdSource != null) crowdSource.Stop();
        }

        public void PlayResult(bool correct, bool hostVoiceCommentary = true, bool neutralNoWrongTone = false)
        {
            if (source == null) return;
            StopFeedbackOneShots();
            if (hostVoiceCommentary)
            {
                if (!neutralNoWrongTone || correct)
                {
                    LiaPunchlineBank.SpeakResultReaction(correct);
                }
            }

            if (correct)
            {
                source.PlayOneShot(okClip, 1.12f);
                if (cheerClip != null && crowdSource != null)
                {
                    crowdSource.PlayOneShot(cheerClip, 1.08f);
                }

                FeedbackVfxController.Instance?.PlayCorrect();
            }
            else if (neutralNoWrongTone)
            {
                if (tapClip != null) source.PlayOneShot(tapClip, 0.28f);
            }
            else
            {
                source.PlayOneShot(badClip, 1.05f);
                FeedbackVfxController.Instance?.PlayWrong();
            }
        }

        /// <summary>Même stratégie que <see cref="ThemeMusicPlayer"/> : éviter FMOD sur fichier local.</summary>
        private static AudioClip TryLoadBlindTrackBypassingFmod(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                return null;
            }

            string ext = Path.GetExtension(fullPath);
            if (ext.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                if (ThemeMusicPlayer.TryLoadPcmWavFileIntoClip(fullPath, out AudioClip w) && w != null)
                {
                    return w;
                }
            }

            string wavTmp = ThemeMusicPlayer.TryTranscodeLocalAudioWithFfmpeg(fullPath);
            if (!string.IsNullOrEmpty(wavTmp) && File.Exists(wavTmp))
            {
                if (ThemeMusicPlayer.TryLoadPcmWavFileIntoClip(wavTmp, out AudioClip pcm) && pcm != null)
                {
                    return pcm;
                }
            }

            return null;
        }

        private void OnDestroy()
        {
            StopBlindDemoMusic();
            if (Instance == this) Instance = null;
        }
    }
}
