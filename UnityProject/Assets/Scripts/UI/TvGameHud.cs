using UnityEngine;
using UnityEngine.UI;
using CongoGames.Core;
using CongoGames.Presentation;

namespace CongoGames.UI
{
    public class TvGameHud : MonoBehaviour
    {
        [SerializeField] private Image timerRing;
        [SerializeField] private Text modeLabel;
        [SerializeField] private Text timerSeconds;
        [SerializeField] private Text brandLine;

        public void Wire(Image ring, Text mode, Text seconds, Text brand)
        {
            timerRing = ring;
            modeLabel = mode;
            timerSeconds = seconds;
            brandLine = brand;
        }

        private void Update()
        {
            GameModeManager gmm = GameModeManager.Instance;
            if (gmm == null)
            {
                if (timerRing != null) timerRing.fillAmount = 0f;
                if (modeLabel != null) modeLabel.text = "Mode : attente…";
                if (timerSeconds != null) timerSeconds.text = "";
                return;
            }

            if (modeLabel != null)
            {
                modeLabel.text = "Mode : " + gmm.ActiveModeDisplayName;
            }

            bool chronoUi = string.Equals(gmm.ActiveModeId, "speed-chrono", System.StringComparison.Ordinal)
                            && (MiniGamePanelContent.Instance == null || MiniGamePanelContent.Instance.IsChronoRoundActive);
            if (timerRing != null) timerRing.enabled = !chronoUi;
            if (timerRing != null && timerRing.transform.parent != null) timerRing.transform.parent.gameObject.SetActive(!chronoUi);
            if (timerSeconds != null) timerSeconds.enabled = !chronoUi;

            if (chronoUi) return;

            float d = Mathf.Max(0.01f, gmm.RoundDuration);
            float t = Mathf.Max(0f, gmm.RoundTimeRemaining);
            if (timerRing != null)
            {
                timerRing.fillAmount = t / d;
            }

            if (timerSeconds != null)
            {
                timerSeconds.text = Mathf.Ceil(t).ToString("0");
            }

            if (brandLine != null && string.IsNullOrEmpty(brandLine.text))
            {
                brandLine.text = "Congo · tricolore · FR · Lingala · Kituba";
            }
        }
    }
}
