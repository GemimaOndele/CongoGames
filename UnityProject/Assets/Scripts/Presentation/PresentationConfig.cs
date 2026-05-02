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
        /// <remarks>Avec vidéos Theme + <see cref="PrefsMix3DWithVideo"/> à 1 (défaut), le fond alterne MP4 et plateau 3D.</remarks>
        public static bool UseVirtual3DShowStage { get; set; } = true;

        /// <summary>PlayerPrefs « CongoMix3DWithVideo » : 1 = alterner plateau 3D et vidéos Theme quand les deux existent (défaut recommandé).</summary>
        public const string PrefsMix3DWithVideo = "CongoMix3DWithVideo";


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

            // Première install : activer l’alternance vidéo ↔ 3D (anciennes builds sans clé utilisaient implicitement « vidéo seule »).
            if (!PlayerPrefs.HasKey(PrefsMix3DWithVideo))
            {
                PlayerPrefs.SetInt(PrefsMix3DWithVideo, 1);
                PlayerPrefs.Save();
            }

            UseVirtual3DShowStage = PlayerPrefs.GetInt(PrefsUseVirtual3D, 1) != 0;
        }
    }
}
