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
        [SerializeField] private Color timerSafe = new Color(0.24f, 0.92f, 0.32f, 0.95f);
        [SerializeField] private Color timerWarn = new Color(1f, 0.72f, 0.2f, 0.97f);
        [SerializeField] private Color timerDanger = new Color(1f, 0.22f, 0.2f, 0.98f);
        private int _hudChronoLastSec = int.MaxValue;
        [SerializeField] [Range(0.2f, 1f)] private float musicDuckWhileChrono = 0.5f;
        private bool _chronoDuckApplied;

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
                if (timerRing != null) timerRing.color = EvaluateTimerColor(auxFill);
                if (timerSeconds != null) timerSeconds.text = auxSec > 0 ? auxSec.ToString() : "0";
                if (auxSec < _hudChronoLastSec && _hudChronoLastSec < 1000000)
                {
                    GameSfxHub.Instance?.PlayChronoTick(0.58f);
                }

                if (!_chronoDuckApplied)
                {
                    ThemeMusicPlayer.Instance?.SetChronoDuckMultiplier(musicDuckWhileChrono);
                    _chronoDuckApplied = true;
                }

                _hudChronoLastSec = auxSec;
                if (brandLine != null && string.IsNullOrEmpty(brandLine.text))
                {
                    brandLine.text = "Congo · tricolore · FR · Lingala · Kituba";
                }

                return;
            }

            _hudChronoLastSec = int.MaxValue;
            if (_chronoDuckApplied)
            {
                ThemeMusicPlayer.Instance?.SetChronoDuckMultiplier(1f);
                _chronoDuckApplied = false;
            }

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
                timerRing.color = EvaluateTimerColor(timerRing.fillAmount);
            }

            if (timerSeconds != null)
            {
                int sec = Mathf.CeilToInt(t);
                timerSeconds.text = sec.ToString("0");
                if (sec < _hudChronoLastSec && _hudChronoLastSec < 1000000)
                {
                    GameSfxHub.Instance?.PlayChronoTick(0.52f);
                }

                _hudChronoLastSec = sec;
            }

            if (brandLine != null && string.IsNullOrEmpty(brandLine.text))
            {
                brandLine.text = "Congo · tricolore · FR · Lingala · Kituba";
            }
        }

        private void OnDisable()
        {
            if (_chronoDuckApplied)
            {
                ThemeMusicPlayer.Instance?.SetChronoDuckMultiplier(1f);
                _chronoDuckApplied = false;
            }
        }

        private Color EvaluateTimerColor(float fill01)
        {
            float f = Mathf.Clamp01(fill01);
            if (f > 0.5f)
            {
                float k = Mathf.InverseLerp(0.5f, 1f, f);
                return Color.Lerp(timerWarn, timerSafe, k);
            }

            float k2 = Mathf.InverseLerp(0f, 0.5f, f);
            return Color.Lerp(timerDanger, timerWarn, k2);
        }
    }
}
