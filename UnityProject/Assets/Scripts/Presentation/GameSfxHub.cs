using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using CongoGames.Audio;

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
        private AudioClip laughClip;

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
            laughClip = Resources.Load<AudioClip>("Audio/sfx_laugh") ?? ProceduralClips.BuildMockingLaugh();
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
            string url = (remoteUrl ?? "").Trim();
            if (url.Length > 10 && (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            {
                AudioType t = GuessAudioTypeFromUrl(url);
                using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, t))
                {
                    yield return uwr.SendWebRequest();
                    if (uwr.result == UnityWebRequest.Result.Success)
                    {
                        loaded = DownloadHandlerAudioClip.GetContent(uwr);
                    }
                }
            }

            if (loaded == null && !string.IsNullOrWhiteSpace(streamingFileBase))
            {
                string baseClean = streamingFileBase.Trim();
                string[] roots =
                {
                    Path.Combine(Application.streamingAssetsPath, "Theme", "BlindTest"),
                    Path.Combine(Application.streamingAssetsPath, "Theme")
                };

                string[] exts = { ".ogg", ".mp3", ".wav" };
                foreach (string root in roots)
                {
                    foreach (string ext in exts)
                    {
                        string full = Path.Combine(root, baseClean + ext);
                        if (!File.Exists(full))
                        {
                            continue;
                        }

                        AudioType at = ext == ".ogg" ? AudioType.OGGVORBIS : ext == ".mp3" ? AudioType.MPEG : AudioType.WAV;
                        string uri = new Uri(full).AbsoluteUri;
                        UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uri, at);
                        yield return uwr.SendWebRequest();
                        if (uwr.result == UnityWebRequest.Result.Success)
                        {
                            loaded = DownloadHandlerAudioClip.GetContent(uwr);
                            uwr.Dispose();
                            if (loaded != null && loaded.loadState != AudioDataLoadState.Failed)
                            {
                                break;
                            }

                            loaded = null;
                        }
                        else
                        {
                            uwr.Dispose();
                        }
                    }

                    if (loaded != null)
                    {
                        break;
                    }
                }
            }

            if (loaded == null)
            {
                loaded = ProceduralClips.BuildBlindMusicStub(seed);
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
            if (u.Contains(".mp3")) return AudioType.MPEG;
            return AudioType.MPEG;
        }

        /// <summary>Stop acclamations / rires (appelé avant la question suivante pour ne pas chevaucher le son).</summary>
        public void StopFeedbackOneShots()
        {
            if (source != null) source.Stop();
            if (crowdSource != null) crowdSource.Stop();
        }

        public void PlayResult(bool correct)
        {
            if (source == null) return;
            StopFeedbackOneShots();
            if (correct)
            {
                source.PlayOneShot(okClip, 1.12f);
                if (cheerClip != null && crowdSource != null)
                {
                    crowdSource.PlayOneShot(cheerClip, 1.08f);
                }

                FeedbackVfxController.Instance?.PlayCorrect();
            }
            else
            {
                source.PlayOneShot(badClip, 1.05f);
                if (laughClip != null && crowdSource != null)
                {
                    crowdSource.PlayOneShot(laughClip, 1.02f);
                }

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
