using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Effets plein écran : flash « sang » si faux, gouttes, acclamation si juste, emojis flottants.
    /// </summary>
    public class FeedbackVfxController : MonoBehaviour
    {
        public static FeedbackVfxController Instance { get; private set; }

        [SerializeField] private RectTransform shakeTarget;
        private Image bloodFlash;
        private readonly List<Text> emojiPool = new List<Text>();

        private void Awake()
        {
            Instance = this;
            RectTransform rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject blood = new GameObject("BloodFlash");
            blood.transform.SetParent(transform, false);
            RectTransform brt = blood.AddComponent<RectTransform>();
            brt.anchorMin = Vector2.zero;
            brt.anchorMax = Vector2.one;
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;
            brt.SetAsLastSibling();
            bloodFlash = blood.AddComponent<Image>();
            bloodFlash.sprite = UiSpriteFactory.GetWhiteSprite();
            bloodFlash.color = new Color(0.62f, 0.01f, 0.02f, 0f);
            bloodFlash.raycastTarget = false;
        }

        public void SetShakeTarget(RectTransform target)
        {
            shakeTarget = target;
        }

        private void BringToFront()
        {
            transform.SetAsLastSibling();
            if (bloodFlash != null)
            {
                bloodFlash.rectTransform.SetAsLastSibling();
            }
        }

        public void PlayCorrect()
        {
            BringToFront();
            StartCoroutine(CoCheer());
            SpawnFloatingEmojis(new[] { "🎉", "⭐", "🇨🇬", "👏", "✨", "🥁", "💃", "🎵", "🔥" }, new Color(1f, 0.92f, 0.2f, 1f));
        }

        public void PlayWrong()
        {
            BringToFront();
            StartCoroutine(CoBloodAndLaughVisual());
            StartCoroutine(CoBloodDrips());
            SpawnFloatingEmojis(new[] { "🩸", "💀", "😈", "❌", "🔻", "😬", "📉" }, new Color(0.95f, 0.15f, 0.12f, 1f));
        }

        private IEnumerator CoBloodAndLaughVisual()
        {
            if (bloodFlash != null)
            {
                float dur = 0.62f;
                float t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = t / dur;
                    float wave = Mathf.Sin(k * Mathf.PI) * 0.88f + Mathf.Sin(k * Mathf.PI * 2f) * 0.22f;
                    float a = Mathf.Clamp01(wave);
                    bloodFlash.color = new Color(0.72f, 0.02f, 0.04f, a);
                    yield return null;
                }

                t = 0f;
                while (t < 0.18f)
                {
                    t += Time.deltaTime;
                    bloodFlash.color = new Color(0.72f, 0.02f, 0.04f, Mathf.Lerp(bloodFlash.color.a, 0f, t / 0.18f));
                    yield return null;
                }

                bloodFlash.color = new Color(0.62f, 0.01f, 0.02f, 0f);
            }

            if (shakeTarget != null)
            {
                Vector2 o = shakeTarget.anchoredPosition;
                float s = 0f;
                const float dur = 0.34f;
                while (s < dur)
                {
                    s += Time.deltaTime;
                    float k = 1f - s / dur;
                    shakeTarget.anchoredPosition = o + new Vector2(Mathf.Sin(s * 68f) * 3.5f * k, Mathf.Cos(s * 54f) * 2.5f * k);
                    yield return null;
                }

                shakeTarget.anchoredPosition = o;
            }
        }

        private IEnumerator CoBloodDrips()
        {
            for (int i = 0; i < 10; i++)
            {
                GameObject drip = new GameObject("BloodDrip");
                drip.transform.SetParent(transform, false);
                RectTransform drt = drip.AddComponent<RectTransform>();
                drt.anchorMin = new Vector2(0.5f, 1f);
                drt.anchorMax = new Vector2(0.5f, 1f);
                drt.pivot = new Vector2(0.5f, 1f);
                drt.sizeDelta = new Vector2(Random.Range(5f, 16f), Random.Range(100f, 280f));
                drt.anchoredPosition = new Vector2(Random.Range(-620f, 620f), Random.Range(40f, 120f));
                Image img = drip.AddComponent<Image>();
                img.sprite = UiSpriteFactory.GetWhiteSprite();
                img.color = new Color(0.55f, 0.02f, 0.05f, 0.92f);
                img.raycastTarget = false;
                drt.SetAsLastSibling();
                StartCoroutine(CoDripFall(drt, img));
                yield return new WaitForSeconds(0.035f);
            }
        }

        private IEnumerator CoDripFall(RectTransform drt, Image img)
        {
            Vector2 start = drt.anchoredPosition;
            float t = 0f;
            float fall = 520f;
            while (t < 1.35f && drt != null)
            {
                t += Time.deltaTime;
                drt.anchoredPosition = start + new Vector2(Mathf.Sin(t * 6f) * 12f, -t * fall);
                if (img != null)
                {
                    Color c = img.color;
                    c.a = Mathf.Max(0f, 0.92f - t * 0.85f);
                    img.color = c;
                }

                yield return null;
            }

            if (drt != null)
            {
                Destroy(drt.gameObject);
            }
        }

        private IEnumerator CoCheer()
        {
            if (bloodFlash == null) yield break;
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                bloodFlash.color = new Color(0.05f, 0.52f, 0.22f, Mathf.Sin((t / 0.28f) * Mathf.PI) * 0.32f);
                yield return null;
            }

            bloodFlash.color = new Color(0.62f, 0.01f, 0.02f, 0f);
        }

        private void SpawnFloatingEmojis(string[] glyphs, Color col)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            foreach (string g in glyphs)
            {
                GameObject go = new GameObject("FloatEmoji");
                go.transform.SetParent(transform, false);
                RectTransform ert = go.AddComponent<RectTransform>();
                ert.anchorMin = new Vector2(0.5f, 0.35f);
                ert.anchorMax = new Vector2(0.5f, 0.35f);
                ert.pivot = new Vector2(0.5f, 0.5f);
                ert.anchoredPosition = new Vector2(Random.Range(-420f, 420f), Random.Range(-40f, 120f));
                ert.sizeDelta = new Vector2(96f, 96f);
                Text tx = go.AddComponent<Text>();
                tx.font = font;
                tx.fontSize = 54;
                tx.alignment = TextAnchor.MiddleCenter;
                tx.color = col;
                tx.text = g;
                tx.raycastTarget = false;
                ert.SetAsLastSibling();
                StartCoroutine(CoFloatAway(ert, tx));
            }
        }

        private IEnumerator CoFloatAway(RectTransform ert, Text tx)
        {
            float t = 0f;
            Vector2 start = ert.anchoredPosition;
            float dur = 2.15f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                ert.anchoredPosition = start + new Vector2(Mathf.Sin(k * 6f) * 30f, k * 220f);
                Color c = tx.color;
                c.a = 1f - k;
                tx.color = c;
                ert.localScale = Vector3.one * (1f + k * 0.35f);
                yield return null;
            }

            Destroy(ert.gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }

    internal static class UiSpriteFactory
    {
        private static Sprite cached;

        public static Sprite GetWhiteSprite()
        {
            if (cached != null) return cached;
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
            tex.Apply(false, true);
            cached = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            return cached;
        }
    }
}
