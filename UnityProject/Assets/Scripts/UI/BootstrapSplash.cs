using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.UI
{
    /// <summary>
    /// Écran de démarrage court (logo + drapeau) puis fondu — pas de texte technique (WebSocket, etc.).
    /// </summary>
    public class BootstrapSplash : MonoBehaviour
    {
        [SerializeField] private float holdSec = 1.1f;
        [SerializeField] private float fadeSec = 0.85f;

        private CanvasGroup group;

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
        }

        private void Start()
        {
            StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(holdSec);
            float t = 0f;
            while (t < fadeSec)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = 1f - Mathf.Clamp01(t / fadeSec);
                yield return null;
            }

            group.alpha = 0f;
            group.blocksRaycasts = false;
            Destroy(gameObject);
        }
    }
}
