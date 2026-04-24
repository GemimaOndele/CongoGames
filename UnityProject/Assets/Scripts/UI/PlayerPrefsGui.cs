using UnityEngine;
using CongoGames.Core;
using CongoGames.Network;
using CongoGames.Presentation;

namespace CongoGames.UI
{
    /// <summary>
    /// F9 : pseudo + solo/équipe (démo). Masqué si WebSocket TikTok connecté.
    /// </summary>
    public class PlayerPrefsGui : MonoBehaviour
    {
        private bool show;
        private string nameDraft = "";
        private bool soloDraft = true;
        private bool useVirtual3dDraft = true;

        private void Awake()
        {
            nameDraft = PlayerProfileStore.DisplayName;
            soloDraft = PlayerProfileStore.SoloOrUnknown;
            useVirtual3dDraft = PlayerPrefs.GetInt(PresentationConfig.PrefsUseVirtual3D, 1) != 0;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F9))
            {
                LiveEventClient c = Object.FindAnyObjectByType<LiveEventClient>();
                if (c != null && c.IsConnected) return;
                show = !show;
            }
        }

        private void OnGUI()
        {
            if (!show) return;
            LiveEventClient c = Object.FindAnyObjectByType<LiveEventClient>();
            if (c != null && c.IsConnected)
            {
                GUILayout.Label("Live TikTok : les noms viennent du chat.");
                return;
            }

            float w = 420f;
            GUILayout.BeginArea(new Rect(16f, 120f, w, 240f), GUI.skin.box);
            GUILayout.Label("Profil démo (F9 pour masquer)");
            GUILayout.Space(6f);
            GUILayout.Label("Pseudo (scores locaux)");
            nameDraft = GUILayout.TextField(nameDraft ?? "", GUILayout.Height(28f));
            soloDraft = GUILayout.Toggle(soloDraft, "Je joue seul(e) (décocher = mode groupe / salon)");
            if (GUILayout.Button("Enregistrer", GUILayout.Height(32f)))
            {
                PlayerProfileStore.DisplayName = string.IsNullOrWhiteSpace(nameDraft) ? PlayerProfileStore.DefaultDisplayName : nameDraft;
                PlayerProfileStore.SoloOrUnknown = soloDraft;
            }

            GUILayout.Space(8f);
            useVirtual3dDraft = GUILayout.Toggle(
                useVirtual3dDraft,
                "Fond 3D « plateau TV » (URP → RenderTexture) — Appliquer pour reconstruire");
            if (GUILayout.Button("Appliquer affichage 3D", GUILayout.Height(28f)))
            {
                PlayerPrefs.SetInt(PresentationConfig.PrefsUseVirtual3D, useVirtual3dDraft ? 1 : 0);
                PlayerPrefs.Save();
                PresentationConfig.UseVirtual3DShowStage = useVirtual3dDraft;
                string modeId = "quiz";
                if (GameModeManager.Instance != null && !string.IsNullOrEmpty(GameModeManager.Instance.ActiveModeId))
                {
                    modeId = GameModeManager.Instance.ActiveModeId;
                }
                else if (ModeSurfaceController.Instance != null && !string.IsNullOrEmpty(ModeSurfaceController.Instance.CurrentModeId))
                {
                    modeId = ModeSurfaceController.Instance.CurrentModeId;
                }

                ThemeBackgroundController bg = Object.FindAnyObjectByType<ThemeBackgroundController>();
                if (bg != null)
                {
                    bg.ApplyGameMode(modeId);
                }
            }

            GUILayout.Space(6f);
            GUILayout.Label(ScoreHistoryStore.BuildSummaryLine());
            GUILayout.EndArea();
        }
    }
}
