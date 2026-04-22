using UnityEngine;

namespace CongoGames.GameModes
{
    public class QuizMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "quiz";

        public void Begin()
        {
            Debug.Log("Quiz mode started.");
        }

        public void Tick(float deltaTime)
        {
            // Question timer/reponse parsing branch here.
        }

        public void End()
        {
            Debug.Log("Quiz mode ended.");
        }
    }
}
