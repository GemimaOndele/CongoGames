using System.Collections.Generic;
using UnityEngine;
using CongoGames.AI;

namespace CongoGames.Core
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [SerializeField] private bool battleActive;
        [SerializeField] private string playerA;
        [SerializeField] private string playerB;

        public bool IsBattleActive => battleActive;
        public string PlayerA => playerA;
        public string PlayerB => playerB;

        private void Awake()
        {
            Instance = this;
        }

        public void StartBattle(string p1, string p2)
        {
            battleActive = true;
            playerA = p1;
            playerB = p2;
            Debug.Log($"Battle started: {p1} vs {p2}");
            string v = LiaPunchlineBank.PickViral();
            AIHostManager.Instance?.Speak(
                "Battle : " + p1 + " contre " + p2 + ". " + LiaPunchlineBank.PickBattle() + " " + v);
        }

        public void StartBattleFromTop(List<PlayerData> top)
        {
            if (top.Count < 2) return;
            StartBattle(top[0].Username, top[1].Username);
        }

        public void EndBattle(string winner)
        {
            battleActive = false;
            Debug.Log("Battle winner: " + winner);
        }
    }
}
