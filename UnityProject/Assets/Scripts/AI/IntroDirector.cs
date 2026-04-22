using UnityEngine;
using CongoGames.Core;

namespace CongoGames.AI
{
    public class IntroDirector : MonoBehaviour
    {
        [SerializeField] private AIHostManager hostManager;

        private void Start()
        {
            if (hostManager == null) hostManager = AIHostManager.Instance;
            PlayIntro();
        }

        public void PlayIntro()
        {
            hostManager.Speak(LanguageManager.Instance.T("intro_1"));
            hostManager.Speak(LanguageManager.Instance.T("intro_2"));
            hostManager.Speak("CongoGames commence maintenant.");
        }
    }
}
