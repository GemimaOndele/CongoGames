using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>Build WebGL seulement : rafraîchit l’échelle UI + pincement 2 doigts.</summary>
    public sealed class WebGlUiScaleRuntime : MonoBehaviour
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        private float _lastPinchDist = -1f;
#endif
        private void OnEnable()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGlCanvasTuning.RefreshAll();
#endif
        }

        private void Start()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGlCanvasTuning.RefreshAll();
#endif
        }

        private void Update()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if (Input.touchCount != 2)
            {
                _lastPinchDist = -1f;
                return;
            }

            UnityEngine.Touch t0 = Input.GetTouch(0);
            UnityEngine.Touch t1 = Input.GetTouch(1);
            float d = (t0.position - t1.position).magnitude;
            if (_lastPinchDist < 0.5f)
            {
                if (t0.phase == UnityEngine.TouchPhase.Began || t1.phase == UnityEngine.TouchPhase.Began)
                {
                    _lastPinchDist = d;
                }
                return;
            }

            float delta = (d - _lastPinchDist) * 0.0018f;
            if (Mathf.Abs(delta) > 0.0001f)
            {
                _lastPinchDist = d;
                WebGlCanvasTuning.SetUserScale(WebGlCanvasTuning.GetUserScale() + delta);
            }
#endif
        }
    }
}
