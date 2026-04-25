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
        private bool guiSized;
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
                show = !show;
            }
        }

        private void OnGUI()
        {
            EnsureReadableGuiScale();
            DrawQuickModeBar();
            if (!show)
            {
                float btnW = 300f;
                float btnH = 50f;
                Rect quick = new Rect(Screen.width - btnW - 16f, Screen.height - btnH - 16f, btnW, btnH);
                if (GUI.Button(quick, "Tests rapides (F9)"))
                {
                    show = true;
                }
                return;
            }
            float w = Mathf.Min(Screen.width * 0.46f, 760f);
            float h = Mathf.Min(Screen.height * 0.76f, 760f);
            GUILayout.BeginArea(new Rect(16f, 110f, w, h), GUI.skin.box);
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
            if (GUILayout.Button("Mode précédent", GUILayout.Height(30f)))
            {
                modePickIndex = (modePickIndex - 1 + modeIds.Length) % modeIds.Length;
                GameModeManager gmm = GameModeManager.Instance;
                if (gmm != null)
                {
                    string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                    gmm.SetLocalDemoModeLock(lockSingleModeDraft, modeId);
                    gmm.StartMode(modeId);
                }
            }

            if (GUILayout.Button("Mode suivant", GUILayout.Height(30f)))
            {
                modePickIndex = (modePickIndex + 1) % modeIds.Length;
                GameModeManager gmm = GameModeManager.Instance;
                if (gmm != null)
                {
                    string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                    gmm.SetLocalDemoModeLock(lockSingleModeDraft, modeId);
                    gmm.StartMode(modeId);
                }
            }
            GUILayout.EndHorizontal();

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

        private void DrawQuickModeBar()
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null) return;

            float barW = Mathf.Min(Screen.width * 0.94f, 1680f);
            float barH = Mathf.Min(Screen.height * 0.24f, 210f);
            float x = (Screen.width - barW) * 0.5f;
            GUILayout.BeginArea(new Rect(x, 10f, barW, barH), GUI.skin.box);
            GUILayout.Label("Test rapide — mode + durée (visible en permanence)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.Width(110f), GUILayout.Height(62f)))
            {
                modePickIndex = (modePickIndex - 1 + modeIds.Length) % modeIds.Length;
                string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                gmm.SetLocalDemoModeLock(lockSingleModeDraft, modeId);
                gmm.StartMode(modeId);
            }

            GUILayout.Label(GameModeManager.GetModeDisplayName(modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)]), GUILayout.Width(460f));

            if (GUILayout.Button("▶", GUILayout.Width(110f), GUILayout.Height(62f)))
            {
                modePickIndex = (modePickIndex + 1) % modeIds.Length;
                string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                gmm.SetLocalDemoModeLock(lockSingleModeDraft, modeId);
                gmm.StartMode(modeId);
            }

            GUILayout.Label("Temps(s)", GUILayout.Width(140f));
            roundSecondsDraft = GUILayout.TextField(roundSecondsDraft ?? "", GUILayout.Width(160f), GUILayout.Height(58f));
            if (GUILayout.Button("Appliquer", GUILayout.Width(190f), GUILayout.Height(62f)))
            {
                if (TryParseSeconds(roundSecondsDraft, out float roundSec))
                {
                    gmm.SetDefaultRoundDuration(roundSec);
                }
            }

            if (GUILayout.Button("Lancer", GUILayout.Width(170f), GUILayout.Height(62f)))
            {
                string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                gmm.SetLocalDemoModeLock(lockSingleModeDraft, modeId);
                gmm.StartMode(modeId);
            }

            if (GUILayout.Button(show ? "Masquer F9" : "Ouvrir F9", GUILayout.Width(210f), GUILayout.Height(62f)))
            {
                show = !show;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void EnsureReadableGuiScale()
        {
            if (guiSized) return;
            guiSized = true;
            int fs = Screen.height >= 2000 ? 32 : (Screen.height >= 1400 ? 26 : 21);
            GUI.skin.label.fontSize = fs;
            GUI.skin.button.fontSize = fs;
            GUI.skin.textField.fontSize = fs;
            GUI.skin.toggle.fontSize = fs - 1;
            GUI.skin.box.fontSize = fs;
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
