using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CongoGames.GameModes;

namespace CongoGames.Core
{
    public class GameModeManager : MonoBehaviour
    {
        public static GameModeManager Instance { get; private set; }

        [SerializeField] private float roundDurationSec = 10f;

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
        private IGameMode activeMode;
        private int modeIndex = 0;
        private float timer;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            RegisterModesFromScene();
            if (modeRotation.Count > 0)
            {
                StartMode(modeRotation[0]);
            }
        }

        private void Update()
        {
            if (activeMode == null) return;

            timer -= Time.deltaTime;
            activeMode.Tick(Time.deltaTime);

            if (timer <= 0f)
            {
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

            activeMode?.End();
            activeMode = mode;
            timer = roundDurationSec;
            activeMode.Begin();
        }

        public void NextMode()
        {
            if (modeRotation.Count == 0) return;
            modeIndex = (modeIndex + 1) % modeRotation.Count;
            StartMode(modeRotation[modeIndex]);
        }

        private void RegisterModesFromScene()
        {
            MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>(true);
            var discovered = behaviours.OfType<IGameMode>();
            foreach (IGameMode mode in discovered)
            {
                RegisterMode(mode);
            }
        }
    }
}
