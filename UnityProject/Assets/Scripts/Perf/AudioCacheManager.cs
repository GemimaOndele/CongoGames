using System.Collections.Generic;
using UnityEngine;

namespace CongoGames.Perf
{
    public class AudioCacheManager : MonoBehaviour
    {
        private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
        [SerializeField] private int maxCacheSize = 32;

        public bool TryGetClip(string key, out AudioClip clip)
        {
            return clipCache.TryGetValue(key, out clip);
        }

        public void SetClip(string key, AudioClip clip)
        {
            if (clipCache.Count >= maxCacheSize)
            {
                // Simple eviction strategy for starter kit.
                foreach (string cacheKey in new List<string>(clipCache.Keys))
                {
                    clipCache.Remove(cacheKey);
                    break;
                }
            }
            clipCache[key] = clip;
        }
    }
}
