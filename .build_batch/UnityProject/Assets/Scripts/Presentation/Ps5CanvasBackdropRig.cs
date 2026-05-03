using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Variation douce d’un calque sombre (vignettage) pour donner de la profondeur au fond plein écran.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public class Ps5CanvasBackdropRig : MonoBehaviour
    {
        [SerializeField] private float baseAlpha = 0.08f;
        [SerializeField] private float pulse = 0.02f;
        [SerializeField] private float speed = 0.4f;

        private Image img;
        private Color c0;

        private void Awake()
        {
            img = GetComponent<Image>();
            c0 = img != null ? img.color : Color.black;
        }

        private void Update()
        {
            if (img == null) return;
            float t = Time.unscaledTime * speed;
            float a = baseAlpha + Mathf.Sin(t) * pulse;
            Color c = c0;
            c.a = Mathf.Clamp01(a);
            img.color = c;
        }
    }
}
