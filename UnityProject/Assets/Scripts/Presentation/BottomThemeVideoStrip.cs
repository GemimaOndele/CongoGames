using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Bande vidéo en bas d’écran, lue depuis une URL (fichier .mp4 / .webm direct).
    /// Le son sort ici pour ne pas dupliquer ThemeMusicPlayer quand la piste est une vidéo musicale.
    /// </summary>
    public class BottomThemeVideoStrip : MonoBehaviour
    {
        public static BottomThemeVideoStrip Instance { get; private set; }

        [SerializeField] private int renderWidth = 960;
        [SerializeField] private int renderHeight = 180;
        [SerializeField] private float audioVolume = 0.65f;

        private RawImage raw;
        private VideoPlayer video;
        private RenderTexture rt;
        private AudioSource audioSource;
        private string activeUrl = "";

        private void Awake()
        {
            Instance = this;
        }

        public void Bind(RawImage target)
        {
            raw = target;
            if (raw != null)
            {
                raw.raycastTarget = false;
            }
        }

        public void ApplyGameMode(string modeId)
        {
            RemoteModeMediaEntry cfg = RemoteThemeMediaConfig.Resolve(modeId);
            string url = cfg.bottomVideoUrl?.Trim() ?? "";
            if (string.IsNullOrEmpty(url))
            {
                StopStrip();
                gameObject.SetActive(false);
                return;
            }

            if (StreamingMediaUrlPolicy.IsNonStreamableContentPageUrl(url))
            {
                StreamingMediaUrlPolicy.LogOnceRejected("Bandeau (bottomVideoUrl)", url);
                StopStrip();
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (url == activeUrl && video != null && video.isPlaying) return;

            activeUrl = url;
            StopVideoOnly();
            StartCoroutine(PlayUrlRoutine(url));
        }

        private IEnumerator PlayUrlRoutine(string url)
        {
            yield return null;
            if (raw == null) yield break;

            rt = new RenderTexture(Mathf.Max(320, renderWidth), Mathf.Max(120, renderHeight), 0, RenderTextureFormat.ARGB32);
            rt.Create();
            raw.texture = rt;
            raw.color = Color.white;
            raw.uvRect = new Rect(0f, 0f, 1f, 1f);

            video = gameObject.AddComponent<VideoPlayer>();
            video.playOnAwake = false;
            video.isLooping = true;
            video.renderMode = VideoRenderMode.RenderTexture;
            video.targetTexture = rt;
            video.url = url;
            video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
                audioSource.volume = audioVolume;
            }

            video.controlledAudioTrackCount = 1;
            video.EnableAudioTrack(0, true);
            video.SetTargetAudioSource(0, audioSource);

            video.prepareCompleted += OnPrepared;
            video.errorReceived += OnError;
            video.Prepare();
        }

        private void OnPrepared(VideoPlayer source)
        {
            source.Play();
        }

        private void OnError(VideoPlayer source, string message)
        {
            Debug.LogWarning("BottomThemeVideoStrip: " + message);
            StopStrip();
            gameObject.SetActive(false);
        }

        private void StopVideoOnly()
        {
            if (video != null)
            {
                video.prepareCompleted -= OnPrepared;
                video.errorReceived -= OnError;
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

        private void StopStrip()
        {
            activeUrl = "";
            StopVideoOnly();
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        private void OnDestroy()
        {
            StopStrip();
            if (Instance == this) Instance = null;
        }
    }
}
