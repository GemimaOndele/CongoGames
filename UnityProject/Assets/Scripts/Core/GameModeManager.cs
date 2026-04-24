using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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
        [Tooltip("Démo locale : touches 1–9 (pavé ou ligne) pour choisir le mini-jeu (si le focus n’est pas dans un champ texte).")]
        [SerializeField] private bool keyboardPickModeInEditor = true;

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
            { "semantic", "Associations" },
            { "word-scramble", "Mots mélangés" },
            { "crossword-lite", "Mots croisés léger" },
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
        /// <summary>Mots mélangés / mots croisés : ne pas enchaîner le mode suivant tant que les 2 grilles thématiques ne sont pas validées (démo) ; en live, le minuteur peut être dépassé.</summary>
        private bool gridThematicBlockComplete;

        /// <summary>Arg1 = mode quitté (vide si premier lancement), Arg2 = mode entrant — pour brefs overlays de transition.</summary>
        public event Action<string, string> OnModeTransition;

        public float RoundDuration => displayedRoundDuration;
        public float RoundTimeRemaining => Mathf.Max(0f, timer);

        public void SetRoundTimeRemaining(float seconds)
        {
            float s = Mathf.Max(0f, seconds);
            timer = s;
            displayedRoundDuration = Mathf.Max(0.01f, s);
        }
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
            if (keyboardPickModeInEditor && !IsLiveTikTokConnected() && EventSystem.current != null)
            {
                GameObject sel = EventSystem.current.currentSelectedGameObject;
                if (sel == null || sel.GetComponent<InputField>() == null)
                {
                    for (int i = 0; i < modeRotation.Count && i < 9; i++)
                    {
                        if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
                        {
                            modeIndex = i;
                            StartMode(modeRotation[i]);
                            return;
                        }
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

                NextMode();
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

            string fromId = activeMode != null ? activeMode.ModeId : "";
            if (string.Equals(fromId, "quiz", StringComparison.OrdinalIgnoreCase) && ScoreManager.Instance != null)
            {
                int hi = ScoreManager.Instance.GetHighestScoreAmongPlayers();
                if (hi > 0)
                {
                    ScoreHistoryStore.RegisterHighWaterIfNeeded(hi);
                }
            }

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

        public void NextMode()
        {
            if (modeRotation.Count == 0) return;
            modeIndex = (modeIndex + 1) % modeRotation.Count;
            StartMode(modeRotation[modeIndex]);
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
            NextMode();
        }

        private static bool IsLiveTikTokConnected()
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
