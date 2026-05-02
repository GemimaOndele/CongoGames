using UnityEngine;
using CongoGames.AI;
using CongoGames.Audio;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Baisse musique / SFX pendant la parole TTS de l’hôte (ducking broadcast).
    /// Brancher sur la même scène que AIHostManager, ThemeMusicPlayer, GameSfxHub.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class BroadcastAudioMixCoordinator : MonoBehaviour
    {
        [Tooltip("0 = silence total pendant la voix IA. Un léger niveau (ex. 0.08) garde une ambiance si besoin.")]
        [SerializeField] [Range(0f, 1f)] private float musicLevelWhileSpeaking = 0f;
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
            // Exigence gameplay: pendant la voix IA, la musique de fond doit être totalement muette.
            float m = speaking ? musicLevelWhileSpeaking : 1f;
            float s = speaking ? sfxLevelWhileSpeaking : 1f;
            ThemeMusicPlayer.Instance?.SetBroadcastDuckMultiplier(m);
            GameAudioManager.Instance?.SetBroadcastDuckMultiplier(m);
            GameSfxHub.Instance?.SetBroadcastDuckMultiplier(s);
        }
    }
}
