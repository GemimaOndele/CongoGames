using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Applique musique + vidéo/fond selon le ModeId du jeu (dossiers StreamingAssets/Theme/&lt;mode&gt;/).
    /// </summary>
    public static class ThemeRuntime
    {
        public static event System.Action<string> OnModeStarted;

        public static void NotifyModeStarted(string modeId)
        {
            string id = string.IsNullOrEmpty(modeId) ? "default" : modeId.Trim().ToLowerInvariant();
            ThemeMusicPlayer music = Object.FindAnyObjectByType<ThemeMusicPlayer>();
            if (music != null)
            {
                music.ApplyGameMode(id);
            }

            ThemeBackgroundController bg = Object.FindAnyObjectByType<ThemeBackgroundController>();
            if (bg != null)
            {
                bg.ApplyGameMode(id);
            }

            BottomThemeVideoStrip strip = Object.FindAnyObjectByType<BottomThemeVideoStrip>();
            if (strip != null)
            {
                strip.ApplyGameMode(id);
            }

            try
            {
                OnModeStarted?.Invoke(id);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
