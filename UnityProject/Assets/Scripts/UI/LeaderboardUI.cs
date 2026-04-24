using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CongoGames.Core;

namespace CongoGames.UI
{
    public class LeaderboardUI : MonoBehaviour
    {
        [SerializeField] private Text leaderboardText;

        public void BindRuntime(Text text)
        {
            leaderboardText = text;
            TryHookScore();
        }

        private void OnEnable()
        {
            TryHookScore();
        }

        private void OnDisable()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnLeaderboardChanged -= Render;
            }
        }

        private void TryHookScore()
        {
            if (leaderboardText == null || ScoreManager.Instance == null)
            {
                return;
            }

            ScoreManager.Instance.OnLeaderboardChanged -= Render;
            ScoreManager.Instance.OnLeaderboardChanged += Render;
            Render(ScoreManager.Instance.GetTopPlayers());
        }

        private void Render(List<PlayerData> players)
        {
            if (leaderboardText == null) return;
            var sb = new StringBuilder();
            string today = DateTime.Now.ToString("d MMMM yyyy", CultureInfo.GetCultureInfo("fr-FR"));
            sb.Append("Aujourd’hui : ").Append(today).Append('\n');
            int n = ScoreManager.Instance != null ? ScoreManager.Instance.GetRegisteredPlayerCount() : players.Count;
            sb.Append("Joueurs (scores) : ").Append(n).Append("\n");
            sb.Append(ScoreHistoryStore.BuildSummaryLine()).Append("\n\n");
            for (int i = 0; i < players.Count; i++)
            {
                sb.Append(i + 1).Append(". ").Append(players[i].Username).Append(" - ").Append(players[i].Score).Append('\n');
            }

            leaderboardText.text = sb.ToString();
        }
    }
}
