using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Variation d’échelle légère par mode (effet « carte »/console) + reste compatible avec HudPanelAnimator.
    /// </summary>
    [DisallowMultipleComponent]
    public class Ps5ModeVisualRig : MonoBehaviour
    {
        private RectTransform rt;
        private Vector3 baseScale;
        private string last = "";

        private void Awake()
        {
            rt = transform as RectTransform;
            baseScale = rt != null ? rt.localScale : Vector3.one;
        }

        private void OnEnable()
        {
            ThemeRuntime.OnModeStarted += OnMode;
        }

        private void OnDisable()
        {
            ThemeRuntime.OnModeStarted -= OnMode;
        }

        private void OnMode(string modeId)
        {
            if (rt == null) return;
            string id = (modeId ?? "").ToLowerInvariant();
            if (id == last) return;
            last = id;
            float f = 1f;
            switch (id)
            {
                case "quiz": f = 0.99f; break;
                case "blind-test": f = 1.03f; break;
                case "speed-chrono": f = 1.02f; break;
                case "word-scramble":
                case "crossword-lite": f = 0.98f; break;
                case "image-guess": f = 1.01f; break;
            }

            rt.localScale = baseScale * f;
        }
    }
}
