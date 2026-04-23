using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CongoGames.Core;

namespace CongoGames.UI
{
    /// <summary>
    /// Bref bandeau « manche / transition » quand on passe d’un mode à l’autre (donne un rythme de jeu, pas seulement un chrono muet).
    /// </summary>
    public class RoundVictoryOverlay : MonoBehaviour
    {
        [SerializeField] private float showSec = 1.15f;
        [SerializeField] private int sortOrder = 75;

        /// <summary>À appeler depuis l’éditeur (menu) pour préparer le canvas sans passer par Play (Awake).</summary>
        public void EnsureDisplayCanvasInEditor()
        {
            SetupDisplayCanvas();
        }

        private GameModeManager gmm;
        private CanvasGroup group;
        private Text line1;
        private Text line2;
        private Coroutine co;

        private void Awake()
        {
            SetupDisplayCanvas();
        }

        private void SetupDisplayCanvas()
        {
            Canvas c = GetComponent<Canvas>();
            if (c == null)
            {
                c = gameObject.AddComponent<Canvas>();
            }

            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.overrideSorting = true;
            c.sortingOrder = sortOrder;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            group = GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            group.blocksRaycasts = false;

            RectTransform root = GetComponent<RectTransform>();
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.pivot = new Vector2(0.5f, 0.5f);
                root.anchoredPosition = Vector2.zero;
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
            }
        }

        private void Start()
        {
            gmm = GameModeManager.Instance;
            if (gmm == null) return;
            gmm.OnModeTransition += OnModeTransition;
        }

        private void OnDestroy()
        {
            if (gmm != null)
            {
                gmm.OnModeTransition -= OnModeTransition;
            }
        }

        private void OnModeTransition(string fromId, string toId)
        {
            if (string.IsNullOrEmpty(toId) || toId == fromId) return;
            BuildUiIfNeeded();
            if (line1 == null) return;
            if (co != null) StopCoroutine(co);
            co = StartCoroutine(FlashTransition(fromId, toId));
        }

        private void BuildUiIfNeeded()
        {
            if (line1 != null) return;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            RectTransform root = GetComponent<RectTransform>();
            if (root == null) return;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            GameObject box = new GameObject("RoundBannerBox");
            box.transform.SetParent(transform, false);
            RectTransform brt = box.AddComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.88f);
            brt.anchorMax = new Vector2(0.5f, 0.88f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = Vector2.zero;
            brt.sizeDelta = new Vector2(720f, 100f);
            Image img = box.AddComponent<Image>();
            img.sprite = CreateOnePixelWhiteSprite();
            img.color = new Color(0.05f, 0.1f, 0.16f, 0.92f);
            img.raycastTarget = false;

            line1 = CreateLine(box.transform, "L1", font, 26, new Vector2(0f, 14f));
            line2 = CreateLine(box.transform, "L2", font, 20, new Vector2(0f, -20f));
        }

        private static Text CreateLine(Transform parent, string name, Font font, int size, Vector2 yOff)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = yOff;
            rt.sizeDelta = new Vector2(680f, 40f);
            Text tx = go.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = size;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = new Color(1f, 0.94f, 0.3f, 1f);
            tx.fontStyle = FontStyle.Bold;
            tx.raycastTarget = false;
            return tx;
        }

        private static Sprite CreateOnePixelWhiteSprite()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                tex.SetPixel(x, y, Color.white);
            }

            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        }

        private IEnumerator FlashTransition(string fromId, string toId)
        {
            string toLabel = GameModeManager.GetModeDisplayName(toId);
            line1.text = "Manche terminée";
            var top = ScoreManager.Instance != null ? ScoreManager.Instance.GetTopPlayers() : new List<PlayerData>();
            if (top != null && top.Count > 0)
            {
                var sb = new StringBuilder("En tête : ");
                sb.Append(top[0].Username);
                sb.Append(" — ");
                sb.Append(top[0].Score);
                sb.Append(" pts");
                line2.text = sb.ToString();
            }
            else
            {
                line2.text = "Suite : " + toLabel;
            }

            if (group != null)
            {
                group.alpha = 1f;
            }

            float w = 0f;
            while (w < showSec)
            {
                w += Time.unscaledDeltaTime;
                if (group != null)
                {
                    group.alpha = Mathf.Lerp(1f, 0f, w / showSec);
                }

                yield return null;
            }

            if (group != null)
            {
                group.alpha = 0f;
            }

            co = null;
        }
    }
}
