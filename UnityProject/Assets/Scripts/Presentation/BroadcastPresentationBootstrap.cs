using UnityEngine;

namespace CongoGames.Presentation
{
    /// <summary>
    /// Paramètres runtime orientés capture live (OBS, fenêtre du jeu).
    /// Placer sur un objet actif dès le chargement (ex. même que RuntimeBootstrap).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class BroadcastPresentationBootstrap : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool runInBackground = true;
        [Tooltip("Désactive la VSync Unity (utile si vous cadencez via OBS / GPU).")]
        [SerializeField] private bool disableVSyncForCapture;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Application.runInBackground = runInBackground;
            if (disableVSyncForCapture)
            {
                QualitySettings.vSyncCount = 0;
            }
        }
    }
}
