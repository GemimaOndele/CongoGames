using System.Collections;
using UnityEngine;

namespace CongoGames.Core
{
    /// <summary>
    /// En WebGL, les navigateurs exigent un geste (clic / toucher) avant toute sortie audio.
    /// Toutes les coroutines passent ici une fois : le 1er geste déverrouille musique, TTS, SFX, etc.
    /// </summary>
    public static class WebAudioGestureGate
    {
        public static bool Unlocked { get; private set; }

        public static IEnumerator CoWaitForUnlock()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            yield break;
#else
            if (Unlocked)
            {
                yield break;
            }

            while (!Unlocked)
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchSupported && Input.touchCount > 0))
                {
                    Unlocked = true;
                    yield break;
                }

                yield return null;
            }
#endif
        }
    }
}
