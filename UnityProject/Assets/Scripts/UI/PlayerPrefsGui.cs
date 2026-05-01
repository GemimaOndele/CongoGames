using UnityEngine;
using CongoGames.Core;
using CongoGames.Network;
using CongoGames.Presentation;
using System.Globalization;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using CongoGames.AI;

namespace CongoGames.UI
{
    /// <summary>
    /// F9 : pseudo + solo/équipe (démo). Masqué si WebSocket TikTok connecté.
    /// </summary>
    public class PlayerPrefsGui : MonoBehaviour
    {
        private bool show;
        private bool guiSized;
        private int guiSizedForW;
        private int guiSizedForH;
        private string nameDraft = "";
        private bool soloDraft = true;
        private bool useVirtual3dDraft = true;
        private string providerDraft = "invité";
        private string avatarUrlDraft = "";
        private bool adminDraft;
        private string oauthTikTokDraft = "";
        private string oauthGoogleDraft = "";
        private string oauthFacebookDraft = "";
        private string authTokenDraft = "";
        private string authStatus = "";
        private bool authBusy;
        private Vector2 panelScroll;
        private const float UiFieldHeight = 40f;
        private const float UiButtonHeight = 42f;
        private const float UiSmallButtonHeight = 36f;
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
        private readonly string[] demoProviders = { "Google", "TikTok", "Facebook", "Invité" };
        private int demoProviderIndex = 3;

        [System.Serializable]
        private class AuthProvidersPayload
        {
            public bool ok;
            public AuthProviders providers;
        }

        [System.Serializable]
        private class AuthProviders
        {
            public string tiktok;
            public string google;
            public string facebook;
        }

        [System.Serializable]
        private class ProfileSyncRequest
        {
            public string authToken;
            public string provider;
            public string displayName;
            public string avatarUrl;
            public bool isAdmin;
        }

        [System.Serializable]
        private class ProfileSyncPayload
        {
            public bool ok;
            public string authToken;
            public ProfileDto profile;
        }

        [System.Serializable]
        private class ProfileDto
        {
            public string provider;
            public string displayName;
            public string avatarUrl;
            public bool isAdmin;
        }

        private void Awake()
        {
            nameDraft = PlayerProfileStore.DisplayName;
            soloDraft = PlayerProfileStore.SoloOrUnknown;
            providerDraft = PlayerProfileStore.AuthProvider;
            avatarUrlDraft = PlayerProfileStore.AvatarUrl;
            adminDraft = PlayerProfileStore.IsAdmin;
            oauthTikTokDraft = PlayerProfileStore.OAuthUrlTikTok;
            oauthGoogleDraft = PlayerProfileStore.OAuthUrlGoogle;
            oauthFacebookDraft = PlayerProfileStore.OAuthUrlFacebook;
            demoProviderIndex = ProviderIndexFromValue(providerDraft);
            useVirtual3dDraft = PlayerPrefs.GetInt(PresentationConfig.PrefsUseVirtual3D, 1) != 0;
            StartCoroutine(CoLoadAuthProviders());
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
            DrawAuthStatusBadge();
            bool isAdmin = PlayerProfileStore.IsAdmin;
            if (isAdmin)
            {
                DrawQuickModeBar();
            }
            if (!show)
            {
                float btnW = 300f;
                float btnH = 50f;
                Rect quick = new Rect(Screen.width - btnW - 16f, Screen.height - btnH - 16f, btnW, btnH);
                string opener = isAdmin ? "Tests rapides (F9)" : "Profil joueur (F9)";
                if (GUI.Button(quick, opener))
                {
                    show = true;
                }
                return;
            }
            float y = isAdmin ? 304f : 74f;
            float w = Mathf.Min(Screen.width * 0.92f, 1320f);
            float h = Mathf.Min(Screen.height - y - 12f, Screen.height * 0.9f);
            float x = Mathf.Max(12f, (Screen.width - w) * 0.5f);
            GUILayout.BeginArea(new Rect(x, y, w, h), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Profil démo (F9 pour masquer)");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Masquer F9", GUILayout.Width(170f), GUILayout.Height(28f)))
            {
                show = false;
            }
            GUILayout.EndHorizontal();
            panelScroll = GUILayout.BeginScrollView(panelScroll, GUILayout.ExpandHeight(true));
            GUILayout.Space(10f);
            GUILayout.Label("Pseudo (scores locaux)");
            nameDraft = GUILayout.TextField(nameDraft ?? "", GUILayout.Height(UiFieldHeight));
            soloDraft = GUILayout.Toggle(soloDraft, "Je joue seul(e) (sinon mode groupe/salon)");
            GUILayout.Space(10f);
            GUILayout.Label("Connexion démo / création compte (profil affiché)");
            demoProviderIndex = GUILayout.SelectionGrid(demoProviderIndex, demoProviders, 2);
            providerDraft = ProviderValueFromIndex(demoProviderIndex);
            adminDraft = GUILayout.Toggle(adminDraft, "Mode administrateur (vue séparée)");
            GUILayout.Label("Avatar URL (optionnel)");
            avatarUrlDraft = GUILayout.TextField(avatarUrlDraft ?? "", GUILayout.Height(UiFieldHeight));
            GUILayout.Space(10f);
            GUILayout.Label("OAuth (priorité: TikTok > Google/Facebook)");
            GUILayout.Label("URL TikTok");
            oauthTikTokDraft = GUILayout.TextField(oauthTikTokDraft ?? "", GUILayout.Height(UiFieldHeight));
            GUILayout.Label("URL Google");
            oauthGoogleDraft = GUILayout.TextField(oauthGoogleDraft ?? "", GUILayout.Height(UiFieldHeight));
            GUILayout.Label("URL Facebook");
            oauthFacebookDraft = GUILayout.TextField(oauthFacebookDraft ?? "", GUILayout.Height(UiFieldHeight));
            GUILayout.Space(8f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Se connecter TikTok", GUILayout.Height(UiButtonHeight)))
            {
                StartCoroutine(CoStartOAuthFlow("tiktok"));
            }
            if (GUILayout.Button("Google", GUILayout.Height(UiButtonHeight)))
            {
                StartCoroutine(CoStartOAuthFlow("google"));
            }
            if (GUILayout.Button("Facebook", GUILayout.Height(UiButtonHeight)))
            {
                StartCoroutine(CoStartOAuthFlow("facebook"));
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sync profil auto", GUILayout.Height(UiSmallButtonHeight)))
            {
                StartCoroutine(CoSyncProfile(authTokenDraft));
            }
            GUILayout.Label(string.IsNullOrWhiteSpace(authStatus) ? "Auth: prêt" : authStatus);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
            if (GUILayout.Button("Enregistrer", GUILayout.Height(UiButtonHeight)))
            {
                PlayerProfileStore.DisplayName = string.IsNullOrWhiteSpace(nameDraft) ? PlayerProfileStore.DefaultDisplayName : nameDraft;
                PlayerProfileStore.SoloOrUnknown = soloDraft;
                PlayerProfileStore.AuthProvider = providerDraft;
                PlayerProfileStore.AvatarUrl = avatarUrlDraft;
                PlayerProfileStore.IsAdmin = adminDraft;
                PlayerProfileStore.OAuthUrlTikTok = oauthTikTokDraft;
                PlayerProfileStore.OAuthUrlGoogle = oauthGoogleDraft;
                PlayerProfileStore.OAuthUrlFacebook = oauthFacebookDraft;
                if (ScoreManager.Instance != null && !string.IsNullOrWhiteSpace(PlayerProfileStore.ScoreUsernameForLocalPlay()))
                {
                    ScoreManager.Instance.UpdatePlayerAvatar(PlayerProfileStore.ScoreUsernameForLocalPlay(), PlayerProfileStore.AvatarUrl);
                }
            }

            if (!PlayerProfileStore.IsAdmin)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Vue joueur active. Outils admin masqués.");
                GUILayout.Label("Passe en Admin pour afficher la barre rapide.");
            }

            GUILayout.Space(12f);
            useVirtual3dDraft = GUILayout.Toggle(
                useVirtual3dDraft,
                "Fond 3D plateau TV (sinon vidéos Theme/).");
            GUILayout.Label(
                "Mix auto: alterne vidéo et 3D si les deux sont disponibles.");
            if (GUILayout.Button("Appliquer affichage 3D", GUILayout.Height(UiButtonHeight)))
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

            GUILayout.Space(12f);
            GUILayout.Label("Ambiance musique (live, sans redémarrage)");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Live soft", GUILayout.Height(UiButtonHeight)))
            {
                ThemeMusicPlayer.Instance?.SetAmbiencePreset(false);
            }

            if (GUILayout.Button("Live agressive", GUILayout.Height(UiButtonHeight)))
            {
                ThemeMusicPlayer.Instance?.SetAmbiencePreset(true);
            }

            if (GUILayout.Button("Reset rotation A/V", GUILayout.Height(UiButtonHeight)))
            {
                ResetThemeRotationNow();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10f);
            GUILayout.Label(ScoreHistoryStore.BuildSummaryLine());

            GUILayout.Space(14f);
            GUILayout.Label("Test local: choisir mini-jeu + durée");
            string[] modeLabels = new string[modeIds.Length];
            for (int i = 0; i < modeIds.Length; i++)
            {
                modeLabels[i] = (i + 1) + ". " + GameModeManager.GetModeDisplayName(modeIds[i]);
            }

            modePickIndex = GUILayout.SelectionGrid(modePickIndex, modeLabels, 2);
            modePickIndex = Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1);

            GUILayout.Space(10f);
            GUILayout.Label("Durée mini-jeux (hors Quiz) en secondes");
            roundSecondsDraft = GUILayout.TextField(roundSecondsDraft ?? "", GUILayout.Height(UiFieldHeight));
            GUILayout.Label("Durée bloc Quiz en secondes");
            quizSecondsDraft = GUILayout.TextField(quizSecondsDraft ?? "", GUILayout.Height(UiFieldHeight));
            lockSingleModeDraft = GUILayout.Toggle(lockSingleModeDraft, "Verrouiller le jeu choisi (sans rotation auto)");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mode précédent", GUILayout.Height(UiButtonHeight)))
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

            if (GUILayout.Button("Mode suivant", GUILayout.Height(UiButtonHeight)))
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
            GUILayout.Space(6f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Appliquer", GUILayout.Height(UiButtonHeight)))
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

                    // "Appliquer durées" en mode test local: garde le mode choisi verrouillé.
                    string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                    gmm.SetLocalDemoModeLock(true, modeId);
                }
            }

            if (GUILayout.Button("Lancer", GUILayout.Height(UiButtonHeight)))
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

                    string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                    // "Lancer ce jeu" = rester sur ce mode pendant la durée choisie.
                    gmm.SetLocalDemoModeLock(true, modeId);
                    gmm.StartMode(modeId);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Astuce: avec verrouillage, le même jeu redémarre à chaque fin de timer.");
            DrawThemeRuntimeDebug();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private IEnumerator CoLoadAuthProviders()
        {
            string baseUrl = ResolveBackendBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl)) yield break;
            string url = baseUrl + "/auth/providers";
            using UnityWebRequest req = UnityWebRequest.Get(url);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success || string.IsNullOrWhiteSpace(req.downloadHandler?.text))
            {
                yield break;
            }

            AuthProvidersPayload payload = JsonUtility.FromJson<AuthProvidersPayload>(req.downloadHandler.text);
            if (payload == null || payload.providers == null) yield break;
            if (string.IsNullOrWhiteSpace(oauthTikTokDraft) && !string.IsNullOrWhiteSpace(payload.providers.tiktok)) oauthTikTokDraft = payload.providers.tiktok.Trim();
            if (string.IsNullOrWhiteSpace(oauthGoogleDraft) && !string.IsNullOrWhiteSpace(payload.providers.google)) oauthGoogleDraft = payload.providers.google.Trim();
            if (string.IsNullOrWhiteSpace(oauthFacebookDraft) && !string.IsNullOrWhiteSpace(payload.providers.facebook)) oauthFacebookDraft = payload.providers.facebook.Trim();
        }

        private IEnumerator CoStartOAuthFlow(string provider)
        {
            if (authBusy) yield break;
            authBusy = true;
            providerDraft = provider;
            demoProviderIndex = ProviderIndexFromValue(provider);
            authStatus = "Auth " + provider + "…";

            string baseUrl = ResolveBackendBaseUrl();
            string startUrl = baseUrl + "/auth/" + provider + "/start";
            Application.OpenURL(startUrl);

            // Auto-sync côté Unity: on poll le dernier profil validé pour ce provider.
            float end = Time.unscaledTime + 90f;
            while (Time.unscaledTime < end)
            {
                string latestUrl = baseUrl + "/auth/" + provider + "/latest";
                using UnityWebRequest req = UnityWebRequest.Get(latestUrl);
                yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(req.downloadHandler?.text))
                {
                    ProfileSyncPayload latest = JsonUtility.FromJson<ProfileSyncPayload>(req.downloadHandler.text);
                    if (latest != null && latest.ok && !string.IsNullOrWhiteSpace(latest.authToken))
                    {
                        authTokenDraft = latest.authToken;
                        yield return CoSyncProfile(authTokenDraft);
                        authBusy = false;
                        yield break;
                    }
                }

                yield return new WaitForSecondsRealtime(2f);
            }

            authStatus = "Auth expirée (timeout).";
            authBusy = false;
        }

        private IEnumerator CoSyncProfile(string authToken)
        {
            string baseUrl = ResolveBackendBaseUrl();
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                authStatus = "Backend introuvable.";
                yield break;
            }

            var payload = new ProfileSyncRequest
            {
                authToken = string.IsNullOrWhiteSpace(authToken) ? "" : authToken.Trim(),
                provider = providerDraft,
                displayName = nameDraft,
                avatarUrl = avatarUrlDraft,
                isAdmin = adminDraft
            };

            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using UnityWebRequest req = new UnityWebRequest(baseUrl + "/profile/sync", UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success || string.IsNullOrWhiteSpace(req.downloadHandler?.text))
            {
                authStatus = "Sync profil échouée.";
                yield break;
            }

            ProfileSyncPayload res = JsonUtility.FromJson<ProfileSyncPayload>(req.downloadHandler.text);
            if (res == null || !res.ok || res.profile == null)
            {
                authStatus = "Profil invalide.";
                yield break;
            }

            authTokenDraft = string.IsNullOrWhiteSpace(res.authToken) ? authTokenDraft : res.authToken.Trim();
            providerDraft = string.IsNullOrWhiteSpace(res.profile.provider) ? providerDraft : res.profile.provider.Trim().ToLowerInvariant();
            nameDraft = string.IsNullOrWhiteSpace(res.profile.displayName) ? nameDraft : res.profile.displayName.Trim();
            avatarUrlDraft = string.IsNullOrWhiteSpace(res.profile.avatarUrl) ? "" : res.profile.avatarUrl.Trim();
            adminDraft = res.profile.isAdmin;
            demoProviderIndex = ProviderIndexFromValue(providerDraft);

            PlayerProfileStore.DisplayName = string.IsNullOrWhiteSpace(nameDraft) ? PlayerProfileStore.DefaultDisplayName : nameDraft;
            PlayerProfileStore.AuthProvider = providerDraft;
            PlayerProfileStore.AvatarUrl = avatarUrlDraft;
            PlayerProfileStore.IsAdmin = adminDraft;
            if (ScoreManager.Instance != null && !string.IsNullOrWhiteSpace(PlayerProfileStore.ScoreUsernameForLocalPlay()))
            {
                ScoreManager.Instance.UpdatePlayerAvatar(PlayerProfileStore.ScoreUsernameForLocalPlay(), PlayerProfileStore.AvatarUrl);
            }

            authStatus = "Profil synchronisé.";
        }

        private static string ResolveBackendBaseUrl()
        {
            string v = "";
            if (AIHostManager.Instance != null && !string.IsNullOrWhiteSpace(AIHostManager.Instance.TtsHttpBase))
            {
                v = AIHostManager.Instance.TtsHttpBase;
            }
            else if (!string.IsNullOrWhiteSpace(WebGlCloudEndpoints.LoadedTtsBase))
            {
                v = WebGlCloudEndpoints.LoadedTtsBase;
            }

            if (string.IsNullOrWhiteSpace(v))
            {
                v = "http://127.0.0.1:3000";
            }

            return v.Trim().TrimEnd('/');
        }

        private static int ProviderIndexFromValue(string provider)
        {
            string p = (provider ?? "").Trim().ToLowerInvariant();
            if (p.Contains("google")) return 0;
            if (p.Contains("tiktok")) return 1;
            if (p.Contains("facebook")) return 2;
            return 3;
        }

        private static string ProviderValueFromIndex(int idx)
        {
            switch (Mathf.Clamp(idx, 0, 3))
            {
                case 0: return "google";
                case 1: return "tiktok";
                case 2: return "facebook";
                default: return "invité";
            }
        }

        private void DrawQuickModeBar()
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null) return;

            float barW = Mathf.Min(Screen.width * 0.94f, 1680f);
            float barH = Mathf.Min(Screen.height * 0.32f, 280f);
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

                // Le flux rapide applique aussi le verrou sur le mode choisi.
                string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                gmm.SetLocalDemoModeLock(true, modeId);
            }

            if (GUILayout.Button("Lancer", GUILayout.Width(170f), GUILayout.Height(62f)))
            {
                if (TryParseSeconds(roundSecondsDraft, out float roundSec))
                {
                    gmm.SetDefaultRoundDuration(roundSec);
                }
                string modeId = modeIds[Mathf.Clamp(modePickIndex, 0, modeIds.Length - 1)];
                // "Lancer" rapide doit aussi respecter la durée saisie.
                gmm.SetLocalDemoModeLock(true, modeId);
                gmm.StartMode(modeId);
            }

            if (GUILayout.Button(show ? "Masquer F9" : "Ouvrir F9", GUILayout.Width(210f), GUILayout.Height(62f)))
            {
                show = !show;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(4f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Ambiance musique", GUILayout.Width(220f));
            if (GUILayout.Button("Live soft", GUILayout.Width(200f), GUILayout.Height(48f)))
            {
                ThemeMusicPlayer.Instance?.SetAmbiencePreset(false);
            }

            if (GUILayout.Button("Live agressive", GUILayout.Width(220f), GUILayout.Height(48f)))
            {
                ThemeMusicPlayer.Instance?.SetAmbiencePreset(true);
            }

            if (GUILayout.Button("Reset rotation A/V", GUILayout.Width(260f), GUILayout.Height(48f)))
            {
                ResetThemeRotationNow();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            DrawThemeRuntimeDebug();
            GUILayout.EndArea();
        }

        private static void ResetThemeRotationNow()
        {
            ThemeMusicPlayer.ResetRotationDebugState();
            ThemeBackgroundController.ResetRotationDebugState();
            string modeId = "quiz";
            if (GameModeManager.Instance != null && !string.IsNullOrEmpty(GameModeManager.Instance.ActiveModeId))
            {
                modeId = GameModeManager.Instance.ActiveModeId;
            }

            ThemeMusicPlayer.Instance?.ApplyGameMode(modeId);
            ThemeBackgroundController bg = Object.FindAnyObjectByType<ThemeBackgroundController>();
            if (bg != null)
            {
                bg.ApplyGameMode(modeId);
            }
        }

        private static void DrawThemeRuntimeDebug()
        {
            ThemeMusicPlayer music = ThemeMusicPlayer.Instance;
            ThemeBackgroundController bg = Object.FindAnyObjectByType<ThemeBackgroundController>();
            string mode = GameModeManager.Instance != null ? GameModeManager.Instance.ActiveModeId : "—";
            GUIStyle rich = new GUIStyle(GUI.skin.label) { richText = true };
            GUILayout.Space(6f);
            GUILayout.Label("Debug runtime thème (audio/vidéo)");
            GUILayout.Label("Mode: " + mode);
            if (music != null)
            {
                string audioLine = "Audio source: " + music.DebugSource
                    + " | piste: " + (music.DebugNowPlaying ?? "—")
                    + " | idx: " + music.DebugRotationIndex
                    + " | total: " + music.DebugPlaylistCount
                    + " | stable: " + Mathf.RoundToInt(music.DebugNowPlayingStableSeconds) + "s";
                if (music.DebugNowPlayingStableSeconds >= 90f)
                {
                    audioLine = "<color=#FF6B6B>" + audioLine + "  [ALERTE: piste figée]</color>";
                    GUILayout.Label(audioLine, rich);
                }
                else if (music.DebugNowPlayingStableSeconds >= 45f)
                {
                    audioLine = "<color=#FFB25E>" + audioLine + "  [SURVEILLER]</color>";
                    GUILayout.Label(audioLine, rich);
                }
                else
                {
                    GUILayout.Label(audioLine);
                }
            }
            else
            {
                GUILayout.Label("Audio: ThemeMusicPlayer introuvable");
            }

            if (bg != null)
            {
                string bgLine = "Background phase: " + bg.DebugPhase
                    + " | vidéo: " + (bg.DebugVideoUrl ?? "—")
                    + " | idx: " + bg.DebugVideoIndex
                    + " | candidats: " + bg.DebugCandidateCount;
                if (bg.DebugCandidateCount <= 0)
                {
                    GUILayout.Label("<color=#FF6B6B>" + bgLine + "  [ALERTE: aucun candidat]</color>", rich);
                }
                else if (bg.DebugCandidateCount == 1)
                {
                    GUILayout.Label("<color=#FFB25E>" + bgLine + "  [SURVEILLER: variation faible]</color>", rich);
                }
                else
                {
                    GUILayout.Label(bgLine);
                }
            }
            else
            {
                GUILayout.Label("Background: ThemeBackgroundController introuvable");
            }
        }

        private void EnsureReadableGuiScale()
        {
            if (guiSized && guiSizedForW == Screen.width && guiSizedForH == Screen.height) return;
            guiSized = true;
            guiSizedForW = Screen.width;
            guiSizedForH = Screen.height;

            int minDim = Mathf.Min(Screen.width, Screen.height);
            int fs = minDim >= 1800 ? 36 : (minDim >= 1300 ? 32 : (minDim >= 900 ? 28 : 26));
            GUI.skin.label.fontSize = fs;
            GUI.skin.button.fontSize = fs;
            GUI.skin.textField.fontSize = fs;
            GUI.skin.toggle.fontSize = fs;
            GUI.skin.box.fontSize = fs;
            GUI.skin.label.wordWrap = true;
            GUI.skin.box.wordWrap = true;
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

        private void DrawAuthStatusBadge()
        {
            string provider = PlayerProfileStore.AuthProvider;
            if (string.IsNullOrWhiteSpace(provider)) provider = "invité";
            string display = PlayerProfileStore.DisplayName;
            if (string.IsNullOrWhiteSpace(display)) display = PlayerProfileStore.DefaultDisplayName;
            string role = PlayerProfileStore.IsAdmin ? "ADMIN" : "JOUEUR";

            string providerLabel = provider.Trim().ToLowerInvariant() switch
            {
                "tiktok" => "TikTok",
                "google" => "Google",
                "facebook" => "Facebook",
                _ => "Invité"
            };

            GUIStyle box = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.Max(16, GUI.skin.box.fontSize - 2)
            };
            Color old = GUI.color;
            GUI.color = PlayerProfileStore.IsAdmin
                ? new Color(0.16f, 0.28f, 0.22f, 0.92f)
                : new Color(0.15f, 0.17f, 0.23f, 0.92f);
            GUI.Box(new Rect(16f, 12f, Mathf.Min(Screen.width * 0.62f, 760f), 40f),
                "Connecté via: " + providerLabel + "   |   Rôle: " + role + "   |   @" + display, box);
            GUI.color = old;
        }
    }
}
