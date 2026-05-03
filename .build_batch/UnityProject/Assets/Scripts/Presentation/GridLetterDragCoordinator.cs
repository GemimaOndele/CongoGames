using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Glisser sur les cases (mots croisés / mots mélangés) pour enchaîner des lettres + ligne de parcours.
    /// </summary>
    public sealed class GridLetterDragCoordinator : MonoBehaviour
    {
        private static Sprite lineSprite;

        private MiniGamePanelContent demo;
        private bool crossword;
        private Text[] cellTexts;
        private RectTransform[] cellRects;
        private RectTransform lineRoot;
        private Canvas rootCanvas;
        private readonly List<Image> linePool = new List<Image>(40);
        private readonly List<int> path = new List<int>(48);
        private bool dragging;

        public void Initialize(MiniGamePanelContent owner, bool isCrosswordMode, Text[] texts)
        {
            demo = owner;
            crossword = isCrosswordMode;
            cellTexts = texts;
            rootCanvas = GetComponentInParent<Canvas>();
            if (texts == null)
            {
                return;
            }

            cellRects = new RectTransform[texts.Length];
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].transform.parent is RectTransform rt)
                {
                    cellRects[i] = rt;
                    GridLetterDragCell cell = rt.GetComponent<GridLetterDragCell>();
                    if (cell == null)
                    {
                        cell = rt.gameObject.AddComponent<GridLetterDragCell>();
                    }

                    cell.Bind(this, i);
                }
            }

            EnsureLineRoot();
            EnsurePool(36);
        }

        private Camera UiEventCamera()
        {
            if (rootCanvas == null) return null;
            return rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        }

        private void EnsureLineRoot()
        {
            Transform existing = transform.Find("LetterDragLineRoot");
            if (existing != null)
            {
                lineRoot = existing.GetComponent<RectTransform>();
                return;
            }

            GameObject go = new GameObject("LetterDragLineRoot");
            go.transform.SetParent(transform, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<CanvasGroup>().blocksRaycasts = false;
            rt.SetAsLastSibling();
            lineRoot = rt;
        }

        private void EnsurePool(int count)
        {
            while (linePool.Count < count)
            {
                GameObject seg = new GameObject("DragSeg" + linePool.Count);
                seg.transform.SetParent(lineRoot, false);
                RectTransform srt = seg.AddComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f);
                srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                Image img = seg.AddComponent<Image>();
                img.sprite = LineSprite();
                img.type = Image.Type.Simple;
                img.color = new Color(1f, 0.88f, 0.15f, 0.9f);
                img.raycastTarget = false;
                seg.SetActive(false);
                linePool.Add(img);
            }
        }

        private static Sprite LineSprite()
        {
            if (lineSprite != null) return lineSprite;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
            tex.Apply(false, true);
            lineSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            return lineSprite;
        }

        public void NotifyBeginDrag(int cellIndex, PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left && e.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            dragging = true;
            path.Clear();
            path.Add(cellIndex);
            HideAllSegments();
        }

        public void NotifyDrag(PointerEventData e)
        {
            if (!dragging || cellRects == null) return;
            int hit = HitCell(e.position);
            if (hit < 0) return;
            if (path.Count == 0 || path[path.Count - 1] != hit)
            {
                path.Add(hit);
            }

            RedrawLines();
        }

        public void NotifyEndDrag(PointerEventData e)
        {
            if (!dragging) return;
            dragging = false;
            if (path.Count > 1)
            {
                CommitPath();
            }

            HideAllSegments();
            path.Clear();
        }

        private int HitCell(Vector2 screenPos)
        {
            for (int i = 0; i < cellRects.Length; i++)
            {
                RectTransform rt = cellRects[i];
                if (rt == null) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, UiEventCamera()))
                {
                    return i;
                }
            }

            return -1;
        }

        private Vector2 ScreenCenterOf(RectTransform rt)
        {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector3 w = (corners[0] + corners[1] + corners[2] + corners[3]) * 0.25f;
            return RectTransformUtility.WorldToScreenPoint(UiEventCamera(), w);
        }

        private void RedrawLines()
        {
            HideAllSegments();
            int n = path.Count - 1;
            for (int s = 0; s < n; s++)
            {
                if (s >= linePool.Count) break;
                PlaceSegment(linePool[s], path[s], path[s + 1]);
            }
        }

        private void PlaceSegment(Image img, int fromIdx, int toIdx)
        {
            RectTransform a = cellRects[fromIdx];
            RectTransform b = cellRects[toIdx];
            if (a == null || b == null || lineRoot == null) return;
            Vector2 sa = ScreenCenterOf(a);
            Vector2 sb = ScreenCenterOf(b);
            Camera cam = UiEventCamera();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(lineRoot, sa, cam, out Vector2 la);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(lineRoot, sb, cam, out Vector2 lb);
            Vector2 delta = lb - la;
            float dist = delta.magnitude;
            if (dist < 2f) return;
            float ang = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            RectTransform rt = img.rectTransform;
            rt.localEulerAngles = new Vector3(0f, 0f, ang);
            rt.sizeDelta = new Vector2(dist, 5f);
            rt.anchoredPosition = (la + lb) * 0.5f;
            img.gameObject.SetActive(true);
        }

        private void HideAllSegments()
        {
            foreach (Image img in linePool)
            {
                if (img != null) img.gameObject.SetActive(false);
            }
        }

        private void CommitPath()
        {
            if (demo == null || cellTexts == null) return;
            StringBuilder sb = new StringBuilder(path.Count);
            foreach (int ix in path)
            {
                if (ix < 0 || ix >= cellTexts.Length || cellTexts[ix] == null) continue;
                string t = cellTexts[ix].text?.Trim() ?? "";
                if (t.Length != 1) continue;
                char ch = char.ToUpperInvariant(t[0]);
                if (ch < 'A' || ch > 'Z') continue;
                sb.Append(ch);
            }

            if (sb.Length == 0) return;
            demo.RegisterGridDragCommitThisFrame();
            if (crossword)
            {
                demo.AppendCrosswordDragWord(sb.ToString());
            }
            else
            {
                demo.AppendWordDragWord(sb.ToString());
            }

            GameSfxHub.Instance?.PlayTap();
        }
    }
}
