#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CongoGames.Network.Editor
{
    /// <summary>
    /// Évite les avertissements « Release of invalid GC handle… previous domain » liés à <see cref="ClientWebSocket"/>
    /// quand le domaine script redémarre ou que le Play Mode se termine pendant une connexion WS ouverte.
    /// </summary>
    [InitializeOnLoad]
    internal static class LiveEventClientPreDomainReload
    {
        static LiveEventClientPreDomainReload()
        {
            AssemblyReloadEvents.beforeAssemblyReload += FlushAllLiveEventClients;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                FlushAllLiveEventClients();
            }
        }

        private static void FlushAllLiveEventClients()
        {
            LiveEventClient[] all = Object.FindObjectsByType<LiveEventClient>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                {
                    all[i].ForceDisconnectBeforeScriptReload();
                }
            }
        }
    }
}
#endif
