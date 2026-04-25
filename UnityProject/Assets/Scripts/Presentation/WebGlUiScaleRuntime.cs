using UnityEngine;
#if UNITY_WEBGL && !UNITY_EDITOR
using UnityEngine.InputSystem.EnhancedTouch;
#endif

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
            EnhancedTouchSupport.Enable();
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
            if (Touch.activeTouches.Count != 2)
            {
                _lastPinchDist = -1f;
                return;
            }

            UnityEngine.InputSystem.EnhancedTouch.Touch t0 = Touch.activeTouches[0];
            UnityEngine.InputSystem.EnhancedTouch.Touch t1 = Touch.activeTouches[1];
            float d = (t0.screenPosition - t1.screenPosition).magnitude;
            if (_lastPinchDist < 0.5f)
            {
                if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
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
