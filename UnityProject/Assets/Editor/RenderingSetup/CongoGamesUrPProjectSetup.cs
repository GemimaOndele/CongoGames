#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CongoGames.EditorTools
{
    /// <summary>
    /// Crée un Universal Renderer + URP Asset « multi-plateforme » et les assigne au projet.
    /// À exécuter après résolution des packages (première ouverture Unity après git pull).
    /// </summary>
    public static class CongoGamesUrPProjectSetup
    {
        private const string RendererPath = "Assets/Settings/URP/CongoGames_UniversalRenderer.asset";
        private const string PipelinePath = "Assets/Settings/URP/CongoGames_UniversalRP.asset";

        [MenuItem("CongoGames/Rendering/Créer et assigner URP (multi-plateforme)", false, 0)]
        [MenuItem("Window/CongoGames/Créer et assigner URP", false, 0)]
        [MenuItem("Tools/CongoGames/Assigner le pipeline URP", false, 0)]
        public static void SetupUrP()
        {
            try
            {
                EnsureFolder("Assets/Settings");
                EnsureFolder("Assets/Settings/URP");

                UniversalRendererData renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
                if (renderer == null)
                {
                    renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                    AssetDatabase.CreateAsset(renderer, RendererPath);
                }

                UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if (pipeline == null)
                {
                    pipeline = UniversalRenderPipelineAsset.Create(renderer);
                    AssetDatabase.CreateAsset(pipeline, PipelinePath);
                }

                GraphicsSettings.defaultRenderPipeline = pipeline;
                GraphicsSettings.lightsUseLinearIntensity = true;
                GraphicsSettings.lightsUseColorTemperature = true;

                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog(
                    "CongoGames — URP",
                    "Pipeline assigné (Project Settings > Graphics).\n\n" +
                    "Si des matériaux deviennent roses : Edit > Rendering > Materials > Convertir.\n" +
                    "Voir docs/URP_Migration_Checklist_CrossPlatform.md",
                    "OK");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog(
                    "CongoGames — URP",
                    "Impossible de configurer URP. Vérifiez que le package « Universal RP » est installé (manifest / Package Manager).\n\n" +
                    e.Message,
                    "OK");
            }
        }

        [MenuItem("CongoGames/Rendering/Activer espace couleur Linear (recommandé URP)")]
        public static void EnableLinearColorSpace()
        {
            if (!EditorUtility.DisplayDialog(
                    "Espace couleur Linear",
                    "Passer le projet en Linear améliore le rendu PBR (PC, mobile récents, consoles). " +
                    "Sur WebGL, testez le build après changement.\n\nContinuer ?",
                    "Oui",
                    "Annuler"))
            {
                return;
            }

            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUtility.DisplayDialog("Linear", "Player Settings > Couleur : Linear.", "OK");
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            string name = Path.GetFileName(assetPath);
            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            string parentForCreate = string.IsNullOrEmpty(parent) ? "Assets" : parent;
            AssetDatabase.CreateFolder(parentForCreate, name);
        }
    }
}
#endif
