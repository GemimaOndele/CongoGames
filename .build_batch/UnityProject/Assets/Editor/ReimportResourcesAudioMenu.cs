using UnityEditor;
using UnityEngine;

namespace CongoGames.Editor
{
    /// <summary>
    /// Force le réimport des clips sous Resources/Audio (Unity ne rescane pas toujours seul après copie fichiers).
    /// </summary>
    public static class ReimportResourcesAudioMenu
    {
        private const string BgmFolder = "Assets/Resources/Audio/BGM";
        private const string SfxFolder = "Assets/Resources/Audio/SFX";

        [MenuItem("CongoGames/Audio/Réimporter Resources/Audio (BGM + SFX)", false, 50)]
        public static void ReimportAudioFolders()
        {
            if (AssetDatabase.IsValidFolder(BgmFolder))
            {
                AssetDatabase.ImportAsset(BgmFolder, ImportAssetOptions.ForceUpdate);
            }

            if (AssetDatabase.IsValidFolder(SfxFolder))
            {
                AssetDatabase.ImportAsset(SfxFolder, ImportAssetOptions.ForceUpdate);
            }

            AssetDatabase.Refresh();
            Debug.Log("[CongoGames] Réimport demandé pour Resources/Audio/BGM et SFX.");
        }
    }
}
