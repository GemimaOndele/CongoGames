using System;
using UnityEngine;
using UnityEngine.UI;
using CongoGames.Network;

namespace CongoGames.UI
{
    public class LiveDebugPanel : MonoBehaviour
    {
        [SerializeField] private LiveEventClient liveClient;
        [SerializeField] private Text connectionText;
        [SerializeField] private Text endpointText;
        [SerializeField] private Text eventText;
        [SerializeField] private Text userText;
        [SerializeField] private Text questionText;
        [SerializeField] private Text timestampText;
        [SerializeField] private Text systemText;
        [SerializeField] private Text errorText;
        [SerializeField] private float refreshIntervalSec = 0.25f;
        [SerializeField] private Color connectedColor = new Color(0.22f, 0.78f, 0.32f);
        [SerializeField] private Color disconnectedColor = new Color(0.90f, 0.25f, 0.25f);
        [SerializeField] private Color neutralColor = Color.white;

        private float nextRefresh;

        private void Start()
        {
            if (liveClient == null)
            {
                liveClient = FindAnyObjectByType<LiveEventClient>();
            }
        }

        private void Update()
        {
            if (Time.time < nextRefresh) return;
            nextRefresh = Time.time + refreshIntervalSec;
            Render();
        }

        private void Render()
        {
            if (liveClient == null)
            {
                SetText(connectionText, "WS: client introuvable");
                SetColor(connectionText, disconnectedColor);
                return;
            }

            SetText(connectionText, liveClient.IsConnected ? "WS: CONNECTE" : "WS: DECONNECTE");
            SetColor(connectionText, liveClient.IsConnected ? connectedColor : disconnectedColor);
            SetText(endpointText, "Endpoint: " + Safe(liveClient.ConnectedEndpoint));
            SetText(eventText, "Dernier event: " + Safe(liveClient.LastEventType));
            SetColor(eventText, liveClient.LastEventType == "gift" ? connectedColor : neutralColor);
            SetText(userText, "Dernier user: " + Safe(liveClient.LastEventUser));
            SetText(questionText, "Derniere question: " + Safe(liveClient.LastQuestionText));
            SetText(systemText, "Systeme: " + Safe(liveClient.LastSystemText));
            SetText(timestampText, "Heure event: " + FormatTimestamp(liveClient.LastEventTimestamp));
            SetText(errorText, "Erreur: " + Safe(liveClient.LastError));
            SetColor(errorText, string.IsNullOrWhiteSpace(liveClient.LastError) ? neutralColor : disconnectedColor);
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }

        private static string FormatTimestamp(long unixMs)
        {
            if (unixMs <= 0) return "-";
            var dt = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToLocalTime();
            return dt.ToString("HH:mm:ss");
        }

        private static void SetColor(Text target, Color color)
        {
            if (target != null) target.color = color;
        }
    }
}
