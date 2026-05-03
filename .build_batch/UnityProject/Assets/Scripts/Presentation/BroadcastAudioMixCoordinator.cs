using UnityEngine;
using CongoGames.AI;
using CongoGames.Audio;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Baisse musique / SFX pendant la parole TTS de l’hôte (ducking broadcast).
    /// Brancher sur la même scène que AIHostManager, ThemeMusicPlayer, GameSfxHub.
    /// Pendant l’écoute blind / manche avec extrait musical, <see cref="MiniGamePanelContent"/> force le fond à 0.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class BroadcastAudioMixCoordinator : MonoBehaviour
    {
        [Tooltip("Niveau du fond thème / BGM Resources pendant la voix IA (hors phase « extrait blind »). Ex. 0,10 ≈ 10 %.")]
        [SerializeField] [Range(0f, 1f)] private float musicLevelWhileSpeaking = 0.1f;
        [SerializeField] [Range(0.05f, 1f)] private float sfxLevelWhileSpeaking = 0.72f;

        private void OnEnable()
        {
            AIHostManager.OnSpeakingChanged += OnHostSpeaking;
        }

        private void OnDisable()
        {
            AIHostManager.OnSpeakingChanged -= OnHostSpeaking;
        }

        private void OnHostSpeaking(bool speaking)
        {
            float m = speaking ? musicLevelWhileSpeaking : 1f;
            float s = speaking ? sfxLevelWhileSpeaking : 1f;
            ThemeMusicPlayer.Instance?.SetBroadcastDuckMultiplier(m);
            GameAudioManager.Instance?.SetBroadcastDuckMultiplier(m);
            GameSfxHub.Instance?.SetBroadcastDuckMultiplier(s);
        }
    }
}
