using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.UI
{
    /// <summary>
    /// Intro plein écran : léger zoom + fondu (remplace l’ancien fondu seul).
    /// </summary>
    public class IntroSplashController : MonoBehaviour
    {
        [SerializeField] private float holdSec = 1.35f;
        [SerializeField] private float fadeSec = 0.9f;
        [SerializeField] private float titlePulseSec = 1.1f;

        private CanvasGroup group;
        private RectTransform titleRt;
        private Vector3 titleBaseScale = Vector3.one;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 1f;
            group.blocksRaycasts = true;
            transform.SetAsLastSibling();

            Transform t = transform.Find("SplashTitle");
            if (t != null)
            {
                titleRt = t.GetComponent<RectTransform>();
                if (titleRt != null)
                {
                    titleBaseScale = titleRt.localScale;
                }
            }
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            float t0 = 0f;
            while (t0 < titlePulseSec)
            {
                t0 += Time.unscaledDeltaTime;
                if (titleRt != null)
                {
                    float u = t0 / titlePulseSec;
                    float s = 1f + 0.08f * Mathf.Sin(u * Mathf.PI);
                    titleRt.localScale = titleBaseScale * s;
                }

                yield return null;
            }

            if (titleRt != null)
            {
                titleRt.localScale = titleBaseScale;
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, holdSec - titlePulseSec));

            float t = 0f;
            while (t < fadeSec)
            {
                t += Time.unscaledDeltaTime;
                if (group != null)
                {
                    group.alpha = 1f - Mathf.Clamp01(t / fadeSec);
                }

                yield return null;
            }

            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }

            Destroy(gameObject);
        }
    }
}
