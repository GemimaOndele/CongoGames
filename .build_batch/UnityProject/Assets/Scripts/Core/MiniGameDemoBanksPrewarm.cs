using System.Collections;
using UnityEngine;

namespace CongoGames.Core
{
    /// <summary>Charge les JSON Datasets (extras blind / image guess) en WebGL.</summary>
    public static class MiniGameDemoBanksPrewarm
    {
        public static IEnumerator CoRun()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string b = null;
            string i = null;
            bool bOk = false;
            bool iOk = false;
            yield return WebGlStreamingPrewarm.CoHttpGetText(
                StreamingAssetsUrl.UrlForRelativePath("Datasets/minigame_blind_extras.json"),
                (t, ok) =>
                {
                    b = t;
                    bOk = ok;
                });
            yield return WebGlStreamingPrewarm.CoHttpGetText(
                StreamingAssetsUrl.UrlForRelativePath("Datasets/minigame_image_guess_extras.json"),
                (t, ok) =>
                {
                    i = t;
                    iOk = ok;
                });
            MiniGameDemoBanks.IngestWebGlDatasetsJson(bOk ? b : null, iOk ? i : null);
#endif
            yield break;
        }
    }
}
