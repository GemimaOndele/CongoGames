using UnityEngine;
using UnityEngine.UI;
using CongoGames.Core;

namespace CongoGames.UI
{
    public class QuestionUI : MonoBehaviour
    {
        [SerializeField] private Text questionText;
        [SerializeField] private Text optionAText;
        [SerializeField] private Text optionBText;
        [SerializeField] private Text optionCText;
        [SerializeField] private Text optionDText;

        public void Render(LiveQuestion q)
        {
            if (q == null) return;
            if (questionText != null) questionText.text = q.question;
            if (q.options == null || q.options.Length < 4) return;

            if (optionAText != null) optionAText.text = "A. " + q.options[0];
            if (optionBText != null) optionBText.text = "B. " + q.options[1];
            if (optionCText != null) optionCText.text = "C. " + q.options[2];
            if (optionDText != null) optionDText.text = "D. " + q.options[3];
        }
    }
}
