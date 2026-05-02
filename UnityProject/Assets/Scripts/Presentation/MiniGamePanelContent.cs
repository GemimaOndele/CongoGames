using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Networking;
using UnityEngine.UI;
using CongoGames.AI;
using CongoGames.Audio;
using CongoGames.Core;
using CongoGames.Network;
using CongoGames.UI;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Données de démo et références UI pour chaque type de mini-jeu.
    /// Doit vivre sur le même GameObject que <see cref="ModeSurfaceController"/> (racine ModePanelsRoot).
    /// </summary>
    [RequireComponent(typeof(ModeSurfaceController))]
    public class MiniGamePanelContent : MonoBehaviour
    {
        public static MiniGamePanelContent Instance { get; private set; }

        private const int CrosswordCols = 10;
        private const int CrosswordTotalCells = CrosswordCols * CrosswordCols;
        private const int ScrambleCols = 14;
        private const int ScrambleTotalCells = ScrambleCols * ScrambleCols;
        private const float CrosswordCellPx = 28f;
        private const float CrosswordGapPx = 10f;
        private const float ScrambleCellPx = 25f;
        private const float ScrambleGapPx = 6f;
        [Header("Mots mélangés")]
        public Text wordScrambleTitle;
        public Text wordScrambleLetters;
        public Text[] wordScrambleTiles;
        public Button[] wordScrambleButtons;
        public InputField wordAnswerInput;
        public Button wordSubmitButton;
        public Button wordClearButton;
        public Text wordHint;
        public Text wordFeedback;

        [Header("Sémantique")]
        public Text semanticTitle;
        public Text[] semanticCells = new Text[9];
        public Text semanticHint;
        public InputField semanticAnswerInput;
        public Button semanticSubmitButton;
        public Button semanticClearButton;
        public Text semanticFeedback;
        public Text[] semanticLiveUsers = new Text[9];
        public Text[] semanticLiveWords = new Text[9];
        public Text[] semanticLiveAvatarFallbacks = new Text[9];
        public RawImage[] semanticLiveAvatarPhotos = new RawImage[9];
        public Text[] semanticLivePtsBadges = new Text[9];
        public Image[] semanticLiveProgressFills = new Image[9];
        public CanvasGroup[] semanticLiveRowGroups = new CanvasGroup[9];
        public Text semanticLiveTicker;
        public Text semanticRevealWordTop;

        /// <summary>Mot attendu pour la démo « sémantique » (sans l’afficher si faux).</summary>
        public string CurrentSemanticTarget { get; private set; } = "CON";
        private int semanticRoundCursor = -1;
        private SemanticRound currentSemanticRound;
        private readonly System.Collections.Generic.List<SemanticLiveEntry> semanticLiveEntries = new System.Collections.Generic.List<SemanticLiveEntry>(8);
        private readonly System.Collections.Generic.List<SemanticLiveEntry> semanticAllRankedEntries = new System.Collections.Generic.List<SemanticLiveEntry>(96);
        private readonly System.Collections.Generic.List<string> semanticAttemptHistory = new System.Collections.Generic.List<string>(160);
        private readonly Queue<string> semanticTickerQueue = new Queue<string>(16);
        private readonly string[] semanticLiveAvatarUrls = new string[12];
        private readonly Coroutine[] semanticLiveAvatarCos = new Coroutine[12];
        private readonly Coroutine[] semanticLivePtsCos = new Coroutine[12];
        private readonly float[] semanticLiveProgressValues = new float[12];
        private LiveEventClient liveChatSource;
        private Coroutine semanticLiveRevealCo;
        private Coroutine semanticTickerCo;
        private Coroutine semanticPostWinCo;
        [SerializeField] private float semanticPostWinRecapSeconds = 6f;
        [SerializeField] private float semanticRoundDurationSeconds = 75f;
        [SerializeField] private float semanticRevealThenNextDelaySeconds = 5f;
        private float semanticBestProgressCue;
        private float semanticNextWarmCueAt;
        private float semanticRoundEndUnscaled;
        private bool semanticRoundResolved;
        private Coroutine semanticTimeoutCo;
        private readonly Dictionary<string, int> semanticAwardTierByWord = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private readonly struct SemanticLiveEntry
        {
            public readonly string User;
            public readonly string Word;
            public readonly string AvatarUrl;
            public readonly float Progress01;

            public SemanticLiveEntry(string user, string word, string avatarUrl, float progress01)
            {
                User = user ?? "";
                Word = word ?? "";
                AvatarUrl = avatarUrl ?? "";
                Progress01 = Mathf.Clamp01(progress01);
            }
        }

        private readonly struct SemanticRound
        {
            public readonly string Target;
            public readonly string Theme;
            public readonly string Prompt;
            public readonly string[] AcceptedAnswers;
            public readonly string[] Decoys;

            public SemanticRound(string target, string theme, string prompt, string[] acceptedAnswers, params string[] decoys)
            {
                Target = (target ?? "").Trim().ToUpperInvariant();
                Theme = (theme ?? "").Trim();
                Prompt = (prompt ?? "").Trim();
                AcceptedAnswers = acceptedAnswers ?? Array.Empty<string>();
                Decoys = decoys ?? Array.Empty<string>();
            }
        }

        private static readonly SemanticRound[] SemanticRounds =
        {
            new SemanticRound("MBOTE", "Sémantique", "FR: salut / bonjour -> Lingala ?",
                new[] { "MBOTE" }, "MALAMU", "BISO", "TATA", "BONJOUR"),
            new SemanticRound("MALAMU", "Sémantique", "FR: bien / ca va -> Kitouba ?",
                new[] { "MALAMU" }, "MBOTE", "MUNTU", "MOSALA", "BIEN"),
            new SemanticRound("MWANA", "Sémantique", "FR: enfant -> Lingala ?",
                new[] { "MWANA" }, "BANA", "MAMA", "TATA", "ENFANT"),
            new SemanticRound("MUNTU", "Sémantique", "FR: personne / humain -> Kitouba ?",
                new[] { "MUNTU" }, "MBOTE", "NZOTO", "BISO", "PERSONNE"),
            new SemanticRound("BISO", "Sémantique", "FR: nous -> Lingala ?",
                new[] { "BISO" }, "MOKO", "BANA", "TATA", "NOUS"),
            new SemanticRound("MAMA", "Sémantique", "Lingala / Kitouba: MAMA -> FR ?",
                new[] { "MERE", "MAMAN" }, "PERE", "TATA", "FAMILLE", "MAMAN"),
            new SemanticRound("TATA", "Sémantique", "Lingala / Kitouba: TATA -> FR ?",
                new[] { "PERE", "PAPA" }, "MAMA", "FAMILLE", "BANA", "PAPA"),
            new SemanticRound("MOSALA", "Sémantique", "Lingala: MOSALA -> FR ?",
                new[] { "TRAVAIL", "BOULOT" }, "ECOLE", "ROUTE", "MBOTE", "TRAVAIL"),
            new SemanticRound("MAIE", "Sémantique", "Kitouba: MAIE -> FR ?",
                new[] { "EAU" }, "OCEAN", "PLUIE", "SABLE", "EAU"),
            new SemanticRound("NZOTO", "Sémantique", "Lingala: NZOTO -> FR ? (definition: corps humain)",
                new[] { "CORPS" }, "MUNTU", "MOSALA", "BISO", "CORPS")
        };

        private readonly struct SemanticLexiconEntry
        {
            public readonly string Lingala;
            public readonly string Kitouba;
            public readonly string Francais;
            public readonly string DefinitionFr;

            public SemanticLexiconEntry(string lingala, string kitouba, string francais, string definitionFr)
            {
                Lingala = lingala ?? "";
                Kitouba = kitouba ?? "";
                Francais = francais ?? "";
                DefinitionFr = definitionFr ?? "";
            }
        }

        private static readonly Dictionary<string, SemanticLexiconEntry> SemanticLexicon =
            new Dictionary<string, SemanticLexiconEntry>(StringComparer.OrdinalIgnoreCase)
            {
                { "MBOTE", new SemanticLexiconEntry("mbote", "mbote", "bonjour / salut", "Formule de salutation.") },
                { "MALAMU", new SemanticLexiconEntry("malamu", "malamu", "bien / ça va", "Exprime un bon état.") },
                { "MWANA", new SemanticLexiconEntry("mwana", "mwana", "enfant", "Jeune personne, fils/fille.") },
                { "MUNTU", new SemanticLexiconEntry("muntu", "muntu", "personne / humain", "Être humain.") },
                { "BISO", new SemanticLexiconEntry("biso", "beto", "nous", "Pronom personnel de première personne pluriel.") },
                { "MAMA", new SemanticLexiconEntry("mama", "mama", "mère / maman", "Parent féminin.") },
                { "TATA", new SemanticLexiconEntry("tata", "tata", "père / papa", "Parent masculin.") },
                { "MOSALA", new SemanticLexiconEntry("mosala", "bisalu", "travail", "Activité professionnelle ou tâche.") },
                { "MAIE", new SemanticLexiconEntry("mai", "maie", "eau", "Liquide vital.") },
                { "NZOTO", new SemanticLexiconEntry("nzoto", "nitu", "corps", "Ensemble physique du corps humain.") }
            };

        private static readonly Dictionary<string, string> MysteryHints = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "CONGO", "Pays au centre du jeu et des thèmes." },
            { "RUMBA", "Style musical majeur d’Afrique centrale." },
            { "KINTELE", "Toponyme souvent cité autour de la capitale." },
            { "NGOMA", "Mot lié au rythme, au chant et à la danse." },
            { "MBOTE", "Salutation courante en lingala." },
            { "SANZA", "Mot culturel lié à la musique traditionnelle." },
            { "LIKOUALA", "Zone/département du nord du pays." },
            { "NDOMBOLO", "Danse/musique populaire contemporaine." }
        };

        [Header("Mots — historique (grilles)")]
        [Tooltip("Liste des mots déjà validés (mots mélangés).")]
        public Text gridFoundList;
        [Tooltip("Même liste pour mots croisés / grille compacte 10×10 (jeu classique : trouver les mots).")]
        public Text gridFoundListCross;
        public Image gridFoundProgressFillWord;
        public Image gridFoundProgressFillCross;

        [Header("Mots croisés")]
        public Text crosswordTitle;
        public Text[] crosswordCells = new Text[CrosswordTotalCells];
        public InputField crosswordAnswerInput;
        public Button crosswordClearButton;
        public Button crosswordSubmitButton;
        public Text crosswordFeedback;
        private Button[] crosswordButtons;
        private int crosswordSelectedCell = -1;

        [Header("Blind test")]
        public Text blindTitle;
        public Text blindPrompt;
        public Text blindSub;
        public Text blindEmoji;
        public Text[] blindChoices = new Text[4];
        private Button[] blindChoiceButtons;

        [Tooltip("Pendant l’écoute, les choix A–D sont désactivés ; après la coupure, tu réponds.")]
        [SerializeField] private float blindListenSeconds = 60f;
        [SerializeField] private bool orchestrateHostForBlindAndImage = true;
        [SerializeField] private bool aiHostNarratesAllModes = true;
        [SerializeField] private float hostSafetyWaitSeconds = 32f;
        [SerializeField] private float preMusicCountdownSeconds = 3f;
        [SerializeField] private float postHostBeforeCountdownSeconds = 2f;
        [SerializeField] private float hostPostAuthorizeDelaySeconds = 5f;
        private bool blindInQuestionPhase;
        private Coroutine blindListenCo;
        private Coroutine imageOrchestrationCo;
        private Coroutine imageMusicHintCo;
        private Coroutine blindUnlockCo;
        private string[] blindDisplayedChoices = new string[4];
        private int blindMaskedChoiceIndex = -1;
        private string blindMaskedChoiceValue = "";
        private float blindListenEndUnscaled;
        private float blindListenWindowSec;
        private Image imageGuessVeil;
        [SerializeField] private float imageGuessRevealSec = 15f;
        private const float ImageGuessMusicClueSec = 20f;
        private Coroutine imageRevealCo;
        private float imageRevealEndUnscaled;
        private float imageRevealWindowSec;
        private const float ImageGuessRevealingAlpha = 0.42f;
        private bool currentImageVisualUnusable;
        private Coroutine modeNarrationCo;
        private bool chronoInputLockedByHost;

        /// <summary>Mot cible pour la démo mots mélangés (Valider).</summary>
        public string CurrentScrambleAnswer { get; private set; } = "CONGO";

        [Header("Mot mystère")]
        public Text mysteryTitle;
        public Text mysteryMask;
        public InputField mysteryAnswerInput;
        public Button mysteryClearButton;
        public Button mysterySubmitButton;
        public Text mysteryFeedback;

        /// <summary>Mot attendu pour la démo « mot mystère » (Valider).</summary>
        public string CurrentMysteryAnswer { get; private set; } = "CONGO";

        [Header("Mémoire")]
        public Text memoryTitle;
        public Text memorySubtitle;
        public Button[] memoryCards = new Button[8];
        private readonly string[] memoryPairLetters = { "CO", "CO", "NG", "NG", "MB", "MB", "RU", "RU" };
        private readonly bool[] memoryCardMatched = new bool[8];
        private int memorySeed;
        private int memoryFirstPickIndex = -1;
        private string[] memoryDeckOrder;
        private Coroutine memoryMismatchCo;
        private int lastBlindCorrectDisplayIndex;
        private Coroutine blindEmojiPulseCo;
        private bool secondaryPanelsEnsured;
        private int suppressGridTapUntilFrame;

        private const int ChronoRoundsPerSession = 3;
        private int chronoSessionRound;
        private int chronoSessionScore;
        private int chronoTargetSlot;
        private float chronoStateUntil;
        private int chronoPhase; // 0=countdown, 1=play, 2=round flash, 3=session end
        private bool chronoModeActive;
        private int chronoStreak;
        private string chronoResultFlash;
        private float chronoPlayWindowSec;
        private int chronoRoundInSession; // 1..ChronoRoundsPerSession
        private int chronoLastRoundPoints;
        private int chronoCountdownIndex;
        private int chronoTimerAudioLastCeilSec = -1;
        private bool chronoTimerUrgentPlayed;

        [Header("Grilles thématiques (mots mélangés / mots croisés)")]
        [Tooltip("Démo : 2 manches = 2 grilles thématiques (5–12 mots) avant le prochain mode ; en live, le chrono de la manche continue.")]
        [SerializeField] private int gridSessionsPerThematicBlock = 2;
        [SerializeField] private int gridThematicWordCount = 8;
        private int gridMotsJeuSessionIndex; // 1 = premier mot, …
        private int gridThematicBlockRound; // 0 = non initialisé, 1 = 1re grille, 2 = 2e…
        private string currentGridThemeLabel = "";
        private System.Collections.Generic.List<string> currentGridAllWords;
        private System.Collections.Generic.HashSet<string> currentGridSolved;
        private readonly System.Collections.Generic.List<string> gridFoundHistory = new System.Collections.Generic.List<string>();
        private RectTransform gridFoundToastRoot;
        private Text gridFoundToastUser;
        private Text gridFoundToastWord;
        private Text gridFoundToastPoints;
        private Text gridFoundToastAvatar;
        private RawImage gridFoundToastAvatarPhoto;
        private Image gridFoundToastBackground;
        private Outline gridFoundToastOutline;
        private Coroutine gridFoundToastCo;
        private Coroutine gridFoundAvatarCo;
        private readonly Dictionary<string, Texture2D> gridAvatarCache = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        [Tooltip("Cible active (mots mélangés + mots croisés) — rétro compatible avec CurrentScrambleAnswer.")]
        public string CurrentScrambleTargetWord { get; private set; } = "CONGO";
        [Tooltip("Dernier(s) mots reconnus en mots croisés (séparateur « · »)")]
        public string LastCrosswordGuessed { get; private set; } = "";
        private char[,] lastWordSearchGrid;
        private char[,] lastScrambleGrid;
        private string lastPopulatedModeId = "";
        private System.Collections.Generic.List<WordLinePlacement> currentWordSearchPlacements;
        private System.Collections.Generic.List<WordLinePlacement> currentScramblePlacements;
        private System.Collections.Generic.List<CrosswordClueEntry> currentCrosswordClues;
        private readonly System.Collections.Generic.HashSet<int> crosswordFoundCellIndices = new System.Collections.Generic.HashSet<int>();
        private readonly System.Collections.Generic.HashSet<int> crosswordHintCellIndices = new System.Collections.Generic.HashSet<int>();
        private readonly System.Collections.Generic.HashSet<int> wordScrambleFoundCellIndices = new System.Collections.Generic.HashSet<int>();
        private readonly System.Collections.Generic.HashSet<int> wordScrambleHintCellIndices = new System.Collections.Generic.HashSet<int>();
        private bool[] wordScramblePlayableMask;
        [Header("Live likes → indice lettre")]
        [SerializeField] private int likesPerHintLetter = 100;
        private int likesProgressToHint;
        private int hintLettersRevealedInRound;
        private LiveEventClient liveLikeSource;
        [Header("Live clavier (sans champ visible)")]
        [SerializeField] private int liveTypedMaxChars = 28;
        private string liveTypedDraft = "";
        private string liveTypedModeId = "";

        private static readonly Color CrosswordFoundCellBg = new Color(0.24f, 0.72f, 0.4f, 1f);
        private static readonly Color CrosswordFoundLetter = new Color(0.95f, 0.98f, 0.94f, 1f);
        private static readonly Color CrosswordDefaultLetter = new Color(0.77f, 0.84f, 0.9f, 1f);

        private static string BuildWordBankProgressBar(int found, int total, int width = 14)
        {
            if (total <= 0) return "";
            int filled = Mathf.RoundToInt((float)found / total * width);
            filled = Mathf.Clamp(filled, 0, width);
            return "<color=#39FF6E>" + new string('█', filled) + "</color><color=#4A4D55>" + new string('░', width - filled) + "</color>";
        }

        private void RefreshGridFoundPanel()
        {
            int found = currentGridSolved != null ? currentGridSolved.Count : 0;
            int total = currentGridAllWords != null ? currentGridAllWords.Count : 0;
            float progress = total > 0 ? Mathf.Clamp01((float)found / total) : 0f;
            if (gridFoundProgressFillWord != null) gridFoundProgressFillWord.fillAmount = progress;
            if (gridFoundProgressFillCross != null) gridFoundProgressFillCross.fillAmount = progress;
            var sb = new System.Text.StringBuilder();
            sb.Append(BuildWordBankProgressBar(found, total)).Append("\n");
            sb.Append("<color=#39FF6E><size=18><b>").Append(found).Append(" / ").Append(total).Append("</b></size></color>  <color=#B8C4D6>mots trouvés</color>\n");

            if (currentGridAllWords != null && currentGridAllWords.Count > 0)
            {
                for (int i = 0; i < currentGridAllWords.Count; i++)
                {
                    if (i > 0) sb.Append("  ");
                    string key = GridThemeBank.SanitizeForGrid(currentGridAllWords[i] ?? "");
                    if (string.IsNullOrEmpty(key)) continue;
                    bool solved = currentGridSolved != null && currentGridSolved.Contains(key);
                    if (solved)
                    {
                        sb.Append("<color=#39FF6E><b>").Append(key).Append("</b></color>");
                    }
                    else
                    {
                        int n = Mathf.Clamp(key.Length, 3, 14);
                        sb.Append("<color=#5C616B>").Append(new string('●', n)).Append("</color>");
                    }
                }
            }
            else if (gridFoundHistory != null && gridFoundHistory.Count > 0)
            {
                sb.Append("\n");
                for (int i = 0; i < gridFoundHistory.Count; i++)
                {
                    if (i > 0) sb.Append("\n");
                    sb.Append("<color=#76FF8B>• ").Append(gridFoundHistory[i]).Append("</color>");
                }
            }
            else
            {
                sb.Append("\n<color=#8A93A0>— Aucun mot validé encore —</color>");
            }

            string block = sb.ToString();
            if (gridFoundList != null) gridFoundList.text = block;
            if (gridFoundListCross != null) gridFoundListCross.text = block;
        }

        private void RecordGridWordFound(string wordUpper)
        {
            if (string.IsNullOrEmpty(wordUpper)) return;
            string gloss = GridThemeBank.TryGlossFr(wordUpper);
            string line = string.IsNullOrEmpty(gloss)
                ? wordUpper
                : wordUpper + " — " + gloss + " (FR)";
            gridFoundHistory.Add(line);
            RefreshGridFoundPanel();
        }

        private void ShowGridFoundToast(string foundWord, int pointsDelta)
        {
            if (gridFoundToastRoot == null || string.IsNullOrWhiteSpace(foundWord))
            {
                return;
            }

            GameSfxHub.Instance?.PlayUiPop(0.2f);

            string user = ResolveCurrentGridPlayerName();
            if (gridFoundToastUser != null)
            {
                gridFoundToastUser.text = user;
            }

            if (gridFoundToastWord != null)
            {
                gridFoundToastWord.text = foundWord;
            }

            if (gridFoundToastPoints != null)
            {
                gridFoundToastPoints.text = "+" + Mathf.Max(1, pointsDelta);
            }

            if (gridFoundToastAvatar != null)
            {
                char c = string.IsNullOrWhiteSpace(user) ? '?' : char.ToUpperInvariant(user.Trim()[0]);
                gridFoundToastAvatar.text = c.ToString();
            }

            if (gridFoundAvatarCo != null)
            {
                StopCoroutine(gridFoundAvatarCo);
                gridFoundAvatarCo = null;
            }
            for (int i = 0; i < semanticLiveAvatarCos.Length; i++)
            {
                if (semanticLiveAvatarCos[i] != null)
                {
                    StopCoroutine(semanticLiveAvatarCos[i]);
                    semanticLiveAvatarCos[i] = null;
                }
                if (semanticLivePtsCos[i] != null)
                {
                    StopCoroutine(semanticLivePtsCos[i]);
                    semanticLivePtsCos[i] = null;
                }
            }

            string avatarUrl = ResolveCurrentGridAvatarUrl();
            if (gridFoundToastAvatarPhoto != null)
            {
                gridFoundToastAvatarPhoto.texture = null;
                gridFoundToastAvatarPhoto.color = new Color(1f, 1f, 1f, 0f);
            }

            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                SetGridAvatarFallbackVisible(true);
            }
            else
            {
                SetGridAvatarFallbackVisible(false);
                gridFoundAvatarCo = StartCoroutine(CoLoadGridAvatar(avatarUrl.Trim()));
            }

            if (gridFoundToastCo != null)
            {
                StopCoroutine(gridFoundToastCo);
            }

            gridFoundToastCo = StartCoroutine(CoShowGridFoundToast());
        }

        private int GrantGridWordPoints()
        {
            const int points = 10;
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddPoints(ResolveCurrentGridPlayerName(), points);
            }

            return points;
        }

        private string ResolveCurrentGridPlayerName()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            if (live != null && live.IsConnected && !string.IsNullOrWhiteSpace(live.LastEventUser))
            {
                return live.LastEventUser.Trim();
            }

            return PlayerProfileStore.ScoreUsernameForLocalPlay() ?? "Joueur";
        }

        private string ResolveCurrentGridAvatarUrl()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            if (live != null && live.IsConnected && !string.IsNullOrWhiteSpace(live.LastEventAvatarUrl))
            {
                return live.LastEventAvatarUrl;
            }

            return "";
        }

        private void SetGridAvatarFallbackVisible(bool visible)
        {
            if (gridFoundToastAvatar != null)
            {
                gridFoundToastAvatar.gameObject.SetActive(visible);
            }

            if (gridFoundToastAvatarPhoto != null)
            {
                gridFoundToastAvatarPhoto.gameObject.SetActive(!visible);
            }
        }

        private IEnumerator CoLoadGridAvatar(string url)
        {
            if (gridFoundToastAvatarPhoto == null)
            {
                yield break;
            }

            if (gridAvatarCache.TryGetValue(url, out Texture2D cached) && cached != null)
            {
                gridFoundToastAvatarPhoto.texture = cached;
                gridFoundToastAvatarPhoto.color = Color.white;
                SetGridAvatarFallbackVisible(false);
                gridFoundAvatarCo = null;
                yield break;
            }

            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url, true))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    SetGridAvatarFallbackVisible(true);
                    gridFoundAvatarCo = null;
                    yield break;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                if (tex == null)
                {
                    SetGridAvatarFallbackVisible(true);
                    gridFoundAvatarCo = null;
                    yield break;
                }

                gridAvatarCache[url] = tex;
                gridFoundToastAvatarPhoto.texture = tex;
                gridFoundToastAvatarPhoto.color = Color.white;
                SetGridAvatarFallbackVisible(false);
            }

            gridFoundAvatarCo = null;
        }

        private IEnumerator CoShowGridFoundToast()
        {
            if (gridFoundToastRoot == null) yield break;
            gridFoundToastRoot.gameObject.SetActive(true);
            CanvasGroup cg = gridFoundToastRoot.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;

            Vector2 basePos = new Vector2(0f, -24f);
            Vector2 startPos = new Vector2(220f, -24f);
            gridFoundToastRoot.anchoredPosition = startPos;
            gridFoundToastRoot.localScale = new Vector3(0.95f, 0.95f, 1f);

            float inDur = 0.26f;
            float hold = 2.6f;
            float outDur = 0.28f;
            float t = 0f;
            while (t < inDur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / inDur);
                float e = 1f - Mathf.Pow(1f - p, 3f);
                if (cg != null) cg.alpha = p;
                gridFoundToastRoot.anchoredPosition = Vector2.LerpUnclamped(startPos, basePos, e);
                gridFoundToastRoot.localScale = Vector3.LerpUnclamped(new Vector3(0.95f, 0.95f, 1f), new Vector3(1.02f, 1.02f, 1f), e);
                yield return null;
            }

            float hw = 0f;
            while (hw < hold)
            {
                hw += Time.unscaledDeltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(hw * 8.4f);
                if (gridFoundToastOutline != null)
                {
                    Color c = Color.Lerp(new Color(0.12f, 0.8f, 0.76f, 0.9f), new Color(0.22f, 1f, 0.92f, 1f), pulse);
                    gridFoundToastOutline.effectColor = c;
                    gridFoundToastOutline.effectDistance = Vector2.Lerp(new Vector2(2f, -2f), new Vector2(4f, -4f), pulse);
                }

                if (gridFoundToastBackground != null)
                {
                    gridFoundToastBackground.color = Color.Lerp(new Color(0.17f, 0.09f, 0.28f, 0.94f), new Color(0.23f, 0.11f, 0.35f, 0.98f), pulse);
                }

                Transform avatarTf = null;
                if (gridFoundToastAvatarPhoto != null && gridFoundToastAvatarPhoto.gameObject.activeSelf)
                {
                    avatarTf = gridFoundToastAvatarPhoto.transform;
                }
                else if (gridFoundToastAvatar != null && gridFoundToastAvatar.gameObject.activeSelf)
                {
                    avatarTf = gridFoundToastAvatar.transform;
                }

                if (avatarTf != null)
                {
                    float s = Mathf.Lerp(0.97f, 1.06f, pulse);
                    avatarTf.localScale = new Vector3(s, s, 1f);
                }

                gridFoundToastRoot.localScale = Vector3.Lerp(gridFoundToastRoot.localScale, Vector3.one, Time.unscaledDeltaTime * 12f);
                yield return null;
            }

            t = 0f;
            while (t < outDur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / outDur);
                if (cg != null) cg.alpha = 1f - p;
                gridFoundToastRoot.anchoredPosition = Vector2.LerpUnclamped(basePos, new Vector2(-180f, -24f), p);
                gridFoundToastRoot.localScale = Vector3.LerpUnclamped(Vector3.one, new Vector3(0.97f, 0.97f, 1f), p);
                yield return null;
            }

            if (gridFoundToastAvatarPhoto != null) gridFoundToastAvatarPhoto.transform.localScale = Vector3.one;
            if (gridFoundToastAvatar != null) gridFoundToastAvatar.transform.localScale = Vector3.one;
            if (gridFoundToastRoot != null) gridFoundToastRoot.gameObject.SetActive(false);
            gridFoundToastCo = null;
        }

        private void AdvanceGridMotsJeu()
        {
            if (currentGridAllWords == null || currentGridSolved == null) return;
            var pick = new System.Collections.Generic.List<string>();
            for (int i = 0; i < currentGridAllWords.Count; i++)
            {
                string w = currentGridAllWords[i];
                if (string.IsNullOrEmpty(w)) continue;
                if (currentGridSolved.Contains(w)) continue;
                pick.Add(w);
            }

            if (pick.Count == 0) return;
            int j = UnityEngine.Random.Range(0, pick.Count);
            string chosen = pick[j];
            CurrentScrambleTargetWord = chosen;
            CurrentScrambleAnswer = chosen;
            gridMotsJeuSessionIndex = Mathf.Max(1, currentGridSolved.Count + 1);
            if (wordHint != null)
            {
                string hint = GridThemeBank.InGameThemeHintFr(currentGridThemeLabel);
                wordHint.text = (currentGridThemeLabel != null ? "Thème : " + currentGridThemeLabel + " — " : "")
                    + hint + " — Mot courant : " + chosen.Length + " lettres.";
            }

            if (wordAnswerInput != null) wordAnswerInput.text = "";
            if (crosswordAnswerInput != null) crosswordAnswerInput.text = "";
        }

        private static void MaybeAdvanceMiniGameAfterResponse()
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null)
            {
                return;
            }

            if (gmm != null
                && GameModeManager.IsGridThematicModeId(gmm.ActiveModeId)
                && Instance != null
                && !Instance.FinishThematicGridIfCompleted())
            {
                return;
            }

            if (Instance != null)
            {
                Instance.QueueAdvanceAfterAnswer();
            }
            else
            {
                gmm.AdvanceRoundOrNextMode();
            }
        }

        [SerializeField] private float postAnswerHoldSeconds = 5f;
        private Coroutine postAnswerAdvanceCo;

        private void QueueAdvanceAfterAnswer()
        {
            if (postAnswerAdvanceCo != null)
            {
                StopCoroutine(postAnswerAdvanceCo);
            }

            postAnswerAdvanceCo = StartCoroutine(CoAdvanceAfterAnswerHold());
        }

        private IEnumerator CoAdvanceAfterAnswerHold()
        {
            float hold = Mathf.Clamp(postAnswerHoldSeconds, 1f, 30f);
            yield return new WaitForSecondsRealtime(hold);
            postAnswerAdvanceCo = null;
            GameModeManager.Instance?.AdvanceRoundOrNextMode();
        }

        private void ResetThematicGridState()
        {
            gridThematicBlockRound = 0;
            gridMotsJeuSessionIndex = 0;
            currentGridThemeLabel = "";
            currentGridAllWords = null;
            currentGridSolved = null;
            LastCrosswordGuessed = "";
            lastWordSearchGrid = null;
            lastScrambleGrid = null;
            currentScramblePlacements = null;
            crosswordHintCellIndices.Clear();
            wordScrambleHintCellIndices.Clear();
            wordScrambleFoundCellIndices.Clear();
            likesProgressToHint = 0;
            hintLettersRevealedInRound = 0;
            gridFoundHistory.Clear();
            RefreshGridFoundPanel();
        }

        private void NewThematicSession()
        {
            gridThematicBlockRound++;
            int n = Mathf.Clamp(gridThematicWordCount, GridThemeBank.MinWords, GridThemeBank.MaxWords);
            GridThemeBank.DrawSessionWords(out currentGridThemeLabel, out currentGridAllWords, n);
            currentGridSolved = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            gridMotsJeuSessionIndex = 0;
            LastCrosswordGuessed = "";
            crosswordHintCellIndices.Clear();
            wordScrambleHintCellIndices.Clear();
            wordScrambleFoundCellIndices.Clear();
            likesProgressToHint = 0;
            hintLettersRevealedInRound = 0;
            gridFoundHistory.Clear();
            RefreshGridFoundPanel();
            AdvanceGridMotsJeu();
        }

        /// <returns>True si le mode peut enchaîner (bloc 2/2 fini, ou session grille mots croisés complète).</returns>
        private bool FinishThematicGridIfCompleted()
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null) return true;
            string id = gmm.ActiveModeId;
            if (string.Equals(id, "word-scramble", StringComparison.Ordinal))
            {
                if (currentGridAllWords == null || currentGridSolved == null) return true;
                if (currentGridSolved.Count < currentGridAllWords.Count)
                {
                    AdvanceGridMotsJeu();
                    BuildWordScrambleForTarget();
                    gmm.ExtendModeTime(45f);
                    return false;
                }

                if (gridThematicBlockRound < gridSessionsPerThematicBlock)
                {
                    NewThematicSession();
                    BuildWordScrambleForTarget();
                    gmm.ExtendModeTime(60f);
                    return false;
                }

                gmm.SetGridThematicBlockComplete();
                return true;
            }

            if (string.Equals(id, "crossword-lite", StringComparison.Ordinal))
            {
                if (currentGridAllWords == null || currentGridSolved == null) return true;
                if (currentGridSolved.Count < currentGridAllWords.Count)
                {
                    AdvanceGridMotsJeu();
                    gmm.ExtendModeTime(25f);
                    return false;
                }

                if (gridThematicBlockRound < gridSessionsPerThematicBlock)
                {
                    NewThematicSession();
                    BuildWordSearchAndFillGrid();
                    gmm.ExtendModeTime(60f);
                    return false;
                }

                gmm.SetGridThematicBlockComplete();
                return true;
            }

            return true;
        }

        private static Color CrosswordDecoBg(int row, int col)
        {
            Color a = new Color(0.16f, 0.17f, 0.2f, 1f);
            Color b = new Color(0.2f, 0.21f, 0.24f, 1f);
            return (row + col) % 2 == 0 ? a : b;
        }

        private void RefreshCrosswordCellDecor(int cellIndex)
        {
            if (crosswordCells == null || cellIndex < 0 || cellIndex >= crosswordCells.Length) return;
            Text tx = crosswordCells[cellIndex];
            if (tx == null) return;
            int r = cellIndex / CrosswordCols;
            int c = cellIndex % CrosswordCols;
            bool openCell = lastWordSearchGrid != null
                && r >= 0 && c >= 0
                && r < lastWordSearchGrid.GetLength(0)
                && c < lastWordSearchGrid.GetLength(1)
                && lastWordSearchGrid[r, c] != '\0';
            Image bg = tx.transform.parent != null ? tx.transform.parent.GetComponent<Image>() : null;
            if (bg != null)
            {
                Color baseColor;
                if (!openCell)
                {
                    baseColor = new Color(0.03f, 0.04f, 0.06f, 0.94f);
                }
                else if (crosswordFoundCellIndices.Contains(cellIndex))
                {
                    baseColor = CrosswordFoundCellBg;
                }
                else if (crosswordHintCellIndices.Contains(cellIndex))
                {
                    baseColor = new Color(0.21f, 0.25f, 0.3f, 1f);
                }
                else if (cellIndex == crosswordSelectedCell)
                {
                    baseColor = new Color(0.95f, 0.45f, 0.12f, 1f);
                }
                else
                {
                    baseColor = CrosswordDecoBg(r, c);
                }

                bg.color = baseColor;
                GridCellHoverFeedback hover = bg.GetComponent<GridCellHoverFeedback>();
                if (hover != null) hover.SetBaseColor(baseColor);
            }

            if (!openCell)
            {
                tx.text = "";
                tx.color = new Color(0f, 0f, 0f, 0f);
            }
            else if (crosswordFoundCellIndices.Contains(cellIndex))
            {
                tx.text = char.ToUpperInvariant(lastWordSearchGrid[r, c]).ToString();
                tx.color = CrosswordFoundLetter;
            }
            else if (crosswordHintCellIndices.Contains(cellIndex))
            {
                tx.text = char.ToUpperInvariant(lastWordSearchGrid[r, c]).ToString();
                tx.color = new Color(0.92f, 0.96f, 0.84f, 1f);
            }
            else
            {
                tx.text = "";
                tx.color = CrosswordDefaultLetter;
            }
        }

        private void HighlightCrosswordSelection(int cellIndex)
        {
            int prev = crosswordSelectedCell;
            crosswordSelectedCell = cellIndex;
            if (prev >= 0) RefreshCrosswordCellDecor(prev);
            if (cellIndex >= 0) RefreshCrosswordCellDecor(cellIndex);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            BindLiveLikeHints();
            EnsureGridsIfMissing();
            ModeSurfaceController surf = GetComponentInParent<ModeSurfaceController>();
            if (surf != null && !string.IsNullOrEmpty(surf.CurrentModeId))
            {
                Populate(surf.CurrentModeId);
            }
        }

        private void OnDestroy()
        {
            UnbindLiveLikeHints();
            if (blindListenCo != null)
            {
                StopCoroutine(blindListenCo);
                blindListenCo = null;
            }

            if (imageRevealCo != null)
            {
                StopCoroutine(imageRevealCo);
                imageRevealCo = null;
            }
            if (imageMusicHintCo != null)
            {
                StopCoroutine(imageMusicHintCo);
                imageMusicHintCo = null;
            }
            if (imageOrchestrationCo != null)
            {
                StopCoroutine(imageOrchestrationCo);
                imageOrchestrationCo = null;
            }

            if (blindEmojiPulseCo != null)
            {
                StopCoroutine(blindEmojiPulseCo);
                blindEmojiPulseCo = null;
            }

            if (gridFoundAvatarCo != null)
            {
                StopCoroutine(gridFoundAvatarCo);
                gridFoundAvatarCo = null;
            }

            foreach (var kv in gridAvatarCache)
            {
                if (kv.Value != null)
                {
                    Destroy(kv.Value);
                }
            }
            gridAvatarCache.Clear();

            GameSfxHub.Instance?.StopBlindDemoMusic();
            if (Instance == this) Instance = null;
        }

        private void BindLiveLikeHints()
        {
            UnbindLiveLikeHints();
            liveLikeSource = FindAnyObjectByType<LiveEventClient>();
            if (liveLikeSource != null)
            {
                liveLikeSource.OnLikePulse += HandleLiveLikePulse;
                liveLikeSource.OnChatPulse += HandleLiveChatPulse;
                liveChatSource = liveLikeSource;
            }
        }

        private void UnbindLiveLikeHints()
        {
            if (liveLikeSource != null)
            {
                liveLikeSource.OnLikePulse -= HandleLiveLikePulse;
                liveLikeSource.OnChatPulse -= HandleLiveChatPulse;
                liveLikeSource = null;
            }
            liveChatSource = null;
        }

        private void HandleLiveChatPulse(string user, string message, string avatarUrl)
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null || !string.Equals(gmm.ActiveModeId, "semantic", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            if (semanticRoundResolved) return;

            string w = GridThemeBank.SanitizeForGrid(message ?? "");
            if (string.IsNullOrEmpty(w) || w.Length < 2) return;
            if (w.Length > 20) w = w.Substring(0, 20);
            string u = string.IsNullOrWhiteSpace(user) ? "joueur" : user.Trim();
            float progress = EstimateSemanticLiveProgress(w);
            UpsertSemanticLiveEntry(u, w, avatarUrl ?? "", progress);
            PushSemanticTicker(u, w);
            TryAwardSemanticProximityPoints(u, avatarUrl ?? "", w, progress);
            RenderSemanticLiveFeed();
            if (semanticLiveRevealCo != null)
            {
                StopCoroutine(semanticLiveRevealCo);
                semanticLiveRevealCo = null;
            }
            semanticLiveRevealCo = StartCoroutine(CoRevealSemanticRows());
        }

        private void UpsertSemanticLiveEntry(string user, string word, string avatarUrl, float progress01)
        {
            string key = GridThemeBank.SanitizeForGrid(word ?? "");
            if (string.IsNullOrEmpty(key)) return;

            int existing = -1;
            for (int i = 0; i < semanticAllRankedEntries.Count; i++)
            {
                if (string.Equals(semanticAllRankedEntries[i].Word, key, StringComparison.OrdinalIgnoreCase))
                {
                    existing = i;
                    break;
                }
            }

            if (existing >= 0)
            {
                SemanticLiveEntry old = semanticAllRankedEntries[existing];
                float best = Mathf.Max(old.Progress01, progress01);
                string finalUser = string.IsNullOrWhiteSpace(user) ? old.User : user.Trim();
                string finalAvatar = string.IsNullOrWhiteSpace(avatarUrl) ? old.AvatarUrl : avatarUrl.Trim();
                semanticAllRankedEntries[existing] = new SemanticLiveEntry(finalUser, key, finalAvatar, best);
            }
            else
            {
                semanticAllRankedEntries.Add(new SemanticLiveEntry(user, key, avatarUrl ?? "", progress01));
            }

            semanticAllRankedEntries.Sort((a, b) => b.Progress01.CompareTo(a.Progress01));
            SyncSemanticDisplayFromAllEntries();
        }

        private void SyncSemanticDisplayFromAllEntries()
        {
            int cap = semanticLiveUsers != null && semanticLiveUsers.Length > 0 ? semanticLiveUsers.Length : 9;
            semanticLiveEntries.Clear();
            int take = Mathf.Min(cap, semanticAllRankedEntries.Count);
            for (int i = 0; i < take; i++)
            {
                semanticLiveEntries.Add(semanticAllRankedEntries[i]);
            }
        }

        private void PushSemanticTicker(string user, string word)
        {
            string u = string.IsNullOrWhiteSpace(user) ? "joueur" : user.Trim();
            string w = GridThemeBank.SanitizeForGrid(word ?? "");
            if (string.IsNullOrEmpty(w)) return;
            semanticAttemptHistory.Add("@" + u + " : " + w);
            if (semanticAttemptHistory.Count > 300)
            {
                semanticAttemptHistory.RemoveAt(0);
            }
            semanticTickerQueue.Enqueue("@" + u + " : " + w);
            while (semanticTickerQueue.Count > 24)
            {
                semanticTickerQueue.Dequeue();
            }

            if (semanticTickerCo == null)
            {
                semanticTickerCo = StartCoroutine(CoSemanticTicker());
            }
        }

        private IEnumerator CoSemanticTicker()
        {
            while (semanticTickerQueue.Count > 0)
            {
                if (semanticPostWinCo != null)
                {
                    yield return null;
                    continue;
                }
                string item = semanticTickerQueue.Dequeue();
                if (semanticLiveTicker != null)
                {
                    semanticLiveTicker.text = "<color=#A8D7FF>Flux :</color> " + item;
                }

                yield return new WaitForSecondsRealtime(6f);
            }

            if (semanticLiveTicker != null)
            {
                semanticLiveTicker.text = "";
            }

            semanticTickerCo = null;
        }

        private void StartSemanticPostWinRecapAndAdvance()
        {
            if (semanticPostWinCo != null)
            {
                StopCoroutine(semanticPostWinCo);
            }

            semanticPostWinCo = StartCoroutine(CoSemanticPostWinRecapThenAdvance());
        }

        private IEnumerator CoSemanticPostWinRecapThenAdvance()
        {
            if (postAnswerAdvanceCo != null)
            {
                StopCoroutine(postAnswerAdvanceCo);
                postAnswerAdvanceCo = null;
            }

            float total = Mathf.Clamp(semanticPostWinRecapSeconds, 5f, 10f);
            float start = Time.unscaledTime;
            int idx = 0;
            float nextSwap = 0f;
            while (Time.unscaledTime - start < total)
            {
                if (semanticLiveTicker != null && semanticAttemptHistory.Count > 0 && Time.unscaledTime >= nextSwap)
                {
                    string item = semanticAttemptHistory[idx % semanticAttemptHistory.Count];
                    semanticLiveTicker.text = "<color=#FFD66A>Trouvé !</color>  <color=#A8D7FF>Récap :</color> " + item;
                    idx++;
                    nextSwap = Time.unscaledTime + 0.6f;
                }

                yield return null;
            }

            if (semanticLiveTicker != null)
            {
                semanticLiveTicker.text = "";
            }

            semanticPostWinCo = null;
            GameModeManager.Instance?.AdvanceRoundOrNextMode();
        }

        private void SemanticTick()
        {
            if (semanticRoundResolved) return;
            if (semanticRoundEndUnscaled <= 0f) return;
            if (Time.unscaledTime < semanticRoundEndUnscaled) return;
            if (semanticTimeoutCo != null) return;
            semanticTimeoutCo = StartCoroutine(CoSemanticTimeoutRevealThenAdvance());
        }

        private IEnumerator CoSemanticTimeoutRevealThenAdvance()
        {
            semanticRoundResolved = true;
            string target = (CurrentSemanticTarget ?? "").Trim().ToUpperInvariant();
            if (semanticRevealWordTop != null)
            {
                semanticRevealWordTop.text = "Mot à trouver : <b><color=#FFD66A><u>" + target + "</u></color></b>";
            }

            if (semanticFeedback != null)
            {
                string cross = BuildSemanticCrossLanguageInfo(currentSemanticRound);
                semanticFeedback.text = "<color=#FFB9A8>Temps écoulé.</color> Le mot était <b>" + target + "</b>."
                    + (string.IsNullOrEmpty(cross) ? "" : "\n<color=#CDEBFF>" + cross + "</color>");
            }

            if (semanticLiveTicker != null)
            {
                semanticLiveTicker.text = "<color=#FFD66A>Question suivante dans 5 secondes…</color>";
            }

            yield return new WaitForSecondsRealtime(Mathf.Clamp(semanticRevealThenNextDelaySeconds, 3f, 8f));
            semanticTimeoutCo = null;
            GameModeManager.Instance?.AdvanceRoundOrNextMode();
        }

        private void TryAwardSemanticProximityPoints(string user, string avatarUrl, string word, float proximity01)
        {
            string w = GridThemeBank.SanitizeForGrid(word ?? "");
            if (string.IsNullOrEmpty(w)) return;

            int tier = proximity01 >= 0.8f ? 3 : (proximity01 >= 0.65f ? 2 : (proximity01 >= 0.5f ? 1 : 0));
            if (tier <= 0) return;
            int prevTier = semanticAwardTierByWord.TryGetValue(w, out int old) ? old : 0;
            if (tier <= prevTier) return;
            semanticAwardTierByWord[w] = tier;

            int delta = tier - prevTier;
            string username = string.IsNullOrWhiteSpace(user) || string.Equals(user, "toi", StringComparison.OrdinalIgnoreCase)
                ? (PlayerProfileStore.ScoreUsernameForLocalPlay() ?? "Joueur")
                : user.Trim();
            ScoreManager.Instance?.AddPoints(username, delta);
            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                ScoreManager.Instance?.UpdatePlayerAvatar(username, avatarUrl);
            }

            ShowGridFoundToast(w, delta);
            ShowSemanticLivePointsBadge(w, delta);
        }

        private void TryPlaySemanticWarmCue(float proximity01)
        {
            float p = Mathf.Clamp01(proximity01);
            float now = Time.unscaledTime;
            if (now < semanticNextWarmCueAt) return;
            if (p < 0.72f) return;
            if (p <= semanticBestProgressCue + 0.02f) return;

            semanticBestProgressCue = p;
            semanticNextWarmCueAt = now + 0.45f;
            GameSfxHub.Instance?.PlayUiPop(0.12f);
        }

        private void RenderSemanticLiveFeed()
        {
            if (semanticLiveUsers == null || semanticLiveUsers.Length == 0) return;
            int rows = semanticLiveUsers.Length;
            for (int i = 0; i < rows; i++)
            {
                bool has = i < semanticLiveEntries.Count;
                if (semanticLiveRowGroups != null && i < semanticLiveRowGroups.Length && semanticLiveRowGroups[i] != null)
                {
                    semanticLiveRowGroups[i].alpha = has ? 1f : 0f;
                    Image rowBg = semanticLiveRowGroups[i].GetComponent<Image>();
                    if (rowBg != null)
                    {
                        float p = has ? semanticLiveProgressValues[i] : 0f;
                        rowBg.color = Color.Lerp(
                            new Color(0.1f, 0.12f, 0.2f, 0.72f),
                            new Color(0.22f, 0.18f, 0.11f, 0.9f),
                            Mathf.Clamp01(p));
                    }
                }
                semanticLiveProgressValues[i] = has ? semanticLiveEntries[i].Progress01 : 0f;
                if (semanticLiveUsers[i] != null) semanticLiveUsers[i].text = has ? ("@" + semanticLiveEntries[i].User) : "";
                if (semanticLiveWords[i] != null)
                {
                    semanticLiveWords[i].text = has ? semanticLiveEntries[i].Word : "";
                    float p = semanticLiveProgressValues[i];
                    semanticLiveWords[i].color = has
                        ? Color.Lerp(new Color(0.94f, 0.95f, 0.98f, 1f), new Color(0.88f, 1f, 0.74f, 1f), Mathf.Clamp01((p - 0.35f) / 0.65f))
                        : new Color(0.98f, 0.98f, 0.98f, 0f);
                }
                if (semanticLiveAvatarFallbacks[i] != null)
                {
                    char c = has && !string.IsNullOrWhiteSpace(semanticLiveEntries[i].User)
                        ? char.ToUpperInvariant(semanticLiveEntries[i].User[0])
                        : '•';
                    semanticLiveAvatarFallbacks[i].text = c.ToString();
                }
                if (semanticLivePtsBadges != null && i < semanticLivePtsBadges.Length && semanticLivePtsBadges[i] != null && semanticLivePtsCos[i] == null)
                {
                    semanticLivePtsBadges[i].text = has ? EmojiForSemanticProgress(semanticLiveProgressValues[i]) : "";
                    float p = semanticLiveProgressValues[i];
                    Color emojiCol = p >= 0.9f
                        ? new Color(1f, 0.58f, 0.22f, 1f)
                        : (p >= 0.75f ? new Color(0.98f, 0.92f, 0.22f, 1f) : new Color(0.86f, 0.9f, 1f, 1f));
                    emojiCol.a = has ? 1f : 0f;
                    semanticLivePtsBadges[i].color = emojiCol;
                }

                string url = has ? (semanticLiveEntries[i].AvatarUrl ?? "").Trim() : "";
                semanticLiveAvatarUrls[i] = url;
                if (semanticLiveAvatarPhotos[i] != null)
                {
                    semanticLiveAvatarPhotos[i].texture = null;
                    semanticLiveAvatarPhotos[i].color = new Color(1f, 1f, 1f, 0f);
                }

                bool hasUrl = !string.IsNullOrWhiteSpace(url);
                if (semanticLiveAvatarFallbacks[i] != null) semanticLiveAvatarFallbacks[i].gameObject.SetActive(!hasUrl);
                if (semanticLiveAvatarPhotos[i] != null) semanticLiveAvatarPhotos[i].gameObject.SetActive(hasUrl);
                if (hasUrl)
                {
                    if (semanticLiveAvatarCos[i] != null)
                    {
                        StopCoroutine(semanticLiveAvatarCos[i]);
                        semanticLiveAvatarCos[i] = null;
                    }
                    semanticLiveAvatarCos[i] = StartCoroutine(CoLoadSemanticLiveAvatar(i, url));
                }

                if (semanticLiveProgressFills != null && i < semanticLiveProgressFills.Length && semanticLiveProgressFills[i] != null)
                {
                    Image fill = semanticLiveProgressFills[i];
                    fill.fillAmount = semanticLiveProgressValues[i];
                    float p = semanticLiveProgressValues[i];
                    fill.color = p >= 0.85f
                        ? new Color(0.4f, 1f, 0.34f, 0.98f)
                        : (p >= 0.6f
                            ? new Color(0.22f, 0.86f, 1f, 0.98f)
                            : new Color(0.44f, 0.56f, 1f, 0.95f));
                }
            }
        }

        private IEnumerator CoRevealSemanticRows()
        {
            if (semanticLiveRowGroups == null || semanticLiveRowGroups.Length == 0)
            {
                yield break;
            }

            int count = Mathf.Min(semanticLiveEntries.Count, semanticLiveRowGroups.Length);
            for (int i = 0; i < semanticLiveRowGroups.Length; i++)
            {
                CanvasGroup cg = semanticLiveRowGroups[i];
                if (cg == null) continue;
                cg.alpha = i < count ? 0f : 0.25f;
            }

            for (int i = 0; i < count; i++)
            {
                CanvasGroup cg = semanticLiveRowGroups[i];
                if (cg == null) continue;
                float t = 0f;
                const float dur = 0.12f;
                while (t < dur)
                {
                    t += Time.unscaledDeltaTime;
                    float p = Mathf.Clamp01(t / dur);
                    cg.alpha = Mathf.Lerp(0f, 1f, p);
                    yield return null;
                }
                cg.alpha = 1f;
                yield return new WaitForSecondsRealtime(0.05f);
            }

            semanticLiveRevealCo = null;
        }

        private float EstimateSemanticLiveProgress(string normalizedWord)
        {
            string w = GridThemeBank.SanitizeForGrid(normalizedWord ?? "");
            if (string.IsNullOrEmpty(w)) return 0f;
            if (SemanticAnswerMatches(currentSemanticRound, w)) return 0.98f;

            float best = 0.04f;
            string target = GridThemeBank.SanitizeForGrid(CurrentSemanticTarget ?? "");
            best = Mathf.Max(best, SemanticWordSimilarity(w, target));

            foreach (string accepted in currentSemanticRound.AcceptedAnswers ?? Array.Empty<string>())
            {
                best = Mathf.Max(best, SemanticWordSimilarity(w, GridThemeBank.SanitizeForGrid(accepted ?? "")));
            }

            string gloss = GridThemeBank.SanitizeForGrid(GridThemeBank.TryGlossFr(CurrentSemanticTarget ?? ""));
            best = Mathf.Max(best, SemanticWordSimilarity(w, gloss));
            return Mathf.Clamp(best, 0.04f, 0.96f);
        }

        private static float SemanticWordSimilarity(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
            if (string.Equals(a, b, StringComparison.Ordinal)) return 1f;
            if (a.StartsWith(b, StringComparison.Ordinal) || b.StartsWith(a, StringComparison.Ordinal))
            {
                return 0.78f;
            }

            int dist = LevenshteinDistance(a, b);
            int maxLen = Mathf.Max(a.Length, b.Length);
            if (maxLen <= 0) return 0f;
            float levScore = 1f - (dist / (float)maxLen);
            return Mathf.Clamp01(levScore);
        }

        private static int LevenshteinDistance(string a, string b)
        {
            int n = a.Length;
            int m = b.Length;
            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Mathf.Min(
                        Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        private static string EmojiForSemanticProgress(float progress01)
        {
            if (progress01 >= 0.9f) return "🔥";
            if (progress01 >= 0.75f) return "😎";
            if (progress01 >= 0.55f) return "🙂";
            if (progress01 >= 0.35f) return "🧐";
            return "☁️";
        }

        private static string SemanticSearchLanguage(SemanticRound round)
        {
            string p = (round.Prompt ?? "").ToUpperInvariant();
            if (p.Contains("-> LINGALA")) return "Lingala";
            if (p.Contains("-> KITOUBA") || p.Contains("-> KITUBA")) return "Kitouba";
            if (p.Contains("-> FR")) return "Français";
            return "Multilingue";
        }

        private static string SemanticThemeForRound(SemanticRound round)
        {
            string t = GridThemeBank.SanitizeForGrid(round.Target);
            return t switch
            {
                "MBOTE" or "MALAMU" => "Salutations et politesse",
                "MWANA" or "MAMA" or "TATA" or "BISO" or "MUNTU" => "Famille et personnes",
                "MOSALA" => "Travail et vie quotidienne",
                "MAIE" or "NZOTO" => "Corps et besoins essentiels",
                _ => "Vocabulaire courant"
            };
        }

        private static string BuildSemanticCrossLanguageInfo(SemanticRound round)
        {
            string key = GridThemeBank.SanitizeForGrid(round.Target);
            if (!SemanticLexicon.TryGetValue(key, out SemanticLexiconEntry x))
            {
                return "";
            }

            string lang = SemanticSearchLanguage(round);
            if (string.Equals(lang, "Lingala", StringComparison.OrdinalIgnoreCase))
            {
                return "Lingala: <b>" + x.Lingala + "</b>  |  FR: <b>" + x.Francais + "</b>  |  Kitouba: <b>" + x.Kitouba + "</b>\nDéfinition: " + x.DefinitionFr;
            }

            if (string.Equals(lang, "Kitouba", StringComparison.OrdinalIgnoreCase))
            {
                return "Kitouba: <b>" + x.Kitouba + "</b>  |  FR: <b>" + x.Francais + "</b>  |  Lingala: <b>" + x.Lingala + "</b>\nDéfinition: " + x.DefinitionFr;
            }

            // Cas FR ou mixte: on expose surtout les équivalents Lingala/Kitouba.
            return "FR: <b>" + x.Francais + "</b>  |  Lingala: <b>" + x.Lingala + "</b>  |  Kitouba: <b>" + x.Kitouba + "</b>\nDéfinition: " + x.DefinitionFr;
        }

        private void ShowSemanticLivePointsBadge(string validatedWord, int points)
        {
            if (semanticLiveEntries == null || semanticLiveEntries.Count == 0 || semanticLivePtsBadges == null || semanticLivePtsBadges.Length == 0)
            {
                return;
            }

            string key = GridThemeBank.SanitizeForGrid(validatedWord ?? "");
            int row = -1;
            for (int i = 0; i < semanticLiveEntries.Count && i < semanticLivePtsBadges.Length; i++)
            {
                if (string.Equals(semanticLiveEntries[i].Word, key, StringComparison.OrdinalIgnoreCase))
                {
                    row = i;
                    break;
                }
            }
            if (row < 0) row = 0;
            if (row >= semanticLivePtsBadges.Length || semanticLivePtsBadges[row] == null) return;

            if (row < semanticLiveEntries.Count)
            {
                SemanticLiveEntry old = semanticLiveEntries[row];
                float boosted = Mathf.Clamp01(Mathf.Max(old.Progress01, 0.75f + Mathf.Min(0.2f, points * 0.05f)));
                semanticLiveEntries[row] = new SemanticLiveEntry(old.User, old.Word, old.AvatarUrl, boosted);
                RenderSemanticLiveFeed();
            }

            if (semanticLivePtsCos[row] != null)
            {
                StopCoroutine(semanticLivePtsCos[row]);
                semanticLivePtsCos[row] = null;
            }
            semanticLivePtsCos[row] = StartCoroutine(CoSemanticPointsBadge(row, Mathf.Max(1, points)));
        }

        private IEnumerator CoSemanticPointsBadge(int row, int points)
        {
            if (semanticLivePtsBadges == null || row < 0 || row >= semanticLivePtsBadges.Length)
            {
                yield break;
            }

            Text badge = semanticLivePtsBadges[row];
            if (badge == null) yield break;
            badge.text = "+" + points;
            Color baseCol = new Color(1f, 0.86f, 0.28f, 1f);
            badge.color = baseCol;
            badge.transform.localScale = Vector3.one * 0.8f;
            float t = 0f;
            float dur = 0.8f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / dur);
                float s = Mathf.Lerp(0.8f, 1.08f, 1f - Mathf.Pow(1f - p, 2f));
                badge.transform.localScale = new Vector3(s, s, 1f);
                Color c = baseCol;
                c.a = 1f - Mathf.Clamp01((p - 0.45f) / 0.55f);
                badge.color = c;
                yield return null;
            }
            badge.text = "";
            badge.transform.localScale = Vector3.one;
            semanticLivePtsCos[row] = null;
        }

        private IEnumerator CoLoadSemanticLiveAvatar(int row, string url)
        {
            if (row < 0 || semanticLiveAvatarPhotos == null || row >= semanticLiveAvatarPhotos.Length)
            {
                yield break;
            }

            RawImage photo = semanticLiveAvatarPhotos[row];
            if (photo == null || string.IsNullOrWhiteSpace(url))
            {
                yield break;
            }

            if (gridAvatarCache.TryGetValue(url, out Texture2D cached) && cached != null)
            {
                if (semanticLiveAvatarUrls[row] == url && photo != null)
                {
                    photo.texture = cached;
                    photo.color = Color.white;
                    if (semanticLiveAvatarFallbacks[row] != null) semanticLiveAvatarFallbacks[row].gameObject.SetActive(false);
                }
                semanticLiveAvatarCos[row] = null;
                yield break;
            }

            using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(url, true))
            {
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    if (semanticLiveAvatarFallbacks[row] != null) semanticLiveAvatarFallbacks[row].gameObject.SetActive(true);
                    semanticLiveAvatarCos[row] = null;
                    yield break;
                }

                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                if (tex == null)
                {
                    if (semanticLiveAvatarFallbacks[row] != null) semanticLiveAvatarFallbacks[row].gameObject.SetActive(true);
                    semanticLiveAvatarCos[row] = null;
                    yield break;
                }

                gridAvatarCache[url] = tex;
                if (semanticLiveAvatarUrls[row] == url && photo != null)
                {
                    photo.texture = tex;
                    photo.color = Color.white;
                    if (semanticLiveAvatarFallbacks[row] != null) semanticLiveAvatarFallbacks[row].gameObject.SetActive(false);
                }
            }

            semanticLiveAvatarCos[row] = null;
        }

        private void HandleLiveLikePulse(int delta, string user)
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null) return;
            string mode = gmm.ActiveModeId ?? "";
            if (!string.Equals(mode, "word-scramble", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(mode, "crossword-lite", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            int step = CurrentLikeThresholdForNextHint();
            likesProgressToHint += Mathf.Max(1, delta);
            bool consumedAny = false;
            while (likesProgressToHint >= step)
            {
                if (!TryRevealOneHintLetter(mode, out string info))
                {
                    likesProgressToHint = step - 1;
                    break;
                }

                likesProgressToHint -= step;
                hintLettersRevealedInRound++;
                consumedAny = true;
                if (string.Equals(mode, "word-scramble", StringComparison.OrdinalIgnoreCase))
                {
                    if (wordFeedback != null) wordFeedback.text = "👍 Indice +1 lettre : " + info;
                }
                else
                {
                    if (crosswordFeedback != null) crosswordFeedback.text = "👍 Indice +1 lettre : " + info;
                }
                GameSfxHub.Instance?.PlayUiPop(0.24f);
                step = CurrentLikeThresholdForNextHint();
            }

            if (!consumedAny)
            {
                int remain = Mathf.Max(0, step - likesProgressToHint);
                if (string.Equals(mode, "word-scramble", StringComparison.OrdinalIgnoreCase))
                {
                    if (wordFeedback != null) wordFeedback.text = "👍 " + remain + " likes pour l’indice suivant";
                }
                else
                {
                    if (crosswordFeedback != null) crosswordFeedback.text = "👍 " + remain + " likes pour l’indice suivant";
                }
            }
        }

        private int CurrentLikeThresholdForNextHint()
        {
            // Progressif: 1er indice accessible, puis coût monte pour éviter de vider toute la grille trop vite.
            int baseStep = Mathf.Max(20, likesPerHintLetter);
            int extra = hintLettersRevealedInRound * 35;
            return Mathf.Clamp(baseStep - 20 + extra, 80, 240);
        }

        private bool TryRevealOneHintLetter(string mode, out string info)
        {
            info = "";
            if (currentGridAllWords == null || currentGridAllWords.Count == 0) return false;
            for (int i = 0; i < currentGridAllWords.Count; i++)
            {
                string w = GridThemeBank.SanitizeForGrid(currentGridAllWords[i] ?? "");
                if (string.IsNullOrEmpty(w)) continue;
                if (currentGridSolved != null && currentGridSolved.Contains(w)) continue;

                if (string.Equals(mode, "word-scramble", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryRevealHintCellInWordScramble(w, out string pos))
                    {
                        info = w + " (" + pos + ")";
                        return true;
                    }
                }
                else
                {
                    if (TryRevealHintCellInCrossword(w, out string pos))
                    {
                        info = w + " (" + pos + ")";
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Construit les panneaux secondaires (grilles, champs) si absents — scène perso ou ordre d’exécution atypique.
        /// Appelé au chargement par <see cref="RuntimeBootstrap"/> si un <see cref="ModeSurfaceController"/> existe déjà.
        /// </summary>
        public void EnsureGridsIfMissing()
        {
            if (secondaryPanelsEnsured) return;
            bool missing =
                crosswordCells == null
                || crosswordCells.Length == 0
                || crosswordCells[0] == null;
            if (!missing) return;
            ModeSurfaceController surf = GetComponent<ModeSurfaceController>();
            if (surf == null) return;
            Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (f == null) return;
            BuildSecondaryPanels(transform, f, surf, this);
            secondaryPanelsEnsured = true;
            surf.Apply(surf.CurrentModeId);
        }

        [Header("Chrono vitesse (réaction)")]
        public Text chronoTitle;
        public Text chronoBig;
        public Text chronoSub;
        [Tooltip("Règles complètes — affiché à côté du compte à rebours pour ne pas masquer le texte.")]
        public Text chronoInstruction;
        [Tooltip("Optionnel (créé au besoin) : numéro de bille / vague.")]
        public Text chronoMeta;

        [Header("Image")]
        public Text imageTitle;
        public RawImage imagePlaceholder;
        public Text imageCaption;
        public InputField imageGuessInput;
        public Button imageGuessSubmit;
        public Text imageGuessFeedback;

        public MiniGameDemoBanks.ImageGuessRound CurrentImageGuessRound { get; private set; }

        private static void ConfigureGridLetter(Text tx, float cell)
        {
            if (tx == null) return;
            tx.resizeTextForBestFit = false;
            tx.fontSize = Mathf.Clamp((int)(cell * 0.46f), 22, 58);
            tx.horizontalOverflow = HorizontalWrapMode.Overflow;
            tx.verticalOverflow = VerticalWrapMode.Overflow;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = new Color(1f, 0.94f, 0.2f, 1f);
            Outline ol = tx.GetComponent<Outline>();
            if (ol == null) ol = tx.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0f, 0f, 0f, 0.94f);
            ol.useGraphicAlpha = false;
            ol.effectDistance = new Vector2(1.25f, -1.25f);
        }

        private static Sprite White()
        {
            Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                tex.SetPixel(x, y, Color.white);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        }

        public void Populate(string modeId)
        {
            if (modeNarrationCo != null)
            {
                StopCoroutine(modeNarrationCo);
                modeNarrationCo = null;
            }

            if (!string.Equals(modeId, "blind-test", StringComparison.Ordinal) && blindListenCo != null)
            {
                StopCoroutine(blindListenCo);
                blindListenCo = null;
            }
            if (!string.Equals(modeId, "blind-test", StringComparison.Ordinal) && blindUnlockCo != null)
            {
                StopCoroutine(blindUnlockCo);
                blindUnlockCo = null;
            }

            if (!string.Equals(modeId, "image-guess", StringComparison.Ordinal) && imageOrchestrationCo != null)
            {
                StopCoroutine(imageOrchestrationCo);
                imageOrchestrationCo = null;
            }
            if (!string.Equals(modeId, "image-guess", StringComparison.Ordinal) && imageMusicHintCo != null)
            {
                StopCoroutine(imageMusicHintCo);
                imageMusicHintCo = null;
            }

            if (!string.Equals(modeId, "blind-test", StringComparison.Ordinal)
                && !string.Equals(modeId, "image-guess", StringComparison.Ordinal))
            {
                GameSfxHub.Instance?.StopBlindDemoMusic();
                ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
            }

            if (!string.Equals(modeId, "speed-chrono", StringComparison.OrdinalIgnoreCase))
            {
                chronoModeActive = false;
            }

            if (!string.Equals(modeId, lastPopulatedModeId, StringComparison.Ordinal)
                && (GameModeManager.IsGridThematicModeId(modeId) || GameModeManager.IsGridThematicModeId(lastPopulatedModeId)))
            {
                if (GameModeManager.IsGridThematicModeId(modeId))
                {
                    ResetThematicGridState();
                }
            }

            lastPopulatedModeId = modeId ?? "";

            switch (modeId)
            {
                case "quiz":
                    MaybeNarrateMode(modeId);
                    break;
                case "semantic":
                    ApplySemanticDemo();
                    MaybeNarrateMode(modeId);
                    break;
                case "word-scramble":
                    ApplyWordScrambleDemo();
                    MaybeNarrateMode(modeId);
                    break;
                case "crossword-lite":
                    ApplyCrosswordDemo();
                    MaybeNarrateMode(modeId);
                    break;
                case "blind-test":
                    ApplyBlindDemo();
                    break;
                case "mystery-word":
                    ApplyMysteryDemo();
                    MaybeNarrateMode(modeId);
                    break;
                case "memory":
                    ApplyMemoryDemo();
                    MaybeNarrateMode(modeId);
                    break;
                case "speed-chrono":
                    ApplyChronoDemo();
                    MaybeNarrateMode(modeId);
                    break;
                case "image-guess":
                    StartCoroutine(CoApplyImageDemo());
                    break;
            }
        }

        private void MaybeNarrateMode(string modeId)
        {
            if (!aiHostNarratesAllModes) return;
            if (string.IsNullOrWhiteSpace(modeId)) return;
            if (string.Equals(modeId, "blind-test", StringComparison.Ordinal) || string.Equals(modeId, "image-guess", StringComparison.Ordinal))
            {
                // Ces deux modes ont déjà une orchestration IA dédiée (intro + 3,2,1 + relance).
                return;
            }

            AIHostManager host = AIHostManager.Instance;
            if (host == null) return;
            SetModeInteractionsLocked(modeId, true);
            host.InterruptSpeech();
            string intro;
            string rules;
            bool hasChoiceLetters;
            string questionLine;
            string hintLine;
            BuildNarrationForMode(modeId, out intro, out rules, out hasChoiceLetters, out questionLine, out hintLine);

            host.Speak(intro);
            host.Speak(rules);
            if (!string.IsNullOrWhiteSpace(questionLine))
            {
                host.Speak("Question. " + questionLine);
            }
            if (!string.IsNullOrWhiteSpace(hintLine))
            {
                host.Speak("Indice. " + hintLine);
            }
            if (hasChoiceLetters)
            {
                host.Speak("Réponses possibles: A, B, C ou D.");
            }

            modeNarrationCo = StartCoroutine(CoUnlockModeAfterHostNarration(modeId));
        }

        private IEnumerator CoUnlockModeAfterHostNarration(string modeId)
        {
            yield return CoWaitHostSilence(hostSafetyWaitSeconds);
            AIHostManager host = AIHostManager.Instance;
            host?.Speak("Tu peux jouer.");
            yield return CoWaitHostSilence(12f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, hostPostAuthorizeDelaySeconds));
            SetModeInteractionsLocked(modeId, false);
            modeNarrationCo = null;
        }

        private void SetModeInteractionsLocked(string modeId, bool locked)
        {
            bool interactable = !locked;
            switch (modeId)
            {
                case "quiz":
                {
                    QuestionUI q = QuestionUI.Instance;
                    if (q != null)
                    {
                        if (locked)
                        {
                            q.SetPhaseReadingOnly("L'animateur explique la manche...");
                        }
                        else
                        {
                            q.SetPhaseAnswerable("À toi de jouer : réponds A, B, C ou D.");
                        }
                    }

                    break;
                }
                case "semantic":
                    if (semanticAnswerInput != null) semanticAnswerInput.interactable = interactable;
                    if (semanticSubmitButton != null) semanticSubmitButton.interactable = interactable;
                    if (semanticClearButton != null) semanticClearButton.interactable = interactable;
                    break;
                case "word-scramble":
                    if (wordAnswerInput != null) wordAnswerInput.interactable = interactable;
                    if (wordSubmitButton != null) wordSubmitButton.interactable = interactable;
                    if (wordClearButton != null) wordClearButton.interactable = interactable;
                    if (wordScrambleButtons != null)
                    {
                        foreach (Button b in wordScrambleButtons)
                        {
                            if (b != null) b.interactable = interactable;
                        }
                    }

                    break;
                case "crossword-lite":
                    if (crosswordAnswerInput != null) crosswordAnswerInput.interactable = interactable;
                    if (crosswordSubmitButton != null) crosswordSubmitButton.interactable = interactable;
                    if (crosswordClearButton != null) crosswordClearButton.interactable = interactable;
                    if (crosswordButtons != null)
                    {
                        foreach (Button b in crosswordButtons)
                        {
                            if (b != null) b.interactable = interactable;
                        }
                    }

                    break;
                case "mystery-word":
                    if (mysteryAnswerInput != null) mysteryAnswerInput.interactable = interactable;
                    if (mysterySubmitButton != null) mysterySubmitButton.interactable = interactable;
                    if (mysteryClearButton != null) mysteryClearButton.interactable = interactable;
                    break;
                case "memory":
                    if (memoryCards != null)
                    {
                        foreach (Button b in memoryCards)
                        {
                            if (b != null) b.interactable = interactable;
                        }
                    }

                    break;
                case "speed-chrono":
                    chronoInputLockedByHost = locked;
                    break;
            }
        }

        private void BuildNarrationForMode(
            string modeId,
            out string intro,
            out string rules,
            out bool hasChoiceLetters,
            out string questionLine,
            out string hintLine)
        {
            intro = "Nouvelle manche. Concentre-toi.";
            rules = "Lis la consigne et réponds avant la fin du temps.";
            hasChoiceLetters = false;
            questionLine = "";
            hintLine = "";

            switch (modeId)
            {
                case "quiz":
                    intro = "Quiz culture. La manche démarre.";
                    rules = "Je lis la question, puis tu choisis la bonne réponse.";
                    hasChoiceLetters = true;
                    questionLine = CleanNarrationLine(QuestionUI.Instance != null ? QuestionUI.Instance.CurrentPromptText : "");
                    break;
                case "semantic":
                    intro = "Mode sémantique. La manche démarre.";
                    rules = "Observe la grille et trouve le mot attendu.";
                    questionLine = CleanNarrationLine(semanticTitle != null ? semanticTitle.text : "");
                    hintLine = CleanNarrationLine(semanticHint != null ? semanticHint.text : "");
                    break;
                case "word-scramble":
                    intro = "Mode mots mélangés. La manche démarre.";
                    rules = "Remets les lettres dans le bon ordre puis valide.";
                    questionLine = CleanNarrationLine(wordScrambleTitle != null ? wordScrambleTitle.text : "");
                    hintLine = CleanNarrationLine(wordHint != null ? wordHint.text : "");
                    break;
                case "crossword-lite":
                    intro = "Mode mots croisés. La manche démarre.";
                    rules = "Trouve les mots dans la grille puis valide.";
                    questionLine = CleanNarrationLine(crosswordTitle != null ? crosswordTitle.text : "");
                    hintLine = CleanNarrationLine(crosswordFeedback != null ? crosswordFeedback.text : "");
                    break;
                case "mystery-word":
                    intro = "Mode mot mystère. La manche démarre.";
                    rules = "Lis les indices, trouve le mot complet, puis valide.";
                    questionLine = CleanNarrationLine(mysteryTitle != null ? mysteryTitle.text : "");
                    hintLine = CleanNarrationLine(mysteryFeedback != null ? mysteryFeedback.text : "");
                    break;
                case "memory":
                    intro = "Mode mémoire. La manche démarre.";
                    rules = "Retourne deux cartes et forme les bonnes paires.";
                    questionLine = CleanNarrationLine(memoryTitle != null ? memoryTitle.text : "");
                    hintLine = CleanNarrationLine(memorySubtitle != null ? memorySubtitle.text : "");
                    break;
                case "speed-chrono":
                    intro = "Mode chrono vitesse. La manche démarre.";
                    rules = "Repère la cible et réponds très vite avec 1, 2, 3 ou 4.";
                    questionLine = CleanNarrationLine(chronoTitle != null ? chronoTitle.text : "");
                    hintLine = CleanNarrationLine(chronoInstruction != null ? chronoInstruction.text : "");
                    hasChoiceLetters = false;
                    break;
            }
        }

        private static string CleanNarrationLine(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return "";
            }

            string s = raw.Trim();
            s = s.Replace("\n", " ");
            while (s.Contains("  "))
            {
                s = s.Replace("  ", " ");
            }

            // Allège les consignes très longues en gardant la première phrase.
            int dot = s.IndexOf(". ", StringComparison.Ordinal);
            if (dot > 12)
            {
                s = s.Substring(0, dot + 1);
            }

            return s.Trim();
        }

        private void ApplySemanticDemo()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool liveMode = live != null && live.IsConnected;
            SemanticRound round = NextSemanticRound();
            currentSemanticRound = round;
            CurrentSemanticTarget = round.Target;
            int roundNo = Mathf.Clamp(semanticRoundCursor + 1, 1, Mathf.Max(1, SemanticRounds.Length));
            int totalRounds = Mathf.Max(1, SemanticRounds.Length);
            int targetLen = 0;
            string target = (round.Target ?? "").Trim().ToUpperInvariant();
            for (int i = 0; i < target.Length; i++)
            {
                if (char.IsLetter(target[i])) targetLen++;
            }
            int nextLikeGoal = CurrentLikeThresholdForNextHint();

            if (semanticTitle != null)
            {
                semanticTitle.text = "Sémantique";
            }

            semanticLiveEntries.Clear();
            semanticAllRankedEntries.Clear();
            semanticAttemptHistory.Clear();
            semanticAwardTierByWord.Clear();
            semanticBestProgressCue = 0f;
            semanticNextWarmCueAt = 0f;
            semanticRoundResolved = false;
            semanticRoundEndUnscaled = Time.unscaledTime + Mathf.Clamp(semanticRoundDurationSeconds, 45f, 180f);
            semanticTickerQueue.Clear();
            if (semanticLiveRevealCo != null)
            {
                StopCoroutine(semanticLiveRevealCo);
                semanticLiveRevealCo = null;
            }
            if (semanticTickerCo != null)
            {
                StopCoroutine(semanticTickerCo);
                semanticTickerCo = null;
            }
            if (semanticPostWinCo != null)
            {
                StopCoroutine(semanticPostWinCo);
                semanticPostWinCo = null;
            }
            if (semanticTimeoutCo != null)
            {
                StopCoroutine(semanticTimeoutCo);
                semanticTimeoutCo = null;
            }
            if (semanticLiveTicker != null) semanticLiveTicker.text = "";
            if (semanticRevealWordTop != null) semanticRevealWordTop.text = "";

            string[] cells = BuildSemanticGridLetters(round);
            Color[] cellBack = new Color[9];
            for (int i = 0; i < cellBack.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;
                cellBack[i] = (row + col) % 2 == 0
                    ? new Color(0.09f, 0.1f, 0.12f, 1f)
                    : new Color(0.12f, 0.13f, 0.16f, 1f);
            }

            for (int i = 0; i < semanticCells.Length && i < cells.Length; i++)
            {
                if (semanticCells[i] == null) continue;
                semanticCells[i].text = cells[i];
                semanticCells[i].fontStyle = FontStyle.Bold;
                semanticCells[i].color = new Color(1f, 0.94f, 0.22f, 1f);
                Image bg = semanticCells[i].transform.parent != null
                    ? semanticCells[i].transform.parent.GetComponent<Image>()
                    : null;
                if (bg != null) bg.color = cellBack[i];
                Outline ol = semanticCells[i].transform.parent != null
                    ? semanticCells[i].transform.parent.GetComponent<Outline>()
                    : null;
                if (ol != null)
                {
                    ol.effectColor = new Color(0.45f, 0.5f, 0.56f, 0.35f);
                    ol.effectDistance = new Vector2(1f, -1f);
                }
                ConfigureGridLetter(semanticCells[i], 86f);
            }

            if (semanticHint != null)
            {
                string langChip = SemanticSearchLanguage(round);
                string themeChip = SemanticThemeForRound(round);
                string chips =
                    "<b><color=#58A6FF> " + roundNo + "/" + totalRounds + " </color></b>   "
                    + "<b><color=#FFD84D> " + Mathf.Clamp(targetLen, 3, 16) + " lettres </color></b>   "
                    + "<b><color=#FFA6A6> " + Mathf.CeilToInt(Mathf.Clamp(semanticRoundDurationSeconds, 45f, 180f)) + " s </color></b>   "
                    + "<b><color=#5EE7A2> " + Mathf.Max(1, nextLikeGoal) + " likes indice </color></b>   "
                    + "<b><color=#B28CFF> Langue: " + langChip + " </color></b>   "
                    + "<b><color=#FF9ED6> Thème: " + themeChip + " </color></b>";
                semanticHint.text = chips + "\n"
                    + (liveMode
                        ? "Mode live: propose des mots, les barres apparaissent au fur et à mesure. " + round.Prompt
                        : "Propose des mots: la proximité (%) et l’emoji montent quand tu te rapproches. " + round.Prompt);
            }

            if (semanticAnswerInput != null)
            {
                semanticAnswerInput.text = "";
                semanticAnswerInput.interactable = false;
                semanticAnswerInput.gameObject.SetActive(false);
            }

            if (semanticSubmitButton != null)
            {
                semanticSubmitButton.gameObject.SetActive(false);
                semanticSubmitButton.interactable = false;
            }

            if (semanticClearButton != null)
            {
                semanticClearButton.gameObject.SetActive(false);
                semanticClearButton.interactable = false;
            }

            if (semanticFeedback != null)
            {
                semanticFeedback.text = "<color=#9BC3D9>Traduction FR <-> Lingala <-> Kitouba. Les barres s'affichent seulement après une proposition.</color>";
            }
            RenderSemanticLiveFeed();
        }

        private SemanticRound NextSemanticRound()
        {
            if (SemanticRounds.Length == 0)
            {
                return new SemanticRound("MBOTE", "Sémantique", "FR: salut -> Lingala ?", new[] { "MBOTE" }, "MALAMU", "BISO");
            }

            semanticRoundCursor = (semanticRoundCursor + 1) % SemanticRounds.Length;
            return SemanticRounds[semanticRoundCursor];
        }

        private static string[] BuildSemanticGridLetters(SemanticRound round)
        {
            const int cellCount = 9;
            string target = (round.Target ?? "CONGO").Trim().ToUpperInvariant();
            if (target.Length < 3) target = "CONGO";

            var letters = new List<string>(cellCount);
            foreach (char c in target)
            {
                if (char.IsLetter(c))
                {
                    letters.Add(c.ToString());
                    if (letters.Count >= 6) break;
                }
            }

            foreach (string d in round.Decoys ?? Array.Empty<string>())
            {
                string w = (d ?? "").Trim().ToUpperInvariant();
                for (int i = 0; i < w.Length && letters.Count < cellCount; i++)
                {
                    char c = w[i];
                    if (char.IsLetter(c)) letters.Add(c.ToString());
                }

                if (letters.Count >= cellCount) break;
            }

            const string fallback = "CONGORUMBANGOMA";
            int f = 0;
            while (letters.Count < cellCount)
            {
                letters.Add(fallback[f % fallback.Length].ToString());
                f++;
            }

            for (int i = letters.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (letters[i], letters[j]) = (letters[j], letters[i]);
            }

            return letters.ToArray();
        }

        private static bool SemanticAnswerMatches(SemanticRound round, string userAnswer)
        {
            string a = (userAnswer ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(a)) return false;
            if (string.Equals(a, (round.Target ?? "").Trim().ToUpperInvariant(), StringComparison.Ordinal)) return true;
            foreach (string x in round.AcceptedAnswers ?? Array.Empty<string>())
            {
                string c = (x ?? "").Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(c)) continue;
                if (string.Equals(a, c, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private void ApplyWordScrambleDemo()
        {
            if (currentGridAllWords == null || currentGridSolved == null
                || (currentGridAllWords != null && currentGridAllWords.Count == 0 && gridThematicBlockRound == 0))
            {
                NewThematicSession();
            }

            if (currentGridAllWords == null || currentGridAllWords.Count == 0)
            {
                NewThematicSession();
            }

            if (string.IsNullOrEmpty((CurrentScrambleTargetWord ?? "").Trim()))
            {
                AdvanceGridMotsJeu();
            }

            if (string.IsNullOrEmpty((CurrentScrambleTargetWord ?? "").Trim()) && currentGridAllWords != null
                && currentGridAllWords.Count > 0)
            {
                CurrentScrambleTargetWord = currentGridAllWords[0];
                CurrentScrambleAnswer = CurrentScrambleTargetWord;
            }

            BuildWordScrambleForTarget();
        }

        private void BuildWordScrambleForTarget()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool liveMode = live != null && live.IsConnected;

            if (wordScrambleTitle != null)
            {
                int found = currentGridSolved != null ? currentGridSolved.Count : 0;
                int total = currentGridAllWords != null ? currentGridAllWords.Count : 0;
                wordScrambleTitle.text = "Mots mélangés — " + CompactThemeLabel(currentGridThemeLabel)
                    + "  <color=#7EE7FF>[" + found + "/" + total + "]</color>";
            }

            Font tileFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (wordScrambleLetters != null)
            {
                wordScrambleLetters.gameObject.SetActive(false);
            }

            int nCells = wordScrambleTiles != null ? wordScrambleTiles.Length : 0;
            for (int i = 0; i < nCells; i++)
            {
                if (wordScrambleTiles[i] != null)
                {
                    wordScrambleTiles[i].text = "";
                    if (tileFont != null) wordScrambleTiles[i].font = tileFont;
                    Transform p = wordScrambleTiles[i].transform.parent;
                    Image bg = p != null ? p.GetComponent<Image>() : null;
                    Color baseColor = ScrambleDecoBg(i);
                    if (bg != null) bg.color = baseColor;
                    GridCellHoverFeedback hover = p != null ? p.GetComponent<GridCellHoverFeedback>() : null;
                    if (hover != null) hover.SetBaseColor(baseColor);
                }
            }

            int size = ScrambleCols;
            var toTry = new System.Collections.Generic.List<string>(currentGridAllWords);
            char[,] grid = null;
            System.Collections.Generic.List<WordLinePlacement> placements = null;
            for (int attempt = 0; attempt < 16 && toTry.Count >= GridThemeBank.MinWords; attempt++)
            {
                if (WordSearchPlacer.TryBuild(size, toTry, 240, out grid, out placements))
                {
                    break;
                }

                toTry.Sort((a, b) => (b != null ? b.Length : 0).CompareTo(a != null ? a.Length : 0));
                if (toTry.Count > GridThemeBank.MinWords) toTry.RemoveAt(0);
            }

            if (grid == null)
            {
                toTry = new System.Collections.Generic.List<string>();
                for (int i = 0; i < currentGridAllWords.Count && i < 6; i++) toTry.Add(currentGridAllWords[i]);
                WordSearchPlacer.TryBuild(size, toTry, 320, out grid, out placements);
            }

            if (grid == null)
            {
                return;
            }

            if (toTry.Count < currentGridAllWords.Count)
            {
                currentGridAllWords = toTry;
                currentGridSolved = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            }

            lastScrambleGrid = grid;
            currentScramblePlacements = placements ?? new System.Collections.Generic.List<WordLinePlacement>();
            wordScrambleFoundCellIndices.Clear();
            if (currentGridSolved != null)
            {
                foreach (string solved in currentGridSolved)
                {
                    ApplyWordScrambleFoundWordHighlight(solved);
                }
            }

            bool[] playableTile = new bool[nCells];
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int idx = r * ScrambleCols + c;
                    if (idx < 0 || idx >= nCells || wordScrambleTiles[idx] == null) continue;
                    if (tileFont != null) wordScrambleTiles[idx].font = tileFont;
                    playableTile[idx] = true;
                    RefreshWordScrambleCellDecor(idx);
                }
            }

            wordScramblePlayableMask = new bool[playableTile.Length];
            System.Array.Copy(playableTile, wordScramblePlayableMask, playableTile.Length);

            if (wordScrambleButtons != null)
            {
                for (int i = 0; i < wordScrambleButtons.Length; i++)
                {
                    Button b = wordScrambleButtons[i];
                    if (b == null) continue;
                    b.onClick.RemoveAllListeners();
                    Text tx = wordScrambleTiles != null && i < wordScrambleTiles.Length ? wordScrambleTiles[i] : null;
                    string ch = tx != null ? tx.text : "";
                    bool has = !string.IsNullOrEmpty(ch);
                    bool playable = i < playableTile.Length && playableTile[i];
                    b.interactable = has;
                    if (!playable || !has) continue;
                    string letter = ch.ToUpperInvariant();
                    b.onClick.AddListener(() =>
                    {
                        AppendWordGuessChar(letter);
                        GameSfxHub.Instance?.PlayTap();
                    });
                }
            }

            if (wordHint != null)
            {
                string line = GridThemeBank.InGameThemeHintFr(currentGridThemeLabel);
                wordHint.text = "Indice : " + CompactHint(line) + " • Repère les mots en ligne/colonne/diagonale";
            }

            if (wordScrambleTiles != null)
            {
                foreach (Text t in wordScrambleTiles)
                {
                    if (t != null) ConfigureGridLetter(t, ScrambleCellPx);
                }
            }

            if (wordAnswerInput != null)
            {
                wordAnswerInput.text = "";
                wordAnswerInput.interactable = !liveMode;
                wordAnswerInput.gameObject.SetActive(!liveMode);
            }

            if (wordSubmitButton != null)
            {
                wordSubmitButton.gameObject.SetActive(!liveMode);
                wordSubmitButton.interactable = !liveMode;
            }

            if (wordClearButton != null)
            {
                wordClearButton.gameObject.SetActive(!liveMode);
                wordClearButton.interactable = !liveMode;
            }

            if (wordFeedback != null) wordFeedback.text = "";
        }

        public void AppendWordGuessChar(string letter)
        {
            if (Time.frameCount <= suppressGridTapUntilFrame) return;
            if (wordAnswerInput == null || string.IsNullOrEmpty(letter)) return;
            wordAnswerInput.text += letter[0];
        }

        public void ClearWordGuess()
        {
            if (wordAnswerInput != null) wordAnswerInput.text = "";
        }

        public void RegisterGridDragCommitThisFrame()
        {
            suppressGridTapUntilFrame = Time.frameCount + 1;
        }

        public void AppendCrosswordDragWord(string letters)
        {
            if (crosswordAnswerInput == null || string.IsNullOrEmpty(letters)) return;
            crosswordAnswerInput.text += letters;
        }

        public void AppendWordDragWord(string letters)
        {
            if (wordAnswerInput == null || string.IsNullOrEmpty(letters)) return;
            wordAnswerInput.text += letters;
        }

        public void AppendCrosswordGuessFromCell(int cellIndex)
        {
            if (Time.frameCount <= suppressGridTapUntilFrame) return;
            if (crosswordCells == null || cellIndex < 0 || cellIndex >= crosswordCells.Length) return;
            Text tx = crosswordCells[cellIndex];
            if (tx == null || crosswordAnswerInput == null) return;
            string s = tx.text?.Trim() ?? "";
            if (s.Length != 1 || s == "·" || s == "." || s == " ") return;
            crosswordAnswerInput.text += s.ToUpperInvariant();
            HighlightCrosswordSelection(cellIndex);
            GameSfxHub.Instance?.PlayTap();
        }

        public void ClearCrosswordGuess()
        {
            if (crosswordAnswerInput != null) crosswordAnswerInput.text = "";
        }

        private static string ScrambleWord(string w, int seed)
        {
            char[] arr = w.ToCharArray();
            System.Random rng = new System.Random(seed);
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }

            return new string(arr);
        }

        private void BuildWordSearchAndFillGrid()
        {
            if (currentGridAllWords == null || currentGridAllWords.Count == 0) return;
            int size = CrosswordCols;
            var toTry = new System.Collections.Generic.List<string>(currentGridAllWords);
            char[,] grid = null;
            System.Collections.Generic.List<WordLinePlacement> placements = null;
            System.Collections.Generic.List<CrosswordClueEntry> clues = null;
            for (int attempt = 0; attempt < 12 && toTry.Count >= GridThemeBank.MinWords; attempt++)
            {
                if (CrosswordClassicPlacer.TryBuild(size, toTry, 220, out grid, out placements, out clues))
                {
                    break;
                }

                toTry.Sort((a, b) => (b != null ? b.Length : 0).CompareTo(a != null ? a.Length : 0));
                if (toTry.Count > GridThemeBank.MinWords) toTry.RemoveAt(0);
            }

            if (grid == null)
            {
                toTry = new System.Collections.Generic.List<string>();
                for (int i = 0; i < currentGridAllWords.Count && i < 5; i++) toTry.Add(currentGridAllWords[i]);
                CrosswordClassicPlacer.TryBuild(size, toTry, 320, out grid, out placements, out clues);
            }

            if (grid == null) return;
            if (toTry.Count < currentGridAllWords.Count)
            {
                currentGridAllWords = toTry;
                currentGridSolved = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                AdvanceGridMotsJeu();
            }

            lastWordSearchGrid = grid;
            currentWordSearchPlacements = placements ?? new System.Collections.Generic.List<WordLinePlacement>();
            currentCrosswordClues = clues ?? new System.Collections.Generic.List<CrosswordClueEntry>();
            crosswordFoundCellIndices.Clear();
            crosswordHintCellIndices.Clear();
            crosswordSelectedCell = -1;
            if (currentWordSearchPlacements != null)
            {
                for (int i = 0; i < currentWordSearchPlacements.Count; i++)
                {
                    WordLinePlacement p = currentWordSearchPlacements[i];
                    int len = string.IsNullOrEmpty(p.Word) ? 0 : p.Word.Length;
                    if (len < 2) continue;
                    int startIdx = p.StartR * CrosswordCols + p.StartC;
                    int endR = p.StartR + p.Dr * (len - 1);
                    int endC = p.StartC + p.Dc * (len - 1);
                    int endIdx = endR * CrosswordCols + endC;
                    if (startIdx >= 0 && startIdx < CrosswordTotalCells) crosswordHintCellIndices.Add(startIdx);
                    if (endIdx >= 0 && endIdx < CrosswordTotalCells) crosswordHintCellIndices.Add(endIdx);
                }
            }

            if (crosswordCells == null || crosswordCells.Length < size * size) return;
            Font fontFallback = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int idx = r * CrosswordCols + c;
                    if (idx < 0 || idx >= crosswordCells.Length || crosswordCells[idx] == null) continue;
                    if (r >= size || c >= size) continue;
                    if (fontFallback != null) crosswordCells[idx].font = fontFallback;
                    crosswordCells[idx].text = grid[r, c] == '\0' ? "" : "";
                    crosswordCells[idx].fontStyle = FontStyle.Bold;
                    Transform parent = crosswordCells[idx].transform.parent;
                    Image cellBg = parent != null ? parent.GetComponent<Image>() : null;
                    if (cellBg != null) cellBg.color = grid[r, c] == '\0'
                        ? new Color(0.03f, 0.04f, 0.06f, 0.94f)
                        : CrosswordDecoBg(r, c);
                }
            }

            for (int r = 0; r < CrosswordCols; r++)
            {
                for (int c = 0; c < CrosswordCols; c++)
                {
                    if (r < size && c < size) continue;
                    int idx = r * CrosswordCols + c;
                    if (idx < crosswordCells.Length && crosswordCells[idx] != null) crosswordCells[idx].text = "";
                }
            }

            for (int z = 0; z < crosswordCells.Length; z++)
            {
                if (crosswordCells[z] != null) ConfigureGridLetter(crosswordCells[z], CrosswordCellPx);
                if (crosswordButtons != null && z < crosswordButtons.Length && crosswordButtons[z] != null)
                {
                    int rr = z / CrosswordCols;
                    int cc = z % CrosswordCols;
                    bool openCell = rr >= 0 && cc >= 0 && rr < size && cc < size && grid[rr, cc] != '\0';
                    crosswordButtons[z].interactable = openCell;
                }
            }

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int idx = r * CrosswordCols + c;
                    RefreshCrosswordCellDecor(idx);
                }
            }
        }

        private bool TryGetStoredWordPlacement(string wordSanitized, out WordLinePlacement pl)
        {
            pl = default;
            if (string.IsNullOrEmpty(wordSanitized) || currentWordSearchPlacements == null) return false;
            for (int i = 0; i < currentWordSearchPlacements.Count; i++)
            {
                WordLinePlacement p = currentWordSearchPlacements[i];
                string pw = GridThemeBank.SanitizeForGrid(p.Word);
                if (string.Equals(pw, wordSanitized, StringComparison.Ordinal))
                {
                    pl = p;
                    pl.Word = pw;
                    return true;
                }
            }

            return false;
        }

        private bool TryGetStoredScramblePlacement(string wordSanitized, out WordLinePlacement pl)
        {
            pl = default;
            if (string.IsNullOrEmpty(wordSanitized) || currentScramblePlacements == null) return false;
            for (int i = 0; i < currentScramblePlacements.Count; i++)
            {
                WordLinePlacement p = currentScramblePlacements[i];
                string pw = GridThemeBank.SanitizeForGrid(p.Word);
                if (string.Equals(pw, wordSanitized, StringComparison.Ordinal))
                {
                    pl = p;
                    pl.Word = pw;
                    return true;
                }
            }

            return false;
        }

        private bool TryScanGridForWordPlacement(string wordSanitized, out WordLinePlacement pl)
        {
            pl = default;
            if (string.IsNullOrEmpty(wordSanitized) || lastWordSearchGrid == null) return false;
            int n = CrosswordCols;
            int len = wordSanitized.Length;
            if (len < 2) return false;
            int[,] dirs =
            {
                { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 }
            };
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    for (int di = 0; di < 4; di++)
                    {
                        int dr = dirs[di, 0];
                        int dc = dirs[di, 1];
                        bool match = true;
                        for (int i = 0; i < len; i++)
                        {
                            int rr = r + dr * i;
                            int cc = c + dc * i;
                            if (rr < 0 || cc < 0 || rr >= n || cc >= n)
                            {
                                match = false;
                                break;
                            }

                            char gch = char.ToUpperInvariant(lastWordSearchGrid[rr, cc]);
                            if (gch != wordSanitized[i])
                            {
                                match = false;
                                break;
                            }
                        }

                        if (!match) continue;
                        pl = new WordLinePlacement
                        {
                            Word = wordSanitized,
                            StartR = r,
                            StartC = c,
                            Dr = dr,
                            Dc = dc
                        };
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryScanScrambleGridForWordPlacement(string wordSanitized, out WordLinePlacement pl)
        {
            pl = default;
            if (string.IsNullOrEmpty(wordSanitized) || lastScrambleGrid == null) return false;
            int n = ScrambleCols;
            int len = wordSanitized.Length;
            if (len < 2) return false;
            int[,] dirs =
            {
                { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 },
                { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 }
            };
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    for (int di = 0; di < 8; di++)
                    {
                        int dr = dirs[di, 0];
                        int dc = dirs[di, 1];
                        bool match = true;
                        for (int i = 0; i < len; i++)
                        {
                            int rr = r + dr * i;
                            int cc = c + dc * i;
                            if (rr < 0 || cc < 0 || rr >= n || cc >= n)
                            {
                                match = false;
                                break;
                            }

                            char gch = char.ToUpperInvariant(lastScrambleGrid[rr, cc]);
                            if (gch != wordSanitized[i])
                            {
                                match = false;
                                break;
                            }
                        }

                        if (!match) continue;
                        pl = new WordLinePlacement
                        {
                            Word = wordSanitized,
                            StartR = r,
                            StartC = c,
                            Dr = dr,
                            Dc = dc
                        };
                        return true;
                    }
                }
            }

            return false;
        }

        private void ApplyCrosswordFoundWordHighlight(string wordSanitized)
        {
            if (crosswordCells == null || string.IsNullOrEmpty(wordSanitized)) return;
            WordLinePlacement pl;
            if (!TryGetStoredWordPlacement(wordSanitized, out pl) && !TryScanGridForWordPlacement(wordSanitized, out pl))
            {
                return;
            }

            int len = wordSanitized.Length;
            for (int i = 0; i < len; i++)
            {
                int rr = pl.StartR + pl.Dr * i;
                int cc = pl.StartC + pl.Dc * i;
                int idx = rr * CrosswordCols + cc;
                if (idx < 0 || idx >= crosswordCells.Length) continue;
                crosswordFoundCellIndices.Add(idx);
                RefreshCrosswordCellDecor(idx);
            }
        }

        private void ApplyWordScrambleFoundWordHighlight(string wordSanitized)
        {
            if (wordScrambleTiles == null || string.IsNullOrEmpty(wordSanitized)) return;
            WordLinePlacement pl;
            if (!TryGetStoredScramblePlacement(wordSanitized, out pl) && !TryScanScrambleGridForWordPlacement(wordSanitized, out pl))
            {
                return;
            }

            int len = wordSanitized.Length;
            for (int i = 0; i < len; i++)
            {
                int rr = pl.StartR + pl.Dr * i;
                int cc = pl.StartC + pl.Dc * i;
                int idx = rr * ScrambleCols + cc;
                if (idx < 0 || idx >= wordScrambleTiles.Length) continue;
                wordScrambleFoundCellIndices.Add(idx);
                RefreshWordScrambleCellDecor(idx);
            }
        }

        private bool TryRevealHintCellInWordScramble(string wordSanitized, out string pos)
        {
            pos = "";
            if (string.IsNullOrEmpty(wordSanitized) || wordScrambleTiles == null) return false;
            WordLinePlacement pl;
            if (!TryGetStoredScramblePlacement(wordSanitized, out pl) && !TryScanScrambleGridForWordPlacement(wordSanitized, out pl))
            {
                return false;
            }

            int len = wordSanitized.Length;
            for (int i = 1; i < len - 1; i++)
            {
                int rr = pl.StartR + pl.Dr * i;
                int cc = pl.StartC + pl.Dc * i;
                int idx = rr * ScrambleCols + cc;
                if (idx < 0 || idx >= wordScrambleTiles.Length) continue;
                if (wordScrambleFoundCellIndices.Contains(idx) || wordScrambleHintCellIndices.Contains(idx)) continue;
                wordScrambleHintCellIndices.Add(idx);
                RefreshWordScrambleCellDecor(idx);
                pos = (rr + 1) + "," + (cc + 1);
                return true;
            }

            return false;
        }

        private bool TryRevealHintCellInCrossword(string wordSanitized, out string pos)
        {
            pos = "";
            if (string.IsNullOrEmpty(wordSanitized) || crosswordCells == null) return false;
            WordLinePlacement pl;
            if (!TryGetStoredWordPlacement(wordSanitized, out pl) && !TryScanGridForWordPlacement(wordSanitized, out pl))
            {
                return false;
            }

            int len = wordSanitized.Length;
            for (int i = 1; i < len - 1; i++)
            {
                int rr = pl.StartR + pl.Dr * i;
                int cc = pl.StartC + pl.Dc * i;
                int idx = rr * CrosswordCols + cc;
                if (idx < 0 || idx >= crosswordCells.Length) continue;
                if (crosswordFoundCellIndices.Contains(idx) || crosswordHintCellIndices.Contains(idx)) continue;
                crosswordHintCellIndices.Add(idx);
                RefreshCrosswordCellDecor(idx);
                pos = (rr + 1) + "," + (cc + 1);
                return true;
            }

            return false;
        }

        private string BuildCrosswordCluesText(int maxPerAxis)
        {
            if (currentCrosswordClues == null || currentCrosswordClues.Count == 0)
            {
                return GridThemeBank.InGameThemeHintFr(currentGridThemeLabel);
            }

            int cap = Mathf.Clamp(maxPerAxis, 1, 3);
            var h = new System.Text.StringBuilder();
            var v = new System.Text.StringBuilder();
            int hc = 0;
            int vc = 0;
            var ordered = new System.Collections.Generic.List<CrosswordClueEntry>(currentCrosswordClues);
            ordered.Sort((a, b) => a.Number.CompareTo(b.Number));
            for (int i = 0; i < ordered.Count; i++)
            {
                CrosswordClueEntry clue = ordered[i];
                string def = CompactHint(GridThemeBank.BuildCrosswordDefinitionFr(clue.Word, currentGridThemeLabel));
                if (clue.Horizontal)
                {
                    if (hc >= cap) continue;
                    if (hc > 0) h.Append("  ");
                    h.Append(clue.Number).Append(". ").Append(def);
                    hc++;
                }
                else
                {
                    if (vc >= cap) continue;
                    if (vc > 0) v.Append("  ");
                    v.Append(clue.Number).Append(". ").Append(def);
                    vc++;
                }
            }

            if (hc == 0 && vc == 0)
            {
                return GridThemeBank.InGameThemeHintFr(currentGridThemeLabel);
            }

            if (hc == 0) return "V: " + v;
            if (vc == 0) return "H: " + h;
            return "H: " + h + "   |   V: " + v;
        }

        private static string CompactHint(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            string s = raw.Replace("Indice :", "").Trim();
            int dot = s.IndexOf('.', StringComparison.Ordinal);
            if (dot > 14) s = s.Substring(0, dot);
            if (s.Length > 56) s = s.Substring(0, 56).TrimEnd() + "...";
            return s;
        }

        private static string CompactThemeLabel(string theme)
        {
            string s = string.IsNullOrWhiteSpace(theme) ? "Thème Congo" : theme.Trim();
            if (s.Length > 28) s = s.Substring(0, 28).TrimEnd() + "...";
            return s;
        }

        private bool TryMatchAnyUnsolvedGridWord(string rawInput, out string matchedWord)
        {
            matchedWord = "";
            string input = GridThemeBank.SanitizeForGrid(rawInput ?? "");
            if (string.IsNullOrEmpty(input)) return false;
            if (currentGridAllWords == null || currentGridAllWords.Count == 0) return false;

            for (int i = 0; i < currentGridAllWords.Count; i++)
            {
                string candidate = GridThemeBank.SanitizeForGrid(currentGridAllWords[i] ?? "");
                if (string.IsNullOrEmpty(candidate)) continue;
                if (!string.Equals(candidate, input, StringComparison.Ordinal)) continue;
                if (currentGridSolved != null && currentGridSolved.Contains(candidate)) return false;
                matchedWord = candidate;
                return true;
            }

            return false;
        }

        private static Color ScrambleDecoBg(int index)
        {
            Color[] pal =
            {
                new Color(0.07f, 0.07f, 0.08f, 1f),
                new Color(0.1f, 0.1f, 0.12f, 1f),
                new Color(0.08f, 0.08f, 0.1f, 1f)
            };
            if (index < 0) index = 0;
            return pal[index % pal.Length];
        }

        private void RefreshWordScrambleCellDecor(int cellIndex)
        {
            if (wordScrambleTiles == null || cellIndex < 0 || cellIndex >= wordScrambleTiles.Length) return;
            Text tx = wordScrambleTiles[cellIndex];
            if (tx == null) return;
            int r = cellIndex / ScrambleCols;
            int c = cellIndex % ScrambleCols;
            bool openCell = lastScrambleGrid != null
                && r >= 0 && c >= 0
                && r < lastScrambleGrid.GetLength(0)
                && c < lastScrambleGrid.GetLength(1)
                && lastScrambleGrid[r, c] != '\0';

            Image bg = tx.transform.parent != null ? tx.transform.parent.GetComponent<Image>() : null;
            if (bg != null)
            {
                Color baseColor = openCell ? ScrambleDecoBg(cellIndex) : new Color(0.03f, 0.04f, 0.06f, 0.92f);
                if (wordScrambleFoundCellIndices.Contains(cellIndex))
                {
                    baseColor = CrosswordFoundCellBg;
                }
                else if (wordScrambleHintCellIndices.Contains(cellIndex))
                {
                    baseColor = new Color(0.2f, 0.25f, 0.32f, 1f);
                }

                bg.color = baseColor;
                GridCellHoverFeedback hover = bg.GetComponent<GridCellHoverFeedback>();
                if (hover != null) hover.SetBaseColor(baseColor);
            }

            if (!openCell)
            {
                tx.text = "";
                tx.color = new Color(0f, 0f, 0f, 0f);
            }
            else
            {
                tx.text = char.ToUpperInvariant(lastScrambleGrid[r, c]).ToString();
                tx.color = wordScrambleFoundCellIndices.Contains(cellIndex)
                    ? CrosswordFoundLetter
                    : (wordScrambleHintCellIndices.Contains(cellIndex)
                        ? new Color(0.9f, 0.95f, 0.82f, 1f)
                        : new Color(0.74f, 0.78f, 0.84f, 1f));
            }
        }

        private void PlaceScrambleWordInLine(string scrambled, bool[] playableTile)
        {
            if (string.IsNullOrEmpty(scrambled) || wordScrambleTiles == null || playableTile == null)
            {
                return;
            }

            int cols = ScrambleCols;
            int rows = ScrambleCols;
            int[,] dirs =
            {
                { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 },
                { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 }
            };

            for (int attempts = 0; attempts < 80; attempts++)
            {
                int di = UnityEngine.Random.Range(0, 8);
                int dr = dirs[di, 0];
                int dc = dirs[di, 1];
                int startR = UnityEngine.Random.Range(0, rows);
                int startC = UnityEngine.Random.Range(0, cols);
                int endR = startR + dr * (scrambled.Length - 1);
                int endC = startC + dc * (scrambled.Length - 1);
                if (endR < 0 || endC < 0 || endR >= rows || endC >= cols)
                {
                    continue;
                }

                for (int i = 0; i < scrambled.Length; i++)
                {
                    int r = startR + dr * i;
                    int c = startC + dc * i;
                    int idx = r * cols + c;
                    if (idx < 0 || idx >= wordScrambleTiles.Length || wordScrambleTiles[idx] == null)
                    {
                        break;
                    }

                    wordScrambleTiles[idx].text = scrambled[i].ToString();
                    wordScrambleTiles[idx].color = new Color(0.9f, 0.86f, 0.58f, 1f);
                    playableTile[idx] = true;
                }

                return;
            }

            // Fallback robuste si aucun segment trouvé rapidement.
            int need = Mathf.Min(scrambled.Length, wordScrambleTiles.Length);
            for (int i = 0; i < need; i++)
            {
                if (wordScrambleTiles[i] == null) continue;
                wordScrambleTiles[i].text = scrambled[i].ToString();
                wordScrambleTiles[i].color = new Color(0.9f, 0.86f, 0.58f, 1f);
                playableTile[i] = true;
            }
        }

        private void HighlightWordScrambleSolved()
        {
            if (wordScrambleTiles == null || wordScramblePlayableMask == null) return;
            for (int i = 0; i < wordScrambleTiles.Length && i < wordScramblePlayableMask.Length; i++)
            {
                if (!wordScramblePlayableMask[i] || wordScrambleTiles[i] == null) continue;
                Transform p = wordScrambleTiles[i].transform.parent;
                Image bg = p != null ? p.GetComponent<Image>() : null;
                if (bg != null) bg.color = CrosswordFoundCellBg;
                GridCellHoverFeedback hover = p != null ? p.GetComponent<GridCellHoverFeedback>() : null;
                if (hover != null) hover.SetBaseColor(CrosswordFoundCellBg);
                wordScrambleTiles[i].color = CrosswordFoundLetter;
            }
        }

        private IEnumerator CoDelayedAdvanceAfterGridSuccess()
        {
            yield return new WaitForSecondsRealtime(0.38f);
            MaybeAdvanceMiniGameAfterResponse();
        }

        private void ApplyCrosswordDemo()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool liveMode = live != null && live.IsConnected;
            if (crosswordCells == null || crosswordCells.Length < CrosswordTotalCells)
            {
                Debug.LogError("MiniGamePanelContent : crosswordCells non initialisés — vérifie que BuildSecondaryPanels a été exécuté sur ce ModePanelsRoot.");
                return;
            }

            if (currentGridAllWords == null || currentGridSolved == null
                || (currentGridAllWords != null && currentGridAllWords.Count == 0 && gridThematicBlockRound == 0))
            {
                NewThematicSession();
            }

            if (currentGridAllWords == null || currentGridAllWords.Count == 0)
            {
                NewThematicSession();
            }

            if (string.IsNullOrEmpty((CurrentScrambleTargetWord ?? "").Trim()))
            {
                AdvanceGridMotsJeu();
            }

            if (string.IsNullOrEmpty((CurrentScrambleTargetWord ?? "").Trim()) && currentGridAllWords != null
                && currentGridAllWords.Count > 0)
            {
                CurrentScrambleTargetWord = currentGridAllWords[0];
                CurrentScrambleAnswer = CurrentScrambleTargetWord;
            }

            if (crosswordTitle != null)
            {
                int found = currentGridSolved != null ? currentGridSolved.Count : 0;
                int total = currentGridAllWords != null ? currentGridAllWords.Count : 0;
                crosswordTitle.text = "Mots croisés — " + CompactThemeLabel(currentGridThemeLabel)
                    + "  <color=#7EE7FF>[" + found + "/" + total + "]</color>";
            }

            crosswordSelectedCell = -1;
            BuildWordSearchAndFillGrid();
            if (crosswordAnswerInput != null)
            {
                crosswordAnswerInput.text = "";
                crosswordAnswerInput.interactable = !liveMode;
                crosswordAnswerInput.gameObject.SetActive(!liveMode);
            }

            if (crosswordClearButton != null)
            {
                crosswordClearButton.gameObject.SetActive(!liveMode);
                crosswordClearButton.interactable = !liveMode;
            }

            if (crosswordSubmitButton != null)
            {
                crosswordSubmitButton.gameObject.SetActive(!liveMode);
                crosswordSubmitButton.interactable = !liveMode;
            }

            if (crosswordFeedback != null)
            {
                int gTotal = currentGridAllWords != null ? currentGridAllWords.Count : 0;
                crosswordFeedback.text = "Définitions : " + BuildCrosswordCluesText(2)
                    + (liveMode
                        ? "\n<color=#89A2BF>Manche " + gridMotsJeuSessionIndex + "/" + gTotal + " • Mode live: réponds via chat/keyboard.</color>"
                        : "\n<color=#89A2BF>Manche " + gridMotsJeuSessionIndex + "/" + gTotal + " • Tape un mot puis Valider.</color>");
            }

            // Interactabilité des cases gérée par BuildWordSearchAndFillGrid (cases noires bloquées).
        }

        private void ApplyBlindDemo()
        {
            blindListenEndUnscaled = 0f;
            if (blindListenCo != null)
            {
                StopCoroutine(blindListenCo);
                blindListenCo = null;
            }

            if (blindEmojiPulseCo != null)
            {
                StopCoroutine(blindEmojiPulseCo);
                blindEmojiPulseCo = null;
            }

            MiniGameDemoBanks.BlindRound raw = MiniGameDemoBanks.NextBlindRound();
            MiniGameDemoBanks.BlindRound r = MiniGameDemoBanks.ToShuffledDisplay(raw);
            string expected = (raw.Choices != null && raw.CorrectIndex >= 0 && raw.CorrectIndex < raw.Choices.Length)
                ? raw.Choices[raw.CorrectIndex]
                : "";
            string[] displayChoices = r.Choices != null ? (string[])r.Choices.Clone() : new string[4];
            int displayCorrect = r.CorrectIndex;
            if (!string.IsNullOrWhiteSpace(expected))
            {
                int found = -1;
                for (int i = 0; i < displayChoices.Length; i++)
                {
                    if (string.Equals(displayChoices[i], expected, StringComparison.Ordinal))
                    {
                        found = i;
                        break;
                    }
                }
                if (found < 0)
                {
                    if (displayChoices.Length < 4) Array.Resize(ref displayChoices, 4);
                    displayChoices[0] = expected;
                    displayCorrect = 0;
                }
                else
                {
                    displayCorrect = found;
                }
            }
            lastBlindCorrectDisplayIndex = Mathf.Clamp(displayCorrect, 0, Mathf.Max(0, displayChoices.Length - 1));
            string cat = string.IsNullOrEmpty(raw.CategoryLabel) ? "Blind test" : raw.CategoryLabel;
            if (blindTitle != null) blindTitle.text = "Blind test — " + cat;
            if (blindPrompt != null) blindPrompt.text = r.Prompt;
            if (blindSub != null)
            {
                int sec = Mathf.Clamp(Mathf.RoundToInt(blindListenSeconds), 15, 90);
                string hint = string.IsNullOrEmpty(r.SubLine) ? "" : (r.SubLine + "\n");
                blindSub.text = hint
                    + "▶ Écoute l’extrait " + sec
                    + " s, puis choisis A, B, C ou D.";
            }

            if (blindEmoji != null)
            {
                blindEmoji.text = "♪  Écoute  ♪";
                blindEmojiPulseCo = StartCoroutine(CoPulseBlindEmoji());
            }
            string[] letters = { "A", "B", "C", "D" };
            blindMaskedChoiceIndex = displayChoices != null && displayChoices.Length >= 4 ? UnityEngine.Random.Range(0, 4) : -1;
            blindMaskedChoiceValue = blindMaskedChoiceIndex >= 0 && blindMaskedChoiceIndex < displayChoices.Length
                ? (displayChoices[blindMaskedChoiceIndex] ?? "")
                : "";
            for (int i = 0; i < blindChoices.Length && displayChoices != null && i < displayChoices.Length; i++)
            {
                if (blindChoices[i] != null)
                {
                    bool masked = i == blindMaskedChoiceIndex;
                    blindChoices[i].text = letters[i] + ". " + (masked ? "Autre" : displayChoices[i]);
                }
                if (i < blindDisplayedChoices.Length)
                {
                    blindDisplayedChoices[i] = displayChoices[i];
                }
            }

            blindInQuestionPhase = false;
            SetBlindChoicesInteractable(false);
            GameSfxHub.Instance?.PlayBlindDrumCue();
            int musicSeed = (r.Prompt ?? "blind").GetHashCode();
            float listen = blindListenSeconds < 0.5f ? 0f : blindListenSeconds;
            blindListenCo = StartCoroutine(CoBlindHostThenListen(musicSeed, raw, listen));
        }

        private void SetBlindChoicesInteractable(bool on)
        {
            if (blindChoiceButtons == null) return;
            foreach (Button b in blindChoiceButtons)
            {
                if (b == null) continue;
                b.interactable = on;
                Image row = b.GetComponent<Image>();
                if (row != null)
                {
                    row.color = BlindRowIdle;
                }
            }
        }

        private IEnumerator CoBlindHostThenListen(int musicSeed, MiniGameDemoBanks.BlindRound raw, float listen)
        {
            int sec = Mathf.Clamp(Mathf.RoundToInt(listen), 15, 90);
            if (orchestrateHostForBlindAndImage)
            {
                ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
                AIHostManager host = AIHostManager.Instance;
                host?.InterruptSpeech();
                host?.Speak("Blind test. Je présente la règle.");
                host?.Speak("Tu écoutes un extrait pendant " + sec + " secondes, puis tu réponds.");
                if (blindPrompt != null && !string.IsNullOrWhiteSpace(blindPrompt.text))
                {
                    host?.Speak("Question. " + blindPrompt.text);
                }
                if (!string.IsNullOrWhiteSpace(raw.SubLine))
                {
                    host?.Speak("Indice. " + raw.SubLine);
                }
                host?.Speak("Les réponses possibles sont A, B, C ou D.");
                host?.Speak("Je lance maintenant le chrono. Prépare-toi.");
                yield return CoWaitHostSilence(hostSafetyWaitSeconds);
            }

            yield return new WaitForSecondsRealtime(Mathf.Clamp(postHostBeforeCountdownSeconds, 0f, 4f));
            yield return CoPreMusicCountdown(preMusicCountdownSeconds);
            yield return CoBlindListenThenQuestion(musicSeed, raw, listen);
            blindListenCo = null;
        }

        private IEnumerator CoBlindListenThenQuestion(int musicSeed, MiniGameDemoBanks.BlindRound raw, float listen)
        {
            GameAudioManager.Instance?.StopOverlayImmediately();
            GameSfxHub.Instance?.SetBroadcastDuckMultiplier(1f);

            // Blind test: silence total du fond pendant l'écoute.
            ThemeMusicPlayer.Instance?.SetBroadcastDuckMultiplier(0f);
            ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(0f);
            GameSfxHub.Instance?.PlayBlindDemoMusic(musicSeed, raw.AudioFileBase, raw.AudioUrl);
            // On laisse la piste liée à la question finir de charger avant fallback.
            float warmupEnd = Time.unscaledTime + 6f;
            while (GameSfxHub.Instance != null
                && !GameSfxHub.Instance.IsBlindMusicPlaying
                && (GameSfxHub.Instance.IsBlindMusicLoading || Time.unscaledTime < warmupEnd))
            {
                yield return null;
            }

            if (GameSfxHub.Instance != null && !GameSfxHub.Instance.IsBlindMusicPlaying)
            {
                // Si la piste fournie n'est pas lisible, on force immédiatement un stub audible
                // pour conserver le rythme "écoute -> chrono -> réponses".
                GameSfxHub.Instance.PlayBlindDemoMusic(musicSeed, null, null);
                float fallbackWarmupEnd = Time.unscaledTime + 0.8f;
                while (GameSfxHub.Instance != null
                    && !GameSfxHub.Instance.IsBlindMusicPlaying
                    && Time.unscaledTime < fallbackWarmupEnd)
                {
                    yield return null;
                }
            }
            float wait = listen > 0.01f ? listen : 0.35f;
            blindListenWindowSec = wait;
            blindListenEndUnscaled = Time.unscaledTime + wait;
            int total = Mathf.Clamp(Mathf.RoundToInt(wait), 1, 180);
            while (Time.unscaledTime < blindListenEndUnscaled)
            {
                // Ré-applique le mute en continu pour éviter tout retour fond (events externes / autres scripts).
                ThemeMusicPlayer.Instance?.SetBroadcastDuckMultiplier(0f);
                ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(0f);
                float rem = Mathf.Max(0f, blindListenEndUnscaled - Time.unscaledTime);
                int secLeft = Mathf.Max(0, Mathf.CeilToInt(rem));
                if (blindSub != null)
                {
                    blindSub.text = (string.IsNullOrEmpty(raw.SubLine) ? "" : raw.SubLine + "\n")
                        + "Écoute en cours… chrono " + secLeft + " s / " + total + " s.";
                }
                if (blindEmoji != null)
                {
                    blindEmoji.text = "♪  " + secLeft + "s  ♪";
                }
                yield return null;
            }
            blindListenEndUnscaled = 0f;
            GameSfxHub.Instance?.StopBlindDemoMusic();
            ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
            ThemeMusicPlayer.Instance?.SetBroadcastDuckMultiplier(1f);
            GameSfxHub.Instance?.SetBroadcastDuckMultiplier(1f);
            blindInQuestionPhase = false;
            SetBlindChoicesInteractable(false);
            if (blindSub != null)
            {
                blindSub.text = (string.IsNullOrEmpty(raw.SubLine) ? "" : raw.SubLine + "\n")
                    + "L’écoute est terminée. Choisis A, B, C ou D ci-dessous.";
            }

            if (blindEmoji != null) blindEmoji.text = "?  Réponds  ?";
            if (blindUnlockCo != null)
            {
                StopCoroutine(blindUnlockCo);
                blindUnlockCo = null;
            }

            if (orchestrateHostForBlindAndImage)
            {
                blindUnlockCo = StartCoroutine(CoUnlockBlindChoicesAfterHost());
            }
            else
            {
                blindInQuestionPhase = true;
                SetBlindChoicesInteractable(true);
            }
            blindListenCo = null;
        }

        private IEnumerator CoUnlockBlindChoicesAfterHost()
        {
            AIHostManager host = AIHostManager.Instance;
            host?.Speak("L'écoute est terminée.");
            host?.Speak("Tu peux jouer.");
            yield return CoWaitHostSilence(12f);
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, hostPostAuthorizeDelaySeconds));
            blindInQuestionPhase = true;
            SetBlindChoicesInteractable(true);
            blindUnlockCo = null;
        }

        private IEnumerator CoPulseBlindEmoji()
        {
            if (blindEmoji == null) yield break;
            RectTransform rt = blindEmoji.rectTransform;
            Vector3 baseScale = rt != null ? rt.localScale : Vector3.one;
            while (blindEmoji != null && blindEmoji.gameObject.activeInHierarchy)
            {
                float t = Time.unscaledTime * 2.1f;
                float s = 1f + 0.06f * Mathf.Sin(t);
                if (rt != null) rt.localScale = baseScale * s;
                yield return null;
            }
        }

        public void NotifyBlindPick(int choiceIndex)
        {
            if (!blindInQuestionPhase)
            {
                return;
            }

            if (blindListenCo != null)
            {
                StopCoroutine(blindListenCo);
                blindListenCo = null;
            }

            blindListenEndUnscaled = 0f;

            if (blindEmojiPulseCo != null)
            {
                StopCoroutine(blindEmojiPulseCo);
                blindEmojiPulseCo = null;
            }

            if (blindEmoji != null)
            {
                blindEmoji.rectTransform.localScale = Vector3.one;
            }

            GameSfxHub.Instance?.StopBlindDemoMusic();
            ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
            bool ok = choiceIndex == lastBlindCorrectDisplayIndex;
            blindInQuestionPhase = false;
            SetBlindChoicesInteractable(false);
            // "Autre" : dévoile uniquement si c'est la bonne réponse.
            bool pickedMasked = choiceIndex == blindMaskedChoiceIndex;
            if (pickedMasked && ok && blindChoices != null && choiceIndex >= 0 && choiceIndex < blindChoices.Length && blindChoices[choiceIndex] != null)
            {
                string[] letters = { "A", "B", "C", "D" };
                string reveal = string.IsNullOrWhiteSpace(blindMaskedChoiceValue) ? blindDisplayedChoices[choiceIndex] : blindMaskedChoiceValue;
                blindChoices[choiceIndex].text = letters[Mathf.Clamp(choiceIndex, 0, 3)] + ". " + reveal;
            }

            if (blindChoiceButtons != null && choiceIndex >= 0 && choiceIndex < blindChoiceButtons.Length)
            {
                Image row = blindChoiceButtons[choiceIndex] != null ? blindChoiceButtons[choiceIndex].GetComponent<Image>() : null;
                if (row != null)
                {
                    row.color = ok ? new Color(0.06f, 0.55f, 0.24f, 0.96f) : new Color(0.55f, 0.14f, 0.14f, 0.96f);
                }
            }

            GameSfxHub.Instance?.PlayResult(ok);
            MaybeAdvanceMiniGameAfterResponse();
        }

        private void ApplyMysteryDemo()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool liveMode = live != null && live.IsConnected;
            string w = MiniGameDemoBanks.NextMysteryWord();
            CurrentMysteryAnswer = (w ?? "CONGO").Trim().ToUpperInvariant();
            int len = CurrentMysteryAnswer.Length;
            string level = len <= 5 ? "Niveau 1/3" : (len <= 7 ? "Niveau 2/3" : "Niveau 3/3");
            if (mysteryTitle != null) mysteryTitle.text = "Mot mystère — " + level;
            if (mysteryMask != null) mysteryMask.text = MiniGameDemoBanks.MysteryDisplayLine(w);

            if (mysteryAnswerInput != null)
            {
                mysteryAnswerInput.text = "";
                mysteryAnswerInput.interactable = true;
                mysteryAnswerInput.gameObject.SetActive(true);
            }

            if (mysteryClearButton != null)
            {
                mysteryClearButton.gameObject.SetActive(true);
                mysteryClearButton.interactable = true;
            }

            if (mysterySubmitButton != null)
            {
                mysterySubmitButton.gameObject.SetActive(true);
                mysterySubmitButton.interactable = true;
            }

            if (mysteryFeedback != null)
            {
                string hint = MysteryHints.TryGetValue(CurrentMysteryAnswer, out string h) ? h : "Observe les lettres affichées.";
                mysteryFeedback.text = liveMode
                    ? "Indice: " + hint + " Réponds dans le chat."
                    : "Indice: " + hint;
            }
        }

        private void ApplyMemoryDemo()
        {
            if (memoryTitle != null) memoryTitle.text = "Mémoire — paires Congo";
            if (memorySubtitle != null)
            {
                memorySubtitle.text = "Trouve les paires CO/NG/MB/RU. Deux cartes identiques restent ouvertes.";
            }

            memorySeed = UnityEngine.Random.Range(0, 99999);
            System.Random rng = new System.Random(memorySeed);
            string[] deck = (string[])memoryPairLetters.Clone();
            for (int i = deck.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (deck[i], deck[j]) = (deck[j], deck[i]);
            }

            memoryDeckOrder = deck;
            memoryFirstPickIndex = -1;
            if (memoryMismatchCo != null)
            {
                StopCoroutine(memoryMismatchCo);
                memoryMismatchCo = null;
            }

            for (int i = 0; i < memoryCardMatched.Length; i++)
            {
                memoryCardMatched[i] = false;
            }

            for (int i = 0; i < memoryCards.Length && i < deck.Length; i++)
            {
                int idx = i;
                Button b = memoryCards[i];
                if (b == null) continue;
                Text tx = b.GetComponentInChildren<Text>();
                if (tx != null)
                {
                    tx.text = "?";
                    tx.fontSize = 46;
                    tx.fontStyle = FontStyle.Bold;
                }

                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnMemoryCardClicked(idx, tx));
            }
        }

        private void OnMemoryCardClicked(int index, Text tx)
        {
            if (tx == null || memoryDeckOrder == null || index < 0 || index >= memoryDeckOrder.Length)
            {
                return;
            }

            if (memoryMismatchCo != null)
            {
                return;
            }

            if (memoryCardMatched[index])
            {
                return;
            }

            if (tx.text != "?" && memoryFirstPickIndex >= 0 && memoryFirstPickIndex != index)
            {
                return;
            }

            GameSfxHub.Instance?.PlayTap();

            if (memoryFirstPickIndex < 0)
            {
                memoryFirstPickIndex = index;
                tx.text = memoryDeckOrder[index];
                if (memorySubtitle != null)
                {
                    memorySubtitle.text = "Bien. Ouvre une 2e carte pour chercher la paire.";
                }

                return;
            }

            if (memoryFirstPickIndex == index)
            {
                return;
            }

            int first = memoryFirstPickIndex;
            memoryFirstPickIndex = -1;
            Text firstTx = memoryCards != null && first < memoryCards.Length && memoryCards[first] != null
                ? memoryCards[first].GetComponentInChildren<Text>()
                : null;
            tx.text = memoryDeckOrder[index];
            bool match = memoryDeckOrder[first] == memoryDeckOrder[index];
            if (match)
            {
                memoryCardMatched[first] = true;
                memoryCardMatched[index] = true;
                GameSfxHub.Instance?.PlayResult(true);
                int openLeft = 0;
                for (int k = 0; k < memoryCardMatched.Length; k++)
                {
                    if (!memoryCardMatched[k]) openLeft++;
                }

                int pairsLeft = openLeft / 2;
                if (memorySubtitle != null)
                {
                    memorySubtitle.text = openLeft == 0
                        ? "Toutes les paires trouvées — bravo !"
                        : "Paire trouvée. Reste " + pairsLeft + " paire(s).";
                }

                if (openLeft == 0)
                {
                    MaybeAdvanceMiniGameAfterResponse();
                }

                return;
            }

            if (memorySubtitle != null)
            {
                memorySubtitle.text = "Pas la même. Les cartes se referment.";
            }

            memoryMismatchCo = StartCoroutine(CoMemoryFlipBack(first, index, firstTx, tx));
        }

        private IEnumerator CoMemoryFlipBack(int i1, int i2, Text t1, Text t2)
        {
            yield return new WaitForSeconds(0.65f);
            if (t1 != null && !memoryCardMatched[i1]) t1.text = "?";
            if (t2 != null && !memoryCardMatched[i2]) t2.text = "?";
            memoryMismatchCo = null;
            if (memorySubtitle != null)
            {
                memorySubtitle.text = "Trouve les paires CO/NG/MB/RU.";
            }
        }

        private void ApplyChronoDemo()
        {
            EnsureChronoSubMeta();
            chronoModeActive = true;
            chronoSessionScore = 0;
            chronoStreak = 0;
            chronoRoundInSession = 0;
            if (chronoTitle != null) chronoTitle.text = "Chrono vitesse — 3 vagues";

            StartChronoNewRound(1);
        }

        public void StartChronoNewRound(int roundN)
        {
            chronoModeActive = true;
            chronoRoundInSession = Mathf.Clamp(roundN, 1, ChronoRoundsPerSession);
            chronoTargetSlot = UnityEngine.Random.Range(0, 4);
            chronoPlayWindowSec = 2.1f + (chronoRoundInSession * 0.45f) - (chronoStreak * 0.06f);
            chronoPlayWindowSec = Mathf.Clamp(chronoPlayWindowSec, 1.5f, 3.2f);
            chronoResultFlash = null;
            chronoPhase = 0;
            chronoCountdownIndex = 0;
            chronoStateUntil = Time.unscaledTime + 0.5f;
            if (chronoMeta != null)
            {
                chronoMeta.text = "Manche " + chronoRoundInSession + " / " + ChronoRoundsPerSession
                    + "  ·  Session " + chronoSessionScore + " pts";
            }
        }

        public void OnChronoInput(int slot) // 0..3, ou <0 = temps écoulé
        {
            if (chronoInputLockedByHost)
            {
                return;
            }

            if (!chronoModeActive || chronoPhase != 1) return;
            if (slot > 3) return;
            bool timeUp = (slot < 0);
            if (timeUp)
            {
                chronoStreak = 0;
                chronoLastRoundPoints = 0;
                chronoResultFlash = "Temps écoulé. C'était " + (1 + chronoTargetSlot) + " (touches 1-4).";
                // Pas de buzz « faux » ni punchline négative : l’utilisateur n’a pas saisi d’action volontaire.
                GameSfxHub.Instance?.PlayResult(false, hostVoiceCommentary: false, neutralNoWrongTone: true);
                chronoPhase = 2;
                chronoStateUntil = Time.unscaledTime + 0.8f;
                return;
            }

            bool hit = (slot == chronoTargetSlot);
            float tLeft = Mathf.Max(0f, chronoStateUntil - Time.unscaledTime);
            int speedBonus = hit ? Mathf.FloorToInt(tLeft / chronoPlayWindowSec * 18f) : 0;
            chronoLastRoundPoints = 0;
            if (hit)
            {
                chronoStreak++;
                chronoLastRoundPoints = 20 + (chronoStreak * 2) + speedBonus;
                chronoSessionScore += chronoLastRoundPoints;
                if (ScoreManager.Instance != null)
                {
                    string u = PlayerProfileStore.ScoreUsernameForLocalPlay() ?? "démo";
                    ScoreManager.Instance.AddPoints(u, chronoLastRoundPoints);
                }
            }
            else
            {
                chronoStreak = 0;
            }

            if (hit)
            {
                GameSfxHub.Instance?.PlayResult(true);
                chronoResultFlash = "Touché ! +" + chronoLastRoundPoints
                    + (speedBonus > 0 ? " (bonus vitesse)" : "");
            }
            else
            {
                GameSfxHub.Instance?.PlayResult(false);
                chronoResultFlash = "Raté ! c’était " + (1 + chronoTargetSlot) + " / 4";
            }

            chronoPhase = 2;
            chronoStateUntil = Time.unscaledTime + 0.7f;
        }

        private void ChronoEndSession()
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm != null && !GameModeManager.IsLiveTikTokConnected() && gmm.RoundTimeRemaining > 0.05f)
            {
                if (chronoTitle != null)
                {
                    chronoTitle.text = "Chrono vitesse — série suivante";
                }

                if (chronoSub != null)
                {
                    chronoSub.text = "Le temps de session continue : on relance une série.";
                }

                StartChronoNewRound(1);
                return;
            }

            chronoModeActive = false;
            if (chronoSessionScore > 0)
            {
                ScoreHistoryStore.RegisterHighWaterIfNeeded(chronoSessionScore);
            }

            if (chronoTitle != null) chronoTitle.text = "Chrono vitesse — fin !";
            if (chronoSub != null) chronoSub.text = "Total : " + chronoSessionScore + " pts. Prochain mini-jeu…";
            if (chronoBig != null) chronoBig.text = "★ " + chronoSessionScore;
            if (chronoMeta != null) chronoMeta.text = "Manche chrono terminée.";
            chronoPhase = 3;
            chronoStateUntil = Time.unscaledTime + 0.5f;
            GameModeManager.Instance?.ScheduleNextMode(1.4f);
        }

        public void ChronoUpdateUi()
        {
            if (!chronoModeActive || chronoBig == null) return;
            if (chronoSub == null) EnsureChronoSubMeta();
            if (chronoSub == null) return;
            if (chronoPhase == 0)
            {
                if (chronoCountdownIndex < 3)
                {
                    chronoBig.text = (3 - chronoCountdownIndex).ToString();
                }
                else if (chronoCountdownIndex == 3)
                {
                    chronoBig.text = "GO !";
                }
                else
                {
                    chronoBig.text = "3";
                }

                chronoSub.text = "3 · 2 · 1 — la cible reste cachée jusqu’au GO";
                if (chronoInstruction != null)
                {
                    chronoInstruction.text = LiaPunchlineBank.ModeRulesOneLiner("speed-chrono");
                }
            }
            else if (chronoPhase == 1)
            {
                float tLeft = Mathf.Max(0f, chronoStateUntil - Time.unscaledTime);
                int ceilSec = Mathf.CeilToInt(tLeft);
                if (ceilSec != chronoTimerAudioLastCeilSec && ceilSec >= 0 && ceilSec < 120)
                {
                    chronoTimerAudioLastCeilSec = ceilSec;
                    GameAudioManager.Instance?.OnTimerTick();
                }

                if (tLeft <= 0.48f && !chronoTimerUrgentPlayed)
                {
                    chronoTimerUrgentPlayed = true;
                    GameAudioManager.Instance?.OnTimerUrgent();
                }

                chronoBig.text = string.Format("{0:0.0}", tLeft);
                chronoSub.text = "Cible 1-4  ·  " + tLeft.ToString("0.0") + " s";
                if (chronoInstruction != null)
                {
                    chronoInstruction.text = LiaPunchlineBank.ModeRulesOneLiner("speed-chrono")
                        + "\n\nEn live : envoie 1, 2, 3 ou 4 dans le chat. Local : pavé 1-4 ou touches du haut.";
                }
            }
            else if (chronoPhase == 2)
            {
                if (chronoSub != null && !string.IsNullOrEmpty(chronoResultFlash))
                {
                    chronoSub.text = chronoResultFlash;
                }
            }
        }

        public void EnsureChronoIfNeeded()
        {
            EnsureChronoSubMeta();
        }

        public void EndChronoMode()
        {
            if (!chronoModeActive) return;
            chronoModeActive = false;
        }

        private void EnsureChronoSubMeta()
        {
            if (chronoSub != null && chronoMeta != null) return;
            Transform host = chronoTitle != null ? chronoTitle.transform.parent : null;
            if (host == null) return;
            Font font = chronoTitle != null
                ? chronoTitle.font
                : (Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                    ?? Resources.GetBuiltinResource<Font>("Arial.ttf"));
            if (chronoSub == null)
            {
                GameObject sgo = new GameObject("ChronoSub");
                sgo.transform.SetParent(host, false);
                RectTransform srt = sgo.AddComponent<RectTransform>();
                srt.anchorMin = new Vector2(0.5f, 0.5f);
                srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.anchoredPosition = new Vector2(0f, 44f);
                srt.sizeDelta = new Vector2(880f, 72f);
                Text t = sgo.AddComponent<Text>();
                t.font = font;
                t.fontSize = 22;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(0.9f, 0.9f, 0.88f, 0.95f);
                t.raycastTarget = false;
                chronoSub = t;
            }

            if (chronoMeta == null)
            {
                GameObject mgo = new GameObject("ChronoMeta");
                mgo.transform.SetParent(host, false);
                RectTransform mrt = mgo.AddComponent<RectTransform>();
                mrt.anchorMin = new Vector2(0.5f, 0.5f);
                mrt.anchorMax = new Vector2(0.5f, 0.5f);
                mrt.pivot = new Vector2(0.5f, 0.5f);
                mrt.anchoredPosition = new Vector2(0f, -52f);
                mrt.sizeDelta = new Vector2(880f, 40f);
                Text t = mgo.AddComponent<Text>();
                t.font = font;
                t.fontSize = 18;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(0.7f, 0.9f, 0.7f, 0.9f);
                t.raycastTarget = false;
                chronoMeta = t;
            }
        }

        /// <summary>Chrono vitesse actif (réaction 1–4) : le minuteur rond en bas s’affiche ailleurs pour éviter la superposition.</summary>
        public bool IsChronoRoundActive => chronoModeActive;

        /// <summary>HUD : pendant l’écoute blind ou la révélation image, le cercle compte le temps restant (pas le bloc 120 s).</summary>
        public bool TryGetHudCountdownOverride(out float fill01, out int secondsCeil)
        {
            fill01 = 0f;
            secondsCeil = 0;
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null) return false;
            string id = gmm.ActiveModeId ?? "";
            if (string.Equals(id, "blind-test", StringComparison.Ordinal) && blindListenEndUnscaled > 0.1f)
            {
                float rem = blindListenEndUnscaled - Time.unscaledTime;
                if (rem > 0.001f)
                {
                    float d = Mathf.Max(0.01f, blindListenWindowSec);
                    fill01 = Mathf.Clamp01(rem / d);
                    secondsCeil = Mathf.Max(0, Mathf.CeilToInt(rem));
                    return true;
                }
            }

            if (string.Equals(id, "image-guess", StringComparison.Ordinal) && imageRevealEndUnscaled > 0.1f)
            {
                float rem = imageRevealEndUnscaled - Time.unscaledTime;
                if (rem > 0.001f)
                {
                    float d = Mathf.Max(0.01f, imageRevealWindowSec);
                    fill01 = Mathf.Clamp01(rem / d);
                    secondsCeil = Mathf.Max(0, Mathf.CeilToInt(rem));
                    return true;
                }
            }

            return false;
        }

        private void Update()
        {
            TickLiveKeyboardDraft();
            SemanticTick();
            if (GameInput.DigitKeyDown1To9(1)) OnChronoInput(0);
            else if (GameInput.DigitKeyDown1To9(2)) OnChronoInput(1);
            else if (GameInput.DigitKeyDown1To9(3)) OnChronoInput(2);
            else if (GameInput.DigitKeyDown1To9(4)) OnChronoInput(3);

            ChronoTick();
        }

        private void TickLiveKeyboardDraft()
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null) return;
            string modeId = gmm.ActiveModeId ?? "";
            bool supported = string.Equals(modeId, "word-scramble", StringComparison.Ordinal)
                             || string.Equals(modeId, "crossword-lite", StringComparison.Ordinal)
                             || string.Equals(modeId, "semantic", StringComparison.Ordinal);
            if (!supported) return;

            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool liveConnected = live != null && live.IsConnected;
            if (!liveConnected)
            {
                liveTypedDraft = "";
                liveTypedModeId = "";
                return;
            }

            if (!string.Equals(liveTypedModeId, modeId, StringComparison.Ordinal))
            {
                liveTypedModeId = modeId;
                liveTypedDraft = "";
            }

            AppendLiveDraftFromKeyboard();
            bool submit = IsSubmitPressedThisFrame();
            if (submit)
            {
                SubmitLiveDraft(modeId);
                return;
            }

            if (!string.IsNullOrEmpty(liveTypedDraft))
            {
                RefreshLiveDraftPreview(modeId);
            }
        }

        private void AppendLiveDraftFromKeyboard()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.backspaceKey.wasPressedThisFrame && liveTypedDraft.Length > 0)
            {
                liveTypedDraft = liveTypedDraft.Substring(0, liveTypedDraft.Length - 1);
            }

            if (liveTypedDraft.Length >= Mathf.Max(6, liveTypedMaxChars)) return;

            // Priorité au caractère localisé (AZERTY/QWERTY) via displayName,
            // avec fallback sur keyCode uniquement pour les chiffres.
            TryAppendLetter(kb.aKey, 'A');
            TryAppendLetter(kb.bKey, 'B');
            TryAppendLetter(kb.cKey, 'C');
            TryAppendLetter(kb.dKey, 'D');
            TryAppendLetter(kb.eKey, 'E');
            TryAppendLetter(kb.fKey, 'F');
            TryAppendLetter(kb.gKey, 'G');
            TryAppendLetter(kb.hKey, 'H');
            TryAppendLetter(kb.iKey, 'I');
            TryAppendLetter(kb.jKey, 'J');
            TryAppendLetter(kb.kKey, 'K');
            TryAppendLetter(kb.lKey, 'L');
            TryAppendLetter(kb.mKey, 'M');
            TryAppendLetter(kb.nKey, 'N');
            TryAppendLetter(kb.oKey, 'O');
            TryAppendLetter(kb.pKey, 'P');
            TryAppendLetter(kb.qKey, 'Q');
            TryAppendLetter(kb.rKey, 'R');
            TryAppendLetter(kb.sKey, 'S');
            TryAppendLetter(kb.tKey, 'T');
            TryAppendLetter(kb.uKey, 'U');
            TryAppendLetter(kb.vKey, 'V');
            TryAppendLetter(kb.wKey, 'W');
            TryAppendLetter(kb.xKey, 'X');
            TryAppendLetter(kb.yKey, 'Y');
            TryAppendLetter(kb.zKey, 'Z');
            TryAppendDigit(kb.digit0Key, '0');
            TryAppendDigit(kb.digit1Key, '1');
            TryAppendDigit(kb.digit2Key, '2');
            TryAppendDigit(kb.digit3Key, '3');
            TryAppendDigit(kb.digit4Key, '4');
            TryAppendDigit(kb.digit5Key, '5');
            TryAppendDigit(kb.digit6Key, '6');
            TryAppendDigit(kb.digit7Key, '7');
            TryAppendDigit(kb.digit8Key, '8');
            TryAppendDigit(kb.digit9Key, '9');
            TryAppendDigit(kb.numpad0Key, '0');
            TryAppendDigit(kb.numpad1Key, '1');
            TryAppendDigit(kb.numpad2Key, '2');
            TryAppendDigit(kb.numpad3Key, '3');
            TryAppendDigit(kb.numpad4Key, '4');
            TryAppendDigit(kb.numpad5Key, '5');
            TryAppendDigit(kb.numpad6Key, '6');
            TryAppendDigit(kb.numpad7Key, '7');
            TryAppendDigit(kb.numpad8Key, '8');
            TryAppendDigit(kb.numpad9Key, '9');
        }

        private void TryAppendLetter(KeyControl key, char fallback)
        {
            if (key == null || !key.wasPressedThisFrame) return;
            if (liveTypedDraft.Length >= Mathf.Max(6, liveTypedMaxChars)) return;
            char c = ResolveLocalizedLetter(key.displayName, fallback);
            if (c == '\0') return;
            liveTypedDraft += c;
        }

        private void TryAppendDigit(KeyControl key, char digit)
        {
            if (key == null || !key.wasPressedThisFrame) return;
            if (liveTypedDraft.Length >= Mathf.Max(6, liveTypedMaxChars)) return;
            liveTypedDraft += digit;
        }

        private static char ResolveLocalizedLetter(string displayName, char fallback)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                char c = displayName.Trim()[0];
                if (char.IsLetter(c)) return char.ToUpperInvariant(c);
            }
            return fallback;
        }

        private static bool IsSubmitPressedThisFrame()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return false;
            return kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame;
        }

        private static char KeyToAlphaNumeric(string displayName, Key keyCode)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                string d = displayName.Trim();
                if (d.Length > 0)
                {
                    char raw = d[0];
                    if (char.IsLetter(raw)) return char.ToUpperInvariant(raw);
                    if (char.IsDigit(raw)) return raw;
                }
            }

            return KeyCodeToAlphaNumeric(keyCode);
        }

        private static char KeyCodeToAlphaNumeric(Key key)
        {
            return key switch
            {
                Key.A => 'A',
                Key.B => 'B',
                Key.C => 'C',
                Key.D => 'D',
                Key.E => 'E',
                Key.F => 'F',
                Key.G => 'G',
                Key.H => 'H',
                Key.I => 'I',
                Key.J => 'J',
                Key.K => 'K',
                Key.L => 'L',
                Key.M => 'M',
                Key.N => 'N',
                Key.O => 'O',
                Key.P => 'P',
                Key.Q => 'Q',
                Key.R => 'R',
                Key.S => 'S',
                Key.T => 'T',
                Key.U => 'U',
                Key.V => 'V',
                Key.W => 'W',
                Key.X => 'X',
                Key.Y => 'Y',
                Key.Z => 'Z',
                Key.Digit0 or Key.Numpad0 => '0',
                Key.Digit1 or Key.Numpad1 => '1',
                Key.Digit2 or Key.Numpad2 => '2',
                Key.Digit3 or Key.Numpad3 => '3',
                Key.Digit4 or Key.Numpad4 => '4',
                Key.Digit5 or Key.Numpad5 => '5',
                Key.Digit6 or Key.Numpad6 => '6',
                Key.Digit7 or Key.Numpad7 => '7',
                Key.Digit8 or Key.Numpad8 => '8',
                Key.Digit9 or Key.Numpad9 => '9',
                _ => '\0'
            };
        }

        private void SubmitLiveDraft(string modeId)
        {
            string draft = liveTypedDraft ?? "";
            if (string.IsNullOrWhiteSpace(draft))
            {
                RefreshLiveDraftPreview(modeId);
                return;
            }

            if (string.Equals(modeId, "semantic", StringComparison.Ordinal))
            {
                if (semanticAnswerInput != null) semanticAnswerInput.text = draft;
                OnSemanticSubmit(this);
            }
            else if (string.Equals(modeId, "word-scramble", StringComparison.Ordinal))
            {
                if (wordAnswerInput != null) wordAnswerInput.text = draft;
                OnWordSubmit(this);
            }
            else if (string.Equals(modeId, "crossword-lite", StringComparison.Ordinal))
            {
                if (crosswordAnswerInput != null) crosswordAnswerInput.text = draft;
                OnCrosswordSubmit(this);
            }

            liveTypedDraft = "";
        }

        private void RefreshLiveDraftPreview(string modeId)
        {
            string draft = string.IsNullOrEmpty(liveTypedDraft) ? "…" : liveTypedDraft;
            if (string.Equals(modeId, "semantic", StringComparison.Ordinal))
            {
                if (semanticFeedback != null)
                {
                    semanticFeedback.text = "<color=#8FD7FF>Live saisie : " + draft + "</color>  <color=#9DB4C7>(Entrée = valider)</color>";
                }
            }
            else if (string.Equals(modeId, "word-scramble", StringComparison.Ordinal))
            {
                if (wordFeedback != null)
                {
                    wordFeedback.text = "<color=#8FD7FF>Live saisie : " + draft + "</color>  <color=#9DB4C7>(Entrée = valider)</color>";
                }
            }
            else if (string.Equals(modeId, "crossword-lite", StringComparison.Ordinal))
            {
                if (crosswordFeedback != null)
                {
                    int gTotal = currentGridAllWords != null ? currentGridAllWords.Count : 0;
                    crosswordFeedback.text = "Définitions : " + BuildCrosswordCluesText(2)
                        + "\n<color=#8FD7FF>Live saisie : " + draft + "</color>  <color=#9DB4C7>(Entrée = valider)</color>"
                        + "\n<color=#89A2BF>Manche " + gridMotsJeuSessionIndex + "/" + gTotal + " • Mode live</color>";
                }
            }
        }

        private void ChronoTick()
        {
            if (!chronoModeActive) return;
            if (Time.unscaledTime < chronoStateUntil)
            {
                ChronoUpdateUi();
                return;
            }

            if (chronoPhase == 0)
            {
                chronoCountdownIndex++;
                if (chronoCountdownIndex <= 2)
                {
                    chronoStateUntil = Time.unscaledTime + 0.42f;
                }
                else if (chronoCountdownIndex == 3)
                {
                    chronoStateUntil = Time.unscaledTime + 0.3f;
                }
                else
                {
                    chronoPhase = 1;
                    chronoStateUntil = Time.unscaledTime + chronoPlayWindowSec;
                }

                ChronoUpdateUi();
                return;
            }

            if (chronoPhase == 1)
            {
                OnChronoInput(-1);
                ChronoUpdateUi();
                return;
            }

            if (chronoPhase == 2)
            {
                if (chronoRoundInSession < ChronoRoundsPerSession)
                {
                    StartChronoNewRound(chronoRoundInSession + 1);
                }
                else
                {
                    ChronoEndSession();
                }

                ChronoUpdateUi();
            }
        }

        private IEnumerator CoApplyImageDemo()
        {
            imageRevealEndUnscaled = 0f;
            currentImageVisualUnusable = false;
            GameSfxHub.Instance?.StopBlindDemoMusic();
            if (imageOrchestrationCo != null)
            {
                StopCoroutine(imageOrchestrationCo);
                imageOrchestrationCo = null;
            }
            if (imageRevealCo != null)
            {
                StopCoroutine(imageRevealCo);
                imageRevealCo = null;
            }

            Texture2D tex = null;
            const int maxImageAttempts = 5;
            for (int attempt = 0; attempt < maxImageAttempts; attempt++)
            {
                CurrentImageGuessRound = MiniGameDemoBanks.NextImageGuessRound();
                Texture2D candidate = null;
                yield return ImageGuessVisuals.CoResolveTexture(
                    CurrentImageGuessRound.StreamingFileBase,
                    CurrentImageGuessRound.StyleSeed,
                    t => candidate = t);
                bool unusable = ImageGuessVisuals.IsLikelyUnusableVisual(candidate);
                if (!unusable || attempt == maxImageAttempts - 1)
                {
                    tex = candidate;
                    currentImageVisualUnusable = unusable;
                    break;
                }

                if (candidate != null)
                {
                    Destroy(candidate);
                }
            }

            if (imageTitle != null) imageTitle.text = "Devine l’image — Congo";
            if (imageCaption != null)
            {
                imageCaption.text = CurrentImageGuessRound.Hint ?? "";
            }

            if (imageGuessInput != null)
            {
                imageGuessInput.text = "";
                imageGuessInput.interactable = true;
            }

            if (imageGuessFeedback != null) imageGuessFeedback.text = "";
            if (imageGuessSubmit != null) imageGuessSubmit.interactable = false;

            if (imagePlaceholder != null)
            {
                Texture2D old = imagePlaceholder.texture as Texture2D;
                imagePlaceholder.texture = null;
                if (old != null)
                {
                    Destroy(old);
                }

                imagePlaceholder.texture = tex;
                imagePlaceholder.uvRect = new Rect(0f, 0f, 1f, 1f);
                imagePlaceholder.color = new Color(1f, 1f, 1f, ImageGuessRevealingAlpha);
                if (imageGuessVeil == null)
                {
                    imageGuessVeil = EnsureImageGuessVeil();
                }

                if (imageGuessVeil != null)
                {
                    imageGuessVeil.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
                }
            }

            imageOrchestrationCo = StartCoroutine(CoImageHostThenRevealAndMusic());
        }

        private IEnumerator CoImageHostThenRevealAndMusic()
        {
            string hint = CurrentImageGuessRound.Hint ?? "";
            bool playLinkedAudio = ShouldPlayImageGuessAudio(CurrentImageGuessRound) && !currentImageVisualUnusable;
            if (orchestrateHostForBlindAndImage)
            {
                ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
                AIHostManager host = AIHostManager.Instance;
                host?.InterruptSpeech();
                host?.Speak("Image devinette. La question porte d'abord sur l'image affichée.");
                host?.Speak(playLinkedAudio
                    ? "Observe l'image et écoute bien l'extrait lié à cette image."
                    : "Observe l'image. Cette manche se joue sans musique car l'image n'est pas assez fiable ou non liée à un extrait.");
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    host?.Speak("Question: " + hint);
                }
                if (playLinkedAudio)
                {
                    host?.Speak("La musique indice démarre dans trois secondes et s'arrêtera automatiquement.");
                }
                yield return CoWaitHostSilence(hostSafetyWaitSeconds);
            }

            if (playLinkedAudio)
            {
                yield return CoPreMusicCountdown(preMusicCountdownSeconds);
                int seed = (CurrentImageGuessRound.Hint ?? "image").GetHashCode();
                GameAudioManager.Instance?.StopOverlayImmediately();
                ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(0f);
                GameSfxHub.Instance?.PlayBlindDemoMusic(seed, CurrentImageGuessRound.AudioFileBase, CurrentImageGuessRound.AudioUrl);
                imageMusicHintCo = StartCoroutine(CoStopImageMusicHintAfter(ImageGuessMusicClueSec));
            }
            else
            {
                GameSfxHub.Instance?.StopBlindDemoMusic();
                ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
            }

            imageRevealCo = StartCoroutine(CoImageRevealUnblur(imageGuessRevealSec));
            yield return imageRevealCo;
            if (playLinkedAudio && imageMusicHintCo == null)
            {
                ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
            }
            if (orchestrateHostForBlindAndImage)
            {
                AIHostManager.Instance?.Speak("Tu peux maintenant répondre à la question en te basant sur l'image. La musique était seulement un indice.");
            }
            imageOrchestrationCo = null;
        }

        private IEnumerator CoStopImageMusicHintAfter(float sec)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, sec));
            GameSfxHub.Instance?.StopBlindDemoMusic();
            ThemeMusicPlayer.Instance?.SetBlindDuckMultiplier(1f);
            imageMusicHintCo = null;
        }

        private static bool ShouldPlayImageGuessAudio(MiniGameDemoBanks.ImageGuessRound round)
        {
            if (string.IsNullOrWhiteSpace(round.AudioFileBase) && string.IsNullOrWhiteSpace(round.AudioUrl))
            {
                return false;
            }

            string trivia = round.Trivia ?? "";
            string triviaLower = trivia.ToLowerInvariant();
            if (triviaLower.Contains("[category:"))
            {
                // Si une catégorie explicite existe, on applique la règle stricte.
                return triviaLower.Contains("[category:music_related]");
            }

            string corpus = ((round.Hint ?? "") + " " + trivia + " " + (round.AnswerKey ?? "") + " " + (round.StreamingFileBase ?? ""))
                .ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(corpus)) return false;

            string[] positive =
            {
                "chanteur", "chanteuse", "artiste", "groupe", "musique", "chanson",
                "titre", "album", "feat", "featuring", "compositeur", "producteur",
                "interpr", "audio", "clip", "rumba", "ndombolo", "orchestre"
            };
            foreach (string k in positive)
            {
                if (corpus.Contains(k))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator CoPreMusicCountdown(float seconds)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(seconds), 1, 5);
            for (int i = count; i >= 1; i--)
            {
                if (blindEmoji != null && blindEmoji.gameObject.activeInHierarchy)
                {
                    blindEmoji.text = i.ToString();
                }

                if (imageCaption != null && imageCaption.gameObject.activeInHierarchy)
                {
                    imageCaption.text = "Démarrage de la musique dans " + i + "…";
                }

                GameSfxHub.Instance?.PlayChronoTick(0.52f);
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        private IEnumerator CoWaitHostSilence(float maxSec)
        {
            AIHostManager host = AIHostManager.Instance;
            if (host == null) yield break;
            float end = Time.unscaledTime + Mathf.Max(1f, maxSec);
            while (host != null && host.IsSpeakingNow && Time.unscaledTime < end)
            {
                yield return null;
            }

            // Si la voix ne se termine pas correctement (réseau/TTS), on force l'arrêt
            // pour garantir la règle: pas de chrono tant que l'IA n'a pas fini.
            if (host != null && host.IsSpeakingNow)
            {
                float graceEnd = Time.unscaledTime + 12f;
                while (host != null && host.IsSpeakingNow && Time.unscaledTime < graceEnd)
                {
                    yield return null;
                }

                if (host != null && host.IsSpeakingNow)
                {
                    host.InterruptSpeech();
                    yield return null;
                }
            }
        }

        private Image EnsureImageGuessVeil()
        {
            if (imagePlaceholder == null) return null;
            Transform ph = imagePlaceholder.transform;
            if (ph.Find("ImageGuessVeil") != null)
            {
                return ph.Find("ImageGuessVeil").GetComponent<Image>();
            }

            GameObject go = new GameObject("ImageGuessVeil");
            go.transform.SetParent(ph, false);
            RectTransform r = go.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            r.SetAsLastSibling();
            Image img = go.AddComponent<Image>();
            img.sprite = White();
            img.color = new Color(0.02f, 0.02f, 0.05f, 0.92f);
            img.raycastTarget = false;
            return img;
        }

        private IEnumerator CoImageRevealUnblur(float totalSec)
        {
            float total = totalSec < 0.2f ? 0.2f : totalSec;
            float t0 = Time.unscaledTime;
            imageRevealWindowSec = total;
            imageRevealEndUnscaled = t0 + total;
            if (imageGuessInput != null) imageGuessInput.interactable = false;
            if (imageGuessSubmit != null) imageGuessSubmit.interactable = false;
            if (imageCaption != null)
            {
                imageCaption.text = "Image floue — observe puis réponds.";
            }

            while (Time.unscaledTime < t0 + total)
            {
                float a = (Time.unscaledTime - t0) / total;
                if (a < 0f) a = 0f;
                if (a > 1f) a = 1f;
                float ease = 1f - (1f - a) * (1f - a);
                if (imagePlaceholder != null)
                {
                    imagePlaceholder.color = new Color(1f, 1f, 1f, Mathf.Lerp(ImageGuessRevealingAlpha, 1f, ease));
                }

                if (imageGuessVeil != null)
                {
                    imageGuessVeil.color = new Color(0.02f, 0.02f, 0.05f, 0.92f * (1f - ease));
                }

                if (a > 0.12f)
                {
                    if (imagePlaceholder != null) imagePlaceholder.uvRect = new Rect(0f, 0f, 0.55f + 0.45f * ease, 0.75f + 0.25f * ease);
                }

                yield return null;
            }

            if (imagePlaceholder != null)
            {
                imagePlaceholder.color = Color.white;
                imagePlaceholder.uvRect = new Rect(0f, 0f, 1f, 1f);
            }

            if (imageGuessVeil != null) imageGuessVeil.color = new Color(0.02f, 0.02f, 0.05f, 0f);
            if (imageGuessInput != null) imageGuessInput.interactable = true;
            if (imageGuessSubmit != null) imageGuessSubmit.interactable = true;
            if (imageCaption != null)
            {
                imageCaption.text = CurrentImageGuessRound.Hint ?? "";
            }

            imageRevealEndUnscaled = 0f;
            imageRevealCo = null;
        }

        private static Transform CreateGridHost(Transform panel, string name, float verticalAnchor01, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = CreateRect(panel, name, new Vector2(0.5f, verticalAnchor01), new Vector2(0.5f, verticalAnchor01), anchoredPosition, sizeDelta);
            return go.transform;
        }

        private static void BuildScrambleLetterGrid(Transform parent, Font font, int cols, int rows, float cell, float gap, Vector2 centerOffset, MiniGamePanelContent demo)
        {
            Text[] cells = new Text[cols * rows];
            Button[] btns = new Button[cols * rows];
            float w = cols * cell + (cols - 1) * gap;
            float h = rows * cell + (rows - 1) * gap;
            Vector2 origin = new Vector2(-w * 0.5f + cell * 0.5f, -h * 0.5f + cell * 0.5f);
            int k = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = origin.x + c * (cell + gap);
                    float y = origin.y + (rows - 1 - r) * (cell + gap);
                    GameObject cellGo = CreateRect(parent, "SCell" + k, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centerOffset + new Vector2(x, y), new Vector2(cell, cell));
                    Image bg = cellGo.AddComponent<Image>();
                    bg.sprite = White();
                    Color[] pal =
                    {
                        new Color(0.07f, 0.07f, 0.08f, 1f),
                        new Color(0.1f, 0.1f, 0.12f, 1f),
                        new Color(0.08f, 0.08f, 0.1f, 1f)
                    };
                    bg.color = pal[(r + c) % pal.Length];
                    bg.raycastTarget = true;
                    Outline ol = cellGo.AddComponent<Outline>();
                    ol.effectColor = new Color(0.3f, 0.34f, 0.4f, 0.42f);
                    ol.effectDistance = new Vector2(1f, -1f);
                    Button btn = cellGo.AddComponent<Button>();
                    btn.targetGraphic = bg;
                    btn.transition = Selectable.Transition.None;
                    cellGo.AddComponent<GridCellHoverFeedback>();
                    GameObject txGo = CreateRect(cellGo.transform, "T", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Text tx = txGo.AddComponent<Text>();
                    tx.font = font;
                    tx.raycastTarget = false;
                    ConfigureGridLetter(tx, cell);
                    cells[k] = tx;
                    btns[k] = btn;
                    k++;
                }
            }

            demo.wordScrambleTiles = cells;
            demo.wordScrambleButtons = btns;
        }

        private static Text[] CrosswordLetterGrid(Transform parent, Font font, int cols, int rows, float cell, float gap, Vector2 centerOffset, MiniGamePanelContent demo)
        {
            Text[] cells = new Text[cols * rows];
            Button[] btns = new Button[cols * rows];
            float w = cols * cell + (cols - 1) * gap;
            float h = rows * cell + (rows - 1) * gap;
            Vector2 origin = new Vector2(-w * 0.5f + cell * 0.5f, -h * 0.5f + cell * 0.5f);
            int k = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = origin.x + c * (cell + gap);
                    float y = origin.y + (rows - 1 - r) * (cell + gap);
                    GameObject cellGo = CreateRect(parent, "CCell" + k, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centerOffset + new Vector2(x, y), new Vector2(cell, cell));
                    Image bg = cellGo.AddComponent<Image>();
                    bg.sprite = White();
                    Color[] cPal =
                    {
                        new Color(0.15f, 0.16f, 0.19f, 1f),
                        new Color(0.19f, 0.2f, 0.23f, 1f),
                        new Color(0.17f, 0.18f, 0.21f, 1f)
                    };
                    bg.color = cPal[(r + c) % cPal.Length];
                    bg.raycastTarget = true;
                    Button btn = cellGo.AddComponent<Button>();
                    btn.targetGraphic = bg;
                    btn.transition = Selectable.Transition.None;
                    cellGo.AddComponent<GridCellHoverFeedback>();
                    int idx = k;
                    btn.onClick.AddListener(() => demo.AppendCrosswordGuessFromCell(idx));
                    GameObject txGo = CreateRect(cellGo.transform, "T", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Text tx = txGo.AddComponent<Text>();
                    tx.font = font;
                    tx.raycastTarget = false;
                    ConfigureGridLetter(tx, cell);
                    cells[k] = tx;
                    btns[k] = btn;
                    k++;
                }
            }

            demo.crosswordButtons = btns;
            return cells;
        }

        private static void WireLetterGridDrag(Transform gridHost, MiniGamePanelContent demo, bool crossword, Text[] cellTexts)
        {
            if (gridHost == null || demo == null || cellTexts == null || cellTexts.Length == 0)
            {
                return;
            }

            GridLetterDragCoordinator coord = gridHost.GetComponent<GridLetterDragCoordinator>();
            if (coord == null)
            {
                coord = gridHost.gameObject.AddComponent<GridLetterDragCoordinator>();
            }

            coord.Initialize(demo, crossword, cellTexts);
        }

        /// <summary>Construit les panneaux secondaires sous le root des modes (le panneau quiz est créé séparément).</summary>
        public static void BuildSecondaryPanels(Transform modeRoot, Font font, ModeSurfaceController surf, MiniGamePanelContent demo)
        {
            Sprite white = White();

            GameObject semantic = PanelShell(modeRoot, "PanelSemantic", "semantic", new Color(0.07f, 0.1f, 0.16f, 0.96f), white, surf);
            demo.semanticTitle = Title(semantic.transform, font, "TitreSemantic", "Sémantique", new Vector2(0f, -22f));
            GameObject semSide = CreateRect(semantic.transform, "SemanticLiveSide", new Vector2(0.06f, 0.16f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
            Image semSideBg = semSide.AddComponent<Image>();
            semSideBg.sprite = white;
            semSideBg.color = new Color(0.04f, 0.06f, 0.11f, 0.92f);
            Text semSideTitle = CreateText(semSide.transform, "SemLiveTitle", font, 19, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -8f), new Vector2(-10f, 28f));
            semSideTitle.fontStyle = FontStyle.Bold;
            semSideTitle.color = new Color(0.47f, 0.92f, 0.98f, 1f);
            semSideTitle.text = "Trouvez le mot !";
            Outline semTitleGlow = semSideTitle.gameObject.AddComponent<Outline>();
            semTitleGlow.effectColor = new Color(0.05f, 0.45f, 0.68f, 0.45f);
            semTitleGlow.effectDistance = new Vector2(1f, -1f);
            demo.semanticLiveUsers = new Text[9];
            demo.semanticLiveWords = new Text[9];
            demo.semanticLiveAvatarFallbacks = new Text[9];
            demo.semanticLiveAvatarPhotos = new RawImage[9];
            demo.semanticLivePtsBadges = new Text[9];
            demo.semanticLiveProgressFills = new Image[9];
            demo.semanticLiveRowGroups = new CanvasGroup[9];
            for (int i = 0; i < 9; i++)
            {
                float y = -44f - i * 48f;
                GameObject row = CreateRect(semSide.transform, "SemLiveRow" + i, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, y), new Vector2(-10f, 40f));
                Image rowBg = row.AddComponent<Image>();
                rowBg.sprite = white;
                rowBg.color = new Color(0.1f, 0.12f, 0.2f, 0.76f);
                Shadow rowShadow = row.AddComponent<Shadow>();
                rowShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
                rowShadow.effectDistance = new Vector2(0f, -2f);
                CanvasGroup rowCg = row.AddComponent<CanvasGroup>();
                rowCg.alpha = 0f;
                demo.semanticLiveRowGroups[i] = rowCg;
                GameObject ringGo = CreateRect(row.transform, "AvatarRing", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(38f, 38f));
                Image ring = ringGo.AddComponent<Image>();
                ring.sprite = white;
                ring.color = new Color(0.9f, 0.93f, 0.98f, 0.98f);
                RawImage photo = ImageBlock(row.transform, white, new Vector2(28f, 0f), new Vector2(32f, 32f));
                photo.texture = null;
                photo.color = new Color(1f, 1f, 1f, 0f);
                photo.raycastTarget = false;
                demo.semanticLiveAvatarPhotos[i] = photo;
                Text av = CreateText(row.transform, "Av" + i, font, 16, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(32f, 32f));
                av.color = new Color(0.14f, 0.2f, 0.3f, 1f);
                demo.semanticLiveAvatarFallbacks[i] = av;
                Text u = CreateText(row.transform, "User" + i, font, 11, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(96f, 20f));
                u.color = new Color(0.72f, 0.84f, 0.96f, 1f);
                demo.semanticLiveUsers[i] = u;
                Text w = CreateText(row.transform, "Word" + i, font, 23, TextAnchor.MiddleLeft, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(150f, 0f), new Vector2(206f, 30f));
                w.color = new Color(0.97f, 0.98f, 0.99f, 1f);
                w.fontStyle = FontStyle.Bold;
                Outline wGlow = w.gameObject.AddComponent<Outline>();
                wGlow.effectColor = new Color(0.07f, 0.34f, 0.55f, 0.35f);
                wGlow.effectDistance = new Vector2(1f, -1f);
                demo.semanticLiveWords[i] = w;
                GameObject pBgGo = CreateRect(row.transform, "ProgBg" + i, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-78f, 0f), new Vector2(118f, 8f));
                Image pBg = pBgGo.AddComponent<Image>();
                pBg.sprite = white;
                pBg.color = new Color(0.13f, 0.15f, 0.24f, 0.95f);
                GameObject pFillGo = CreateRect(pBgGo.transform, "ProgFill" + i, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                Image pFill = pFillGo.AddComponent<Image>();
                pFill.sprite = white;
                pFill.type = Image.Type.Filled;
                pFill.fillMethod = Image.FillMethod.Horizontal;
                pFill.fillOrigin = 0;
                pFill.fillAmount = 0f;
                pFill.color = new Color(0.3f, 0.58f, 1f, 0.95f);
                demo.semanticLiveProgressFills[i] = pFill;
                Text pts = CreateText(row.transform, "Pts" + i, font, 19, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, -12f), new Vector2(56f, 22f));
                pts.color = new Color(1f, 0.86f, 0.28f, 0f);
                pts.fontStyle = FontStyle.Bold;
                demo.semanticLivePtsBadges[i] = pts;
            }
            Transform semHost = CreateGridHost(semantic.transform, "SemanticGridHost", 0.56f, new Vector2(-124f, 64f), new Vector2(650f, 352f));
            demo.semanticCells = LetterGrid(semHost, font, 3, 3, 86f, 16f, Vector2.zero);
            semHost.gameObject.SetActive(false);
            GameObject semFoot = CreateRect(semantic.transform, "SemanticFooter", new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.15f), Vector2.zero, Vector2.zero);
            Image semFootBg = semFoot.AddComponent<Image>();
            semFootBg.sprite = white;
            semFootBg.color = new Color(0.04f, 0.06f, 0.08f, 0.65f);
            semFootBg.raycastTarget = false;
            demo.semanticHint = Sub(semFoot.transform, font, "SemHint", new Vector2(0f, -18f));
            demo.semanticHint.rectTransform.sizeDelta = new Vector2(900f, 76f);
            demo.semanticHint.fontSize = 20;
            demo.semanticAnswerInput = BuildInputField(semFoot.transform, font, "SemAnswer", new Vector2(0f, -64f), 500f, "Tape ta réponse…");
            demo.semanticClearButton = BuildSecondaryButton(semFoot.transform, font, "Effacer", new Vector2(-260f, -122f), () =>
            {
                if (demo.semanticAnswerInput != null) demo.semanticAnswerInput.text = "";
            });
            demo.semanticSubmitButton = BuildPrimaryButton(semFoot.transform, font, "Valider", new Vector2(260f, -122f), () => OnSemanticSubmit(demo));
            demo.semanticFeedback = Sub(semFoot.transform, font, "SemFb", new Vector2(0f, -162f));
            demo.semanticFeedback.rectTransform.sizeDelta = new Vector2(900f, 54f);
            demo.semanticFeedback.fontSize = 20;
            demo.semanticFeedback.alignment = TextAnchor.MiddleCenter;
            demo.semanticFeedback.horizontalOverflow = HorizontalWrapMode.Wrap;
            demo.semanticFeedback.verticalOverflow = VerticalWrapMode.Truncate;
            demo.semanticFeedback.rectTransform.anchoredPosition = new Vector2(0f, -126f);

            demo.semanticLiveTicker = CreateText(
                semantic.transform,
                "SemLiveTicker",
                font,
                17,
                TextAnchor.MiddleLeft,
                new Vector2(0.06f, 0.86f),
                new Vector2(0.94f, 0.94f),
                new Vector2(8f, 0f),
                new Vector2(-10f, 0f));
            demo.semanticLiveTicker.color = new Color(0.82f, 0.92f, 1f, 0.95f);
            demo.semanticLiveTicker.fontStyle = FontStyle.Bold;
            demo.semanticLiveTicker.text = "";
            demo.semanticRevealWordTop = CreateText(
                semantic.transform,
                "SemRevealTop",
                font,
                24,
                TextAnchor.MiddleCenter,
                new Vector2(0.2f, 0.905f),
                new Vector2(0.8f, 0.965f),
                Vector2.zero,
                Vector2.zero);
            demo.semanticRevealWordTop.color = new Color(1f, 0.88f, 0.3f, 1f);
            demo.semanticRevealWordTop.fontStyle = FontStyle.Bold;
            demo.semanticRevealWordTop.text = "";

            GameObject word = PanelShell(modeRoot, "PanelWordScramble", "word-scramble", new Color(0.13f, 0.09f, 0.2f, 0.96f), white, surf);
            demo.wordScrambleTitle = Title(word.transform, font, "WTitle", "Mots mélangés (mots mêlés)", new Vector2(0f, -16f));
            demo.wordScrambleLetters = null;
            GameObject wordSide = CreateRect(word.transform, "WordFoundSide", new Vector2(0.78f, 0.13f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero);
            Image wsBg = wordSide.AddComponent<Image>();
            wsBg.sprite = white;
            wsBg.color = new Color(0.05f, 0.1f, 0.12f, 0.9f);
            Text wsTitle = CreateText(wordSide.transform, "FoundTitle", font, 20, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -8f), new Vector2(-16f, 32f));
            wsTitle.fontStyle = FontStyle.Bold;
            wsTitle.color = new Color(0.4f, 0.95f, 0.55f, 1f);
            wsTitle.text = "Déjà trouvés";
            GameObject wProgBgGo = CreateRect(wordSide.transform, "FoundProgressBg", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-16f, 16f));
            Image wProgBg = wProgBgGo.AddComponent<Image>();
            wProgBg.sprite = white;
            wProgBg.color = new Color(0.08f, 0.12f, 0.16f, 0.96f);
            GameObject wProgFillGo = CreateRect(wProgBgGo.transform, "FoundProgressFill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            Image wProgFill = wProgFillGo.AddComponent<Image>();
            wProgFill.sprite = white;
            wProgFill.type = Image.Type.Filled;
            wProgFill.fillMethod = Image.FillMethod.Horizontal;
            wProgFill.fillOrigin = 0;
            wProgFill.fillAmount = 0f;
            wProgFill.color = new Color(0.2f, 1f, 0.46f, 1f);
            demo.gridFoundProgressFillWord = wProgFill;
            demo.gridFoundList = CreateText(wordSide.transform, "FoundList", font, 16, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            demo.gridFoundList.color = new Color(0.9f, 0.95f, 0.75f, 1f);
            demo.gridFoundList.alignment = TextAnchor.UpperLeft;
            demo.gridFoundList.horizontalOverflow = HorizontalWrapMode.Wrap;
            demo.gridFoundList.verticalOverflow = VerticalWrapMode.Truncate;
            demo.gridFoundList.text = "—";
            RectTransform wordListRt = demo.gridFoundList.rectTransform;
            wordListRt.anchorMin = new Vector2(0f, 0f);
            wordListRt.anchorMax = new Vector2(1f, 1f);
            wordListRt.offsetMin = new Vector2(8f, 8f);
            wordListRt.offsetMax = new Vector2(-8f, -58f);
            Transform wordHost = CreateGridHost(word.transform, "WordGridHost", 0.50f, new Vector2(-146f, 92f), new Vector2(660f, 470f));
            BuildScrambleLetterGrid(wordHost, font, ScrambleCols, ScrambleCols, ScrambleCellPx, ScrambleGapPx, Vector2.zero, demo);
            WireLetterGridDrag(wordHost, demo, false, demo.wordScrambleTiles);

            GameObject wordFoot = CreateRect(word.transform, "WordFooter", new Vector2(0.06f, 0.02f), new Vector2(0.76f, 0.26f), Vector2.zero, Vector2.zero);
            Image wfBg = wordFoot.AddComponent<Image>();
            wfBg.sprite = white;
            wfBg.color = new Color(0.04f, 0.05f, 0.08f, 0.62f);
            wfBg.raycastTarget = false;

            demo.wordHint = Sub(wordFoot.transform, font, "Hint", new Vector2(0f, -10f));
            demo.wordHint.rectTransform.sizeDelta = new Vector2(660f, 28f);
            demo.wordHint.fontSize = 13;
            demo.wordHint.lineSpacing = 0.9f;
            demo.wordHint.alignment = TextAnchor.UpperCenter;
            demo.wordHint.verticalOverflow = VerticalWrapMode.Truncate;
            demo.wordAnswerInput = BuildInputField(wordFoot.transform, font, "AnswerField", new Vector2(0f, -52f), 460f);
            demo.wordClearButton = BuildSecondaryButton(wordFoot.transform, font, "Effacer", new Vector2(-220f, -102f), () => demo.ClearWordGuess());
            demo.wordSubmitButton = BuildPrimaryButton(wordFoot.transform, font, "Valider", new Vector2(220f, -102f), () => OnWordSubmit(demo));
            demo.wordFeedback = Sub(wordFoot.transform, font, "WordFb", new Vector2(0f, -136f));
            demo.wordFeedback.rectTransform.sizeDelta = new Vector2(620f, 26f);
            demo.wordFeedback.fontSize = 13;
            demo.wordFeedback.verticalOverflow = VerticalWrapMode.Truncate;

            GameObject cross = PanelShell(modeRoot, "PanelCrossword", "crossword-lite", new Color(0.05f, 0.15f, 0.13f, 0.96f), white, surf);
            demo.crosswordTitle = Title(cross.transform, font, "CTitle", "Mots croisés", new Vector2(0f, -16f));
            GameObject crossSide = CreateRect(cross.transform, "CrossFoundSide", new Vector2(0.78f, 0.13f), new Vector2(0.98f, 0.84f), Vector2.zero, Vector2.zero);
            Image crBg = crossSide.AddComponent<Image>();
            crBg.sprite = white;
            crBg.color = new Color(0.04f, 0.1f, 0.12f, 0.9f);
            Text crTi = CreateText(crossSide.transform, "CFoundTitle", font, 20, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -8f), new Vector2(-16f, 32f));
            crTi.fontStyle = FontStyle.Bold;
            crTi.color = new Color(0.5f, 0.9f, 0.95f, 1f);
            crTi.text = "Déjà trouvés";
            GameObject cProgBgGo = CreateRect(crossSide.transform, "CFoundProgressBg", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -38f), new Vector2(-16f, 16f));
            Image cProgBg = cProgBgGo.AddComponent<Image>();
            cProgBg.sprite = white;
            cProgBg.color = new Color(0.08f, 0.12f, 0.16f, 0.96f);
            GameObject cProgFillGo = CreateRect(cProgBgGo.transform, "CFoundProgressFill", new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            Image cProgFill = cProgFillGo.AddComponent<Image>();
            cProgFill.sprite = white;
            cProgFill.type = Image.Type.Filled;
            cProgFill.fillMethod = Image.FillMethod.Horizontal;
            cProgFill.fillOrigin = 0;
            cProgFill.fillAmount = 0f;
            cProgFill.color = new Color(0.2f, 1f, 0.46f, 1f);
            demo.gridFoundProgressFillCross = cProgFill;
            demo.gridFoundListCross = CreateText(crossSide.transform, "CFoundList", font, 16, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            demo.gridFoundListCross.color = new Color(0.85f, 0.95f, 0.98f, 1f);
            demo.gridFoundListCross.alignment = TextAnchor.UpperLeft;
            demo.gridFoundListCross.horizontalOverflow = HorizontalWrapMode.Wrap;
            demo.gridFoundListCross.verticalOverflow = VerticalWrapMode.Truncate;
            demo.gridFoundListCross.text = "—";
            RectTransform crossListRt = demo.gridFoundListCross.rectTransform;
            crossListRt.anchorMin = new Vector2(0f, 0f);
            crossListRt.anchorMax = new Vector2(1f, 1f);
            crossListRt.offsetMin = new Vector2(8f, 8f);
            crossListRt.offsetMax = new Vector2(-8f, -58f);
            Transform crossHost = CreateGridHost(cross.transform, "CrossGridHost", 0.50f, new Vector2(-156f, 124f), new Vector2(520f, 430f));
            demo.crosswordCells = CrosswordLetterGrid(crossHost, font, CrosswordCols, CrosswordCols, CrosswordCellPx, CrosswordGapPx, Vector2.zero, demo);
            WireLetterGridDrag(crossHost, demo, true, demo.crosswordCells);

            GameObject crossFoot = CreateRect(cross.transform, "CrossFooter", new Vector2(0.06f, 0.02f), new Vector2(0.76f, 0.29f), Vector2.zero, Vector2.zero);
            Image cfBg = crossFoot.AddComponent<Image>();
            cfBg.sprite = white;
            cfBg.color = new Color(0.04f, 0.05f, 0.08f, 0.62f);
            cfBg.raycastTarget = false;
            demo.crosswordFeedback = Sub(crossFoot.transform, font, "CrossFb", new Vector2(0f, -12f));
            RectTransform crossFbRt = demo.crosswordFeedback.rectTransform;
            crossFbRt.sizeDelta = new Vector2(700f, 52f);
            demo.crosswordFeedback.fontSize = 14;
            demo.crosswordFeedback.lineSpacing = 0.9f;
            demo.crosswordFeedback.alignment = TextAnchor.UpperCenter;
            demo.crosswordFeedback.verticalOverflow = VerticalWrapMode.Truncate;
            demo.crosswordAnswerInput = BuildInputField(crossFoot.transform, font, "CrossDraft", new Vector2(0f, -72f), 500f, "Tape un mot selon une définition…");
            demo.crosswordClearButton = BuildSecondaryButton(crossFoot.transform, font, "Effacer", new Vector2(-260f, -128f), () => demo.ClearCrosswordGuess());
            demo.crosswordSubmitButton = BuildPrimaryButton(crossFoot.transform, font, "Valider", new Vector2(260f, -128f), () => OnCrosswordSubmit(demo));

            // Popup style live (pseudo + mot trouvé + points) inspiré des overlays type Braingame.
            GameObject gridToast = CreateRect(modeRoot, "GridFoundToast", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -24f), new Vector2(360f, 250f));
            Image gridToastBg = gridToast.AddComponent<Image>();
            gridToastBg.sprite = white;
            gridToastBg.color = new Color(0.17f, 0.09f, 0.28f, 0.96f);
            Outline gridToastOl = gridToast.AddComponent<Outline>();
            gridToastOl.effectColor = new Color(0.12f, 0.95f, 0.86f, 0.98f);
            gridToastOl.effectDistance = new Vector2(3f, -3f);
            CanvasGroup gridToastCg = gridToast.AddComponent<CanvasGroup>();
            gridToastCg.alpha = 0f;
            gridToast.SetActive(false);
            demo.gridFoundToastRoot = gridToast.GetComponent<RectTransform>();
            demo.gridFoundToastBackground = gridToastBg;
            demo.gridFoundToastOutline = gridToastOl;

            GameObject avatarRingGo = CreateRect(gridToast.transform, "AvatarRing", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(82f, 82f));
            Image avatarRing = avatarRingGo.AddComponent<Image>();
            avatarRing.sprite = white;
            avatarRing.color = new Color(0.95f, 0.95f, 1f, 0.98f);
            avatarRing.raycastTarget = false;

            RawImage avatarPhoto = ImageBlock(gridToast.transform, white, new Vector2(0f, -44f), new Vector2(74f, 74f));
            avatarPhoto.texture = null;
            avatarPhoto.color = new Color(1f, 1f, 1f, 0f);
            avatarPhoto.raycastTarget = false;
            demo.gridFoundToastAvatarPhoto = avatarPhoto;

            Text avatar = CreateText(gridToast.transform, "Avatar", font, 34, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(74f, 74f));
            avatar.color = new Color(0.15f, 0.22f, 0.35f, 1f);
            avatar.transform.SetAsLastSibling();
            demo.gridFoundToastAvatar = avatar;

            demo.gridFoundToastUser = CreateText(gridToast.transform, "User", font, 24, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 20f), new Vector2(-26f, 34f));
            demo.gridFoundToastUser.color = new Color(0.95f, 0.95f, 1f, 1f);
            demo.gridFoundToastWord = CreateText(gridToast.transform, "Word", font, 52, TextAnchor.MiddleCenter, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -20f), new Vector2(-26f, 72f));
            demo.gridFoundToastWord.color = new Color(0.3f, 1f, 0.55f, 1f);
            demo.gridFoundToastPoints = CreateText(gridToast.transform, "Points", font, 34, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 22f), new Vector2(-26f, 42f));
            demo.gridFoundToastPoints.color = new Color(1f, 0.84f, 0.25f, 1f);

            GameObject blind = PanelShell(modeRoot, "PanelBlind", "blind-test", new Color(0.1f, 0.06f, 0.14f, 0.96f), white, surf);
            demo.blindTitle = Title(blind.transform, font, "BTitle", "Blind test", new Vector2(0f, -30f));
            demo.blindPrompt = Sub(blind.transform, font, "BlindQ", new Vector2(0f, -94f));
            demo.blindPrompt.rectTransform.sizeDelta = new Vector2(940f, 178f);
            demo.blindPrompt.fontSize = 32;
            demo.blindSub = Sub(blind.transform, font, "BlindSub", new Vector2(0f, -214f));
            demo.blindSub.rectTransform.sizeDelta = new Vector2(920f, 140f);
            demo.blindSub.fontSize = 26;
            demo.blindSub.lineSpacing = 1.1f;
            Transform blindHost = CreateGridHost(blind.transform, "BlindEmojiHost", 0.5f, new Vector2(0f, 68f), new Vector2(960f, 156f));
            demo.blindEmoji = BigLettersCentered(blindHost, font, "BEmoji", 64);
            GameObject blindChoicesRoot = CreateRect(blind.transform, "BlindChoicesRoot", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 92f), new Vector2(980f, 288f));
            BlindChoiceColumn(blindChoicesRoot.transform, font, new Vector2(0f, -12f), 54f, demo);

            GameObject myst = PanelShell(modeRoot, "PanelMystery", "mystery-word", new Color(0.07f, 0.09f, 0.13f, 0.96f), white, surf);
            demo.mysteryTitle = Title(myst.transform, font, "MTitle", "Mot mystère", new Vector2(0f, -30f));
            Transform mystHost = CreateGridHost(myst.transform, "MysteryMaskHost", 0.54f, new Vector2(0f, 114f), new Vector2(960f, 124f));
            demo.mysteryMask = BigLettersCentered(mystHost, font, "Mask", 46);

            // Remonté pour rester entièrement dans le panneau (évite le débordement bas en 4K).
            GameObject mystFoot = CreateRect(myst.transform, "MysteryFooter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 126f), new Vector2(980f, 220f));
            Image mystFootBg = mystFoot.AddComponent<Image>();
            mystFootBg.sprite = white;
            mystFootBg.color = new Color(0.04f, 0.05f, 0.08f, 0.62f);
            mystFootBg.raycastTarget = false;
            demo.mysteryAnswerInput = BuildInputField(mystFoot.transform, font, "MystAnswer", new Vector2(0f, -72f), 540f, "Tape le mot complet…");
            demo.mysteryClearButton = BuildSecondaryButton(mystFoot.transform, font, "Effacer", new Vector2(-300f, -132f), () =>
            {
                if (demo.mysteryAnswerInput != null) demo.mysteryAnswerInput.text = "";
            });
            demo.mysterySubmitButton = BuildPrimaryButton(mystFoot.transform, font, "Valider", new Vector2(300f, -132f), () => OnMysterySubmit(demo));
            demo.mysteryFeedback = Sub(mystFoot.transform, font, "MystFb", new Vector2(0f, -176f));

            GameObject mem = PanelShell(modeRoot, "PanelMemory", "memory", new Color(0.06f, 0.11f, 0.09f, 0.96f), white, surf);
            demo.memoryTitle = Title(mem.transform, font, "MemTitle", "Mémoire", new Vector2(0f, -32f));
            Transform memHost = CreateGridHost(mem.transform, "MemoryGridHost", 0.50f, new Vector2(0f, 6f), new Vector2(900f, 330f));
            demo.memoryCards = MemoryGrid(memHost, font, white, 132f, 14f);
            demo.memorySubtitle = Sub(mem.transform, font, "MemSub", new Vector2(0f, -218f));
            demo.memorySubtitle.fontSize = 26;
            demo.memorySubtitle.lineSpacing = 1.08f;
            demo.memorySubtitle.rectTransform.sizeDelta = new Vector2(940f, 120f);

            GameObject chrono = PanelShell(modeRoot, "PanelChrono", "speed-chrono", new Color(0.12f, 0.07f, 0.06f, 0.96f), white, surf);
            demo.chronoTitle = Title(chrono.transform, font, "ChTitle", "Chrono vitesse", new Vector2(0f, -30f));
            GameObject chronoRule = CreateRect(chrono.transform, "ChronoRules", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            {
                RectTransform crR = chronoRule.GetComponent<RectTransform>();
                crR.anchorMin = new Vector2(0.03f, 0.1f);
                crR.anchorMax = new Vector2(0.56f, 0.92f);
                crR.offsetMin = new Vector2(6f, 6f);
                crR.offsetMax = new Vector2(-4f, -6f);
            }

            Image chRuleBg = chronoRule.AddComponent<Image>();
            chRuleBg.sprite = white;
            chRuleBg.color = new Color(0.04f, 0.04f, 0.08f, 0.6f);
            chRuleBg.raycastTarget = false;
            demo.chronoInstruction = CreateText(chronoRule.transform, "ChronoInstr", font, 19, TextAnchor.UpperLeft, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            demo.chronoInstruction.color = new Color(0.9f, 0.9f, 0.88f, 0.95f);
            demo.chronoInstruction.alignment = TextAnchor.UpperLeft;
            demo.chronoInstruction.horizontalOverflow = HorizontalWrapMode.Wrap;
            demo.chronoInstruction.text = "";
            {
                RectTransform iRt = demo.chronoInstruction.rectTransform;
                iRt.anchorMin = new Vector2(0f, 0f);
                iRt.anchorMax = new Vector2(1f, 1f);
                iRt.offsetMin = new Vector2(8f, 8f);
                iRt.offsetMax = new Vector2(-8f, -8f);
            }

            GameObject chronoNumHost = CreateRect(chrono.transform, "ChronoNumHost", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            {
                RectTransform cnhR = chronoNumHost.GetComponent<RectTransform>();
                cnhR.anchorMin = new Vector2(0.57f, 0.1f);
                cnhR.anchorMax = new Vector2(0.98f, 0.92f);
                cnhR.offsetMin = new Vector2(4f, 6f);
                cnhR.offsetMax = new Vector2(-10f, -6f);
            }

            demo.chronoMeta = CreateText(chronoNumHost.transform, "ChMeta", font, 16, TextAnchor.UpperCenter, new Vector2(0.04f, 0.7f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
            demo.chronoMeta.color = new Color(0.75f, 0.9f, 0.8f, 0.95f);
            demo.chronoMeta.alignment = TextAnchor.MiddleCenter;
            demo.chronoMeta.horizontalOverflow = HorizontalWrapMode.Wrap;
            {
                RectTransform mRt = demo.chronoMeta.rectTransform;
                mRt.offsetMin = new Vector2(4f, 0f);
                mRt.offsetMax = new Vector2(-4f, 0f);
            }

            demo.chronoBig = BigLettersInRegion(chronoNumHost.transform, font, "ChBig", 64, 0.04f, 0.1f, 0.96f, 0.65f);
            demo.chronoSub = CreateText(chronoNumHost.transform, "ChSub", font, 20, TextAnchor.LowerCenter, new Vector2(0.04f, 0f), new Vector2(0.96f, 0.22f), Vector2.zero, Vector2.zero);
            demo.chronoSub.color = new Color(1f, 0.9f, 0.3f, 1f);
            demo.chronoSub.alignment = TextAnchor.MiddleCenter;
            demo.chronoSub.horizontalOverflow = HorizontalWrapMode.Wrap;
            {
                RectTransform sRt = demo.chronoSub.rectTransform;
                sRt.offsetMin = new Vector2(2f, 2f);
                sRt.offsetMax = new Vector2(-2f, -2f);
            }

            GameObject img = PanelShell(modeRoot, "PanelImageGuess", "image-guess", new Color(0.08f, 0.09f, 0.11f, 0.96f), white, surf);
            demo.imageTitle = Title(img.transform, font, "ImgTitle", "Devine l’image", new Vector2(0f, -24f));
            Transform imgHost = CreateGridHost(img.transform, "ImageBlockHost", 0.59f, new Vector2(0f, 66f), new Vector2(980f, 390f));
            demo.imagePlaceholder = ImageBlockCentered(imgHost, white, new Vector2(820f, 340f));
            GameObject imgCapBox = CreateRect(img.transform, "ImgCaptionBox", new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.40f), Vector2.zero, Vector2.zero);
            Image imgCapBg = imgCapBox.AddComponent<Image>();
            imgCapBg.sprite = white;
            imgCapBg.color = new Color(0.02f, 0.03f, 0.07f, 0.74f);
            imgCapBg.raycastTarget = false;
            demo.imageCaption = Sub(imgCapBox.transform, font, "ImgCap", new Vector2(0f, -18f));
            RectTransform capRt = demo.imageCaption.rectTransform;
            capRt.sizeDelta = new Vector2(900f, 58f);
            demo.imageCaption.fontSize = 21;
            demo.imageCaption.color = new Color(0.98f, 0.95f, 0.76f, 0.98f);

            GameObject imgFoot = CreateRect(img.transform, "ImageGuessFooter", new Vector2(0.06f, 0.03f), new Vector2(0.94f, 0.21f), Vector2.zero, Vector2.zero);
            Image imgFootBg = imgFoot.AddComponent<Image>();
            imgFootBg.sprite = white;
            imgFootBg.color = new Color(0.05f, 0.06f, 0.09f, 0.58f);
            imgFootBg.raycastTarget = false;
            demo.imageGuessInput = BuildInputField(imgFoot.transform, font, "ImgAnswer", new Vector2(-120f, -42f), 520f, "Écris ta réponse puis Valider…");
            demo.imageGuessSubmit = BuildPrimaryButton(imgFoot.transform, font, "Valider", new Vector2(280f, -42f), () => OnImageGuessSubmit(demo));
            demo.imageGuessFeedback = Sub(imgFoot.transform, font, "ImgFb", new Vector2(0f, -90f));
            demo.imageGuessFeedback.rectTransform.sizeDelta = new Vector2(880f, 42f);
            demo.imageGuessFeedback.fontSize = 20;
        }

        private static void OnSemanticSubmit(MiniGamePanelContent demo)
        {
            if (demo.semanticAnswerInput == null) return;
            if (demo.semanticRoundResolved) return;
            string raw = demo.semanticAnswerInput.text?.Trim() ?? "";
            string a = GridThemeBank.SanitizeForGrid(raw);
            bool hadInput = !string.IsNullOrEmpty(a);
            string target = (demo.CurrentSemanticTarget ?? "CON").Trim().ToUpperInvariant();
            bool ok = hadInput && SemanticAnswerMatches(demo.currentSemanticRound, a);
            if (hadInput)
            {
                float score = demo.EstimateSemanticLiveProgress(a);
                demo.UpsertSemanticLiveEntry("toi", a, "", score);
                demo.PushSemanticTicker("toi", a);
                demo.TryAwardSemanticProximityPoints("toi", "", a, score);
                demo.RenderSemanticLiveFeed();
                if (demo.semanticLiveRevealCo != null)
                {
                    demo.StopCoroutine(demo.semanticLiveRevealCo);
                    demo.semanticLiveRevealCo = null;
                }

                demo.semanticLiveRevealCo = demo.StartCoroutine(demo.CoRevealSemanticRows());
            }

            float proximity = hadInput ? demo.EstimateSemanticLiveProgress(a) : 0f;
            string emoji = EmojiForSemanticProgress(proximity);
            int percent = Mathf.RoundToInt(proximity * 100f);
            if (demo.semanticFeedback != null)
            {
                string crossInfo = BuildSemanticCrossLanguageInfo(demo.currentSemanticRound);
                demo.semanticFeedback.text = !hadInput
                    ? "Réponse vide — propose un mot pour lancer la barre."
                    : (ok
                        ? "<color=#5EE7A2><b>Bravo !</b></color>  <color=#BCE6FF>" + a + "</color>  <color=#7EE7FF>+ score global</color>"
                          + (string.IsNullOrEmpty(crossInfo) ? "" : "\n<color=#CDEBFF>" + crossInfo + "</color>")
                        : "<color=#BCE6FF>" + a + "</color> <color=#8FD7FF>(" + percent + "%)</color> " + emoji
                          + "  <color=#FFB25E>Continue, tu te rapproches du mot cible.</color>");
            }
            if (ok)
            {
                demo.semanticRoundResolved = true;
                if (demo.semanticTimeoutCo != null)
                {
                    demo.StopCoroutine(demo.semanticTimeoutCo);
                    demo.semanticTimeoutCo = null;
                }
                demo.ShowSemanticLivePointsBadge(a, 2);
                GameSfxHub.Instance?.PlayResult(true);
                demo.StartSemanticPostWinRecapAndAdvance();
                return;
            }

            demo.TryPlaySemanticWarmCue(proximity);

            // En mode sémantique, une proposition non exacte n'est pas une "erreur":
            // on garde uniquement les indicateurs visuels (barre + emoji), sans son ni croix rouge.
        }

        private static void OnWordSubmit(MiniGamePanelContent demo)
        {
            if (demo.wordAnswerInput == null) return;
            string a = GridThemeBank.SanitizeForGrid(demo.wordAnswerInput.text ?? "");
            bool hadInput = !string.IsNullOrEmpty(a);
            string matchedWord = "";
            bool ok = hadInput && demo.TryMatchAnyUnsolvedGridWord(a, out matchedWord);
            if (demo.wordFeedback != null)
            {
                demo.wordFeedback.text = !hadInput
                    ? "Réponse vide — tape le mot puis Valider."
                    : (ok
                        ? "Bravo — c’est " + matchedWord + " ! (" + (demo.currentGridSolved != null ? demo.currentGridSolved.Count : 0) + "/"
                        + (demo.currentGridAllWords != null ? demo.currentGridAllWords.Count : 0) + " pour cette grille.)"
                        : "Pas exact — ce mot n’est pas dans la liste pour cette grille (ou déjà trouvé).");
            }

            GameSfxHub.Instance?.PlayResult(ok);
            if (ok && demo.currentGridSolved != null && !string.IsNullOrEmpty(matchedWord) && !demo.currentGridSolved.Contains(matchedWord))
            {
                demo.currentGridSolved.Add(matchedWord);
                demo.RecordGridWordFound(matchedWord);
                int points = demo.GrantGridWordPoints();
                demo.ShowGridFoundToast(matchedWord, points);
            }

            if (!ok) return;

            GameModeManager gmm = GameModeManager.Instance;
            if (gmm != null && string.Equals(gmm.ActiveModeId, "word-scramble", StringComparison.OrdinalIgnoreCase))
            {
                demo.ApplyWordScrambleFoundWordHighlight(matchedWord);
                demo.StartCoroutine(demo.CoDelayedAdvanceAfterGridSuccess());
                return;
            }

            MaybeAdvanceMiniGameAfterResponse();
        }

        private static void OnCrosswordSubmit(MiniGamePanelContent demo)
        {
            if (demo.crosswordAnswerInput == null) return;
            string a = GridThemeBank.SanitizeForGrid(demo.crosswordAnswerInput.text ?? "");
            bool hadInput = !string.IsNullOrEmpty(a);
            string matchedWord = "";
            bool ok = hadInput && demo.TryMatchAnyUnsolvedGridWord(a, out matchedWord);
            if (demo.crosswordFeedback != null)
            {
                demo.crosswordFeedback.text = !hadInput
                    ? "Écris une réponse d'après les définitions puis Valider."
                    : (ok
                        ? "Trouvé ! « " + matchedWord + " ». (" + (demo.currentGridSolved != null ? demo.currentGridSolved.Count : 0) + "/"
                        + (demo.currentGridAllWords != null ? demo.currentGridAllWords.Count : 0) + " pour cette session.)"
                        : "Ce mot n’apparaît pas ainsi dans la grille, ou il est déjà validé — réessaie.");
            }

            GameSfxHub.Instance?.PlayResult(ok);
            if (ok && (demo.currentGridSolved == null || !demo.currentGridSolved.Contains(matchedWord)))
            {
                if (demo.currentGridSolved != null) demo.currentGridSolved.Add(matchedWord);
                demo.LastCrosswordGuessed = string.IsNullOrEmpty(demo.LastCrosswordGuessed) ? matchedWord : (demo.LastCrosswordGuessed + " · " + matchedWord);
                demo.RecordGridWordFound(matchedWord);
                int points = demo.GrantGridWordPoints();
                demo.ShowGridFoundToast(matchedWord, points);
                demo.ApplyCrosswordFoundWordHighlight(matchedWord);
            }

            if (ok) MaybeAdvanceMiniGameAfterResponse();
        }

        private static void OnMysterySubmit(MiniGamePanelContent demo)
        {
            if (demo.mysteryAnswerInput == null) return;
            string a = demo.mysteryAnswerInput.text?.Trim().ToUpperInvariant() ?? "";
            bool hadInput = !string.IsNullOrEmpty(a);
            string target = (demo.CurrentMysteryAnswer ?? "").Trim().ToUpperInvariant();
            bool ok = hadInput && a == target;
            if (demo.mysteryFeedback != null)
            {
                demo.mysteryFeedback.text = !hadInput
                    ? "Réponse vide — passage au mode suivant."
                    : (ok ? "Bravo !" : "Pas exact — le mot complet était « " + target + " ».");
            }

            GameSfxHub.Instance?.PlayResult(ok);
            MaybeAdvanceMiniGameAfterResponse();
        }

        private static void OnImageGuessSubmit(MiniGamePanelContent demo)
        {
            if (demo.imageRevealCo != null)
            {
                if (demo.imageGuessFeedback != null)
                {
                    demo.imageGuessFeedback.text = "Attends la fin de la révélation (flou → net), puis valide.";
                }

                return;
            }

            if (demo.imageGuessInput == null) return;
            string t = demo.imageGuessInput.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(t))
            {
                if (demo.imageGuessFeedback != null)
                {
                    demo.imageGuessFeedback.text = "Écris une réponse avant de valider.";
                }

                return;
            }

            bool ok = MiniGameDemoBanks.ImageGuessMatches(demo.CurrentImageGuessRound, t);
            GameSfxHub.Instance?.StopBlindDemoMusic();
            if (demo.imageGuessFeedback != null)
            {
                string key = demo.CurrentImageGuessRound.AnswerKey;
                demo.imageGuessFeedback.text = ok
                    ? "Bravo — on attendait bien quelque chose comme « " + key + " »."
                    : "Ce n’était pas ça — la réponse attendue était : « " + key + " ».";
            }

            GameSfxHub.Instance?.PlayResult(ok);
            MaybeAdvanceMiniGameAfterResponse();
        }

        private static GameObject PanelShell(Transform parent, string name, string modeId, Color bg, Sprite white, ModeSurfaceController surf)
        {
            GameObject go = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image img = go.AddComponent<Image>();
            img.sprite = white;
            img.color = bg;
            img.raycastTarget = true;
            Outline ol = go.AddComponent<Outline>();
            ol.effectColor = new Color(0.95f, 0.82f, 0.2f, 0.34f);
            ol.effectDistance = new Vector2(2f, -2f);
            go.SetActive(false);
            surf.Register(modeId, go);
            return go;
        }

        private static Text Title(Transform parent, Font font, string name, string txt, Vector2 pos)
        {
            GameObject go = CreateRect(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(900f, 56f));
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 34;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.9f, 0.2f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 20;
            t.resizeTextMaxSize = 32;
            t.text = txt;
            return t;
        }

        private static Text Sub(Transform parent, Font font, string name, Vector2 pos)
        {
            GameObject go = CreateRect(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(940f, 108f));
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 28;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.text = "";
            return t;
        }

        private static Text CreateText(Transform parent, string name, Font font, int fontSize, TextAnchor align,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = CreateRect(parent, name, anchorMin, anchorMax, anchoredPosition, sizeDelta);
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = Color.white;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = "";
            return t;
        }

        private static Text BigLetters(Transform parent, Font font, string name, Vector2 pos, int size)
        {
            GameObject go = CreateRect(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(1000f, 120f));
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = "";
            return t;
        }

        private static Text BigLettersCentered(Transform parent, Font font, string name, int size)
        {
            GameObject go = CreateRect(parent, name, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 120f));
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = "";
            return t;
        }

        /// <summary>Chiffre chrono ancré dans un pourcentage du parent (évite 960px qui débordent d’un hôte 400px).</summary>
        private static Text BigLettersInRegion(Transform parent, Font font, string name, int fontSize, float ax, float ay, float bx, float by)
        {
            GameObject go = CreateRect(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(bx, by);
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 30;
            t.resizeTextMaxSize = fontSize;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            t.text = "";
            return t;
        }

        private static Text[] LetterGrid(Transform parent, Font font, int cols, int rows, float cell, float gap, Vector2 centerOffset)
        {
            Text[] cells = new Text[cols * rows];
            float w = cols * cell + (cols - 1) * gap;
            float h = rows * cell + (rows - 1) * gap;
            Vector2 origin = new Vector2(-w * 0.5f + cell * 0.5f, -h * 0.5f + cell * 0.5f);
            int k = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    float x = origin.x + c * (cell + gap);
                    float y = origin.y + (rows - 1 - r) * (cell + gap);
                    GameObject cellGo = CreateRect(parent, "Cell" + k, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), centerOffset + new Vector2(x, y), new Vector2(cell, cell));
                    Image bg = cellGo.AddComponent<Image>();
                    bg.sprite = White();
                    Color[] sPal =
                    {
                        new Color(0.15f, 0.16f, 0.19f, 0.98f),
                        new Color(0.19f, 0.2f, 0.23f, 0.98f),
                        new Color(0.17f, 0.18f, 0.21f, 0.98f)
                    };
                    bg.color = sPal[(r + c) % sPal.Length];
                    bg.raycastTarget = false;
                    GameObject txGo = CreateRect(cellGo.transform, "T", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Text tx = txGo.AddComponent<Text>();
                    tx.font = font;
                    tx.raycastTarget = false;
                    ConfigureGridLetter(tx, cell);
                    cells[k++] = tx;
                }
            }

            return cells;
        }

        private static Text[] ChoiceColumn(Transform parent, Font font, Vector2 start, float stepY)
        {
            Text[] arr = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject go = CreateRect(parent, "Choice" + i, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), start + new Vector2(0f, -i * stepY), new Vector2(920f, 42f));
                Text t = go.AddComponent<Text>();
                t.font = font;
                t.fontSize = 24;
                t.alignment = TextAnchor.MiddleLeft;
                t.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                arr[i] = t;
            }

            return arr;
        }

        private static void BlindChoiceColumn(Transform parent, Font font, Vector2 start, float stepY, MiniGamePanelContent demo)
        {
            Text[] arr = new Text[4];
            Button[] btns = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                GameObject go = CreateRect(parent, "BlindChoice" + i, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), start + new Vector2(0f, -i * stepY), new Vector2(940f, 60f));
                Image rowBg = go.AddComponent<Image>();
                rowBg.sprite = White();
                rowBg.color = new Color(0.08f, 0.1f, 0.16f, 0.96f);
                Button hit = go.AddComponent<Button>();
                hit.targetGraphic = rowBg;
                int ix = i;
                hit.onClick.AddListener(() =>
                {
                    GameSfxHub.Instance?.PlayTap();
                    demo.NotifyBlindPick(ix);
                });
                WireBlindAnswerRowHover(hit, rowBg);
                GameObject txGo = CreateRect(go.transform, "Txt", Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-24f, 0f));
                Text t = txGo.AddComponent<Text>();
                t.font = font;
                t.fontSize = 28;
                t.alignment = TextAnchor.MiddleLeft;
                t.color = new Color(0.96f, 0.96f, 0.98f, 1f);
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                t.verticalOverflow = VerticalWrapMode.Overflow;
                t.raycastTarget = false;
                arr[i] = t;
                btns[i] = hit;
            }

            demo.blindChoices = arr;
            demo.blindChoiceButtons = btns;
        }

        private static readonly Color BlindRowIdle = new Color(0.08f, 0.1f, 0.16f, 0.96f);
        private static readonly Color BlindRowHover = new Color(0.16f, 0.28f, 0.48f, 0.96f);

        private static void WireBlindAnswerRowHover(Button btn, Image rowBg)
        {
            if (btn == null || rowBg == null)
            {
                return;
            }

            EventTrigger trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = btn.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (btn.interactable)
                {
                    rowBg.color = BlindRowHover;
                }
            });
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (btn.interactable)
                {
                    rowBg.color = BlindRowIdle;
                }
            });
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);
        }

        private static Button[] MemoryGrid(Transform parent, Font font, Sprite white, float cell = 118f, float gap = 14f)
        {
            Button[] btns = new Button[8];
            int col = 4;
            int row = 2;
            float w = col * cell + (col - 1) * gap;
            float h = row * cell + (row - 1) * gap;
            Vector2 o = new Vector2(-w * 0.5f + cell * 0.5f, -h * 0.5f + cell * 0.5f);
            for (int r = 0; r < row; r++)
            {
                for (int c = 0; c < col; c++)
                {
                    int idx = r * col + c;
                    GameObject go = CreateRect(parent, "Card" + idx, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), o + new Vector2(c * (cell + gap), (row - 1 - r) * (cell + gap)), new Vector2(cell, cell));
                    Image img = go.AddComponent<Image>();
                    img.sprite = white;
                    img.color = new Color(0.15f, 0.18f, 0.24f, 1f);
                    Button b = go.AddComponent<Button>();
                    GameObject txGo = CreateRect(go.transform, "Lbl", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
                    Text tx = txGo.AddComponent<Text>();
                    tx.font = font;
                    tx.fontSize = 44;
                    tx.alignment = TextAnchor.MiddleCenter;
                    tx.color = Color.white;
                    tx.raycastTarget = false;
                    tx.text = "?";
                    btns[idx] = b;
                }
            }

            return btns;
        }

        private static RawImage ImageBlock(Transform parent, Sprite white, Vector2 pos, Vector2 size)
        {
            GameObject go = CreateRect(parent, "ImgBlock", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, size);
            RawImage r = go.AddComponent<RawImage>();
            r.color = new Color(0.7f, 0.65f, 0.2f, 1f);
            return r;
        }

        private static RawImage ImageBlockCentered(Transform parent, Sprite white, Vector2 size)
        {
            GameObject go = CreateRect(parent, "ImgBlock", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            RawImage r = go.AddComponent<RawImage>();
            r.color = new Color(0.7f, 0.65f, 0.2f, 1f);
            return r;
        }

        private static InputField BuildInputField(Transform parent, Font font, string name, Vector2 pos, float width, string placeholder = "Tape le mot…")
        {
            GameObject go = CreateRect(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(width, 62f));
            Image bg = go.AddComponent<Image>();
            bg.sprite = White();
            bg.color = new Color(0.03f, 0.05f, 0.08f, 1f);
            InputField field = go.AddComponent<InputField>();
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            RectTransform trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 8f);
            trt.offsetMax = new Vector2(-14f, -8f);
            Text text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 28;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.supportRichText = false;
            GameObject phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            RectTransform prt = phGo.AddComponent<RectTransform>();
            prt.anchorMin = Vector2.zero;
            prt.anchorMax = Vector2.one;
            prt.offsetMin = new Vector2(14f, 8f);
            prt.offsetMax = new Vector2(-14f, -8f);
            Text ph = phGo.AddComponent<Text>();
            ph.font = font;
            ph.fontSize = 24;
            ph.fontStyle = FontStyle.Italic;
            ph.alignment = TextAnchor.MiddleLeft;
            ph.color = new Color(1f, 1f, 1f, 0.35f);
            ph.text = placeholder;
            field.textComponent = text;
            field.placeholder = ph;
            return field;
        }

        private static Button BuildPrimaryButton(Transform parent, Font font, string label, Vector2 pos, Action onClick)
        {
            GameObject go = CreateRect(parent, "BtnSubmit", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(260f, 56f));
            Image img = go.AddComponent<Image>();
            img.sprite = White();
            img.color = new Color(0.12f, 0.48f, 0.28f, 1f);
            Button b = go.AddComponent<Button>();
            GameObject txGo = CreateRect(go.transform, "Txt", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text tx = txGo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 23;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            tx.text = label;
            b.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        private static Button BuildSecondaryButton(Transform parent, Font font, string label, Vector2 pos, Action onClick)
        {
            GameObject go = CreateRect(parent, "BtnSecondary", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(220f, 52f));
            Image img = go.AddComponent<Image>();
            img.sprite = White();
            img.color = new Color(0.22f, 0.25f, 0.32f, 1f);
            Button b = go.AddComponent<Button>();
            GameObject txGo = CreateRect(go.transform, "Txt", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text tx = txGo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 21;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = new Color(0.95f, 0.95f, 0.95f, 1f);
            tx.text = label;
            b.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        private static GameObject CreateRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
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
    }
}
