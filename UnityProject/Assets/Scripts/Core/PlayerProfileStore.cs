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
