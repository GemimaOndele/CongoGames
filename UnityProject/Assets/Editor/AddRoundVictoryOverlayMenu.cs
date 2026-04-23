#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CongoGames.UI;

namespace CongoGames.EditorTools
{
    /// <summary>
    /// Scène personnalisée sans RuntimeBootstrap : ajoute l’overlay de transition des manches.
    /// </summary>
    public static class AddRoundVictoryOverlayMenu
    {
        private const int MenuOrder = 50;

        [MenuItem("CongoGames/UI/Ajouter RoundVictoryOverlay (scène perso, sans RuntimeBootstrap)", false, MenuOrder)]
        public static void AddOverlayToCurrentScene()
        {
            RoundVictoryOverlay existing = Object.FindFirstObjectByType<RoundVictoryOverlay>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "CongoGames",
                    "Un « RoundVictoryOverlay » existe déjà sur « " + existing.gameObject.name + " ». Sélectionner ce GameObject ?",
                    "Oui",
                    "Annuler"))
                {
                    return;
                }

                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing.gameObject);
                return;
            }

            GameObject go = new GameObject("RoundVictoryOverlay");
            Undo.RegisterCreatedObjectUndo(go, "Add RoundVictoryOverlay");
            RoundVictoryOverlay rvo = go.AddComponent<RoundVictoryOverlay>();
            rvo.EnsureDisplayCanvasInEditor();
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log("[CongoGames] RoundVictoryOverlay ajouté. Nécessite un GameModeManager + modes en scène. Sorting 75, au-dessus du menu quiz.");
        }
    }
}
#endif
