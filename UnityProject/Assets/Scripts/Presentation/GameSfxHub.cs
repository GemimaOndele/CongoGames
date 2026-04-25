using System;
using System.Collections;
using System.IO;
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
        private AudioSource blindLoop;
        private AudioClip blindLoopClip;
        private Coroutine blindMusicCo;
        private AudioClip tapClip;
        private AudioClip okClip;
        private AudioClip badClip;
        private AudioClip cheerClip;
        private AudioClip chronoTickClip;
        public bool IsBlindMusicPlaying => blindLoop != null && blindLoop.isPlaying && blindLoop.clip != null;

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

            if (blindLoop != null)
            {
                // Bus blind un peu plus présent (extrait 30–60 s à écouter).
                blindLoop.volume = volume * 0.72f * duckMultiplier;
            }
        }

        public void PlayTap()
        {
            if (source != null && tapClip != null)
            {
                source.PlayOneShot(tapClip);
            }
        }

        /// <summary>Cue blind test (tam-tam court, sans attendre la musique de fond).</summary>
        public void PlayBlindDrumCue()
        {
            if (source == null) return;
            AudioClip drum = ProceduralClips.BuildTamTamIntro();
            source.PlayOneShot(drum, 0.88f);
            Destroy(drum, 2f);
        }

        public void PlayBlindDemoMusic(int seed, string streamingFileBase = null, string remoteUrl = null)
        {
            StopBlindDemoMusic();
            if (blindLoop == null) return;
            blindLoop.loop = true;
            blindMusicCo = StartCoroutine(CoLoadAndPlayBlindMusic(seed, streamingFileBase, remoteUrl));
        }

        public void StopBlindDemoMusic()
        {
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
        }

        private IEnumerator CoLoadAndPlayBlindMusic(int seed, string streamingFileBase, string remoteUrl)
        {
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
                string baseClean = streamingFileBase.Trim();
                string[] exts = { ".ogg", ".mp3", ".wav" };
                if (StreamingAssetsUrl.IsWebGlData)
                {
                    string[] relRoots = { "Theme/BlindTest", "Theme" };
                    foreach (string r in relRoots)
                    {
                        foreach (string ext in exts)
                        {
                            string u = StreamingAssetsUrl.UrlForRelativePath(r + "/" + baseClean + ext);
                            AudioType at = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".mp3" ? AudioType.UNKNOWN : AudioType.WAV;
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
                }
                else
                {
                    string[] rootFolders =
                    {
                        Path.Combine(Application.streamingAssetsPath, "Theme", "BlindTest"),
                        Path.Combine(Application.streamingAssetsPath, "Theme", "playlist"),
                        Path.Combine(Application.streamingAssetsPath, "Theme")
                    };
                    foreach (string root in rootFolders)
                    {
                        foreach (string ext in exts)
                        {
                            string full = Path.Combine(root, baseClean + ext);
                            if (!File.Exists(full))
                            {
                                continue;
                            }

                            AudioType at = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".mp3" ? AudioType.UNKNOWN : AudioType.WAV;
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
                                    Debug.LogWarning("[BlindTest] Fichier local rejeté: " + full + " | " + reason);
                                }
                                else
                                {
                                    lastRejectedSource = full;
                                    lastRejectedReason = "Erreur réseau locale: " + uwr.error;
                                }
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
            blindLoop.clip = blindLoopClip;
            blindLoop.Play();
            blindMusicCo = null;
        }

        private static AudioType GuessAudioTypeFromUrl(string urlLower)
        {
            string u = urlLower.ToLowerInvariant();
            if (u.Contains(".ogg")) return AudioType.OGGVORBIS;
            if (u.Contains(".wav")) return AudioType.WAV;
            if (u.Contains(".mp3")) return AudioType.UNKNOWN;
            return AudioType.UNKNOWN;
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
            AIHostManager.Instance?.InterruptSpeech();
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

        private void OnDestroy()
        {
            StopBlindDemoMusic();
            if (Instance == this) Instance = null;
        }
    }
}
