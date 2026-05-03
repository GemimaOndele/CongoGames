using UnityEngine;
using CongoGames.Audio;

namespace CongoGames.Perf
{
    /// <summary>
    /// Musique d'ambiance légère (pad synthétique) pour entendre quelque chose en local sans fichier audio.
    /// Remplacez par un AudioClip via Resources.Load("Audio/ambient") si vous ajoutez un vrai morceau.
    /// </summary>
    public class AmbientMusicLoop : MonoBehaviour
    {
        [SerializeField] private float volume = 0.14f;
        [SerializeField] private string resourcesClipPath = "";

        private AudioSource musicSource;

        private void Awake()
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.volume = volume;

            if (!string.IsNullOrEmpty(resourcesClipPath))
            {
                AudioClip fromRes = Resources.Load<AudioClip>(resourcesClipPath);
                if (fromRes != null)
                {
                    musicSource.clip = fromRes;
                    return;
                }
            }

            musicSource.clip = ProceduralClips.BuildAmbientPadLoop();
        }

        private void Start()
        {
            if (musicSource.clip != null)
            {
                musicSource.Play();
            }
        }
    }
}
