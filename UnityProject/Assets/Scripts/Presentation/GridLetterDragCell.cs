using UnityEngine;
using UnityEngine.EventSystems;

namespace CongoGames.Presentation
{
    /// <summary>Point d’entrée glisser-déposer sur une case de lettre (délègue au coordinateur).</summary>
    public sealed class GridLetterDragCell : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private GridLetterDragCoordinator coord;
        private int cellIndex;

        public void Bind(GridLetterDragCoordinator c, int index)
        {
            coord = c;
            cellIndex = index;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            coord?.NotifyBeginDrag(cellIndex, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            coord?.NotifyDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            coord?.NotifyEndDrag(eventData);
        }
    }
}
