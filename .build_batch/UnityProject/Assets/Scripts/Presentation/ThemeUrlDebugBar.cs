using UnityEngine;
using CongoGames.Core;

namespace CongoGames.Presentation
{
    /// <summary>Coller des URLs HTTPS (fichier direct) sans éditer remote_media.json — coin supérieur droit.</summary>
    public class ThemeUrlDebugBar : MonoBehaviour
    {
        [Tooltip("Désactivé par défaut : la barre ne masque plus l’écran (F10 pour afficher).")]
        [SerializeField] private bool visible = false;

        private string musicUrl = "";
        private string bottomUrl = "";
        private string backgroundUrl = "";

        private void Update()
        {
            if (GameInput.F10Down())
            {
                visible = !visible;
            }
        }

        private void OnGUI()
        {
            if (!visible) return;

            float w = 340f;
            GUILayout.BeginArea(new Rect(Screen.width - w - 12f, 72f, w, 380f), GUI.skin.box);
            GUILayout.Label("URLs médias — F10 masque/affiche");
            GUILayout.Label("(session, priorité sur JSON)");
            GUILayout.Space(4f);
            GUILayout.Label("Musique (.mp3/.ogg)");
            musicUrl = GUILayout.TextField(musicUrl ?? "", GUILayout.Height(26f));
            GUILayout.Label("Vidéo bas d’écran");
            bottomUrl = GUILayout.TextField(bottomUrl ?? "", GUILayout.Height(26f));
            GUILayout.Label("Fond plein écran");
            backgroundUrl = GUILayout.TextField(backgroundUrl ?? "", GUILayout.Height(26f));
            if (GUILayout.Button("Appliquer + recharger le thème", GUILayout.Height(32f)))
            {
                RemoteThemeMediaConfig.SetRuntimeOverrides(musicUrl, bottomUrl, backgroundUrl);
                string id = ModeSurfaceController.Instance != null ? ModeSurfaceController.Instance.CurrentModeId : "quiz";
                ThemeRuntime.NotifyModeStarted(id);
            }

            GUILayout.Space(6f);
#if UNITY_WEBGL && !UNITY_EDITOR
            GUILayout.Label("Zoom interface (WebGL — F10)");
            float z = WebGlCanvasTuning.GetUserScale();
            float z2 = GUILayout.HorizontalSlider(z, 0.72f, 1.55f);
            if (Mathf.Abs(z2 - z) > 0.001f)
            {
                WebGlCanvasTuning.SetUserScale(z2);
            }

            GUILayout.Space(4f);
#endif
            GUILayout.Label("Vider = laisser le JSON / défaut");
            if (GUILayout.Button("Effacer surcharges", GUILayout.Height(28f)))
            {
                musicUrl = bottomUrl = backgroundUrl = "";
                RemoteThemeMediaConfig.SetRuntimeOverrides(null, null, null);
                string id = ModeSurfaceController.Instance != null ? ModeSurfaceController.Instance.CurrentModeId : "quiz";
                ThemeRuntime.NotifyModeStarted(id);
            }

            GUILayout.EndArea();
        }
    }
}
