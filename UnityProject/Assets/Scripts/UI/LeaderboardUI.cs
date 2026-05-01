using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using CongoGames.Core;
using CongoGames.Network;

namespace CongoGames.UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private Text leaderboardText;
        [SerializeField] private float scrollStepSeconds = 1.4f;
        private readonly List<PlayerData> cachedPlayers = new List<PlayerData>();
        private readonly List<PlayerData> displayPlayers = new List<PlayerData>();
        private readonly List<LeaderboardRow> rows = new List<LeaderboardRow>();
        private readonly Dictionary<string, Texture2D> avatarCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> previousTop3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private float nextScrollAt;
        private int rowOffset;
        private RectTransform rowsRoot;
        private Texture2D fallbackAvatarTexture;
        private Sprite circleMaskSprite;
        private static readonly Color[] RankRingColors =
        {
            new Color(1f, 0.84f, 0.2f, 1f),
            new Color(0.72f, 0.88f, 1f, 1f),
            new Color(0.95f, 0.62f, 0.3f, 1f),
            new Color(0.36f, 0.8f, 0.95f, 1f)
        };

        private sealed class LeaderboardRow
        {
            public GameObject Root;
            public Image AvatarRing;
            public RawImage Avatar;
            public Text Badge;
            public Text User;
            public Text Score;
            public Coroutine PulseCo;
        }

        public void BindRuntime(Text text)
        {
            leaderboardText = text;
            TryHookScore();
        }

        private void OnEnable()
        {
            TryHookScore();
        }

        private void OnDisable()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnLeaderboardChanged -= Render;
            }
        }

        private void Update()
        {
            if (displayPlayers.Count <= 1 || leaderboardText == null) return;
            if (Time.unscaledTime < nextScrollAt) return;
            nextScrollAt = Time.unscaledTime + Mathf.Max(0.6f, scrollStepSeconds);
            rowOffset = (rowOffset + 1) % Mathf.Max(1, displayPlayers.Count);
            RenderWindow();
        }

        private void TryHookScore()
        {
            if (leaderboardText == null || ScoreManager.Instance == null)
            {
                return;
            }

            ScoreManager.Instance.OnLeaderboardChanged -= Render;
            ScoreManager.Instance.OnLeaderboardChanged += Render;
            Render(ScoreManager.Instance.GetTopPlayers());
        }

        private void Render(List<PlayerData> players)
        {
            if (leaderboardText == null) return;
            cachedPlayers.Clear();
            if (players != null) cachedPlayers.AddRange(players);
            rowOffset = 0;
            nextScrollAt = Time.unscaledTime + Mathf.Max(0.6f, scrollStepSeconds);
            EnsureRowsBuilt();
            RenderWindow();
        }

        private void RenderWindow()
        {
            if (leaderboardText == null) return;
            EnsureRowsBuilt();
            bool portrait = Screen.height > Screen.width;
            BuildDisplayPlayers();
            EvaluateTop3Pulse();
            int total = ScoreManager.Instance != null ? ScoreManager.Instance.GetRegisteredPlayerCount() : displayPlayers.Count;
            int maxRows = portrait ? 4 : 6;
            var sb = new StringBuilder();
            sb.Append("Classement global · ").Append(total).Append(" joueur(s)\n");
            if (displayPlayers.Count <= 0)
            {
                sb.Append("<color=#8A93A0>En attente des scores…</color>");
                leaderboardText.text = sb.ToString();
                return;
            }

            for (int i = 0; i < maxRows; i++)
            {
                int idx = (rowOffset + i) % displayPlayers.Count;
                PlayerData p = displayPlayers[idx];
                string u = p != null ? p.Username : "";
                if (string.IsNullOrWhiteSpace(u)) u = "joueur";
                if (!u.StartsWith("@", StringComparison.Ordinal)) u = "@" + u;
                if (u.Length > (portrait ? 14 : 18)) u = u.Substring(0, portrait ? 13 : 17) + "…";
                sb.Append(u).Append("  ")
                    .Append("<color=#7EE7FF><b>")
                    .Append((p != null ? p.Score : 0).ToString(CultureInfo.InvariantCulture))
                    .Append("</b></color>\n");
                if (i < rows.Count)
                {
                    PopulateVisualRow(rows[i], p, u, idx + 1);
                }
                if (i >= displayPlayers.Count - 1) break;
            }

            leaderboardText.text = sb.ToString();
            if (rows.Count > maxRows)
            {
                for (int i = maxRows; i < rows.Count; i++)
                {
                    if (rows[i] != null && rows[i].Root != null) rows[i].Root.SetActive(false);
                }
            }
        }

        private void EnsureRowsBuilt()
        {
            if (rowsRoot != null || leaderboardText == null) return;
            Transform parent = leaderboardText.transform.parent;
            if (parent == null) return;
            leaderboardText.enabled = false;

            var rootGo = new GameObject("LeaderboardRows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rootGo.transform.SetParent(parent, false);
            rowsRoot = rootGo.GetComponent<RectTransform>();
            rowsRoot.anchorMin = new Vector2(0f, 0f);
            rowsRoot.anchorMax = new Vector2(1f, 1f);
            rowsRoot.offsetMin = new Vector2(12f, 12f);
            rowsRoot.offsetMax = new Vector2(-12f, -54f);

            VerticalLayoutGroup v = rootGo.GetComponent<VerticalLayoutGroup>();
            v.spacing = 6f;
            v.childControlWidth = true;
            v.childControlHeight = false;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            v.padding = new RectOffset(0, 0, 0, 0);

            bool portrait = Screen.height > Screen.width;
            int maxRows = portrait ? 4 : 6;
            for (int i = 0; i < maxRows; i++)
            {
                rows.Add(CreateRow(i));
            }
        }

        private LeaderboardRow CreateRow(int index)
        {
            var rowGo = new GameObject("Row_" + index, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(rowsRoot, false);
            RectTransform rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0f, 52f);
            Image bg = rowGo.GetComponent<Image>();
            bg.color = new Color(0.09f, 0.12f, 0.17f, 0.78f);
            bg.raycastTarget = false;

            HorizontalLayoutGroup h = rowGo.GetComponent<HorizontalLayoutGroup>();
            h.spacing = 10f;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = false;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.padding = new RectOffset(8, 8, 6, 6);

            var avatarWrapGo = new GameObject("AvatarWrap", typeof(RectTransform), typeof(LayoutElement), typeof(Image));
            avatarWrapGo.transform.SetParent(rowGo.transform, false);
            Image ring = avatarWrapGo.GetComponent<Image>();
            ring.color = RankRingColors[Mathf.Min(index, RankRingColors.Length - 1)];
            LayoutElement avatarLayout = avatarWrapGo.GetComponent<LayoutElement>();
            avatarLayout.preferredWidth = 42f;
            avatarLayout.preferredHeight = 42f;

            var maskGo = new GameObject("AvatarMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            maskGo.transform.SetParent(avatarWrapGo.transform, false);
            RectTransform maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = new Vector2(0.5f, 0.5f);
            maskRt.anchorMax = new Vector2(0.5f, 0.5f);
            maskRt.pivot = new Vector2(0.5f, 0.5f);
            maskRt.sizeDelta = new Vector2(34f, 34f);
            Image maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = GetCircleMaskSprite();
            maskImg.color = Color.white;
            Mask mask = maskGo.GetComponent<Mask>();
            mask.showMaskGraphic = false;

            var avatarGo = new GameObject("Avatar", typeof(RectTransform), typeof(RawImage));
            avatarGo.transform.SetParent(maskGo.transform, false);
            RectTransform avatarRt = avatarGo.GetComponent<RectTransform>();
            avatarRt.anchorMin = Vector2.zero;
            avatarRt.anchorMax = Vector2.one;
            avatarRt.offsetMin = Vector2.zero;
            avatarRt.offsetMax = Vector2.zero;
            RawImage avatar = avatarGo.GetComponent<RawImage>();
            avatar.color = Color.white;
            avatar.texture = GetFallbackAvatarTexture();

            var badgeGo = new GameObject("RankBadge", typeof(RectTransform), typeof(Text));
            badgeGo.transform.SetParent(avatarWrapGo.transform, false);
            RectTransform badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(1f, 0f);
            badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot = new Vector2(1f, 0f);
            badgeRt.anchoredPosition = new Vector2(8f, -6f);
            badgeRt.sizeDelta = new Vector2(38f, 18f);
            Text badge = badgeGo.GetComponent<Text>();
            badge.font = leaderboardText != null ? leaderboardText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            badge.alignment = TextAnchor.MiddleCenter;
            badge.fontSize = 12;
            badge.fontStyle = FontStyle.Bold;
            badge.color = new Color(1f, 0.9f, 0.24f, 1f);

            Font font = leaderboardText != null ? leaderboardText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            nameGo.transform.SetParent(rowGo.transform, false);
            Text name = nameGo.GetComponent<Text>();
            name.font = font;
            name.fontSize = 18;
            name.color = new Color(0.96f, 0.97f, 1f, 1f);
            name.alignment = TextAnchor.MiddleLeft;
            name.horizontalOverflow = HorizontalWrapMode.Overflow;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            LayoutElement nameLayout = nameGo.GetComponent<LayoutElement>();
            nameLayout.preferredWidth = 180f;
            nameLayout.flexibleWidth = 1f;

            var scoreGo = new GameObject("Score", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            scoreGo.transform.SetParent(rowGo.transform, false);
            Text score = scoreGo.GetComponent<Text>();
            score.font = font;
            score.fontStyle = FontStyle.Bold;
            score.fontSize = 19;
            score.color = new Color(0.49f, 0.91f, 1f, 1f);
            score.alignment = TextAnchor.MiddleRight;
            LayoutElement scoreLayout = scoreGo.GetComponent<LayoutElement>();
            scoreLayout.preferredWidth = 78f;

            return new LeaderboardRow
            {
                Root = rowGo,
                AvatarRing = ring,
                Avatar = avatar,
                Badge = badge,
                User = name,
                Score = score
            };
        }

        private void PopulateVisualRow(LeaderboardRow row, PlayerData player, string userLabel, int rank)
        {
            if (row == null || row.Root == null) return;
            row.Root.SetActive(true);
            if (row.User != null) row.User.text = userLabel;
            if (row.Score != null) row.Score.text = (player != null ? player.Score : 0).ToString(CultureInfo.InvariantCulture);
            if (row.AvatarRing != null) row.AvatarRing.color = RankRingColors[Mathf.Min(Mathf.Max(rank - 1, 0), RankRingColors.Length - 1)];
            if (row.Badge != null) row.Badge.text = rank <= 3 ? ("#" + rank) : "";
            if (row.Avatar == null) return;

            string url = player != null ? (player.AvatarUrl ?? "").Trim() : "";
            if (string.IsNullOrWhiteSpace(url))
            {
                row.Avatar.texture = GetFallbackAvatarTexture();
                return;
            }

            if (avatarCache.TryGetValue(url, out Texture2D tx) && tx != null)
            {
                row.Avatar.texture = tx;
                return;
            }

            row.Avatar.texture = GetFallbackAvatarTexture();
            StartCoroutine(CoLoadAvatar(url, row.Avatar));
        }

        private void EvaluateTop3Pulse()
        {
            var currentTop3 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < displayPlayers.Count && i < 3; i++)
            {
                string u = displayPlayers[i] != null ? (displayPlayers[i].Username ?? "") : "";
                if (string.IsNullOrWhiteSpace(u)) continue;
                currentTop3.Add(u);
                if (!previousTop3.Contains(u) && i < rows.Count)
                {
                    StartPulse(rows[i]);
                }
            }

            previousTop3.Clear();
            foreach (string u in currentTop3) previousTop3.Add(u);
        }

        private void StartPulse(LeaderboardRow row)
        {
            if (row == null || row.Root == null) return;
            if (row.PulseCo != null) StopCoroutine(row.PulseCo);
            row.PulseCo = StartCoroutine(CoPulseRow(row));
        }

        private System.Collections.IEnumerator CoPulseRow(LeaderboardRow row)
        {
            if (row == null || row.Root == null) yield break;
            Transform t = row.Root.transform;
            Vector3 baseScale = Vector3.one;
            float start = Time.unscaledTime;
            const float dur = 0.36f;
            while (Time.unscaledTime - start < dur)
            {
                float k = (Time.unscaledTime - start) / dur;
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.08f;
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            t.localScale = baseScale;
            row.PulseCo = null;
        }

        private System.Collections.IEnumerator CoLoadAvatar(string url, RawImage target)
        {
            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success) yield break;
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                if (tex == null) yield break;
                avatarCache[url] = tex;
                if (target != null) target.texture = tex;
            }
        }

        private Texture2D GetFallbackAvatarTexture()
        {
            if (fallbackAvatarTexture != null) return fallbackAvatarTexture;
            fallbackAvatarTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color c = new Color(0.25f, 0.3f, 0.38f, 1f);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = c;
            fallbackAvatarTexture.SetPixels(pixels);
            fallbackAvatarTexture.Apply(false, true);
            return fallbackAvatarTexture;
        }

        private Sprite GetCircleMaskSprite()
        {
            if (circleMaskSprite != null) return circleMaskSprite;
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float r = size * 0.48f;
            Color clear = new Color(1f, 1f, 1f, 0f);
            Color white = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, d <= r ? white : clear);
                }
            }

            tex.Apply(false, true);
            circleMaskSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            return circleMaskSprite;
        }

        private void BuildDisplayPlayers()
        {
            displayPlayers.Clear();
            if (cachedPlayers.Count > 0)
            {
                displayPlayers.AddRange(cachedPlayers);
                return;
            }

            bool liveConnected = false;
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            if (live != null) liveConnected = live.IsConnected;
            if (liveConnected) return;
            string local = PlayerProfileStore.ScoreUsernameForLocalPlay();
            if (!string.IsNullOrWhiteSpace(local))
            {
                var p = new PlayerData(local);
                p.Score = 0;
                p.AvatarUrl = PlayerProfileStore.AvatarUrl ?? "";
                displayPlayers.Add(p);
            }
        }
    }
}
