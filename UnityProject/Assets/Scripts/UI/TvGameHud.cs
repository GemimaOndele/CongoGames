using UnityEngine;
using UnityEngine.UI;
using CongoGames.Core;

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

            if (modeLabel != null)
            {
                modeLabel.text = "Mode : " + gmm.ActiveModeDisplayName;
            }

            if (brandLine != null && string.IsNullOrEmpty(brandLine.text))
            {
                brandLine.text = "Congo · V·J·R · FR · Lingala · Kituba";
            }
        }
    }
}
