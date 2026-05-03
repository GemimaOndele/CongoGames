using System;
using UnityEngine;

namespace CongoGames.Network
{
    [Serializable]
    public class WebGlCloudEndpointsData
    {
        public string wsUrl = "";
        public string ttsHttpBase = "";
    }

    /// <summary>
    /// Build WebGL : lecture de <c>Resources/CloudEndpoints</c> (fichier texte .json) pour WSS + HTTPS
    /// sans localhost (navigateur sur téléphone / autre machine).
    /// </summary>
    public static class WebGlCloudEndpoints
    {
        public static string LoadedWsUrl { get; private set; } = "";
        public static string LoadedTtsBase { get; private set; } = "";
        public static bool HasLoaded { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HasLoaded = false;
            LoadedWsUrl = "";
            LoadedTtsBase = "";
        }

        public static void TryLoadAndApply()
        {
            if (HasLoaded) return;
            if (Application.platform != RuntimePlatform.WebGLPlayer)
            {
                HasLoaded = true;
                return;
            }

            TextAsset json = Resources.Load<TextAsset>("CloudEndpoints");
            if (json == null)
            {
                HasLoaded = true;
                if (Debug.isDebugBuild)
                {
                    Debug.Log("WebGL: pas de Resources/CloudEndpoints (optionnel) — rappels WS/TTS manuels.");
                }

                return;
            }

            try
            {
                WebGlCloudEndpointsData d = JsonUtility.FromJson<WebGlCloudEndpointsData>(json.text);
                if (d == null) return;
                if (!string.IsNullOrWhiteSpace(d.wsUrl))
                {
                    LoadedWsUrl = d.wsUrl.Trim();
                }

                if (!string.IsNullOrWhiteSpace(d.ttsHttpBase))
                {
                    LoadedTtsBase = d.ttsHttpBase.Trim().TrimEnd('/');
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("CloudEndpoints.json: " + e.Message);
            }

            HasLoaded = true;
        }
    }
}
