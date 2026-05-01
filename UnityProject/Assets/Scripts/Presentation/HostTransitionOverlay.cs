using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Petit écran central pendant l’annonce IA : jeu suivant ou question suivante (sablier animé).
    /// </summary>
    public sealed class HostTransitionOverlay : MonoBehaviour
    {
        public static HostTransitionOverlay Instance { get; private set; }

        private GameObject panel;
        private RawImage backgroundRaw;
        private Image dimLayer;
        private Text messageText;
        private Text subtitleText;
        private RectTransform hourglassRt;
        private Image cardBgImage;
        private Outline cardOutline;
        private float spinDeg;

        private void Awake()
        {
            Instance = this;
            EnsureBuilt();
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void Update()
        {
            if (panel == null || !panel.activeSelf || hourglassRt == null)
            {
                return;
            }

            spinDeg += Time.unscaledDeltaTime * 120f;
            hourglassRt.localEulerAngles = new Vector3(0f, 0f, spinDeg);
            float pulse = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 3.4f);
            hourglassRt.localScale = Vector3.one * pulse;
            if (cardOutline != null)
            {
                float glow = 0.58f + 0.42f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.2f));
                cardOutline.effectColor = Color.Lerp(
                    new Color(0.2f, 0.55f, 1f, 0.78f),
                    new Color(0.14f, 0.9f, 0.5f, 0.98f),
                    glow);
            }
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        private void EnsureBuilt()
        {
            if (panel != null)
            {
                return;
            }

            Canvas hostCanvas = GetComponent<Canvas>();
            if (hostCanvas == null)
            {
                hostCanvas = gameObject.AddComponent<Canvas>();
                hostCanvas.overrideSorting = true;
                hostCanvas.sortingOrder = 950;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            panel = new GameObject("HostTransitionPanel");
            panel.transform.SetParent(transform, false);
            RectTransform prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            backgroundRaw = panel.AddComponent<RawImage>();
            backgroundRaw.color = Color.white;
            backgroundRaw.raycastTarget = false;
            backgroundRaw.texture = Texture2D.blackTexture;

            GameObject dimGo = new GameObject("DimLayer");
            dimGo.transform.SetParent(panel.transform, false);
            RectTransform drt = dimGo.AddComponent<RectTransform>();
            drt.anchorMin = Vector2.zero;
            drt.anchorMax = Vector2.one;
            drt.offsetMin = Vector2.zero;
            drt.offsetMax = Vector2.zero;
            dimLayer = dimGo.AddComponent<Image>();
            dimLayer.color = new Color(0.02f, 0.03f, 0.06f, 0.46f);
            dimLayer.raycastTarget = true;

            GameObject card = new GameObject("Card");
            card.transform.SetParent(panel.transform, false);
            RectTransform crt = card.AddComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(760f, 300f);

            cardBgImage = card.AddComponent<Image>();
            cardBgImage.color = new Color(0.08f, 0.12f, 0.22f, 0.95f);
            cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(0.18f, 0.82f, 0.48f, 0.92f);
            cardOutline.effectDistance = new Vector2(3f, -3f);
            cardOutline.useGraphicAlpha = true;

            GameObject hg = new GameObject("Hourglass");
            hg.transform.SetParent(card.transform, false);
            hourglassRt = hg.AddComponent<RectTransform>();
            hourglassRt.anchorMin = new Vector2(0.1f, 0.58f);
            hourglassRt.anchorMax = new Vector2(0.1f, 0.58f);
            hourglassRt.sizeDelta = new Vector2(92f, 92f);
            Text hgTx = hg.AddComponent<Text>();
            hgTx.font = font;
            hgTx.fontSize = 72;
            hgTx.alignment = TextAnchor.MiddleCenter;
            hgTx.color = new Color(0.85f, 0.92f, 1f, 1f);
            hgTx.text = "\u231b";

            GameObject txGo = new GameObject("Message");
            txGo.transform.SetParent(card.transform, false);
            RectTransform txRt = txGo.AddComponent<RectTransform>();
            txRt.anchorMin = new Vector2(0.22f, 0.12f);
            txRt.anchorMax = new Vector2(0.95f, 0.9f);
            txRt.offsetMin = Vector2.zero;
            txRt.offsetMax = Vector2.zero;
            messageText = txGo.AddComponent<Text>();
            messageText.font = font;
            messageText.fontSize = 34;
            messageText.alignment = TextAnchor.MiddleLeft;
            messageText.color = new Color(0.95f, 0.94f, 0.92f, 1f);
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            messageText.verticalOverflow = VerticalWrapMode.Overflow;
            messageText.text = "";

            GameObject subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(card.transform, false);
            RectTransform subRt = subGo.AddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.22f, 0.04f);
            subRt.anchorMax = new Vector2(0.95f, 0.24f);
            subRt.offsetMin = Vector2.zero;
            subRt.offsetMax = Vector2.zero;
            subtitleText = subGo.AddComponent<Text>();
            subtitleText.font = font;
            subtitleText.fontSize = 22;
            subtitleText.alignment = TextAnchor.MiddleLeft;
            subtitleText.color = new Color(0.72f, 0.9f, 1f, 0.98f);
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
            subtitleText.text = "";
        }

        public void ShowQuestionIncoming()
        {
            EnsureBuilt();
            RefreshThemeBackgroundTexture();
            if (messageText != null)
            {
                messageText.text = "La question suivante arrive.";
            }

            if (subtitleText != null)
            {
                subtitleText.text = "Annonce IA en cours, prepare-toi.";
            }

            if (panel != null)
            {
                panel.transform.SetAsLastSibling();
                panel.SetActive(true);
            }
        }

        public void ShowNewGameIncoming(string modeDisplayName)
        {
            EnsureBuilt();
            RefreshThemeBackgroundTexture();
            if (messageText != null)
            {
                messageText.text = "Un nouveau jeu arrive"
                    + (string.IsNullOrWhiteSpace(modeDisplayName) ? "." : (" : " + modeDisplayName + "."));
            }

            if (subtitleText != null)
            {
                subtitleText.text = "Annonce IA en cours, lancement juste apres.";
            }

            if (panel != null)
            {
                panel.transform.SetAsLastSibling();
                panel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void RefreshThemeBackgroundTexture()
        {
            if (backgroundRaw == null)
            {
                return;
            }

            ThemeBackgroundController bg = Object.FindAnyObjectByType<ThemeBackgroundController>();
            if (bg == null)
            {
                backgroundRaw.texture = Texture2D.blackTexture;
                backgroundRaw.color = new Color(0.08f, 0.1f, 0.14f, 1f);
                return;
            }

            RawImage bgImage = bg.GetComponent<RawImage>();
            if (bgImage != null && bgImage.texture != null)
            {
                backgroundRaw.texture = bgImage.texture;
                backgroundRaw.uvRect = bgImage.uvRect;
                backgroundRaw.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            }
            else
            {
                backgroundRaw.texture = Texture2D.blackTexture;
                backgroundRaw.color = new Color(0.08f, 0.1f, 0.14f, 1f);
            }
        }
    }
}
