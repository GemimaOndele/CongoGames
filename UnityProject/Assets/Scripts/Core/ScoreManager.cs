using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace CongoGames.Core
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [SerializeField] private int basePoints = 10;
        [SerializeField] private int streakBonus = 2;
        [SerializeField] private int maxLeaderboardSize = 5;

        private readonly Dictionary<string, PlayerData> players = new Dictionary<string, PlayerData>();
        private readonly Dictionary<string, float> multiplierUntil = new Dictionary<string, float>();
        private readonly Dictionary<string, int> multiplierValue = new Dictionary<string, int>();

        public event Action<List<PlayerData>> OnLeaderboardChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void RegisterAnswer(string username, bool isCorrect, bool isFastest)
        {
            PlayerData player = GetOrCreate(username);
            if (isCorrect)
            {
                player.Streak++;
                int points = basePoints + (player.Streak * streakBonus) + (isFastest ? 5 : 0);
                if (TryGetActiveMultiplier(username, out int mult))
                {
                    points *= mult;
                }
                player.Score += points;
            }
            else
            {
                player.Streak = 0;
            }

            NotifyLeaderboardChanged();
        }

        public void AddPoints(string username, int points)
        {
            PlayerData player = GetOrCreate(username);
            player.Score += points;
            NotifyLeaderboardChanged();
        }

        public void ApplyMultiplier(string username, int multiplier, int durationSec)
        {
            if (multiplier < 2 || durationSec <= 0) return;
            multiplierValue[username] = multiplier;
            multiplierUntil[username] = Time.time + durationSec;
        }

        public void ResetScores()
        {
            players.Clear();
            multiplierUntil.Clear();
            multiplierValue.Clear();
            NotifyLeaderboardChanged();
        }

        public List<PlayerData> GetTopPlayers()
        {
            return players.Values
                .OrderByDescending(p => p.Score)
                .Take(maxLeaderboardSize)
                .ToList();
        }

        public int GetHighestScoreAmongPlayers()
        {
            if (players.Count == 0) return 0;
            return players.Values.Max(p => p.Score);
        }

        private PlayerData GetOrCreate(string username)
        {
            if (!players.TryGetValue(username, out PlayerData player))
            {
                player = new PlayerData(username);
                players.Add(username, player);
            }
            return player;
        }

        private bool TryGetActiveMultiplier(string username, out int multiplier)
        {
            multiplier = 1;
            if (!multiplierUntil.TryGetValue(username, out float until)) return false;
            if (Time.time > until)
            {
                multiplierUntil.Remove(username);
                multiplierValue.Remove(username);
                return false;
            }
            multiplier = multiplierValue.TryGetValue(username, out int value) ? value : 1;
            return multiplier > 1;
        }

        private void NotifyLeaderboardChanged()
        {
            OnLeaderboardChanged?.Invoke(GetTopPlayers());
        }
    }
}
