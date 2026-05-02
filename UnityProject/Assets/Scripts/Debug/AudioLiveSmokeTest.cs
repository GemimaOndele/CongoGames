#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using CongoGames.Audio;

namespace CongoGames.DebugTools
{
    /// <summary>
    /// Test clavier sans TikTok (spec Claude) : ajouter sur un GameObject en scène pour Play mode.
    /// </summary>
    public sealed class AudioLiveSmokeTest : MonoBehaviour
    {
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                GameAudioManager.Instance?.OnQuizStart();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                GameAudioManager.Instance?.OnBattleStart();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                GameAudioManager.Instance?.OnSpeedChronoStart();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                GameAudioManager.Instance?.OnMemoryStart();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                GameAudioManager.Instance?.OnLobby();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                GameAudioManager.Instance?.OnSemanticStart();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                GameAudioManager.Instance?.OnCrosswordStart();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                GameAudioManager.Instance?.OnMysteryWordStart();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                GameAudioManager.Instance?.OnImageToWordStart();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                GameAudioManager.Instance?.OnCorrectAnswer();
            }
            else if (Input.GetKeyDown(KeyCode.W))
            {
                GameAudioManager.Instance?.OnWrongAnswer();
            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                GameAudioManager.Instance?.OnGiftReceived();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                GameAudioManager.Instance?.OnNewViewer();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                GameAudioManager.Instance?.OnWsMetric("pulse", 1);
            }
        }
    }
}
#endif
