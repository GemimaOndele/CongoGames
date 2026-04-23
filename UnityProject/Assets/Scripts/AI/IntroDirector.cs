using UnityEngine;
using CongoGames.Core;

namespace CongoGames.AI
{
    public class IntroDirector : MonoBehaviour
    {
        [SerializeField] private AIHostManager hostManager;
        private static bool introAlreadyQueued;

        private void Start()
        {
            if (hostManager == null) hostManager = AIHostManager.Instance;
            if (introAlreadyQueued)
            {
                return;
            }

            introAlreadyQueued = true;
            PlayIntro();
        }

        public void PlayIntro()
        {
            if (hostManager == null || LanguageManager.Instance == null)
            {
                return;
            }

            hostManager.Speak(LanguageManager.Instance.T("intro_1"));
            hostManager.Speak(LanguageManager.Instance.T("intro_2"));
            hostManager.Speak(LanguageManager.Instance.T("intro_3"));
        }
    }
}
