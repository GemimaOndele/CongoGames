using System;
using UnityEngine;

namespace CongoGames.Core
{
    [Serializable]
    public class LiveQuestion
    {
        public string category;
        public string difficulty;
        public string question;
        public string[] options;
        public string correctAnswer;
        public string explanation;
    }

    public class QuestionManager : MonoBehaviour
    {
        public static QuestionManager Instance { get; private set; }

        public LiveQuestion CurrentQuestion { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SetQuestion(LiveQuestion question)
        {
            CurrentQuestion = question;
        }

        public bool ValidateAnswer(string answer)
        {
            if (CurrentQuestion == null || string.IsNullOrEmpty(CurrentQuestion.correctAnswer)) return false;
            return string.Equals(answer?.Trim(), CurrentQuestion.correctAnswer.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
