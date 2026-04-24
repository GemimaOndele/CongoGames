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
        private int _hudChronoLastSec = int.MaxValue;

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
                _hudChronoLastSec = int.MaxValue;
                return;
            }

            if (modeLabel != null)
            {
                modeLabel.text = "Mode : " + gmm.ActiveModeDisplayName;
            }

            MiniGamePanelContent panel = MiniGamePanelContent.Instance;
            if (panel != null
                && panel.TryGetHudCountdownOverride(out float auxFill, out int auxSec))
            {
                if (timerRing != null) timerRing.enabled = true;
                if (timerRing != null && timerRing.transform.parent != null) timerRing.transform.parent.gameObject.SetActive(true);
                if (timerSeconds != null) timerSeconds.enabled = true;
                if (timerRing != null) timerRing.fillAmount = auxFill;
                if (timerSeconds != null) timerSeconds.text = auxSec > 0 ? auxSec.ToString() : "0";
                if (auxSec < _hudChronoLastSec && _hudChronoLastSec < 1000000)
                {
                    GameSfxHub.Instance?.PlayChronoTick(0.58f);
                }

                _hudChronoLastSec = auxSec;
                if (brandLine != null && string.IsNullOrEmpty(brandLine.text))
                {
                    brandLine.text = "Congo · tricolore · FR · Lingala · Kituba";
                }

                return;
            }

            _hudChronoLastSec = int.MaxValue;

            bool chronoUi = string.Equals(gmm.ActiveModeId, "speed-chrono", System.StringComparison.Ordinal)
                            && (panel == null || panel.IsChronoRoundActive);
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
