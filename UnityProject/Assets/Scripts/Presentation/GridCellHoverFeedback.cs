using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>Survol de case grille (mots croisés, etc.) sans masquer le texte.</summary>
    [RequireComponent(typeof(Image))]
    public class GridCellHoverFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Image img;
        private Color normal;

        private void Awake()
        {
            img = GetComponent<Image>();
            normal = img.color;
        }

        public void SetBaseColor(Color c)
        {
            normal = c;
            if (img != null)
            {
                img.color = c;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (img == null) return;
            // Reste fidèle à la couleur courante (ex: vert "mot trouvé"), puis applique juste un léger lift.
            normal = img.color;
            img.color = new Color(normal.r * 1.35f, normal.g * 1.35f, normal.b * 1.45f, Mathf.Min(1f, normal.a + 0.06f));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (img != null) img.color = normal;
        }
    }
}
