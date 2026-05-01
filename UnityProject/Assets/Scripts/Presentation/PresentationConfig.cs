using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Niveau de richesse visuelle du fond 3D procédural (sans assets HD externes).
    /// PlayerPrefs « CongoPresentationQuality » : 0 = Compact, 1 = Standard, 2 = Cinematic (défaut).
    /// </summary>
    public enum PresentationQualityTier
    {
        Compact = 0,
        Standard = 1,
        Cinematic = 2
    }

    public static class PresentationConfig
    {
        public const string PrefsUseVirtual3D = "CongoUseVirtual3D";
        public const string PrefsPresentQuality = "CongoPresentationQuality";

        public static PresentationQualityTier Tier { get; set; } = PresentationQualityTier.Cinematic;

        /// <summary>Scène 3D « plateau TV » (Render Texture → fond) au lieu du seul fond 2D / vidéo.</summary>
        /// <remarks>Défaut aligné PlayerPrefs (1) : 3D actif ; avec des MP4 Theme/, alternance 3D ↔ vidéo.</remarks>
        public static bool UseVirtual3DShowStage { get; set; } = true;


        public static int VirtualStageWidth => Tier switch
        {
            PresentationQualityTier.Compact => 960,
            PresentationQualityTier.Standard => 1280,
            _ => 1600
        };

        public static int VirtualStageHeight => Tier switch
        {
            PresentationQualityTier.Compact => 540,
            PresentationQualityTier.Standard => 720,
            _ => 900
        };

        public static float SceneRichness => Tier switch
        {
            PresentationQualityTier.Compact => 0.72f,
            PresentationQualityTier.Standard => 1f,
            PresentationQualityTier.Cinematic => 1.58f,
            _ => 1f
        };

        public static void ApplyFromPlayerPrefs()
        {
            int v = PlayerPrefs.GetInt(PrefsPresentQuality, (int)PresentationQualityTier.Cinematic);
            Tier = (PresentationQualityTier)Mathf.Clamp(v, 0, 2);

            // Défaut 1 : plateau 3D actif ; si des vidéos Theme/ existent, ThemeBackgroundController alterne 3D et MP4.
            UseVirtual3DShowStage = PlayerPrefs.GetInt(PrefsUseVirtual3D, 1) != 0;
        }
    }
}
