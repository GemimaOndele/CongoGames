using UnityEngine;
using CongoGames.Core;
using CongoGames.Network;
using CongoGames.Presentation;
using System.Globalization;

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
        private int modePickIndex;
        private string roundSecondsDraft = "120";
        private string quizSecondsDraft = "180";
        private bool lockSingleModeDraft;
        private readonly string[] modeIds =
        {
            "quiz",
            "semantic",
            "word-scramble",
            "crossword-lite",
            "blind-test",
            "mystery-word",
            "memory",
            "speed-chrono",
            "image-guess"
        };

        private void Awake()
        {
            nameDraft = PlayerProfileStore.DisplayName;
            soloDraft = PlayerProfileStore.SoloOrUnknown;
            useVirtual3dDraft = PlayerPrefs.GetInt(PresentationConfig.PrefsUseVirtual3D, 1) != 0;
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm != null)
            {
                roundSecondsDraft = Mathf.RoundToInt(gmm.RoundDuration).ToString(CultureInfo.InvariantCulture);
                quizSecondsDraft = "180";
                lockSingleModeDraft = gmm.IsLocalDemoModeLocked;
                string locked = gmm.LockedModeId;
                for (int i = 0; i < modeIds.Length; i++)
                {
                    if (modeIds[i] == locked)
                    {
                        modePickIndex = i;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (GameInput.F9Down())
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

            float w = 520f;
            GUILayout.BeginArea(new Rect(16f, 120f, w, 560f), GUI.skin.box);
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

            GUILayout.Space(10f);
            GUILayout.Label("Test local — choisir le mini-jeu + durée");
            string[] modeLabels = new string[modeIds.Length];
            for (int i = 0; i < modeIds.Length; i++)
            {
                modeLabels[i] = (i + 1) + ". " + GameModeManager.GetModeDisplayName(modeIds[i]);
            }

            modePickIndex = GUILayout.SelectionGrid(modePickIndex, modeLabels, 2);
            modePickIndex = Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1);

            GUILayout.Space(6f);
            GUILayout.Label("Durée mini-jeux (hors Quiz) en secondes");
            roundSecondsDraft = GUILayout.TextField(roundSecondsDraft ?? "", GUILayout.Height(28f));
            GUILayout.Label("Durée bloc Quiz en secondes");
            quizSecondsDraft = GUILayout.TextField(quizSecondsDraft ?? "", GUILayout.Height(28f));
            lockSingleModeDraft = GUILayout.Toggle(lockSingleModeDraft, "Verrouiller le jeu choisi (pas de rotation auto)");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Appliquer durées", GUILayout.Height(30f)))
            {
                GameModeManager gmm = GameModeManager.Instance;
                if (gmm != null)
                {
                    if (TryParseSeconds(roundSecondsDraft, out float roundSec))
                    {
                        gmm.SetDefaultRoundDuration(roundSec);
                    }

                    if (TryParseSeconds(quizSecondsDraft, out float quizSec))
                    {
                        gmm.SetQuizSessionDuration(quizSec);
                    }
                }
            }

            if (GUILayout.Button("Lancer ce jeu", GUILayout.Height(30f)))
            {
                GameModeManager gmm = GameModeManager.Instance;
                if (gmm != null)
                {
                    string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                    gmm.SetLocalDemoModeLock(lockSingleModeDraft, modeId);
                    gmm.StartMode(modeId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Astuce : si verrouillage activé, le même jeu redémarre à chaque fin de timer.");
            GUILayout.EndArea();
        }

        private static bool TryParseSeconds(string raw, out float seconds)
        {
            seconds = 0f;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            string s = raw.Trim().Replace(',', '.');
            if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
            {
                return false;
            }

            seconds = Mathf.Clamp(v, 5f, 7200f);
            return true;
        }
    }
}
