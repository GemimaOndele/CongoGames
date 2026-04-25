using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CongoGames.AI;
using CongoGames.Core;
using CongoGames.Network;

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
        private const float CrosswordCellPx = 36f;
        private const float CrosswordGapPx = 5f;
        private const float ScrambleCellPx = 46f;
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

        /// <summary>Mot attendu pour la démo « associations » (sans l’afficher si faux).</summary>
        public string CurrentSemanticTarget { get; private set; } = "CON";

        [Header("Mots — historique (grilles)")]
        [Tooltip("Liste des mots déjà validés (mots mélangés).")]
        public Text gridFoundList;
        [Tooltip("Même liste pour mots cachés (grille 10×10).")]
        public Text gridFoundListCross;

        [Header("Mots croisés")]
        public Text crosswordTitle;
        public Text[] crosswordCells = new Text[100];
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
        private bool blindInQuestionPhase;
        private Coroutine blindListenCo;
        private float blindListenEndUnscaled;
        private float blindListenWindowSec;
        private Image imageGuessVeil;
        [SerializeField] private float imageGuessRevealSec = 15f;
        private Coroutine imageRevealCo;
        private float imageRevealEndUnscaled;
        private float imageRevealWindowSec;
        private const float ImageGuessRevealingAlpha = 0.42f;

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
        private readonly string[] memoryPairLetters = { "A", "A", "B", "B", "C", "C", "D", "D" };
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

        [Header("Grilles thématiques (mots mélangés / mots cachés)")]
        [Tooltip("Démo : 2 manches = 2 grilles thématiques (5–12 mots) avant le prochain mode ; en live, le chrono de la manche continue.")]
        [SerializeField] private int gridSessionsPerThematicBlock = 2;
        [SerializeField] private int gridThematicWordCount = 8;
        private int gridMotsJeuSessionIndex; // 1 = premier mot, …
        private int gridThematicBlockRound; // 0 = non initialisé, 1 = 1re grille, 2 = 2e…
        private string currentGridThemeLabel = "";
        private System.Collections.Generic.List<string> currentGridAllWords;
        private System.Collections.Generic.HashSet<string> currentGridSolved;
        private readonly System.Collections.Generic.List<string> gridFoundHistory = new System.Collections.Generic.List<string>();
        [Tooltip("Cible active (mots mélangés + mots cachés) — rétro compatible avec CurrentScrambleAnswer.")]
        public string CurrentScrambleTargetWord { get; private set; } = "CONGO";
        [Tooltip("Dernier(s) mots reconnus sur mots cachés (séparateur « · »)")]
        public string LastCrosswordGuessed { get; private set; } = "";
        [Tooltip("Grille mots cachés : 10×10 (H/V/diagonales, mots moins « alignés » visuellement).")]
        private const int WordSearchSize = 10;
        private char[,] lastWordSearchGrid;
        private string lastPopulatedModeId = "";

        private void RefreshGridFoundPanel()
        {
            if (gridFoundHistory == null || gridFoundHistory.Count == 0)
            {
                string empty = "— Aucun mot validé encore —";
                if (gridFoundList != null) gridFoundList.text = empty;
                if (gridFoundListCross != null) gridFoundListCross.text = empty;
                return;
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < gridFoundHistory.Count; i++)
            {
                if (i > 0) sb.Append("\n");
                sb.Append("• ").Append(gridFoundHistory[i]);
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
                wordHint.text = "Thème : " + (currentGridThemeLabel ?? "—")
                    + " — " + pick.Count + " mot(s) encore à trouver (ordre libre) — "
                    + chosen.Length + " lettres : réorganise la ligne jaune (leurres autour), Valider.";
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

            gmm.ScheduleNextMode(1.1f);
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
            gridFoundHistory.Clear();
            RefreshGridFoundPanel();
            AdvanceGridMotsJeu();
        }

        /// <returns>True si le mode peut enchaîner (bloc 2/2 fini, ou mots cachés session complète).</returns>
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

        private void RestoreCrosswordCellDecor(int cellIndex)
        {
            if (crosswordCells == null || cellIndex < 0 || cellIndex >= crosswordCells.Length) return;
            Text tx = crosswordCells[cellIndex];
            if (tx == null) return;
            int r = cellIndex / CrosswordCols;
            int c = cellIndex % CrosswordCols;
            Image bg = tx.transform.parent != null ? tx.transform.parent.GetComponent<Image>() : null;
            if (bg != null) bg.color = CrosswordDecoBg(r, c);
        }

        private void HighlightCrosswordSelection(int cellIndex)
        {
            if (crosswordSelectedCell >= 0)
            {
                RestoreCrosswordCellDecor(crosswordSelectedCell);
            }

            crosswordSelectedCell = cellIndex;
            if (cellIndex < 0 || crosswordCells == null || cellIndex >= crosswordCells.Length) return;
            Text tx = crosswordCells[cellIndex];
            if (tx == null) return;
            Image bg = tx.transform.parent != null ? tx.transform.parent.GetComponent<Image>() : null;
            if (bg != null) bg.color = new Color(0.32f, 0.38f, 0.48f, 1f);
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            EnsureGridsIfMissing();
            ModeSurfaceController surf = GetComponentInParent<ModeSurfaceController>();
            if (surf != null && !string.IsNullOrEmpty(surf.CurrentModeId))
            {
                Populate(surf.CurrentModeId);
            }
        }

        private void OnDestroy()
        {
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

            if (blindEmojiPulseCo != null)
            {
                StopCoroutine(blindEmojiPulseCo);
                blindEmojiPulseCo = null;
            }

            GameSfxHub.Instance?.StopBlindDemoMusic();
            if (Instance == this) Instance = null;
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
                    break;
                case "semantic":
                    ApplySemanticDemo();
                    break;
                case "word-scramble":
                    ApplyWordScrambleDemo();
                    break;
                case "crossword-lite":
                    ApplyCrosswordDemo();
                    break;
                case "blind-test":
                    ApplyBlindDemo();
                    break;
                case "mystery-word":
                    ApplyMysteryDemo();
                    break;
                case "memory":
                    ApplyMemoryDemo();
                    break;
                case "speed-chrono":
                    ApplyChronoDemo();
                    break;
                case "image-guess":
                    StartCoroutine(CoApplyImageDemo());
                    break;
            }
        }

        private void ApplySemanticDemo()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool liveMode = live != null && live.IsConnected;
            CurrentSemanticTarget = "CON";

            if (semanticTitle != null)
            {
                semanticTitle.text = "Associations — lettres uniquement (démo)";
            }

            string[] cells =
            {
                "C", "O", "N",
                "G", "O", "H",
                "R", "I", "V"
            };
            Color[] cellBack = new Color[9];
            for (int i = 0; i < cellBack.Length; i++)
            {
                int row = i / 3;
                int col = i % 3;
                cellBack[i] = (row + col) % 2 == 0
                    ? new Color(0.14f, 0.15f, 0.18f, 1f)
                    : new Color(0.19f, 0.2f, 0.23f, 1f);
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
                ConfigureGridLetter(semanticCells[i], 86f);
            }

            if (semanticHint != null)
            {
                semanticHint.text = liveMode
                    ? "Mode live : propose ta réponse dans le chat."
                    : "Associe les lettres de la grille à un mot ; tape ta proposition puis Valider.";
            }

            if (semanticAnswerInput != null)
            {
                semanticAnswerInput.text = "";
                semanticAnswerInput.interactable = true;
                semanticAnswerInput.gameObject.SetActive(true);
            }

            if (semanticSubmitButton != null)
            {
                semanticSubmitButton.gameObject.SetActive(true);
                semanticSubmitButton.interactable = true;
            }

            if (semanticClearButton != null)
            {
                semanticClearButton.gameObject.SetActive(true);
                semanticClearButton.interactable = true;
            }

            if (semanticFeedback != null) semanticFeedback.text = "";
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
                wordScrambleTitle.text = "Mots mélangés — " + (currentGridThemeLabel ?? "thème Congo")
                    + " · mot " + gridMotsJeuSessionIndex + "/" + (currentGridAllWords != null ? currentGridAllWords.Count : 0);
            }

            Font tileFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            string target = (CurrentScrambleTargetWord ?? "CONGO").Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(target) && currentGridAllWords != null && currentGridAllWords.Count > 0)
            {
                target = currentGridAllWords[0];
            }

            CurrentScrambleAnswer = target;
            CurrentScrambleTargetWord = target;
            int seed = (target + gridThematicBlockRound + Time.frameCount).GetHashCode() ^ 0x5a5a5a5a;
            string scrambled = ScrambleWord(target, seed);
            for (int guard = 0; guard < 8 && string.Equals(scrambled, target, StringComparison.Ordinal) && target.Length > 1; guard++)
            {
                seed = seed * 31 + 7;
                scrambled = ScrambleWord(target, seed);
            }
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
                }
            }

            List<int> slots = new List<int>(nCells);
            for (int i = 0; i < nCells; i++) slots.Add(i);
            for (int i = slots.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (slots[i], slots[j]) = (slots[j], slots[i]);
            }

            bool[] playableTile = new bool[nCells];
            int need = Mathf.Min(scrambled.Length, slots.Count);
            for (int k = 0; k < need; k++)
            {
                int idx = slots[k];
                if (wordScrambleTiles != null && idx < wordScrambleTiles.Length && wordScrambleTiles[idx] != null)
                {
                    wordScrambleTiles[idx].text = scrambled[k].ToString();
                    wordScrambleTiles[idx].color = new Color(1f, 0.96f, 0.35f, 1f);
                    playableTile[idx] = true;
                }
            }

            const string decoyPool = "AEIOUBCDFGHJKLMNPQRSTVWXYZ";
            for (int i = 0; i < nCells; i++)
            {
                if (wordScrambleTiles == null || i >= wordScrambleTiles.Length) continue;
                Text cell = wordScrambleTiles[i];
                if (cell == null || !string.IsNullOrEmpty(cell.text)) continue;
                cell.text = decoyPool[UnityEngine.Random.Range(0, decoyPool.Length)].ToString();
                cell.color = new Color(0.88f, 0.9f, 0.96f, 1f);
            }

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
                int n = (CurrentScrambleAnswer ?? "").Trim().Length;
                string th = (currentGridThemeLabel ?? "—");
                if (n > 0)
                {
                    wordHint.text = liveMode
                        ? "Thème : " + th + " — " + n + " lettres · mot " + gridMotsJeuSessionIndex + "/" + (currentGridAllWords != null ? currentGridAllWords.Count : 0) + "."
                        : "Thème : " + th + " — mot " + gridMotsJeuSessionIndex + "/" + (currentGridAllWords != null ? currentGridAllWords.Count : 0) + " · " + n + " lettres.";
                }
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
                wordAnswerInput.interactable = true;
                wordAnswerInput.gameObject.SetActive(true);
            }

            if (wordSubmitButton != null)
            {
                wordSubmitButton.gameObject.SetActive(true);
                wordSubmitButton.interactable = true;
            }

            if (wordClearButton != null)
            {
                wordClearButton.gameObject.SetActive(true);
                wordClearButton.interactable = true;
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
            int size = WordSearchSize;
            var toTry = new System.Collections.Generic.List<string>(currentGridAllWords);
            char[,] grid = null;
            for (int attempt = 0; attempt < 12 && toTry.Count >= GridThemeBank.MinWords; attempt++)
            {
                if (WordSearchPlacer.TryBuild(size, toTry, 200, out grid))
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
                WordSearchPlacer.TryBuild(size, toTry, 300, out grid);
            }

            if (grid == null) return;
            if (toTry.Count < currentGridAllWords.Count)
            {
                currentGridAllWords = toTry;
                currentGridSolved = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
                AdvanceGridMotsJeu();
            }

            lastWordSearchGrid = grid;
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
                    char ch = grid[r, c];
                    crosswordCells[idx].text = ch.ToString();
                    crosswordCells[idx].fontStyle = FontStyle.Bold;
                    crosswordCells[idx].color = new Color(1f, 0.94f, 0.22f, 1f);
                    Transform parent = crosswordCells[idx].transform.parent;
                    Image cellBg = parent != null ? parent.GetComponent<Image>() : null;
                    if (cellBg != null) cellBg.color = CrosswordDecoBg(r, c);
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
            }
        }

        private void ApplyCrosswordDemo()
        {
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
                crosswordTitle.text = "Mots cachés (7×7) — " + (currentGridThemeLabel ?? "Congo");
            }

            crosswordSelectedCell = -1;
            BuildWordSearchAndFillGrid();
            if (crosswordAnswerInput != null)
            {
                crosswordAnswerInput.text = "";
                crosswordAnswerInput.interactable = true;
                crosswordAnswerInput.gameObject.SetActive(true);
            }

            if (crosswordClearButton != null)
            {
                crosswordClearButton.gameObject.SetActive(true);
                crosswordClearButton.interactable = true;
            }

            if (crosswordSubmitButton != null)
            {
                crosswordSubmitButton.gameObject.SetActive(true);
                crosswordSubmitButton.interactable = true;
            }

            if (crosswordFeedback != null)
            {
                string t = (CurrentScrambleTargetWord ?? "CONGO").Trim();
                crosswordFeedback.text = "Trouve un mot caché en ligne ou en colonne (7×7) — tente " + t.Length
                    + " lettres, mot " + gridMotsJeuSessionIndex + " / " + (currentGridAllWords != null ? currentGridAllWords.Count : 0) + " — thème " + (currentGridThemeLabel ?? "—");
            }

            if (crosswordButtons != null)
            {
                foreach (Button b in crosswordButtons)
                {
                    if (b != null) b.interactable = true;
                }
            }
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
            lastBlindCorrectDisplayIndex = r.CorrectIndex;
            string cat = string.IsNullOrEmpty(raw.CategoryLabel) ? "Blind test" : raw.CategoryLabel;
            if (blindTitle != null) blindTitle.text = "Blind test — " + cat;
            if (blindPrompt != null) blindPrompt.text = r.Prompt;
            if (blindSub != null)
            {
                int sec = Mathf.Clamp(Mathf.RoundToInt(blindListenSeconds), 15, 90);
                blindSub.text = r.SubLine
                    + "\n▶ Écoute l’extrait " + sec
                    + " s (fichier : StreamingAssets/Theme/BlindTest/ ou URL dans la banque) — la musique s’arrête, puis seulement tu choisis A–D.";
            }

            if (blindEmoji != null)
            {
                blindEmoji.text = "♪  Écoute  ♪";
                blindEmojiPulseCo = StartCoroutine(CoPulseBlindEmoji());
            }
            string[] letters = { "A", "B", "C", "D" };
            for (int i = 0; i < blindChoices.Length && r.Choices != null && i < r.Choices.Length; i++)
            {
                if (blindChoices[i] != null)
                {
                    blindChoices[i].text = letters[i] + ". " + r.Choices[i];
                }
            }

            blindInQuestionPhase = false;
            SetBlindChoicesInteractable(false);
            GameSfxHub.Instance?.PlayBlindDrumCue();
            int musicSeed = (r.Prompt ?? "blind").GetHashCode();
            float listen = blindListenSeconds < 0.5f ? 0f : blindListenSeconds;
            blindListenCo = StartCoroutine(CoBlindListenThenQuestion(musicSeed, raw, listen));
        }

        private void SetBlindChoicesInteractable(bool on)
        {
            if (blindChoiceButtons == null) return;
            foreach (Button b in blindChoiceButtons)
            {
                if (b != null) b.interactable = on;
            }
        }

        private IEnumerator CoBlindListenThenQuestion(int musicSeed, MiniGameDemoBanks.BlindRound raw, float listen)
        {
            GameSfxHub.Instance?.PlayBlindDemoMusic(musicSeed, raw.AudioFileBase, raw.AudioUrl);
            float wait = listen > 0.01f ? listen : 0.35f;
            blindListenWindowSec = wait;
            blindListenEndUnscaled = Time.unscaledTime + wait;
            yield return new WaitForSecondsRealtime(wait);
            blindListenEndUnscaled = 0f;
            GameSfxHub.Instance?.StopBlindDemoMusic();
            blindInQuestionPhase = true;
            SetBlindChoicesInteractable(true);
            if (blindSub != null)
            {
                blindSub.text = (string.IsNullOrEmpty(raw.SubLine) ? "" : raw.SubLine + "\n")
                    + "L’écoute est terminée. Choisis A, B, C ou D ci-dessous.";
            }

            if (blindEmoji != null) blindEmoji.text = "?  Réponds  ?";
            blindListenCo = null;
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
            bool ok = choiceIndex == lastBlindCorrectDisplayIndex;
            blindInQuestionPhase = false;
            SetBlindChoicesInteractable(false);
            GameSfxHub.Instance?.PlayResult(ok);
            MaybeAdvanceMiniGameAfterResponse();
        }

        private void ApplyMysteryDemo()
        {
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool liveMode = live != null && live.IsConnected;
            string w = MiniGameDemoBanks.NextMysteryWord();
            CurrentMysteryAnswer = (w ?? "CONGO").Trim().ToUpperInvariant();
            if (mysteryTitle != null) mysteryTitle.text = "Mot mystère — devine le mot (indices ci-dessous)";
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

            if (mysteryFeedback != null) mysteryFeedback.text = "";
        }

        private void ApplyMemoryDemo()
        {
            if (memoryTitle != null) memoryTitle.text = "Mémoire — les paires cachées";
            if (memorySubtitle != null)
            {
                memorySubtitle.text = "Touche deux cartes : si les deux lettres sont pareilles, la paire reste ouverte.";
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
                    tx.fontSize = 52;
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
                    memorySubtitle.text = "Bien — ouvre une autre carte pour trouver la même lettre.";
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
                        : "Paire trouvée — il reste " + pairsLeft + " paire(s) à trouver.";
                }

                if (openLeft == 0)
                {
                    MaybeAdvanceMiniGameAfterResponse();
                }

                return;
            }

            if (memorySubtitle != null)
            {
                memorySubtitle.text = "Pas la même — les cartes se referment.";
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
                memorySubtitle.text = "Touche deux cartes : si les deux lettres sont pareilles, la paire reste ouverte.";
            }
        }

        private void ApplyChronoDemo()
        {
            EnsureChronoSubMeta();
            chronoModeActive = true;
            chronoSessionScore = 0;
            chronoStreak = 0;
            chronoRoundInSession = 0;
            if (chronoTitle != null) chronoTitle.text = "Chrono vitesse — 3 vagues (réagis vite)";

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
            if (!chronoModeActive || chronoPhase != 1) return;
            if (slot > 3) return;
            bool timeUp = (slot < 0);
            if (timeUp)
            {
                chronoStreak = 0;
                chronoLastRoundPoints = 0;
                chronoResultFlash = "Temps ! c’était " + (1 + chronoTargetSlot) + " (touches 1–4). Aucun point : tu n’as pas appuyé à temps — ce n’est pas une « mauvaise réponse ».";
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
            if (GameInput.DigitKeyDown1To9(1)) OnChronoInput(0);
            else if (GameInput.DigitKeyDown1To9(2)) OnChronoInput(1);
            else if (GameInput.DigitKeyDown1To9(3)) OnChronoInput(2);
            else if (GameInput.DigitKeyDown1To9(4)) OnChronoInput(3);

            ChronoTick();
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
            if (imageRevealCo != null)
            {
                StopCoroutine(imageRevealCo);
                imageRevealCo = null;
            }

            CurrentImageGuessRound = MiniGameDemoBanks.NextImageGuessRound();
            if (imageTitle != null) imageTitle.text = "Devine l’image — Congo";
            if (imageCaption != null)
            {
                string extra = string.IsNullOrEmpty(CurrentImageGuessRound.Trivia)
                    ? ""
                    : "\n" + CurrentImageGuessRound.Trivia;
                imageCaption.text = CurrentImageGuessRound.Hint + extra;
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

                Texture2D tex = null;
                yield return ImageGuessVisuals.CoResolveTexture(
                    CurrentImageGuessRound.StreamingFileBase,
                    CurrentImageGuessRound.StyleSeed,
                    t => tex = t);
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

                imageRevealCo = StartCoroutine(CoImageRevealUnblur(imageGuessRevealSec));
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
                string baseHint = (CurrentImageGuessRound.Hint ?? "") +
                    (string.IsNullOrEmpty(CurrentImageGuessRound.Trivia) ? "" : "\n" + CurrentImageGuessRound.Trivia);
                imageCaption.text = "Image floue : devine d’abord, puis l’image se précise. " + baseHint;
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
                string h = (CurrentImageGuessRound.Hint ?? "") +
                    (string.IsNullOrEmpty(CurrentImageGuessRound.Trivia) ? "" : "\n" + CurrentImageGuessRound.Trivia);
                imageCaption.text = "Image révélée. " + h;
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
                        new Color(0.15f, 0.16f, 0.19f, 1f),
                        new Color(0.19f, 0.2f, 0.23f, 1f),
                        new Color(0.17f, 0.18f, 0.21f, 1f)
                    };
                    bg.color = pal[(r + c) % pal.Length];
                    bg.raycastTarget = true;
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

            GameObject semantic = PanelShell(modeRoot, "PanelSemantic", "semantic", new Color(0.06f, 0.12f, 0.1f, 0.96f), white, surf);
            demo.semanticTitle = Title(semantic.transform, font, "TitreSemantic", "Associations — grille", new Vector2(0f, -34f));
            Transform semHost = CreateGridHost(semantic.transform, "SemanticGridHost", 0.57f, new Vector2(0f, 92f), new Vector2(920f, 360f));
            demo.semanticCells = LetterGrid(semHost, font, 3, 3, 86f, 16f, Vector2.zero);
            GameObject semFoot = CreateRect(semantic.transform, "SemanticFooter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(980f, 220f));
            Image semFootBg = semFoot.AddComponent<Image>();
            semFootBg.sprite = white;
            semFootBg.color = new Color(0.04f, 0.06f, 0.08f, 0.65f);
            semFootBg.raycastTarget = false;
            demo.semanticHint = Sub(semFoot.transform, font, "SemHint", new Vector2(0f, -28f));
            demo.semanticHint.rectTransform.sizeDelta = new Vector2(900f, 56f);
            demo.semanticAnswerInput = BuildInputField(semFoot.transform, font, "SemAnswer", new Vector2(35f, -92f), 400f, "Tape ta réponse…");
            demo.semanticClearButton = BuildSecondaryButton(semFoot.transform, font, "Effacer", new Vector2(-400f, -92f), () =>
            {
                if (demo.semanticAnswerInput != null) demo.semanticAnswerInput.text = "";
            });
            demo.semanticSubmitButton = BuildPrimaryButton(semFoot.transform, font, "Valider", new Vector2(300f, -92f), () => OnSemanticSubmit(demo));
            demo.semanticFeedback = Sub(semFoot.transform, font, "SemFb", new Vector2(0f, -156f));

            GameObject word = PanelShell(modeRoot, "PanelWordScramble", "word-scramble", new Color(0.08f, 0.08f, 0.14f, 0.96f), white, surf);
            demo.wordScrambleTitle = Title(word.transform, font, "WTitle", "Mots mélangés", new Vector2(0f, -34f));
            demo.wordScrambleLetters = null;
            GameObject wordSide = CreateRect(word.transform, "WordFoundSide", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(220f, 480f));
            Image wsBg = wordSide.AddComponent<Image>();
            wsBg.sprite = white;
            wsBg.color = new Color(0.05f, 0.1f, 0.12f, 0.9f);
            Text wsTitle = CreateText(wordSide.transform, "FoundTitle", font, 20, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -8f), new Vector2(-16f, 32f));
            wsTitle.fontStyle = FontStyle.Bold;
            wsTitle.color = new Color(0.4f, 0.95f, 0.55f, 1f);
            wsTitle.text = "Déjà trouvés";
            demo.gridFoundList = CreateText(wordSide.transform, "FoundList", font, 18, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -40f), new Vector2(-12f, 420f));
            demo.gridFoundList.color = new Color(0.9f, 0.95f, 0.75f, 1f);
            demo.gridFoundList.alignment = TextAnchor.UpperLeft;
            demo.gridFoundList.horizontalOverflow = HorizontalWrapMode.Wrap;
            demo.gridFoundList.text = "—";
            Transform wordHost = CreateGridHost(word.transform, "WordGridHost", 0.53f, new Vector2(-100f, 124f), new Vector2(760f, 430f));
            BuildScrambleLetterGrid(wordHost, font, 7, 7, ScrambleCellPx, ScrambleGapPx, Vector2.zero, demo);
            WireLetterGridDrag(wordHost, demo, false, demo.wordScrambleTiles);

            GameObject wordFoot = CreateRect(word.transform, "WordFooter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(980f, 220f));
            Image wfBg = wordFoot.AddComponent<Image>();
            wfBg.sprite = white;
            wfBg.color = new Color(0.04f, 0.05f, 0.08f, 0.62f);
            wfBg.raycastTarget = false;

            demo.wordHint = Sub(wordFoot.transform, font, "Hint", new Vector2(0f, -32f));
            demo.wordHint.rectTransform.sizeDelta = new Vector2(900f, 52f);
            demo.wordAnswerInput = BuildInputField(wordFoot.transform, font, "AnswerField", new Vector2(20f, -96f), 400f);
            demo.wordClearButton = BuildSecondaryButton(wordFoot.transform, font, "Effacer", new Vector2(-400f, -96f), () => demo.ClearWordGuess());
            demo.wordSubmitButton = BuildPrimaryButton(wordFoot.transform, font, "Valider", new Vector2(300f, -96f), () => OnWordSubmit(demo));
            demo.wordFeedback = Sub(wordFoot.transform, font, "WordFb", new Vector2(0f, -162f));

            GameObject cross = PanelShell(modeRoot, "PanelCrossword", "crossword-lite", new Color(0.05f, 0.1f, 0.12f, 0.96f), white, surf);
            demo.crosswordTitle = Title(cross.transform, font, "CTitle", "Mots cachés 10×10", new Vector2(0f, -34f));
            GameObject crossSide = CreateRect(cross.transform, "CrossFoundSide", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(220f, 480f));
            Image crBg = crossSide.AddComponent<Image>();
            crBg.sprite = white;
            crBg.color = new Color(0.04f, 0.1f, 0.12f, 0.9f);
            Text crTi = CreateText(crossSide.transform, "CFoundTitle", font, 20, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -8f), new Vector2(-16f, 32f));
            crTi.fontStyle = FontStyle.Bold;
            crTi.color = new Color(0.5f, 0.9f, 0.95f, 1f);
            crTi.text = "Déjà trouvés";
            demo.gridFoundListCross = CreateText(crossSide.transform, "CFoundList", font, 18, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -40f), new Vector2(-12f, 420f));
            demo.gridFoundListCross.color = new Color(0.85f, 0.95f, 0.98f, 1f);
            demo.gridFoundListCross.alignment = TextAnchor.UpperLeft;
            demo.gridFoundListCross.horizontalOverflow = HorizontalWrapMode.Wrap;
            demo.gridFoundListCross.text = "—";
            Transform crossHost = CreateGridHost(cross.transform, "CrossGridHost", 0.53f, new Vector2(-100f, 128f), new Vector2(760f, 430f));
            demo.crosswordCells = CrosswordLetterGrid(crossHost, font, CrosswordCols, CrosswordCols, CrosswordCellPx, CrosswordGapPx, Vector2.zero, demo);
            WireLetterGridDrag(crossHost, demo, true, demo.crosswordCells);

            GameObject crossFoot = CreateRect(cross.transform, "CrossFooter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(980f, 166f));
            Image cfBg = crossFoot.AddComponent<Image>();
            cfBg.sprite = white;
            cfBg.color = new Color(0.04f, 0.05f, 0.08f, 0.62f);
            cfBg.raycastTarget = false;
            demo.crosswordAnswerInput = BuildInputField(crossFoot.transform, font, "CrossDraft", new Vector2(25f, -44f), 380f, "Clic sur les lettres ou tape ici…");
            demo.crosswordClearButton = BuildSecondaryButton(crossFoot.transform, font, "Effacer", new Vector2(-400f, -44f), () => demo.ClearCrosswordGuess());
            demo.crosswordSubmitButton = BuildPrimaryButton(crossFoot.transform, font, "Valider", new Vector2(300f, -44f), () => OnCrosswordSubmit(demo));
            demo.crosswordFeedback = Sub(crossFoot.transform, font, "CrossFb", new Vector2(0f, -98f));
            RectTransform crossFbRt = demo.crosswordFeedback.rectTransform;
            crossFbRt.sizeDelta = new Vector2(900f, 36f);

            GameObject blind = PanelShell(modeRoot, "PanelBlind", "blind-test", new Color(0.1f, 0.06f, 0.14f, 0.96f), white, surf);
            demo.blindTitle = Title(blind.transform, font, "BTitle", "Blind test", new Vector2(0f, -36f));
            demo.blindPrompt = Sub(blind.transform, font, "BlindQ", new Vector2(0f, -102f));
            demo.blindPrompt.rectTransform.sizeDelta = new Vector2(940f, 178f);
            demo.blindPrompt.fontSize = 32;
            demo.blindSub = Sub(blind.transform, font, "BlindSub", new Vector2(0f, -232f));
            demo.blindSub.rectTransform.sizeDelta = new Vector2(920f, 140f);
            demo.blindSub.fontSize = 26;
            demo.blindSub.lineSpacing = 1.1f;
            Transform blindHost = CreateGridHost(blind.transform, "BlindEmojiHost", 0.5f, new Vector2(0f, 84f), new Vector2(960f, 164f));
            demo.blindEmoji = BigLettersCentered(blindHost, font, "BEmoji", 64);
            GameObject blindChoicesRoot = CreateRect(blind.transform, "BlindChoicesRoot", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 124f), new Vector2(980f, 300f));
            BlindChoiceColumn(blindChoicesRoot.transform, font, new Vector2(0f, -12f), 54f, demo);

            GameObject myst = PanelShell(modeRoot, "PanelMystery", "mystery-word", new Color(0.07f, 0.09f, 0.13f, 0.96f), white, surf);
            demo.mysteryTitle = Title(myst.transform, font, "MTitle", "Mot mystère", new Vector2(0f, -36f));
            Transform mystHost = CreateGridHost(myst.transform, "MysteryMaskHost", 0.55f, new Vector2(0f, 140f), new Vector2(960f, 132f));
            demo.mysteryMask = BigLettersCentered(mystHost, font, "Mask", 46);

            GameObject mystFoot = CreateRect(myst.transform, "MysteryFooter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 118f), new Vector2(980f, 220f));
            Image mystFootBg = mystFoot.AddComponent<Image>();
            mystFootBg.sprite = white;
            mystFootBg.color = new Color(0.04f, 0.05f, 0.08f, 0.62f);
            mystFootBg.raycastTarget = false;
            demo.mysteryAnswerInput = BuildInputField(mystFoot.transform, font, "MystAnswer", new Vector2(20f, -96f), 400f, "Tape le mot complet…");
            demo.mysteryClearButton = BuildSecondaryButton(mystFoot.transform, font, "Effacer", new Vector2(-400f, -96f), () =>
            {
                if (demo.mysteryAnswerInput != null) demo.mysteryAnswerInput.text = "";
            });
            demo.mysterySubmitButton = BuildPrimaryButton(mystFoot.transform, font, "Valider", new Vector2(300f, -96f), () => OnMysterySubmit(demo));
            demo.mysteryFeedback = Sub(mystFoot.transform, font, "MystFb", new Vector2(0f, -162f));

            GameObject mem = PanelShell(modeRoot, "PanelMemory", "memory", new Color(0.06f, 0.11f, 0.09f, 0.96f), white, surf);
            demo.memoryTitle = Title(mem.transform, font, "MemTitle", "Mémoire", new Vector2(0f, -36f));
            Transform memHost = CreateGridHost(mem.transform, "MemoryGridHost", 0.52f, new Vector2(0f, 40f), new Vector2(900f, 350f));
            demo.memoryCards = MemoryGrid(memHost, font, white, 132f, 14f);
            demo.memorySubtitle = Sub(mem.transform, font, "MemSub", new Vector2(0f, -214f));
            demo.memorySubtitle.fontSize = 24;
            demo.memorySubtitle.lineSpacing = 1.08f;
            demo.memorySubtitle.rectTransform.sizeDelta = new Vector2(920f, 106f);

            GameObject chrono = PanelShell(modeRoot, "PanelChrono", "speed-chrono", new Color(0.12f, 0.07f, 0.06f, 0.96f), white, surf);
            demo.chronoTitle = Title(chrono.transform, font, "ChTitle", "Chrono vitesse", new Vector2(0f, -36f));
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
            demo.imageTitle = Title(img.transform, font, "ImgTitle", "Devine l’image", new Vector2(0f, -34f));
            Transform imgHost = CreateGridHost(img.transform, "ImageBlockHost", 0.57f, new Vector2(0f, 84f), new Vector2(980f, 470f));
            demo.imagePlaceholder = ImageBlockCentered(imgHost, white, new Vector2(860f, 420f));
            demo.imageCaption = Sub(img.transform, font, "ImgCap", new Vector2(0f, -320f));
            RectTransform capRt = demo.imageCaption.rectTransform;
            capRt.sizeDelta = new Vector2(940f, 92f);
            demo.imageCaption.fontSize = 30;

            GameObject imgFoot = CreateRect(img.transform, "ImageGuessFooter", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 94f), new Vector2(980f, 170f));
            Image imgFootBg = imgFoot.AddComponent<Image>();
            imgFootBg.sprite = white;
            imgFootBg.color = new Color(0.05f, 0.06f, 0.09f, 0.58f);
            imgFootBg.raycastTarget = false;
            demo.imageGuessInput = BuildInputField(imgFoot.transform, font, "ImgAnswer", new Vector2(0f, -54f), 580f, "Écris ta réponse puis Valider…");
            demo.imageGuessSubmit = BuildPrimaryButton(imgFoot.transform, font, "Valider", new Vector2(350f, -54f), () => OnImageGuessSubmit(demo));
            demo.imageGuessFeedback = Sub(imgFoot.transform, font, "ImgFb", new Vector2(0f, -126f));
            demo.imageGuessFeedback.rectTransform.sizeDelta = new Vector2(920f, 48f);
            demo.imageGuessFeedback.fontSize = 22;
        }

        private static void OnSemanticSubmit(MiniGamePanelContent demo)
        {
            if (demo.semanticAnswerInput == null) return;
            string a = demo.semanticAnswerInput.text?.Trim().ToUpperInvariant() ?? "";
            bool hadInput = !string.IsNullOrEmpty(a);
            string target = (demo.CurrentSemanticTarget ?? "CON").Trim().ToUpperInvariant();
            bool ok = hadInput && a == target;
            if (demo.semanticFeedback != null)
            {
                demo.semanticFeedback.text = !hadInput
                    ? "Réponse vide — passage au mode suivant."
                    : (ok ? "Bravo !" : "Pas exact — on cherchait « " + target + " ».");
            }

            GameSfxHub.Instance?.PlayResult(ok);
            MaybeAdvanceMiniGameAfterResponse();
        }

        private static void OnWordSubmit(MiniGamePanelContent demo)
        {
            if (demo.wordAnswerInput == null) return;
            string a = demo.wordAnswerInput.text?.Trim().ToUpperInvariant() ?? "";
            bool hadInput = !string.IsNullOrEmpty(a);
            string target = (demo.CurrentScrambleTargetWord ?? demo.CurrentScrambleAnswer ?? "CONGO").Trim().ToUpperInvariant();
            bool ok = hadInput && a == target;
            if (demo.wordFeedback != null)
            {
                demo.wordFeedback.text = !hadInput
                    ? "Réponse vide — tape le mot cible puis Valider."
                    : (ok
                        ? "Bravo — c’est " + target + " ! (" + (demo.currentGridSolved != null ? demo.currentGridSolved.Count : 0) + "/"
                        + (demo.currentGridAllWords != null ? demo.currentGridAllWords.Count : 0) + " pour cette grille.)"
                        : "Pas exact — le mot recherché maintenant : « " + target + " ».");
            }

            GameSfxHub.Instance?.PlayResult(ok);
            if (ok && demo.currentGridSolved != null && !string.IsNullOrEmpty(target))
            {
                demo.currentGridSolved.Add(target);
                demo.RecordGridWordFound(target);
            }

            if (ok) MaybeAdvanceMiniGameAfterResponse();
        }

        private static void OnCrosswordSubmit(MiniGamePanelContent demo)
        {
            if (demo.crosswordAnswerInput == null) return;
            string a = GridThemeBank.SanitizeForGrid(demo.crosswordAnswerInput.text ?? "");
            bool hadInput = !string.IsNullOrEmpty(a);
            string target = GridThemeBank.SanitizeForGrid(demo.CurrentScrambleTargetWord ?? demo.CurrentScrambleAnswer ?? "");
            bool ok = hadInput && a == target;
            if (demo.crosswordFeedback != null)
            {
                demo.crosswordFeedback.text = !hadInput
                    ? "Écris le mot caché exact (7×7) puis Valider."
                    : (ok
                        ? "Trouvé ! « " + a + " ». (" + (demo.currentGridSolved != null ? demo.currentGridSolved.Count : 0) + "/"
                        + (demo.currentGridAllWords != null ? demo.currentGridAllWords.Count : 0) + " pour cette session.)"
                        : "Essaie le mot cible actuel : « " + target + " » (même longueur que les lettres qu’on cherche ici).");
            }

            GameSfxHub.Instance?.PlayResult(ok);
            if (ok)
            {
                if (demo.currentGridSolved != null) demo.currentGridSolved.Add(a);
                demo.LastCrosswordGuessed = string.IsNullOrEmpty(demo.LastCrosswordGuessed) ? a : (demo.LastCrosswordGuessed + " · " + a);
                demo.RecordGridWordFound(a);
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
            GameObject go = CreateRect(parent, name, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(1000f, 68f));
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = 40;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(1f, 0.9f, 0.2f, 1f);
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 24;
            t.resizeTextMaxSize = 40;
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
            GameObject go = CreateRect(parent, "BtnSubmit", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(230f, 54f));
            Image img = go.AddComponent<Image>();
            img.sprite = White();
            img.color = new Color(0.12f, 0.48f, 0.28f, 1f);
            Button b = go.AddComponent<Button>();
            GameObject txGo = CreateRect(go.transform, "Txt", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text tx = txGo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 24;
            tx.fontStyle = FontStyle.Bold;
            tx.alignment = TextAnchor.MiddleCenter;
            tx.color = Color.white;
            tx.text = label;
            b.onClick.AddListener(() => onClick?.Invoke());
            return b;
        }

        private static Button BuildSecondaryButton(Transform parent, Font font, string label, Vector2 pos, Action onClick)
        {
            GameObject go = CreateRect(parent, "BtnSecondary", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), pos, new Vector2(180f, 50f));
            Image img = go.AddComponent<Image>();
            img.sprite = White();
            img.color = new Color(0.22f, 0.25f, 0.32f, 1f);
            Button b = go.AddComponent<Button>();
            GameObject txGo = CreateRect(go.transform, "Txt", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Text tx = txGo.AddComponent<Text>();
            tx.font = font;
            tx.fontSize = 22;
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
