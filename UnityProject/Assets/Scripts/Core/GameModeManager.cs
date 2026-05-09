using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using CongoGames.AI;
using CongoGames.GameModes;
using CongoGames.Network;
using CongoGames.Presentation;

namespace CongoGames.Core
{
    [DefaultExecutionOrder(-100)]
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        [SerializeField] private float roundDurationSec = 120f;
        [Tooltip("Durée totale du bloc Quiz (plusieurs questions enchaînées avant le mini-jeu suivant).")]
        [SerializeField] private float quizSessionDurationSec = 180f;
        [Tooltip("Démo locale (éditeur et build .exe) : touches 1–9 pour choisir le mini-jeu tant que TikTok live n’est pas connecté et que le focus n’est pas dans un champ texte.")]
        [SerializeField] private bool keyboardPickModeInEditor = true;
        [Tooltip("Démo locale : garde le même mini-jeu en boucle au lieu d'enchaîner la rotation.")]
        [SerializeField] private bool lockToSingleModeInLocalDemo = false;
        [SerializeField] private string lockedModeId = "blind-test";

        private readonly Dictionary<string, IGameMode> modes = new Dictionary<string, IGameMode>();
        private readonly List<string> modeRotation = new List<string>
        {
            "quiz",
            "semantic",
            "word-scramble",
            "crossword-lite",
            "blind-test",
            "mystery-word",
            "memory",
            "speed-chrono",
            "image-guess"
        };

        private static readonly Dictionary<string, string> ModeLabels = new Dictionary<string, string>
        {
            { "quiz", "Quiz culture" },
            { "semantic", "Sémantique" },
            { "word-scramble", "Mots mélangés (mots mêlés)" },
            { "crossword-lite", "Mots croisés" },
            { "blind-test", "Blind test" },
            { "mystery-word", "Mot mystère" },
            { "memory", "Mémoire" },
            { "speed-chrono", "Chrono vitesse" },
            { "image-guess", "Devine l’image" }
        };
        private IGameMode activeMode;
        private int modeIndex = 0;
        private float timer;
        private float displayedRoundDuration;
        private Coroutine scheduleNextCo;
        private Coroutine restartKeepCo;
        private Coroutine modeTransitionOverlayCo;
        private Coroutine startModeCo;
        private bool modeTransitionLocked;

        /// <summary>Délai après la fin de voix IA avant déblocage / lancement effectif (aligné UX).</summary>
        public const float PostHostAnnounceDelaySec = 5f;
        /// <summary>Mots mélangés / mots croisés : ne pas enchaîner le mode suivant tant que les 2 grilles thématiques ne sont pas validées (démo) ; en live, le minuteur peut être dépassé.</summary>
        private bool gridThematicBlockComplete;

        /// <summary>Arg1 = mode quitté (vide si premier lancement), Arg2 = mode entrant — pour brefs overlays de transition.</summary>
        public event Action<string, string> OnModeTransition;

        public float RoundDuration => displayedRoundDuration;
        public float RoundTimeRemaining => Mathf.Max(0f, timer);
        public IReadOnlyList<string> ModeRotation => modeRotation;

        public void SetRoundTimeRemaining(float seconds)
        {
            float s = Mathf.Max(0f, seconds);
            timer = s;
            displayedRoundDuration = Mathf.Max(0.01f, s);
        }

        public void SetDefaultRoundDuration(float seconds)
        {
            roundDurationSec = Mathf.Clamp(seconds, 5f, 3600f);
            if (activeMode != null && !string.Equals(activeMode.ModeId, "quiz", StringComparison.OrdinalIgnoreCase))
            {
                displayedRoundDuration = roundDurationSec;
                timer = roundDurationSec;
            }
        }

        public void SetQuizSessionDuration(float seconds)
        {
            quizSessionDurationSec = Mathf.Clamp(seconds, 10f, 7200f);
            if (activeMode != null && string.Equals(activeMode.ModeId, "quiz", StringComparison.OrdinalIgnoreCase))
            {
                displayedRoundDuration = quizSessionDurationSec;
                timer = quizSessionDurationSec;
            }
        }

        public void SetLocalDemoModeLock(bool enabled, string modeId)
        {
            lockToSingleModeInLocalDemo = enabled;
            if (!string.IsNullOrEmpty(modeId))
            {
                lockedModeId = modeId;
            }
        }

        public bool IsLocalDemoModeLocked => lockToSingleModeInLocalDemo;
        public string LockedModeId => lockedModeId ?? "";
        public string ActiveModeId => activeMode != null ? activeMode.ModeId : "";

        public void SetGridThematicBlockComplete()
        {
            gridThematicBlockComplete = true;
        }

        /// <summary>Allonge le chrono quand on enchaîne la 2e grille thématique (démo : deux sessions dans le même mode).</summary>
        public void ExtendModeTime(float extraSeconds)
        {
            if (extraSeconds <= 0f) return;
            timer += extraSeconds;
            displayedRoundDuration += extraSeconds;
        }

        public static bool IsGridThematicModeId(string id)
        {
            return string.Equals(id, "word-scramble", StringComparison.Ordinal)
                   || string.Equals(id, "crossword-lite", StringComparison.Ordinal);
        }

        public string ActiveModeDisplayName
        {
            get
            {
                if (activeMode == null)
                {
                    return "—";
                }

                return ModeLabels.TryGetValue(activeMode.ModeId, out string label) ? label : activeMode.ModeId;
            }
        }

        public static string GetModeDisplayName(string modeId)
        {
            if (string.IsNullOrEmpty(modeId))
            {
                return "—";
            }

            return ModeLabels.TryGetValue(modeId, out string label) ? label : modeId;
        }

        private void Awake()
        {
            Instance = this;
            displayedRoundDuration = roundDurationSec;
            EnsureBuiltinModes();
        }

        private void OnDestroy()
        {
            if (scheduleNextCo != null)
            {
                StopCoroutine(scheduleNextCo);
                scheduleNextCo = null;
            }

            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            RegisterModesFromScene();
            if (modeRotation.Count > 0)
            {
                modeIndex = UnityEngine.Random.Range(0, modeRotation.Count);
                StartMode(modeRotation[modeIndex]);
            }
        }

        private void EnsureBuiltinModes()
        {
            if (transform.Find("BuiltinModes") != null)
            {
                return;
            }

            GameObject root = new GameObject("BuiltinModes");
            root.transform.SetParent(transform, false);
            root.AddComponent<QuizMode>();
            root.AddComponent<SemanticMode>();
            root.AddComponent<WordScrambleMode>();
            root.AddComponent<CrosswordLiteMode>();
            root.AddComponent<BlindTestMode>();
            root.AddComponent<MysteryWordMode>();
            root.AddComponent<MemoryMode>();
            root.AddComponent<SpeedChronoMode>();
            root.AddComponent<ImageGuessMode>();
        }

        private void Update()
        {
            if (modeTransitionLocked)
            {
                return;
            }

            if (keyboardPickModeInEditor && !IsLiveTikTokConnected() && EventSystem.current != null)
            {
                GameObject sel = EventSystem.current.currentSelectedGameObject;
                if (sel == null || sel.GetComponent<InputField>() == null)
                {
                    if (GameInput.TryGetModeSlotKey0To8Down(out int slot) && slot < modeRotation.Count)
                    {
                        modeIndex = slot;
                        string selected = modeRotation[slot];
                        if (lockToSingleModeInLocalDemo)
                        {
                            lockedModeId = selected;
                        }
                        StartMode(selected);
                        return;
                    }
                }
            }

            if (activeMode == null) return;

            timer -= Time.deltaTime;
            activeMode.Tick(Time.deltaTime);

            if (timer <= 0f)
            {
                if (activeMode != null
                    && IsGridThematicModeId(activeMode.ModeId)
                    && !gridThematicBlockComplete
                    && !IsLiveTikTokConnected())
                {
                    timer = roundDurationSec;
                    return;
                }

                if (lockToSingleModeInLocalDemo && !IsLiveTikTokConnected() && !string.IsNullOrEmpty(lockedModeId))
                {
                    StartMode(lockedModeId);
                }
                else
                {
                    NextMode();
                }
            }
        }

        public void RegisterMode(IGameMode mode)
        {
            if (!modes.ContainsKey(mode.ModeId))
            {
                modes.Add(mode.ModeId, mode);
            }
        }

        public void StartMode(string modeId)
        {
            if (!modes.TryGetValue(modeId, out IGameMode mode))
            {
                Debug.LogWarning("Unknown mode: " + modeId);
                return;
            }

            if (scheduleNextCo != null)
            {
                StopCoroutine(scheduleNextCo);
                scheduleNextCo = null;
            }
            if (restartKeepCo != null)
            {
                StopCoroutine(restartKeepCo);
                restartKeepCo = null;
            }
            if (modeTransitionOverlayCo != null)
            {
                StopCoroutine(modeTransitionOverlayCo);
                modeTransitionOverlayCo = null;
            }
            if (startModeCo != null)
            {
                StopCoroutine(startModeCo);
                startModeCo = null;
            }
            modeTransitionLocked = false;

            string fromId = activeMode != null ? activeMode.ModeId : "";
            if (string.Equals(fromId, "quiz", StringComparison.OrdinalIgnoreCase) && ScoreManager.Instance != null)
            {
                int hi = ScoreManager.Instance.GetHighestScoreAmongPlayers();
                if (hi > 0)
                {
                    ScoreHistoryStore.RegisterHighWaterIfNeeded(hi);
                }
            }

            bool transitionBetweenModes = !string.IsNullOrEmpty(fromId) && fromId != modeId;
            if (transitionBetweenModes)
            {
                modeTransitionLocked = true;
                startModeCo = StartCoroutine(CoStartModeAfterAnnouncement(fromId, modeId, mode));
                return;
            }

            ApplyAndBeginMode(fromId, modeId, mode);
        }

        public void NextMode()
        {
            if (modeRotation.Count == 0) return;
            modeIndex = (modeIndex + 1) % modeRotation.Count;
            StartMode(modeRotation[modeIndex]);
        }

        /// <summary>
        /// Après une réponse locale : reste sur le même mini-jeu tant que le chrono de manche n'est pas épuisé,
        /// sinon enchaîne le mini-jeu suivant (live : rotation rapide conservée).
        /// </summary>
        public void AdvanceRoundOrNextMode()
        {
            if (lockToSingleModeInLocalDemo && activeMode != null)
            {
                RestartCurrentModeKeepTimer();
                return;
            }

            if (!IsLiveTikTokConnected() && RoundTimeRemaining > 0.05f)
            {
                RestartCurrentModeKeepTimer();
                return;
            }

            ScheduleNextMode(1.1f);
        }

        /// <summary>Enchaîne sur le mini-jeu suivant après un court délai (réponse locale, bonne ou mauvaise).</summary>
        public void ScheduleNextMode(float delaySec)
        {
            if (scheduleNextCo != null)
            {
                StopCoroutine(scheduleNextCo);
            }

            scheduleNextCo = StartCoroutine(CoScheduleNextMode(delaySec));
        }

        private IEnumerator CoScheduleNextMode(float delaySec)
        {
            yield return new WaitForSeconds(Mathf.Clamp(delaySec, 0.12f, 30f));
            scheduleNextCo = null;
            if (lockToSingleModeInLocalDemo && activeMode != null)
            {
                RestartCurrentModeKeepTimer();
                yield break;
            }

            NextMode();
        }

        private void RestartCurrentModeKeepTimer()
        {
            if (activeMode == null)
            {
                return;
            }

            if (restartKeepCo != null)
            {
                StopCoroutine(restartKeepCo);
            }

            restartKeepCo = StartCoroutine(CoRestartCurrentModeKeepTimer());
        }

        private IEnumerator CoRestartCurrentModeKeepTimer()
        {
            if (activeMode == null)
            {
                restartKeepCo = null;
                yield break;
            }

            string modeId = activeMode.ModeId;
            float keepTimer = Mathf.Max(0.01f, timer);
            float keepDisplayed = Mathf.Max(0.01f, displayedRoundDuration);

            float toastWaitUntil = Time.unscaledTime + 8f;
            while (MiniGamePanelContent.Instance != null
                && MiniGamePanelContent.Instance.IsScorePauseOverlayVisible
                && Time.unscaledTime < toastWaitUntil)
            {
                yield return null;
            }

            HostTransitionOverlay.Instance?.ShowQuestionIncoming();
            AIHostManager.Instance?.InterruptSpeech();
            AIHostManager.Instance?.Speak(LiaPunchlineBank.BuildNextQuestionLine(modeId));
            yield return CoWaitUntilHostSilent(45f);
            yield return new WaitForSecondsRealtime(PostHostAnnounceDelaySec);
            HostTransitionOverlay.Instance?.Hide();

            activeMode.End();
            ModeSurfaceController.Instance?.Apply(modeId);
            activeMode.Begin();
            timer = keepTimer;
            displayedRoundDuration = keepDisplayed;
            restartKeepCo = null;
        }

        private IEnumerator CoHideOverlayAfterHostAnnounce()
        {
            yield return CoWaitUntilHostSilent(45f);
            yield return new WaitForSecondsRealtime(PostHostAnnounceDelaySec);
            HostTransitionOverlay.Instance?.Hide();
            modeTransitionOverlayCo = null;
        }

        private IEnumerator CoStartModeAfterAnnouncement(string fromId, string toId, IGameMode mode)
        {
            float toastWaitUntil = Time.unscaledTime + 8f;
            while (MiniGamePanelContent.Instance != null
                && MiniGamePanelContent.Instance.IsScorePauseOverlayVisible
                && Time.unscaledTime < toastWaitUntil)
            {
                yield return null;
            }

            HostTransitionOverlay.Instance?.ShowNewGameIncoming(GetModeDisplayName(toId));
            AIHostManager.Instance?.InterruptSpeech();
            string liaLine = LiaPunchlineBank.BuildTransitionWithRules(fromId, toId);
            if (!string.IsNullOrEmpty(liaLine))
            {
                AIHostManager.Instance?.Speak(liaLine);
            }

            yield return CoWaitUntilHostSilent(45f);
            yield return new WaitForSecondsRealtime(PostHostAnnounceDelaySec);
            HostTransitionOverlay.Instance?.Hide();
            ApplyAndBeginMode(fromId, toId, mode);
            modeTransitionLocked = false;
            startModeCo = null;
        }

        private void ApplyAndBeginMode(string fromId, string modeId, IGameMode mode)
        {
            activeMode?.End();
            activeMode = mode;
            if (IsGridThematicModeId(modeId))
            {
                gridThematicBlockComplete = false;
            }

            float blockDuration = string.Equals(modeId, "quiz", StringComparison.OrdinalIgnoreCase)
                ? quizSessionDurationSec
                : roundDurationSec;
            displayedRoundDuration = blockDuration;
            timer = blockDuration;
            ModeSurfaceController.Instance?.Apply(modeId);
            activeMode.Begin();
            ThemeRuntime.NotifyModeStarted(modeId);
            if (!string.IsNullOrEmpty(fromId) && fromId != modeId)
            {
                try
                {
                    OnModeTransition?.Invoke(fromId, modeId);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private IEnumerator CoWaitUntilHostSilent(float maxSec)
        {
            float end = Time.unscaledTime + Mathf.Max(2f, maxSec);
            while (AIHostManager.Instance != null && AIHostManager.Instance.IsSpeakingNow && Time.unscaledTime < end)
            {
                yield return null;
            }
        }

        /// <summary>Live TikTok connecté : comportement présentateur / chat inchangé.</summary>
        public static bool IsLiveTikTokConnected()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            return live != null && live.IsConnected;
        }

        private void RegisterModesFromScene()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            var discovered = behaviours.OfType<IGameMode>();
            foreach (IGameMode mode in discovered)
            {
                RegisterMode(mode);
            }
        }
    }
}
