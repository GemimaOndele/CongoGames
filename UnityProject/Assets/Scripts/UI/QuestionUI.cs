using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CongoGames.Core;
using CongoGames.Presentation;

namespace CongoGames.UI
{
    public class QuestionUI : MonoBehaviour
    {
        public static QuestionUI Instance { get; private set; }

        [SerializeField] private Text questionText;
        [SerializeField] private Text phaseBanner;
        [SerializeField] private Button buttonA;
        [SerializeField] private Button buttonB;
        [SerializeField] private Button buttonC;
        [SerializeField] private Button buttonD;
        [SerializeField] private Image backgroundA;
        [SerializeField] private Image backgroundB;
        [SerializeField] private Image backgroundC;
        [SerializeField] private Image backgroundD;
        [SerializeField] private Text labelA;
        [SerializeField] private Text labelB;
        [SerializeField] private Text labelC;
        [SerializeField] private Text labelD;

        private readonly List<Image> optionBgs = new List<Image>(4);
        private readonly List<Button> optionButtons = new List<Button>(4);
        private readonly Color idleColor = new Color(0.14f, 0.16f, 0.22f, 1f);
        private readonly Color correctColor = new Color(0.02f, 0.82f, 0.32f, 1f);
        private readonly Color accentColor = new Color(0.95f, 0.78f, 0.1f, 1f);

        private LiveQuestion currentQuestion;
        private bool choicesClickable;
        private Coroutine animCo;
        private Coroutine resultPulseCo;
        private readonly string[] bufferedChoiceLines = new string[4];

        public event Action<string> AnswerChosen;

        private void Awake()
        {
            Instance = this;
        }

        public void BindRuntime(
            Text question,
            Text banner,
            Button a, Button b, Button c, Button d,
            Image bgA, Image bgB, Image bgC, Image bgD,
            Text la, Text lb, Text lc, Text ld)
        {
            questionText = question;
            phaseBanner = banner;
            buttonA = a;
            buttonB = b;
            buttonC = c;
            buttonD = d;
            backgroundA = bgA;
            backgroundB = bgB;
            backgroundC = bgC;
            backgroundD = bgD;
            labelA = la;
            labelB = lb;
            labelC = lc;
            labelD = ld;

            optionBgs.Clear();
            optionBgs.Add(backgroundA);
            optionBgs.Add(backgroundB);
            optionBgs.Add(backgroundC);
            optionBgs.Add(backgroundD);
            optionButtons.Clear();
            optionButtons.Add(buttonA);
            optionButtons.Add(buttonB);
            optionButtons.Add(buttonC);
            optionButtons.Add(buttonD);

            WireClick("A", buttonA);
            WireClick("B", buttonB);
            WireClick("C", buttonC);
            WireClick("D", buttonD);
        }

        private void WireClick(string letter, Button btn)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnOptionClicked(letter));
        }

        private void OnOptionClicked(string letter)
        {
            if (!choicesClickable) return;
            choicesClickable = false;
            GameSfxHub.Instance?.PlayTap();
            AnswerChosen?.Invoke(letter);
        }

        /// <summary>Prépare une manche quiz (texte + labels) sans révéler les réponses tout de suite.</summary>
        public void SetupQuizRound(LiveQuestion q)
        {
            GameSfxHub.Instance?.StopFeedbackOneShots();
            currentQuestion = q;
            ClearFeedbackVisuals();
            if (q == null) return;

            if (questionText != null)
            {
                questionText.text = q.question;
            }

            if (q.options != null && q.options.Length >= 4)
            {
                bufferedChoiceLines[0] = "A. " + q.options[0];
                bufferedChoiceLines[1] = "B. " + q.options[1];
                bufferedChoiceLines[2] = "C. " + q.options[2];
                bufferedChoiceLines[3] = "D. " + q.options[3];
            }
        }

        public void SetPhaseReadingOnly(string bannerMessage)
        {
            if (phaseBanner != null)
            {
                phaseBanner.gameObject.SetActive(true);
                phaseBanner.text = bannerMessage;
            }

            foreach (Button b in optionButtons)
            {
                if (b != null) b.interactable = false;
            }

            for (int i = 0; i < optionBgs.Count; i++)
            {
                if (optionBgs[i] != null)
                {
                    optionBgs[i].color = idleColor;
                }
            }

            for (int i = 0; i < 4; i++)
            {
                SetLabel(i, i == 0 ? "A. ···" : i == 1 ? "B. ···" : i == 2 ? "C. ···" : "D. ···");
            }

            choicesClickable = false;
            ScaleOptions(Vector3.one * 0.92f);
        }

        public void SetPhaseAnswerable(string bannerMessage)
        {
            if (phaseBanner != null)
            {
                phaseBanner.gameObject.SetActive(true);
                phaseBanner.text = bannerMessage;
            }

            for (int i = 0; i < 4; i++)
            {
                if (!string.IsNullOrEmpty(bufferedChoiceLines[i]))
                {
                    SetLabel(i, bufferedChoiceLines[i]);
                }
            }

            choicesClickable = true;
            foreach (Button b in optionButtons)
            {
                if (b != null) b.interactable = true;
            }

            if (animCo != null) StopCoroutine(animCo);
            animCo = StartCoroutine(AnimateChoicesReveal());
        }

        /// <summary>
        /// Affiche un retour visuel sans révéler la bonne réponse si la manche est ratée
        /// (pas de surlignage vert sur la bonne lettre).
        /// </summary>
        public void ShowQuizOutcome(bool isCorrect, string playerLetter, LiveQuestion q = null)
        {
            choicesClickable = false;
            foreach (Button b in optionButtons)
            {
                if (b != null) b.interactable = false;
            }

            if (resultPulseCo != null)
            {
                StopCoroutine(resultPulseCo);
                resultPulseCo = null;
            }

            string p = (playerLetter ?? "").Trim().ToUpperInvariant();
            if (isCorrect && !string.IsNullOrEmpty(p))
            {
                ApplyColorForLetter(p, correctColor);
                SetLabelEmphasis(p, true);
                resultPulseCo = StartCoroutine(CoPulseResultOptions(p, p));
                return;
            }

            if (!isCorrect && q != null && q.options != null && q.options.Length >= 4)
            {
                string correctL = (q.correctAnswer ?? "").Trim().ToUpperInvariant();
                if (!string.IsNullOrEmpty(correctL) && correctL.Length == 1)
                {
                    ApplyColorForLetter(correctL, correctColor);
                    SetLabelEmphasis(correctL, true);
                }

                if (!string.IsNullOrEmpty(p) && p.Length == 1 && p != correctL)
                {
                    ApplyColorForLetter(p, new Color(0.52f, 0.14f, 0.18f, 1f));
                }

                return;
            }

            foreach (Image img in optionBgs)
            {
                if (img != null) img.color = idleColor;
            }

            ResetChoiceLabelsStyle();
        }

        public void ClearFeedbackVisuals()
        {
            if (resultPulseCo != null)
            {
                StopCoroutine(resultPulseCo);
                resultPulseCo = null;
            }

            foreach (Image img in optionBgs)
            {
                if (img != null) img.color = idleColor;
            }

            ScaleOptions(Vector3.one);
            ResetChoiceLabelsStyle();
        }

        private void ResetChoiceLabelsStyle()
        {
            for (int i = 0; i < 4; i++)
            {
                Text t = i switch
                {
                    0 => labelA,
                    1 => labelB,
                    2 => labelC,
                    3 => labelD,
                    _ => null
                };
                if (t != null)
                {
                    t.fontStyle = FontStyle.Bold;
                    t.color = Color.white;
                }
            }
        }

        public void SetBanner(string message)
        {
            if (phaseBanner == null) return;
            phaseBanner.gameObject.SetActive(true);
            phaseBanner.text = message ?? "";
        }

        public void RenderLiveImmediate(LiveQuestion q)
        {
            SetupQuizRound(q);
            if (phaseBanner != null)
            {
                phaseBanner.gameObject.SetActive(true);
                phaseBanner.text = "Réponds dans le chat : A, B, C ou D";
            }

            for (int i = 0; i < 4; i++)
            {
                if (!string.IsNullOrEmpty(bufferedChoiceLines[i]))
                {
                    SetLabel(i, bufferedChoiceLines[i]);
                }
            }

            choicesClickable = false;
            foreach (Button b in optionButtons)
            {
                if (b != null) b.interactable = false;
            }

            ScaleOptions(Vector3.one);
        }

        public void Render(LiveQuestion q)
        {
            RenderLiveImmediate(q);
        }

        public void RenderPrompt(string title, string a = "—", string b = "—", string c = "—", string d = "—")
        {
            currentQuestion = null;
            for (int i = 0; i < 4; i++)
            {
                bufferedChoiceLines[i] = null;
            }

            ClearFeedbackVisuals();
            if (questionText != null) questionText.text = title;
            if (phaseBanner != null)
            {
                phaseBanner.gameObject.SetActive(true);
                phaseBanner.text = "Mode démo — la manche complète arrive au prochain quiz.";
            }

            SetLabel(0, "A. " + a);
            SetLabel(1, "B. " + b);
            SetLabel(2, "C. " + c);
            SetLabel(3, "D. " + d);
            choicesClickable = false;
            foreach (Button btn in optionButtons)
            {
                if (btn != null) btn.interactable = false;
            }

            ScaleOptions(Vector3.one);
        }

        private void SetLabel(int index, string text)
        {
            Text t = index switch
            {
                0 => labelA,
                1 => labelB,
                2 => labelC,
                3 => labelD,
                _ => null
            };
            if (t != null)
            {
                t.text = text;
            }
        }

        private void ApplyColorForLetter(string letter, Color col)
        {
            int i = LetterIndex(letter);
            if (i >= 0 && i < optionBgs.Count && optionBgs[i] != null)
            {
                optionBgs[i].color = col;
            }
        }

        private void SetLabelEmphasis(string letter, bool correctPick)
        {
            int i = LetterIndex(letter);
            Text t = i switch
            {
                0 => labelA,
                1 => labelB,
                2 => labelC,
                3 => labelD,
                _ => null
            };
            if (t == null) return;
            t.fontStyle = FontStyle.Bold;
            t.color = correctPick ? new Color(1f, 1f, 1f, 1f) : new Color(1f, 0.92f, 0.92f, 1f);
        }

        private static int LetterIndex(string letter)
        {
            return (letter ?? "").Trim().ToUpperInvariant() switch
            {
                "A" => 0,
                "B" => 1,
                "C" => 2,
                "D" => 3,
                _ => -1
            };
        }

        private IEnumerator CoPulseResultOptions(string correctLetter, string playerLetter)
        {
            string c = (correctLetter ?? "").Trim().ToUpperInvariant();
            string p = (playerLetter ?? "").Trim().ToUpperInvariant();
            const float peak = 1.12f;
            const float dur = 0.13f;
            for (int rep = 0; rep < 2; rep++)
            {
                float t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    float s = Mathf.Lerp(1f, peak, k);
                    ApplyScaleForLetter(c, s);
                    if (!string.IsNullOrEmpty(p) && p != c)
                    {
                        ApplyScaleForLetter(p, s);
                    }

                    yield return null;
                }

                t = 0f;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                    float s = Mathf.Lerp(peak, 1f, k);
                    ApplyScaleForLetter(c, s);
                    if (!string.IsNullOrEmpty(p) && p != c)
                    {
                        ApplyScaleForLetter(p, s);
                    }

                    yield return null;
                }
            }

            ApplyScaleForLetter(c, 1f);
            if (!string.IsNullOrEmpty(p) && p != c)
            {
                ApplyScaleForLetter(p, 1f);
            }

            resultPulseCo = null;
        }

        private void ApplyScaleForLetter(string letter, float scale)
        {
            int i = LetterIndex(letter);
            if (i >= 0 && i < optionButtons.Count && optionButtons[i] != null)
            {
                optionButtons[i].transform.localScale = Vector3.one * scale;
            }
        }

        private void ScaleOptions(Vector3 s)
        {
            for (int i = 0; i < optionButtons.Count; i++)
            {
                if (optionButtons[i] != null)
                {
                    optionButtons[i].transform.localScale = s;
                }
            }
        }

        private IEnumerator AnimateChoicesReveal()
        {
            for (int i = 0; i < optionButtons.Count; i++)
            {
                Button b = optionButtons[i];
                if (b == null) continue;
                Transform tr = b.transform;
                tr.localScale = Vector3.one * 0.85f;
                float t = 0f;
                while (t < 1f)
                {
                    t += Time.deltaTime * 6f;
                    float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));
                    tr.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, k);
                    if (optionBgs[i] != null)
                    {
                        optionBgs[i].color = Color.Lerp(idleColor, accentColor, k * 0.35f);
                    }

                    yield return null;
                }

                tr.localScale = Vector3.one;
                if (optionBgs[i] != null) optionBgs[i].color = idleColor;
                yield return new WaitForSeconds(0.04f);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
