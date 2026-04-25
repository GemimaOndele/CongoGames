using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using CongoGames.AI;
using CongoGames.Network;
using CongoGames.Perf;
using CongoGames.Presentation;
using CongoGames.UI;

namespace CongoGames.Core
{
    public static class RuntimeBootstrap
    {
        private static Sprite whiteSprite;
        private static Texture2D congoFlagTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            PresentationConfig.ApplyFromPlayerPrefs();

            ModeSurfaceController sceneModeSurface = Object.FindAnyObjectByType<ModeSurfaceController>();
            if (sceneModeSurface != null)
            {
                HarmonizeSceneModeRoot(sceneModeSurface);
                return;
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject services = new GameObject("CongoGames_Services");
            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                services.AddComponent<AudioListener>();
            }

            GameObject musicHost = new GameObject("ThemeMusic");
            musicHost.transform.SetParent(services.transform, false);
            musicHost.AddComponent<ThemeMusicPlayer>();

            services.AddComponent<GameSfxHub>();

            services.AddComponent<LanguageManager>();
            services.AddComponent<ScoreManager>();
            services.AddComponent<QuestionManager>();
            services.AddComponent<GameModeManager>();
            services.AddComponent<BattleManager>();
            services.AddComponent<AIHostManager>();
            services.AddComponent<BroadcastAudioMixCoordinator>();
            services.AddComponent<LiveHostDirector>();
            services.AddComponent<IntroDirector>();
            LiveEventClient live = services.AddComponent<LiveEventClient>();

            GameObject canvasGo = new GameObject("CongoGames_Canvas");
            canvasGo.AddComponent<ThemeUrlDebugBar>();
            canvasGo.AddComponent<PlayerPrefsGui>();
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.55f;
            WebGlCanvasTuning.ApplyToScaler(scaler);
            canvasGo.AddComponent<GraphicRaycaster>();
#if UNITY_WEBGL && !UNITY_EDITOR
            canvasGo.AddComponent<WebGlUiScaleRuntime>();
#endif

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            RectTransform canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.anchorMin = Vector2.zero;
            canvasRt.anchorMax = Vector2.one;
            canvasRt.offsetMin = Vector2.zero;
            canvasRt.offsetMax = Vector2.zero;

            GameObject themeBg = CreateUiRect(canvasGo.transform, "ThemeBackground", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            themeBg.transform.SetAsFirstSibling();
            RawImage themeRaw = themeBg.AddComponent<RawImage>();
            themeRaw.color = new Color(0.06f, 0.1f, 0.08f, 1f);
            themeRaw.raycastTarget = false;
            themeBg.AddComponent<ThemeBackgroundController>();

            GameObject backdrop = CreateUiRect(canvasGo.transform, "Backdrop", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image bg = backdrop.AddComponent<Image>();
            bg.sprite = GetWhiteSprite();
            bg.color = new Color(0.02f, 0.05f, 0.09f, 0.22f);
            bg.raycastTarget = false;
            backdrop.AddComponent<Ps5CanvasBackdropRig>();

            GameObject topBar = CreateUiRect(canvasGo.transform, "TopBar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), new Vector2(0f, 88f));
            Image bar = topBar.AddComponent<Image>();
            bar.sprite = GetWhiteSprite();
            bar.color = new Color(0.04f, 0.12f, 0.08f, 1f);

            CreateFlagBadge(topBar.transform);

            const float logoMaxH = 126f;
            const float logoMaxW = 620f;
            float headerLogoH = 0f;
            GameObject headerLogoInstance = null;
            Texture2D logoTex = Resources.Load<Texture2D>("Branding/CongoGameslogo");
            if (logoTex != null)
            {
                float aspect = (float)logoTex.width / Mathf.Max(1, logoTex.height);
                float lh = logoMaxH;
                float lw = lh * aspect;
                if (lw > logoMaxW)
                {
                    lw = logoMaxW;
                    lh = lw / aspect;
                }

                headerLogoH = lh;
                GameObject logoGo = new GameObject("CongoGamesLogo");
                headerLogoInstance = logoGo;
                logoGo.transform.SetParent(canvasGo.transform, false);
                RectTransform lrt = logoGo.AddComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0.5f, 1f);
                lrt.anchorMax = new Vector2(0.5f, 1f);
                lrt.pivot = new Vector2(0.5f, 1f);
                lrt.anchoredPosition = new Vector2(0f, -8f);
                lrt.sizeDelta = new Vector2(lw, lh);
                RawImage logoRaw = logoGo.AddComponent<RawImage>();
                logoRaw.texture = logoTex;
                logoRaw.uvRect = new Rect(0f, 0f, 1f, 1f);
                logoRaw.color = Color.white;
                logoRaw.raycastTarget = false;
            }
            else
            {
                Text title = CreateText(canvasGo.transform, "Title", font, 48, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(72f, -40f), new Vector2(0f, 58f));
                title.fontStyle = FontStyle.Bold;
                title.color = new Color(1f, 0.92f, 0.2f);
                title.text = "CONGOGAMES";
                headerLogoH = 58f;
            }

            // Sous le logo (ou le titre texte) : aligné comme avant quand pas de PNG
            float brandY = 10f + headerLogoH + 2f + 18f;
            float modeY = 10f + headerLogoH + 2f + 36f + 8f + 20f;
            if (logoTex == null)
            {
                brandY = 90f;
                modeY = 122f;
            }

            Text brand = CreateText(canvasGo.transform, "Brand", font, 22, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -brandY), new Vector2(0f, 40f));
            brand.color = new Color(0.92f, 0.92f, 0.92f, 0.9f);
            brand.text = "Congo · tricolore vert · jaune · rouge · FR · Lingala · Kituba";

            Text modeLabel = CreateText(canvasGo.transform, "ModeLabel", font, 28, TextAnchor.MiddleLeft, new Vector2(0f, 1f), new Vector2(0.44f, 1f), new Vector2(24f, -modeY), new Vector2(0f, 44f));
            modeLabel.color = Color.white;
            modeLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            modeLabel.verticalOverflow = VerticalWrapMode.Overflow;
            modeLabel.resizeTextForBestFit = true;
            modeLabel.resizeTextMinSize = 18;
            modeLabel.resizeTextMaxSize = 30;

            // Minuteur compact en bas-gauche : n'occulte ni drapeau, ni logo, ni titre.
            GameObject timerBlock = CreateUiRect(canvasGo.transform, "TimerBlock", new Vector2(0.015f, 0.015f), new Vector2(0.072f, 0.095f), Vector2.zero, Vector2.zero);
            RectTransform timerRt = timerBlock.GetComponent<RectTransform>();
            timerRt.pivot = new Vector2(0.5f, 0.5f);
            timerRt.offsetMin = new Vector2(2f, 2f);
            timerRt.offsetMax = new Vector2(-2f, -2f);
            Image ringBg = timerBlock.AddComponent<Image>();
            ringBg.sprite = GetWhiteSprite();
            ringBg.color = new Color(0.15f, 0.15f, 0.18f, 0.9f);

            GameObject ringGo = CreateUiRect(timerBlock.transform, "TimerRing", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image ring = ringGo.AddComponent<Image>();
            ring.sprite = GetWhiteSprite();
            ring.color = new Color(0.95f, 0.78f, 0.08f, 1f);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillAmount = 1f;
            ring.raycastTarget = false;

            Text timerSec = CreateText(timerBlock.transform, "TimerSec", font, 20, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 44f));
            timerSec.fontStyle = FontStyle.Bold;
            timerSec.color = Color.white;
            if (headerLogoInstance != null)
            {
                headerLogoInstance.transform.SetSiblingIndex(timerBlock.transform.GetSiblingIndex() + 1);
            }

            GameObject lbPanel = CreateUiRect(canvasGo.transform, "LeaderboardPanel", new Vector2(0.77f, 0.12f), new Vector2(0.99f, 0.9f), Vector2.zero, Vector2.zero);
            Image lbBg = lbPanel.AddComponent<Image>();
            lbBg.sprite = GetWhiteSprite();
            lbBg.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);
            Outline lbOutline = lbPanel.AddComponent<Outline>();
            lbOutline.effectColor = new Color(0.96f, 0.82f, 0.18f, 0.45f);
            lbOutline.effectDistance = new Vector2(2f, -2f);
            RectTransform lbPanelRt = lbPanel.GetComponent<RectTransform>();
            lbPanelRt.offsetMin = new Vector2(10f, 10f);
            lbPanelRt.offsetMax = new Vector2(-10f, -10f);

            Text lbTitle = CreateText(lbPanel.transform, "LbTitle", font, 22, TextAnchor.MiddleCenter, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f), new Vector2(-12f, 38f));
            lbTitle.fontStyle = FontStyle.Bold;
            lbTitle.color = new Color(1f, 0.85f, 0.15f);
            lbTitle.text = "Classement";
            lbTitle.resizeTextForBestFit = true;
            lbTitle.resizeTextMinSize = 18;
            lbTitle.resizeTextMaxSize = 24;

            Text lbBody = CreateText(lbPanel.transform, "LbBody", font, 16, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            lbBody.color = new Color(0.95f, 0.95f, 0.95f);
            lbBody.alignment = TextAnchor.UpperLeft;
            lbBody.horizontalOverflow = HorizontalWrapMode.Wrap;
            lbBody.verticalOverflow = VerticalWrapMode.Overflow;
            lbBody.lineSpacing = 1.15f;
            RectTransform lbRt = lbBody.rectTransform;
            lbRt.anchorMin = new Vector2(0f, 0f);
            lbRt.anchorMax = new Vector2(1f, 1f);
            lbRt.pivot = new Vector2(0.5f, 0.5f);
            lbRt.anchoredPosition = Vector2.zero;
            lbRt.offsetMin = new Vector2(10f, 10f);
            lbRt.offsetMax = new Vector2(-10f, -48f);

            LeaderboardUI lbUi = lbPanel.AddComponent<LeaderboardUI>();
            lbUi.BindRuntime(lbBody);

            // Zone de jeu un peu plus large pour laisser respirer les textes.
            GameObject modeRoot = CreateUiRect(canvasGo.transform, "ModePanelsRoot", new Vector2(0.015f, 0.10f), new Vector2(0.75f, 0.86f), Vector2.zero, Vector2.zero);
            RectTransform modeRootRt = modeRoot.GetComponent<RectTransform>();
            modeRootRt.offsetMin = new Vector2(6f, 6f);
            modeRootRt.offsetMax = new Vector2(-8f, -6f);
            modeRoot.AddComponent<ModeSurfaceController>();
            modeRoot.AddComponent<MiniGamePanelContent>();
            modeRoot.AddComponent<HudPanelAnimator>();
            modeRoot.AddComponent<Ps5HudParallax>();
            modeRoot.AddComponent<Ps5ModeVisualRig>();

            GameObject panelQuiz = CreateUiRect(modeRoot.transform, "PanelQuiz", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            GameObject board = CreateUiRect(panelQuiz.transform, "QuestionBoard", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image boardBg = board.AddComponent<Image>();
            boardBg.sprite = GetWhiteSprite();
            boardBg.color = new Color(0.06f, 0.08f, 0.11f, 0.78f);
            boardBg.raycastTarget = false;

            Text questionText = CreateQuestionBody(board.transform, font);
            Text phaseBanner = CreatePhaseBanner(board.transform, font);

            GameObject strip = CreateUiRect(board.transform, "AnswersStrip", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f), new Vector2(0f, 262f));
            RectTransform stripRt = strip.GetComponent<RectTransform>();
            stripRt.pivot = new Vector2(0.5f, 0f);
            Image stripBg = strip.AddComponent<Image>();
            stripBg.sprite = GetWhiteSprite();
            stripBg.color = new Color(0.03f, 0.04f, 0.06f, 0.94f);
            stripBg.raycastTarget = false;

            Text stripLabel = CreateText(strip.transform, "AnswersStripLabel", font, 19, TextAnchor.UpperCenter, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0f, -3f), new Vector2(0f, 30f));
            stripLabel.color = new Color(1f, 0.88f, 0.25f, 0.9f);
            stripLabel.fontStyle = FontStyle.Bold;
            stripLabel.text = "Réponses — touche une lettre";
            stripLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            stripLabel.verticalOverflow = VerticalWrapMode.Overflow;
            stripLabel.alignment = TextAnchor.UpperLeft;

            const float rowH = 48f;
            const float gap = 4f;
            const float baseY = 8f;
            CreateChoiceStripButton(strip.transform, font, "A", baseY + 3f * (rowH + gap), out Button btnA, out Image bgA, out Text lblA);
            CreateChoiceStripButton(strip.transform, font, "B", baseY + 2f * (rowH + gap), out Button btnB, out Image bgB, out Text lblB);
            CreateChoiceStripButton(strip.transform, font, "C", baseY + 1f * (rowH + gap), out Button btnC, out Image bgC, out Text lblC);
            CreateChoiceStripButton(strip.transform, font, "D", baseY, out Button btnD, out Image bgD, out Text lblD);

            GameObject uiHost = new GameObject("QuestionUI");
            uiHost.transform.SetParent(canvasGo.transform, false);
            QuestionUI questionUI = uiHost.AddComponent<QuestionUI>();
            questionUI.BindRuntime(questionText, phaseBanner, btnA, btnB, btnC, btnD, bgA, bgB, bgC, bgD, lblA, lblB, lblC, lblD);
            live.BindQuestionUI(questionUI);

            questionText.text = "Bienvenue sur CongoGames.";
            phaseBanner.text = "La première question arrive…";
            lblA.text = "A. —";
            lblB.text = "B. —";
            lblC.text = "C. —";
            lblD.text = "D. —";

            GameObject hudGo = new GameObject("TvGameHud");
            hudGo.transform.SetParent(canvasGo.transform, false);
            TvGameHud hud = hudGo.AddComponent<TvGameHud>();
            hud.Wire(ring, modeLabel, timerSec, brand);

            ModeSurfaceController surf = modeRoot.GetComponent<ModeSurfaceController>();
            MiniGamePanelContent panelContent = modeRoot.GetComponent<MiniGamePanelContent>();
            surf.Register("quiz", panelQuiz);
            MiniGamePanelContent.BuildSecondaryPanels(modeRoot.transform, font, surf, panelContent);
            surf.Apply("quiz");

            GameObject bottomStripGo = new GameObject("BottomThemeVideoStripRoot");
            bottomStripGo.transform.SetParent(canvasGo.transform, false);
            RectTransform bsRt = bottomStripGo.AddComponent<RectTransform>();
            bsRt.anchorMin = new Vector2(0f, 0f);
            bsRt.anchorMax = new Vector2(1f, 0f);
            bsRt.pivot = new Vector2(0.5f, 0f);
            bsRt.anchoredPosition = Vector2.zero;
            bsRt.sizeDelta = new Vector2(0f, 96f);
            RawImage bsRaw = bottomStripGo.AddComponent<RawImage>();
            bsRaw.raycastTarget = false;
            bsRaw.color = new Color(0.05f, 0.05f, 0.08f, 0.76f);
            BottomThemeVideoStrip bottomStrip = bottomStripGo.AddComponent<BottomThemeVideoStrip>();
            bottomStrip.Bind(bsRaw);
            bottomStripGo.transform.SetSiblingIndex(modeRoot.transform.GetSiblingIndex() + 1);

            GameObject vfxGo = CreateUiRect(canvasGo.transform, "FeedbackVfx", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Canvas vfxCanvas = vfxGo.AddComponent<Canvas>();
            vfxCanvas.overrideSorting = true;
            vfxCanvas.sortingOrder = 80;
            FeedbackVfxController vfx = vfxGo.AddComponent<FeedbackVfxController>();
            vfx.SetShakeTarget(modeRoot.GetComponent<RectTransform>());

            ThemeBackgroundController bootBg = themeBg.GetComponent<ThemeBackgroundController>();
            bootBg?.ApplyGameMode("quiz");
            ThemeMusicPlayer bootMu = musicHost.GetComponent<ThemeMusicPlayer>();
            bootMu?.ApplyGameMode("quiz");
            bottomStrip?.ApplyGameMode("quiz");

            BuildSplashOverlay(canvasGo.transform, font);

            GameObject roundVfx = new GameObject("RoundVictoryOverlay");
            roundVfx.transform.SetParent(canvasGo.transform, false);
            roundVfx.AddComponent<RoundVictoryOverlay>();

            if (modeRoot != null && timerBlock != null)
            {
                timerBlock.transform.SetSiblingIndex(modeRoot.transform.GetSiblingIndex() + 1);
            }

            if (roundVfx != null)
            {
                roundVfx.transform.SetAsLastSibling();
            }
        }

        /// <summary>
        /// Scène perso : un <see cref="ModeSurfaceController"/> existe déjà sans le canvas généré par ce bootstrap.
        /// On garantit <see cref="MiniGamePanelContent"/> + grilles, puis on rafraîchit le mode courant.
        /// </summary>
        private static void HarmonizeSceneModeRoot(ModeSurfaceController surf)
        {
            if (surf == null) return;
            GameObject root = surf.gameObject;
            if (root.GetComponent<MiniGamePanelContent>() == null)
            {
                root.AddComponent<MiniGamePanelContent>();
            }

            MiniGamePanelContent panel = root.GetComponent<MiniGamePanelContent>();
            panel.EnsureGridsIfMissing();
            surf.Apply(surf.CurrentModeId);

            EnsureThemeMusicForCustomScene(surf);
            PatchAllCanvasScalersForWebGl();
            Canvas modeCanvas = surf.GetComponentInParent<Canvas>();
            if (modeCanvas != null && modeCanvas.GetComponent<ThemeUrlDebugBar>() == null)
            {
                modeCanvas.gameObject.AddComponent<ThemeUrlDebugBar>();
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (Object.FindAnyObjectByType<WebGlUiScaleRuntime>() == null)
            {
                GameObject z = new GameObject("CongoWebGlUiServices");
                z.AddComponent<WebGlUiScaleRuntime>();
                Object.DontDestroyOnLoad(z);
            }
#endif
        }

        /// <summary>
        /// Scène perso avec <see cref="ModeSurfaceController"/> : le bootstrap court-circuite avant de créer ThemeMusic.
        /// </summary>
        private static void EnsureThemeMusicForCustomScene(ModeSurfaceController surf)
        {
            if (Object.FindAnyObjectByType<ThemeMusicPlayer>() != null)
            {
                return;
            }

            GameObject host = new GameObject("ThemeMusic");
            host.AddComponent<ThemeMusicPlayer>();
            if (Object.FindAnyObjectByType<AudioListener>() == null)
            {
                new GameObject("CongoGames_AudioListener").AddComponent<AudioListener>();
            }

            string id = surf != null && !string.IsNullOrEmpty(surf.CurrentModeId) ? surf.CurrentModeId : "quiz";
            ThemeMusicPlayer tmp = host.GetComponent<ThemeMusicPlayer>();
            if (tmp != null)
            {
                tmp.ApplyGameMode(id);
            }
        }

        private static void PatchAllCanvasScalersForWebGl()
        {
            CanvasScaler[] scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Exclude);
            for (int i = 0; i < scalers.Length; i++)
            {
                WebGlCanvasTuning.ApplyToScaler(scalers[i]);
            }
        }

        private static void BuildSplashOverlay(Transform canvas, Font font)
        {
            GameObject splash = CreateUiRect(canvas, "SplashLoader", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            splash.transform.SetAsLastSibling();
            Image dim = splash.AddComponent<Image>();
            dim.sprite = GetWhiteSprite();
            dim.color = new Color(0.02f, 0.04f, 0.08f, 1f);
            dim.raycastTarget = true;

            const float spMaxH = 200f;
            const float spMaxW = 720f;
            Texture2D splashLogo = Resources.Load<Texture2D>("Branding/CongoGameslogo");
            if (splashLogo != null)
            {
                float asp = (float)splashLogo.width / Mathf.Max(1, splashLogo.height);
                float sh = spMaxH;
                float sw = sh * asp;
                if (sw > spMaxW)
                {
                    sw = spMaxW;
                    sh = sw / asp;
                }

                GameObject splashTitleGo = CreateUiRect(splash.transform, "SplashTitle", new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), new Vector2(0f, 20f), new Vector2(sw, sh));
                RectTransform sTrt = splashTitleGo.GetComponent<RectTransform>();
                sTrt.pivot = new Vector2(0.5f, 0.5f);
                RawImage spRaw = splashTitleGo.AddComponent<RawImage>();
                spRaw.texture = splashLogo;
                spRaw.uvRect = new Rect(0f, 0f, 1f, 1f);
                spRaw.color = Color.white;
                spRaw.raycastTarget = false;
            }
            else
            {
                Text splashTitle = CreateText(splash.transform, "SplashTitle", font, 52, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.58f), Vector2.zero, new Vector2(900f, 120f));
                splashTitle.fontStyle = FontStyle.Bold;
                splashTitle.color = new Color(1f, 0.92f, 0.2f);
                splashTitle.text = "CONGOGAMES";
            }

            Text splashSub = CreateText(splash.transform, "SplashSub", font, 22, TextAnchor.UpperCenter, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(800f, 80f));
            splashSub.color = new Color(0.9f, 0.9f, 0.9f, 0.92f);
            splashSub.text = "Congo · culture · fierté";

            GameObject bar = CreateUiRect(splash.transform, "SplashFlagStrip", new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), Vector2.zero, new Vector2(280f, 12f));
            Image barImg = bar.AddComponent<Image>();
            barImg.sprite = GetWhiteSprite();
            barImg.color = new Color(0f, 0.55f, 0.24f, 1f);

            splash.AddComponent<IntroSplashController>();
        }

        private static Text CreateQuestionBody(Transform parent, Font font)
        {
            GameObject go = new GameObject("QuestionBody");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.04f, 0.40f);
            rt.anchorMax = new Vector2(0.96f, 0.92f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(12f, 8f);
            rt.offsetMax = new Vector2(-12f, -12f);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = 34;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 22;
            text.resizeTextMaxSize = 44;
            text.raycastTarget = false;
            Outline ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.55f);
            ol.effectDistance = new Vector2(1f, -1f);
            return text;
        }

        private static Text CreatePhaseBanner(Transform parent, Font font)
        {
            GameObject go = new GameObject("PhaseBanner");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.36f);
            rt.anchorMax = new Vector2(0.95f, 0.45f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = new Vector2(6f, 0f);
            rt.offsetMax = new Vector2(-6f, 0f);
            Text text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.85f, 0.2f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 30;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateChoiceStripButton(Transform strip, Font font, string letter, float yFromBottom, out Button button, out Image background, out Text label)
        {
            GameObject go = new GameObject("Choice" + letter);
            go.transform.SetParent(strip, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.03f, 0f);
            rt.anchorMax = new Vector2(0.97f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, yFromBottom);
            rt.sizeDelta = new Vector2(0f, 52f);

            background = go.AddComponent<Image>();
            background.sprite = GetWhiteSprite();
            background.color = new Color(0.11f, 0.13f, 0.18f, 1f);

            button = go.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(0.2f, 0.24f, 0.32f, 1f);
            colors.pressedColor = new Color(0.26f, 0.3f, 0.38f, 1f);
            colors.disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            button.colors = colors;

            GameObject textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(18f, 6f);
            trt.offsetMax = new Vector2(-18f, -6f);
            label = textGo.AddComponent<Text>();
            label.font = font;
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
        }

        private static void CreateFlagBadge(Transform topBar)
        {
            GameObject holder = CreateUiRect(topBar, "FlagBadge", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(112f, 66f));
            holder.transform.SetAsLastSibling();
            RawImage flag = holder.AddComponent<RawImage>();
            flag.texture = GetCongoFlagTexture();
            flag.color = Color.white;
            flag.raycastTarget = false;
            Outline ol = holder.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.45f);
            ol.effectDistance = new Vector2(1f, -1f);
        }

        private static Texture2D GetCongoFlagTexture()
        {
            if (congoFlagTexture != null)
            {
                return congoFlagTexture;
            }

            const int w = 256;
            const int h = 160;
            congoFlagTexture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color green = new Color(0f, 0.58f, 0.26f, 1f);
            Color yellow = new Color(0.95f, 0.84f, 0.29f, 1f);
            Color red = new Color(0.86f, 0.14f, 0.12f, 1f);
            const float band = 0.18f;

            for (int y = 0; y < h; y++)
            {
                float v = y / (h - 1f);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (w - 1f);
                    float d = v - u;
                    Color c = Mathf.Abs(d) <= band ? yellow : (d > 0f ? green : red);
                    congoFlagTexture.SetPixel(x, y, c);
                }
            }

            congoFlagTexture.Apply(false, true);
            return congoFlagTexture;
        }

        private static GameObject CreateUiRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            return go;
        }

        private static Text CreateText(Transform parent, string name, Font font, int fontSize, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            RectTransform rt = text.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.supportRichText = true;
            return text;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    tex.SetPixel(x, y, Color.white);
                }
            }

            tex.Apply(false, true);
            whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
            return whiteSprite;
        }
    }
}
