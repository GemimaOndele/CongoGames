using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Animation continue du bloc central (respiration) pour un rendu « vivant » proche du loader.
    /// </summary>
    public class HudPanelAnimator : MonoBehaviour
    {
        [SerializeField] private float amplitude = 0.022f;
        [SerializeField] private float speed = 1.65f;

        private RectTransform rt;
        private Vector3 baseScale;

        private void Awake()
        {
            rt = transform as RectTransform;
            baseScale = rt != null ? rt.localScale : Vector3.one;
        }

        private void Update()
        {
            if (rt == null) return;
            float wobble = 1f + Mathf.Sin(Time.unscaledTime * speed) * amplitude;
            float wobble2 = 1f + Mathf.Cos(Time.unscaledTime * (speed * 0.73f)) * (amplitude * 0.45f);
            float beat = ThemeMusicPlayer.NormalizedPhase01;
            float rhythm = 1f + Mathf.Sin(beat * Mathf.PI * 2f) * (amplitude * 0.4f);
            rt.localScale = baseScale * (wobble * wobble2 * rhythm);
        }
    }
}
