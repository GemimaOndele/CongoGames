using System;

namespace CongoGames.Core
{
    [Serializable]
    public class PlayerData
    {
        public string Username;
        public int Score;
        public int Streak;
        public long LastAnswerTs;
        public string AvatarUrl;

        public PlayerData(string username)
        {
            Username = username;
            Score = 0;
            Streak = 0;
            LastAnswerTs = 0;
            AvatarUrl = "";
        }
    }
}
