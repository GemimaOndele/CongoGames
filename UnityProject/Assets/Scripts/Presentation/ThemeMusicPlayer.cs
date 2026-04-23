using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using CongoGames.Audio;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Musique par mode : playlist track01/track02… dans Theme/&lt;mode&gt;/, sinon music.*, puis thème global, Resources, pad procédural.
    /// </summary>
    public class ThemeMusicPlayer : MonoBehaviour
    {
        public static ThemeMusicPlayer Instance { get; private set; }

        /// <summary>Phase 0–1 sur la piste en cours (pour synchroniser légèrement l’UI avec le rythme).</summary>
        public static float NormalizedPhase01 { get; private set; }

        [SerializeField] private float volume = 0.62f;
        [Tooltip("Optionnel : bus Audio Mixer (ex. Bus_Music) pour le mix broadcast.")]
        [SerializeField] private AudioMixerGroup musicOutputGroup;
        private float duckMultiplier = 1f;

        private AudioSource music;
        private Coroutine playlistRoutine;
        private readonly List<AudioClip> playlistClips = new List<AudioClip>();

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

        private void RefreshMusicVolume()
        {
            if (music != null)
            {
                music.volume = volume * duckMultiplier;
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
            if (music != null && music.isPlaying && music.clip != null && music.clip.length > 0.05f)
            {
                NormalizedPhase01 = Mathf.Repeat(music.time / music.clip.length, 1f);
            }
        }

        public void ApplyGameMode(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId.Trim().ToLowerInvariant();
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
            RemoteModeMediaEntry remote = RemoteThemeMediaConfig.Resolve(modeId);
            bool bottomProvidesAudio = !string.IsNullOrWhiteSpace(remote.bottomVideoUrl);

            if (!bottomProvidesAudio && !string.IsNullOrWhiteSpace(remote.musicUrl))
            {
                AudioClip httpClip = null;
                yield return DownloadClipFromHttp(remote.musicUrl.Trim(), c => httpClip = c);
                if (httpClip != null)
                {
                    yield return CoPlayWithIntro(httpClip, true);
                    yield break;
                }
            }

            if (bottomProvidesAudio)
            {
                yield break;
            }

            string root = Path.Combine(Application.streamingAssetsPath, "Theme");
            string folder = Path.Combine(root, modeId);
            List<string> trackPaths = new List<string>();
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

                trackPaths.Sort(StringComparer.OrdinalIgnoreCase);
            }

            if (trackPaths.Count == 0 && Directory.Exists(root))
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

                trackPaths.Sort(StringComparer.OrdinalIgnoreCase);
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

                if (playlistClips.Count >= 2)
                {
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
                    yield return CoPlayWithIntro(one, true);
                    yield break;
                }
            }

            string[] names = { "music.ogg", "music.wav", "music.mp3", "theme.ogg", "theme.wav" };
            foreach (string file in names)
            {
                string sub = Path.Combine(folder, file);
                if (!File.Exists(sub))
                {
                    continue;
                }

                AudioClip subClip = null;
                yield return DownloadClip(sub, file, c => subClip = c);
                if (subClip != null)
                {
                    yield return CoPlayWithIntro(subClip, true);
                    yield break;
                }
            }

            string resPath = "Audio/theme_" + SanitizeForResources(modeId);
            AudioClip res = Resources.Load<AudioClip>(resPath);
            if (res != null)
            {
                yield return CoPlayWithIntro(res, true);
                yield break;
            }

            if (modeId == "quiz" || modeId == "default")
            {
                foreach (string file in names)
                {
                    string globalPath = Path.Combine(root, file);
                    if (!File.Exists(globalPath))
                    {
                        continue;
                    }

                    AudioClip gClip = null;
                    yield return DownloadClip(globalPath, file, c => gClip = c);
                    if (gClip != null)
                    {
                        yield return CoPlayWithIntro(gClip, true);
                        yield break;
                    }
                }

                res = Resources.Load<AudioClip>("Audio/theme");
                if (res != null)
                {
                    yield return CoPlayWithIntro(res, true);
                    yield break;
                }
            }

            yield return CoProceduralModeLoop(modeId);
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
                float len = pad != null && pad.length > 0.2f ? pad.length : 6f;
                len = Mathf.Clamp(len, 5f, 12f);
                yield return new WaitForSeconds(len * 0.96f);
                music.Stop();
                Destroy(pad);
                phase++;
            }
        }

        private IEnumerator PlayPlaylistLoop()
        {
            bool needIntro = true;
            int i = 0;
            while (enabled && playlistClips.Count > 0)
            {
                AudioClip c = playlistClips[i % playlistClips.Count];
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

                float len = c != null && c.length > 0.1f ? c.length : 120f;
                yield return new WaitForSeconds(len * 0.995f);
                i++;
            }
        }

        private IEnumerator DownloadClip(string fullPath, string fileName, Action<AudioClip> assign)
        {
            string uri = FileUrl(fullPath);
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
            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(uri, audioType);
            req.timeout = 60;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            assign?.Invoke(clip);
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

        private static string FileUrl(string path)
        {
            return "file:///" + Path.GetFullPath(path).Replace("\\", "/");
        }
    }
}
