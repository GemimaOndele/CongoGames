using UnityEngine;
using CongoGames.Network;

namespace CongoGames.Core
{
    /// <summary>
    /// Démonstration : pseudo local + type de partie, persistés. En live TikTok, les noms viennent du chat (hors de ce profil).
    /// </summary>
    public static class PlayerProfileStore
    {
        private const string PrefsName = "cg_demo_display_name";
        private const string PrefsSolo = "cg_demo_solo";
        private const string PrefsProvider = "cg_demo_provider";
        private const string PrefsAvatarUrl = "cg_demo_avatar_url";
        private const string PrefsAdmin = "cg_demo_is_admin";
        private const string PrefsOAuthTikTok = "cg_oauth_url_tiktok";
        private const string PrefsOAuthGoogle = "cg_oauth_url_google";
        private const string PrefsOAuthFacebook = "cg_oauth_url_facebook";
        public const string DefaultDisplayName = "Joueur";

        public static string DisplayName
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefsName, DefaultDisplayName);
                return string.IsNullOrWhiteSpace(s) ? DefaultDisplayName : s.Trim();
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value)) return;
                PlayerPrefs.SetString(PrefsName, value.Trim().Length > 24 ? value.Trim().Substring(0, 24) : value.Trim());
                PlayerPrefs.Save();
            }
        }

        public static bool SoloOrUnknown
        {
            get => PlayerPrefs.GetInt(PrefsSolo, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefsSolo, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static string AuthProvider
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefsProvider, "invité");
                return string.IsNullOrWhiteSpace(s) ? "invité" : s.Trim();
            }
            set
            {
                string v = string.IsNullOrWhiteSpace(value) ? "invité" : value.Trim().ToLowerInvariant();
                if (v.Length > 20) v = v.Substring(0, 20);
                PlayerPrefs.SetString(PrefsProvider, v);
                PlayerPrefs.Save();
            }
        }

        public static string AvatarUrl
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefsAvatarUrl, "");
                return string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
            }
            set
            {
                string v = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
                if (v.Length > 400) v = v.Substring(0, 400);
                PlayerPrefs.SetString(PrefsAvatarUrl, v);
                PlayerPrefs.Save();
            }
        }

        public static bool IsAdmin
        {
            get => PlayerPrefs.GetInt(PrefsAdmin, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefsAdmin, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static string OAuthUrlTikTok
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefsOAuthTikTok, "");
                return string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
            }
            set
            {
                string v = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
                if (v.Length > 500) v = v.Substring(0, 500);
                PlayerPrefs.SetString(PrefsOAuthTikTok, v);
                PlayerPrefs.Save();
            }
        }

        public static string OAuthUrlGoogle
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefsOAuthGoogle, "");
                return string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
            }
            set
            {
                string v = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
                if (v.Length > 500) v = v.Substring(0, 500);
                PlayerPrefs.SetString(PrefsOAuthGoogle, v);
                PlayerPrefs.Save();
            }
        }

        public static string OAuthUrlFacebook
        {
            get
            {
                string s = PlayerPrefs.GetString(PrefsOAuthFacebook, "");
                return string.IsNullOrWhiteSpace(s) ? "" : s.Trim();
            }
            set
            {
                string v = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
                if (v.Length > 500) v = v.Substring(0, 500);
                PlayerPrefs.SetString(PrefsOAuthFacebook, v);
                PlayerPrefs.Save();
            }
        }

        public static string GetOAuthUrlForProvider(string provider)
        {
            string p = string.IsNullOrWhiteSpace(provider) ? "" : provider.Trim().ToLowerInvariant();
            if (p.Contains("tiktok")) return OAuthUrlTikTok;
            if (p.Contains("google")) return OAuthUrlGoogle;
            if (p.Contains("facebook")) return OAuthUrlFacebook;
            return "";
        }

        /// <summary>Identifiant pour les points en démo (pas de TikTok connecté).</summary>
        public static string ScoreUsernameForLocalPlay()
        {
            if (IsLiveTiktok())
            {
                return null;
            }

            return DisplayName;
        }

        public static bool IsLiveTiktokSession()
        {
            return IsLiveTiktok();
        }

        private static bool IsLiveTiktok()
        {
            LiveEventClient c = Object.FindAnyObjectByType<LiveEventClient>();
            return c != null && c.IsConnected;
        }
    }
}
