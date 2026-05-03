using System.Collections;
using UnityEngine;
using CongoGames.AI;
using CongoGames.Core;
using CongoGames.Presentation;
using CongoGames.UI;

namespace CongoGames.GameModes
{
    public class QuizMode : MonoBehaviour, IGameMode
    {
        public const string LocalPlayer = "Toi";

        public string ModeId => "quiz";

        [SerializeField] private float resultShowSeconds = GameFlowDurations.QuizShowResult;
        [SerializeField] private float betweenQuestionsGap = GameFlowDurations.BetweenQuestionsGap;

        private Coroutine roundRoutine;
        private string pendingPick;
        private bool pickReceived;

        public void Begin()
        {
            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
            }

            roundRoutine = StartCoroutine(QuizSessionLoop());
        }

        public void Tick(float deltaTime)
        {
        }

        public void End()
        {
            if (roundRoutine != null)
            {
                StopCoroutine(roundRoutine);
                roundRoutine = null;
            }
        }

        private IEnumerator QuizSessionLoop()
        {
            GameModeManager gmm = GameModeManager.Instance;
            while (gmm != null && gmm.RoundTimeRemaining > 0.25f)
            {
                yield return RunSingleQuestion();
                if (gmm.RoundTimeRemaining <= 0.25f)
                {
                    break;
                }

                yield return new WaitForSeconds(betweenQuestionsGap);
            }

            roundRoutine = null;
        }

        private IEnumerator RunSingleQuestion()
        {
            GameModeManager gmm = GameModeManager.Instance;
            LiveQuestion q = CongoLocalQuizBank.PickRandom();
            if (QuestionManager.Instance != null)
            {
                QuestionManager.Instance.SetQuestion(q);
            }

            QuestionUI ui = QuestionUI.Instance ?? FindAnyObjectByType<QuestionUI>();
            if (ui == null || q == null)
            {
                yield break;
            }

            ui.SetupQuizRound(q);
            pendingPick = null;
            pickReceived = false;
            ui.AnswerChosen -= OnAnswerChosen;
            ui.AnswerChosen += OnAnswerChosen;

            ui.SetPhaseAnswerable("Lis la question — choisis A, B, C ou D quand tu es prêt (pas de limite de temps).");
            AIHostManager.Instance?.Speak(q.question);

            if (gmm != null && gmm.RoundTimeRemaining <= 0.25f)
            {
                ui.AnswerChosen -= OnAnswerChosen;
                yield break;
            }

            while (!pickReceived)
            {
                if (gmm != null && gmm.RoundTimeRemaining <= 0.25f)
                {
                    break;
                }

                yield return null;
            }

            ui.AnswerChosen -= OnAnswerChosen;
            bool correct = !string.IsNullOrEmpty(pendingPick) &&
                           string.Equals(pendingPick?.Trim(), q.correctAnswer?.Trim(), System.StringComparison.OrdinalIgnoreCase);
            if (ScoreManager.Instance != null && !PlayerProfileStore.IsLiveTiktokSession())
            {
                string who = PlayerProfileStore.ScoreUsernameForLocalPlay() ?? LocalPlayer;
                ScoreManager.Instance.RegisterAnswer(who, correct, false);
            }

            ui.ShowQuizOutcome(correct, pendingPick, q);
            if (correct)
            {
                ui.SetBanner("Bravo — bonne réponse !");
            }
            else if (string.IsNullOrEmpty(pendingPick))
            {
                ui.SetBanner("Fin de manche — question suivante.");
            }
            else
            {
                string letter = (q.correctAnswer ?? "").Trim().ToUpperInvariant();
                int ix = letter.Length == 1 ? letter[0] - 'A' : -1;
                string rightText = ix >= 0 && ix < 4 && q.options != null && ix < q.options.Length
                    ? q.options[ix]
                    : "";
                ui.SetBanner(
                    string.IsNullOrEmpty(rightText)
                        ? "Pas exact — la bonne réponse était la " + letter + ". On passe à la suite."
                        : "Pas exact — la bonne réponse était la " + letter + " : " + rightText + ". Suite.");
            }

            GameSfxHub.Instance?.PlayResult(correct);

            yield return new WaitForSeconds(resultShowSeconds);
            ui.ClearFeedbackVisuals();
        }

        private void OnAnswerChosen(string letter)
        {
            pendingPick = letter;
            pickReceived = true;
        }
    }
}
