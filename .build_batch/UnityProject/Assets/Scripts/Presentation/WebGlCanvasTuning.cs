using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Ajuste tous les <see cref="CanvasScaler"/> en build WebGL : textes plus lisibles
    /// + <see cref="PrefsKey"/> (zoom utilisateur, slider F10 / pincement).
    /// </summary>
    public static class WebGlCanvasTuning
    {
        public const string PrefsKey = "CongoWebGlUiScale";

        /// <summary>1 = taille de base Web (1280×720 de référence) ; &gt;1 = plus grand.</summary>
        public static void SetUserScale(float scale)
        {
            scale = Mathf.Clamp(scale, 0.72f, 1.55f);
            PlayerPrefs.SetFloat(PrefsKey, scale);
            PlayerPrefs.Save();
            RefreshAll();
        }

        public static float GetUserScale()
        {
            return Mathf.Clamp(PlayerPrefs.GetFloat(PrefsKey, 1f), 0.72f, 1.55f);
        }

        public static void ApplyToScaler(CanvasScaler s)
        {
            if (s == null)
            {
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            float u = GetUserScale();
            const float baseW = 1280f;
            const float baseH = 720f;
            s.referenceResolution = new Vector2(baseW / u, baseH / u);
            s.matchWidthOrHeight = 0.52f;
#endif
        }

        public static void RefreshAll()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Exclude);
            for (int i = 0; i < scalers.Length; i++)
            {
                ApplyToScaler(scalers[i]);
            }

            Canvas.ForceUpdateCanvases();
#endif
        }
    }
}
