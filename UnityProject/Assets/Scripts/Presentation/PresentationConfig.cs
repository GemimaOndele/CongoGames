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
        public static PresentationQualityTier Tier { get; set; } = PresentationQualityTier.Cinematic;

        public static float SceneRichness => Tier switch
        {
            PresentationQualityTier.Compact => 0.72f,
            PresentationQualityTier.Standard => 1f,
            PresentationQualityTier.Cinematic => 1.58f,
            _ => 1f
        };

        public static void ApplyFromPlayerPrefs()
        {
            int v = PlayerPrefs.GetInt("CongoPresentationQuality", (int)PresentationQualityTier.Cinematic);
            Tier = (PresentationQualityTier)Mathf.Clamp(v, 0, 2);
        }
    }
}
