using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CongoGames.Presentation;

namespace CongoGames.Audio
{
    /// <summary>
    /// BGM optionnelle par mini-jeu (clips dans Assets/Audio/BGM) + SFX live.
    /// En mode <see cref="blendDedicatedClipsWithStreamingMusic"/> (défaut), les pistes Theme restent
    /// et les clips Inspector se superposent à faible volume si assignés.
    /// Si <see cref="preferDedicatedBgm"/> est activé sans blend, la musique StreamingAssets est remplacée.
    /// Le dossier <c>playlist/</c> reste réservé au blind test (non géré ici).
    /// </summary>
    public sealed class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager Instance { get; private set; }

        [Header("Musique — Theme/ + clips projet (recommandé)")]
        [Tooltip(
            "Activé : la musique chargée depuis Theme/ (StreamingAssets) continue ; les clips BGM ci-dessous "
            + "s’ajoutent en surcouche (volume « overlay ») quand un clip existe pour le mode. "
            + "Désactivé : seul le thème classique joue, sauf si « remplacer » est coché.")]
        [SerializeField] private bool blendDedicatedClipsWithStreamingMusic = true;

        [Range(0f, 1f)]
        [Tooltip("Volume de la surcouche BGM (Inspector) quand le blend est actif. La piste Theme reste au niveau normal du ThemeMusicPlayer.")]
        [SerializeField] private float dedicatedOverlayVolume = 0.24f;

        [Tooltip(
            "Sans clip Inspector ni fichier Resources : joue un pad procédural léger par mode (deuxième couche audio). "
            + "Désactiver pour n’entendre que la BGM Theme/ seule tant qu’aucun clip n’est assigné.")]
        [SerializeField] private bool proceduralOverlayWhenNoAsset = true;

        [Header("Optionnel — remplacer entièrement la BGM Theme")]
        [Tooltip(
            "Désactivé (défaut) : garder Theme + overlay si blend. Activé : les clips Inspector remplacent "
            + "Theme pour ce mode (comme avant) ; ignore le blend.")]
        [SerializeField] private bool preferDedicatedBgm;

        [Header("BGM — assigner des fichiers dans Assets/Audio/BGM")]
        [SerializeField] private AudioClip lobbyTheme;
        [SerializeField] private AudioClip quizTheme;
        [SerializeField] private AudioClip battleTheme;
        [SerializeField] private AudioClip speedChronoTheme;
        [SerializeField] private AudioClip memoryTheme;
        [SerializeField] private AudioClip wordScrambleTheme;
        [SerializeField] private AudioClip crosswordTheme;
        [SerializeField] private AudioClip mysteryWordTheme;
        [SerializeField] private AudioClip semanticTheme;
        [SerializeField] private AudioClip imageToWordTheme;
        [SerializeField] private AudioClip blindTestTheme;

        [Header("SFX — assigner dans Assets/Audio/SFX (ou laisser vide → GameSfxHub)")]
        [SerializeField] private AudioClip correctAnswer;
        [SerializeField] private AudioClip wrongAnswer;
        [SerializeField] private AudioClip giftReceived;
        [SerializeField] private AudioClip newViewer;
        [SerializeField] private AudioClip battleStart;
        [SerializeField] private AudioClip roundWin;
        [SerializeField] private AudioClip timerTick;
        [SerializeField] private AudioClip timerUrgent;
        [SerializeField] private AudioClip crowdCheer;

        [Header("Volumes")]
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 0.6f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

        private float broadcastDuckMul = 1f;

        private static readonly float FadeQuiz = 2f;
        private static readonly float FadeBattle = 0.8f;
        private static readonly float FadeSpeed = 0.5f;
        private static readonly float FadeMemory = 2.5f;
        private static readonly float FadeScramble = 1.5f;
        private static readonly float FadeCrossword = 2f;
        private static readonly float FadeMystery = 1.8f;
        private static readonly float FadeSemantic = 2f;
        private static readonly float FadeImage = 1.5f;
        private static readonly float FadeLobby = 3f;
        private static readonly float FadeBlind = 1.2f;

        private AudioSource bgmA;
        private AudioSource bgmB;
        private AudioSource overlayBgm;
        private AudioSource sfxSrc;
        private Coroutine fadeCo;
        private Coroutine overlayFadeCo;
        private bool usingA = true;
        private bool inspectorBgmActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            bgmA = gameObject.AddComponent<AudioSource>();
            bgmB = gameObject.AddComponent<AudioSource>();
            overlayBgm = gameObject.AddComponent<AudioSource>();
            sfxSrc = gameObject.AddComponent<AudioSource>();

            bgmA.loop = true;
            bgmB.loop = true;
            overlayBgm.loop = true;
            bgmA.playOnAwake = false;
            bgmB.playOnAwake = false;
            overlayBgm.playOnAwake = false;
            bgmA.volume = 0f;
            bgmB.volume = 0f;
            overlayBgm.volume = 0f;
            overlayBgm.priority = 80;
            sfxSrc.playOnAwake = false;
            sfxSrc.spatialBlend = 0f;
        }

        private void OnDestroy()
        {
            foreach (KeyValuePair<string, AudioClip> kv in proceduralOverlayByMode)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }

            proceduralOverlayByMode.Clear();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Appelé depuis <see cref="ThemeRuntime.NotifyModeStarted"/> avant <see cref="ThemeMusicPlayer.ApplyGameMode"/>.
        /// Retourne <c>true</c> si la BGM inspector remplace Theme (pas de chargement StreamingAssets pour ce mode).
        /// </summary>
        public bool TryBeginModeBgm(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "quiz" : modeId.Trim().ToLowerInvariant();

            if (blendDedicatedClipsWithStreamingMusic && !preferDedicatedBgm)
            {
                StopExclusiveInspectorBgmOnly(0.35f);
                return false;
            }

            if (!preferDedicatedBgm)
            {
                StopInspectorBgm(0.35f);
                StopOverlayBgm(0.35f);
                return false;
            }

            if (string.Equals(id, "blind-test", System.StringComparison.Ordinal)
                || string.Equals(id, "image-guess", System.StringComparison.Ordinal))
            {
                StopInspectorBgm(0.35f);
                StopOverlayBgm(0.35f);
                return false;
            }

            AudioClip clip = GetClipForMode(id);
            if (clip == null)
            {
                StopInspectorBgm(0.35f);
                StopOverlayBgm(0.35f);
                return false;
            }

            StopOverlayBgm(0.25f);
            ThemeMusicPlayer.Instance?.SuppressStreamingBgmForExternalManager();
            float fade = GetFadeForMode(id);
            PlayBgm(clip, fade);
            inspectorBgmActive = true;
            return true;
        }

        /// <summary>
        /// Après <see cref="ThemeMusicPlayer.ApplyGameMode"/> : démarre une surcouche BGM si blend + clip assigné.
        /// </summary>
        public void ScheduleBlendOverlayAfterTheme(string modeId)
        {
            if (!blendDedicatedClipsWithStreamingMusic || preferDedicatedBgm)
            {
                return;
            }

            string id = string.IsNullOrEmpty(modeId) ? "quiz" : modeId.Trim().ToLowerInvariant();
            if (string.Equals(id, "blind-test", System.StringComparison.Ordinal)
                || string.Equals(id, "image-guess", System.StringComparison.Ordinal))
            {
                StopOverlayBgm(0.35f);
                return;
            }

            if (overlayFadeCo != null)
            {
                StopCoroutine(overlayFadeCo);
                overlayFadeCo = null;
            }

            overlayFadeCo = StartCoroutine(CoFadeOverlayForMode(id));
        }

        private IEnumerator CoFadeOverlayForMode(string modeId)
        {
            yield return null;
            yield return null;

            AudioClip clip = ResolveOverlayClip(modeId);
            if (clip == null)
            {
                float s = overlayBgm != null ? overlayBgm.volume : 0f;
                float t2 = 0f;
                const float fd = 0.35f;
                while (t2 < fd && overlayBgm != null)
                {
                    t2 += Time.unscaledDeltaTime;
                    overlayBgm.volume = Mathf.Lerp(s, 0f, Mathf.Clamp01(t2 / fd));
                    yield return null;
                }

                if (overlayBgm != null)
                {
                    overlayBgm.Stop();
                    overlayBgm.clip = null;
                }

                overlayFadeCo = null;
                yield break;
            }

            if (overlayBgm.clip == clip && overlayBgm.isPlaying)
            {
                ApplyOverlayEffectiveVolume();
                overlayFadeCo = null;
                yield break;
            }

            overlayBgm.clip = clip;
            overlayBgm.volume = 0f;
            overlayBgm.Play();
            float target = Mathf.Clamp01(dedicatedOverlayVolume) * broadcastDuckMul;
            float t = 0f;
            const float dur = 1.1f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                overlayBgm.volume = Mathf.Lerp(0f, target, k);
                yield return null;
            }

            overlayBgm.volume = target;
            overlayFadeCo = null;
        }

        private IEnumerator FadeOverlayVolume(float to, float duration)
        {
            float from = overlayBgm != null ? overlayBgm.volume : 0f;
            float t = 0f;
            while (t < duration && overlayBgm != null)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / duration);
                overlayBgm.volume = Mathf.Lerp(from, to, k);
                yield return null;
            }

            if (overlayBgm != null)
            {
                overlayBgm.volume = to;
            }
        }

        private void StopOverlayBgm(float fadeDuration)
        {
            if (overlayBgm == null || (!overlayBgm.isPlaying && overlayBgm.clip == null))
            {
                return;
            }

            if (overlayFadeCo != null)
            {
                StopCoroutine(overlayFadeCo);
                overlayFadeCo = null;
            }

            overlayFadeCo = StartCoroutine(CoStopOverlay(fadeDuration));
        }

        private IEnumerator CoStopOverlay(float fadeDuration)
        {
            yield return FadeOverlayVolume(0f, Mathf.Max(0.05f, fadeDuration));
            if (overlayBgm != null)
            {
                overlayBgm.Stop();
                overlayBgm.clip = null;
            }

            overlayFadeCo = null;
        }

        private void ApplyOverlayEffectiveVolume()
        {
            if (overlayBgm != null && overlayBgm.isPlaying)
            {
                overlayBgm.volume = Mathf.Clamp01(dedicatedOverlayVolume) * broadcastDuckMul;
            }
        }

        /// <summary>Arrête uniquement les pistes « exclusives » A/B, pas l’overlay blend.</summary>
        private void StopExclusiveInspectorBgmOnly(float fadeDuration)
        {
            if (!inspectorBgmActive && !bgmA.isPlaying && !bgmB.isPlaying)
            {
                return;
            }

            if (fadeCo != null)
            {
                StopCoroutine(fadeCo);
            }

            fadeCo = StartCoroutine(CoFadeOutAll(Mathf.Max(0.05f, fadeDuration)));
        }

        public void SetBroadcastDuckMultiplier(float linear01)
        {
            broadcastDuckMul = Mathf.Clamp01(linear01);
            ApplyImmediateBgmDuck();
        }

        public void PlayBgm(AudioClip clip, float fadeDuration = 1.5f)
        {
            if (clip == null)
            {
                return;
            }

            AudioSource current = usingA ? bgmA : bgmB;
            if (current.clip == clip && current.isPlaying)
            {
                return;
            }

            if (fadeCo != null)
            {
                StopCoroutine(fadeCo);
            }

            fadeCo = StartCoroutine(CoCrossFade(clip, Mathf.Max(0.05f, fadeDuration)));
        }

        public void StopInspectorBgm(float fadeDuration = 1f)
        {
            if (!inspectorBgmActive && !bgmA.isPlaying && !bgmB.isPlaying)
            {
                return;
            }

            if (fadeCo != null)
            {
                StopCoroutine(fadeCo);
            }

            fadeCo = StartCoroutine(CoFadeOutAll(Mathf.Max(0.05f, fadeDuration)));
        }

        private IEnumerator CoCrossFade(AudioClip nextClip, float duration)
        {
            AudioSource outSrc = usingA ? bgmA : bgmB;
            AudioSource inSrc = usingA ? bgmB : bgmA;

            inSrc.clip = nextClip;
            inSrc.volume = 0f;
            inSrc.Play();

            float t = 0f;
            float startOut = outSrc.volume;
            float target = EffectiveBgmVolume;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(t / duration);
                outSrc.volume = Mathf.Lerp(startOut, 0f, ratio);
                inSrc.volume = Mathf.Lerp(0f, target, ratio);
                yield return null;
            }

            outSrc.Stop();
            outSrc.clip = null;
            inSrc.volume = target;
            usingA = !usingA;
            inspectorBgmActive = true;
            fadeCo = null;
        }

        private IEnumerator CoFadeOutAll(float duration)
        {
            AudioSource active = usingA ? bgmA : bgmB;
            AudioSource other = usingA ? bgmB : bgmA;
            float startA = bgmA.volume;
            float startB = bgmB.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float ratio = Mathf.Clamp01(t / duration);
                bgmA.volume = Mathf.Lerp(startA, 0f, ratio);
                bgmB.volume = Mathf.Lerp(startB, 0f, ratio);
                yield return null;
            }

            bgmA.Stop();
            bgmB.Stop();
            bgmA.clip = null;
            bgmB.clip = null;
            inspectorBgmActive = false;
            fadeCo = null;
        }

        private float EffectiveBgmVolume => Mathf.Clamp01(bgmVolume) * broadcastDuckMul;

        private void ApplyImmediateBgmDuck()
        {
            float v = EffectiveBgmVolume;
            if (bgmA.isPlaying)
            {
                bgmA.volume = v;
            }

            if (bgmB.isPlaying)
            {
                bgmB.volume = v;
            }

            ApplyOverlayEffectiveVolume();
        }

        public void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSrc == null)
            {
                return;
            }

            sfxSrc.PlayOneShot(clip, sfxVolume);
        }

        public void OnLobby() => PlayBgm(lobbyTheme, FadeLobby);

        public void OnQuizStart() => PlayBgm(quizTheme, FadeQuiz);

        public void OnBattleStart()
        {
            if (battleTheme != null)
            {
                PlayBgm(battleTheme, FadeBattle);
            }

            PlaySfxOrFallbackBattle();
        }

        public void OnSpeedChronoStart() => PlayBgm(speedChronoTheme, FadeSpeed);
        public void OnMemoryStart() => PlayBgm(memoryTheme, FadeMemory);
        public void OnWordScrambleStart() => PlayBgm(wordScrambleTheme, FadeScramble);
        public void OnCrosswordStart() => PlayBgm(crosswordTheme, FadeCrossword);
        public void OnMysteryWordStart() => PlayBgm(mysteryWordTheme, FadeMystery);
        public void OnSemanticStart() => PlayBgm(semanticTheme, FadeSemantic);
        public void OnImageToWordStart() => PlayBgm(imageToWordTheme, FadeImage);
        public void OnBlindTestRound() => PlayBgm(blindTestTheme, FadeBlind);

        public void OnLiveCorrectAnswer()
        {
            if (correctAnswer != null)
            {
                PlaySfx(correctAnswer);
                if (crowdCheer != null)
                {
                    PlaySfx(crowdCheer);
                }
            }
            else
            {
                GameSfxHub.Instance?.PlayResult(true, hostVoiceCommentary: false);
            }
        }

        public void OnLiveWrongAnswer()
        {
            if (wrongAnswer != null)
            {
                PlaySfx(wrongAnswer);
            }
            else
            {
                GameSfxHub.Instance?.PlayResult(false, hostVoiceCommentary: false);
            }
        }

        public void OnGiftReceived()
        {
            if (giftReceived != null)
            {
                PlaySfx(giftReceived);
            }
            else
            {
                GameSfxHub.Instance?.PlayUiPop(0.35f);
            }
        }

        public void OnNewViewer()
        {
            if (newViewer != null)
            {
                PlaySfx(newViewer);
            }
            else
            {
                GameSfxHub.Instance?.PlayUiPop(0.28f);
            }
        }

        public void OnRoundWin()
        {
            if (roundWin != null)
            {
                PlaySfx(roundWin);
            }
            else
            {
                GameSfxHub.Instance?.PlayUiPop(0.45f);
            }

            StopInspectorBgm(1.5f);
        }

        public void OnTimerTick()
        {
            if (timerTick != null)
            {
                PlaySfx(timerTick);
            }
            else
            {
                GameSfxHub.Instance?.PlayChronoTick(0.58f);
            }
        }

        public void OnTimerUrgent()
        {
            if (timerUrgent != null)
            {
                PlaySfx(timerUrgent);
            }
            else
            {
                GameSfxHub.Instance?.PlayChronoTick(0.85f);
            }
        }

        private void PlaySfxOrFallbackBattle()
        {
            if (battleStart != null)
            {
                PlaySfx(battleStart);
            }
            else
            {
                GameSfxHub.Instance?.PlayUiPop(0.5f);
            }
        }

        private AudioClip GetClipForMode(string modeId)
        {
            switch (modeId)
            {
                case "quiz": return quizTheme;
                case "semantic": return semanticTheme;
                case "word-scramble": return wordScrambleTheme;
                case "crossword-lite": return crosswordTheme;
                case "mystery-word": return mysteryWordTheme;
                case "memory": return memoryTheme;
                case "speed-chrono": return speedChronoTheme;
                case "image-guess": return imageToWordTheme;
                case "lobby":
                case "menu":
                    return lobbyTheme;
                default: return null;
            }
        }

        /// <summary>
        /// Inspector → Resources/Audio/BgmOverlay/&lt;mode&gt; → pad procédural (cache par mode).
        /// </summary>
        private AudioClip ResolveOverlayClip(string modeId)
        {
            AudioClip c = GetClipForMode(modeId);
            if (c != null)
            {
                return c;
            }

            string id = string.IsNullOrEmpty(modeId) ? "quiz" : modeId.Trim().ToLowerInvariant();
            c = Resources.Load<AudioClip>("Audio/BgmOverlay/" + id);
            if (c != null)
            {
                return c;
            }

            if (!proceduralOverlayWhenNoAsset)
            {
                return null;
            }

            if (!proceduralOverlayByMode.TryGetValue(id, out AudioClip pad) || pad == null)
            {
                pad = ProceduralClips.BuildAmbientPadForMode(id);
                proceduralOverlayByMode[id] = pad;
            }

            return pad;
        }

        private static float GetFadeForMode(string modeId)
        {
            switch (modeId)
            {
                case "quiz": return FadeQuiz;
                case "semantic": return FadeSemantic;
                case "word-scramble": return FadeScramble;
                case "crossword-lite": return FadeCrossword;
                case "mystery-word": return FadeMystery;
                case "memory": return FadeMemory;
                case "speed-chrono": return FadeSpeed;
                case "image-guess": return FadeImage;
                case "blind-test": return FadeBlind;
                default: return 1.5f;
            }
        }
    }
}
