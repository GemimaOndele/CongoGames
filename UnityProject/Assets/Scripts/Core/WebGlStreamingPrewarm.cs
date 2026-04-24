using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using CongoGames.Presentation;

namespace CongoGames.Core
{
    /// <summary>
    /// Prewarm : JSON StreamingAssets (WebGL) + exécution unique concurrente.
    /// </summary>
    public static class WebGlStreamingPrewarm
    {
        private static bool _done;
        private static bool _running;

        public static IEnumerator CoRunOnce()
        {
            if (_done)
            {
                yield break;
            }

            if (_running)
            {
                while (!_done)
                {
                    yield return null;
                }

                yield break;
            }

            _running = true;
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                yield return RemoteThemeMediaConfig.CoLoadFromWeb();
                yield return MiniGameDemoBanksPrewarm.CoRun();
#endif
            }
            finally
            {
                _done = true;
                _running = false;
            }
        }

        /// <summary>
        /// Indique si la ressource est joignable (200/206). D’abord <c>HEAD</c> ; si échec ou indécis
        /// (hébergeur sans HEAD, 405, etc.), repli <c>GET</c> avec <c>Range: bytes=0-0</c> pour n’en télécharger
        /// qu’un octet (évite les gros fichiers en prod). Si le serveur ignore Range et renvoie 200 + corps entier,
        /// le coût peut monter : rare ; timeout court.
        /// </summary>
        public static IEnumerator CoHttpHeadOk(string url, Action<bool> onResult)
        {
            if (onResult == null) yield break;

            bool headOk = false;
            long headCode = 0;
            using (UnityWebRequest head = UnityWebRequest.Head(url))
            {
                head.timeout = 8;
                yield return head.SendWebRequest();
                headCode = head.responseCode;
                headOk = head.result == UnityWebRequest.Result.Success
                    && (head.responseCode == 200 || head.responseCode == 206);
            }

            if (headOk)
            {
                onResult(true);
                yield break;
            }

            if (headCode == 404)
            {
                onResult(false);
                yield break;
            }

            using (UnityWebRequest get = UnityWebRequest.Get(url))
            {
                get.timeout = 6;
                get.SetRequestHeader("Range", "bytes=0-0");
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success)
                {
                    onResult(false);
                    yield break;
                }

                long code = get.responseCode;
                if (code == 404 || code == 410)
                {
                    onResult(false);
                    yield break;
                }

                if (code == 200 || code == 206)
                {
                    onResult(true);
                    yield break;
                }

                onResult(false);
            }
        }

        public static IEnumerator CoHttpGetText(string url, Action<string, bool> onResult)
        {
            using (UnityWebRequest u = UnityWebRequest.Get(url))
            {
                u.timeout = 20;
                yield return u.SendWebRequest();
                bool ok = u.result == UnityWebRequest.Result.Success;
                onResult?.Invoke(ok ? (u.downloadHandler?.text ?? "") : null, ok);
            }
        }

        public static IEnumerator CoHttpGetBytes(string url, Action<byte[], bool> onResult)
        {
            using (UnityWebRequest u = UnityWebRequest.Get(url))
            {
                u.timeout = 30;
                yield return u.SendWebRequest();
                bool ok = u.result == UnityWebRequest.Result.Success;
                onResult?.Invoke(ok ? (u.downloadHandler?.data) : null, ok);
            }
        }
    }
}
