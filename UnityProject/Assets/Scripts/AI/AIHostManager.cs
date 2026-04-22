using UnityEngine;

namespace CongoGames.AI
{
    public class AIHostManager : MonoBehaviour
    {
        public static AIHostManager Instance { get; private set; }

        [SerializeField] private AudioSource audioSource;

        private void Awake()
        {
            Instance = this;
        }

        public void Speak(string line)
        {
            // Hook ElevenLabs audio playback here.
            Debug.Log("AI Host: " + line);
            if (audioSource != null && !audioSource.isPlaying)
            {
                // Optional local fallback beep/clip.
            }
        }
    }
}
