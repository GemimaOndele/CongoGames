using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Mouvement subtil 2.5D (parallax) sur le bloc central : rendu plus « console » sans assets 3D lourds.
    /// Remplace partiellement l’attente d’une scène 3D complète (voir ROADMAP_UI_3D).
    /// </summary>
    [DisallowMultipleComponent]
    public class Ps5HudParallax : MonoBehaviour
    {
        [SerializeField] private float posAmp = 2.5f;
        [SerializeField] private float rotAmpDeg = 0.2f;
        [SerializeField] private float speed = 0.7f;

        private RectTransform rt;
        private Vector2 basePos;
        private Vector3 baseRot;

        private void Awake()
        {
            rt = transform as RectTransform;
            if (rt != null)
            {
                basePos = rt.anchoredPosition;
                baseRot = rt.localEulerAngles;
            }
        }

        private void OnDisable()
        {
            if (rt != null)
            {
                rt.anchoredPosition = basePos;
                rt.localEulerAngles = baseRot;
            }
        }

        private void Update()
        {
            if (rt == null) return;
            float t = Time.unscaledTime * speed;
            float px = Mathf.Sin(t * 0.9f) * posAmp;
            float py = Mathf.Cos(t * 0.7f) * (posAmp * 0.55f);
            float rz = Mathf.Sin(t * 0.55f) * rotAmpDeg;
            rt.anchoredPosition = basePos + new Vector2(px, py);
            Vector3 e = baseRot;
            e.z = baseRot.z + rz;
            rt.localEulerAngles = e;
        }
    }
}
