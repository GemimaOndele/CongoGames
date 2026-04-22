using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using CongoGames.Core;
using CongoGames.AI;
using CongoGames.UI;

namespace CongoGames.Network
{
    [Serializable]
    public class LiveMessage
    {
        public string type;
        public long ts;
        public string user;
        public string message;
        public string giftName;
        public bool accepted;
        public string action;
        public int value;
        public int durationSec;

        public string category;
        public string difficulty;
        public string question;
        public string[] options;
        public string correctAnswer;
        public string explanation;
    }

    public class LiveEventClient : MonoBehaviour
    {
        [SerializeField] private string wsUrl = "wss://congogames-ws-production.up.railway.app";
        [SerializeField] private string[] localFallbackWsUrls = { "ws://localhost:8080", "ws://localhost:8081", "ws://localhost:8082" };
        [SerializeField] private QuestionUI questionUI;
        private ClientWebSocket socket;
        private CancellationTokenSource cts;

        private async void Start()
        {
            cts = new CancellationTokenSource();
            if (questionUI == null) questionUI = FindObjectOfType<QuestionUI>();
            await ConnectAndListen(cts.Token);
        }

        private async Task ConnectAndListen(CancellationToken token)
        {
            socket = await ConnectWithFallback(token);
            _ = ReceiveLoop(token);
        }

        private async Task<ClientWebSocket> ConnectWithFallback(CancellationToken token)
        {
            string[] targets = BuildTargets();
            foreach (string target in targets)
            {
                try
                {
                    var candidate = new ClientWebSocket();
                    await candidate.ConnectAsync(new Uri(target), token);
                    Debug.Log("Connected WS: " + target);
                    return candidate;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("WS connect failed: " + target + " (" + ex.Message + ")");
                }
            }

            throw new Exception("Unable to connect to any WS endpoint.");
        }

        private string[] BuildTargets()
        {
            if (string.IsNullOrWhiteSpace(wsUrl))
            {
                return localFallbackWsUrls;
            }

            string[] targets = new string[1 + localFallbackWsUrls.Length];
            targets[0] = wsUrl;
            for (int i = 0; i < localFallbackWsUrls.Length; i++)
            {
                targets[i + 1] = localFallbackWsUrls[i];
            }
            return targets;
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            byte[] buffer = new byte[4096];
            while (socket != null && socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleRawMessage(json);
            }
        }

        private void HandleRawMessage(string json)
        {
            LiveMessage msg = JsonUtility.FromJson<LiveMessage>(json);
            if (msg == null || string.IsNullOrEmpty(msg.type)) return;

            if (msg.type == "chat")
            {
                HandleChat(msg);
                return;
            }

            if (msg.type == "gift")
            {
                HandleGift(msg);
                return;
            }

            if (msg.type == "question")
            {
                HandleQuestion(msg);
            }
        }

        private void HandleChat(LiveMessage msg)
        {
            string answer = (msg.message ?? string.Empty).Trim().ToUpperInvariant();
            if (answer != "A" && answer != "B" && answer != "C" && answer != "D") return;

            bool isCorrect = QuestionManager.Instance != null && QuestionManager.Instance.ValidateAnswer(answer);
            ScoreManager.Instance.RegisterAnswer(msg.user, isCorrect, false);

            if (isCorrect)
            {
                AIHostManager.Instance?.Speak(msg.user + " bonne reponse.");
            }
        }

        private void HandleGift(LiveMessage msg)
        {
            if (!msg.accepted) return;

            if (msg.action == "points")
            {
                ScoreManager.Instance.AddPoints(msg.user, msg.value);
                AIHostManager.Instance?.Speak(msg.user + " gagne un bonus cadeau.");
                return;
            }

            if (msg.action == "multiplier")
            {
                ScoreManager.Instance.ApplyMultiplier(msg.user, msg.value, msg.durationSec);
                AIHostManager.Instance?.Speak("Multiplicateur x" + msg.value + " pour " + msg.user);
                return;
            }

            if (msg.action == "battle")
            {
                BattleManager.Instance.StartBattleFromTop(ScoreManager.Instance.GetTopPlayers());
                AIHostManager.Instance?.Speak("Battle declenchee.");
                return;
            }

            if (msg.action == "bonusRound")
            {
                GameModeManager.Instance.StartMode("speed-chrono");
                AIHostManager.Instance?.Speak("Bonus round active.");
            }
        }

        private void HandleQuestion(LiveMessage msg)
        {
            LiveQuestion question = new LiveQuestion
            {
                category = msg.category,
                difficulty = msg.difficulty,
                question = msg.question,
                options = msg.options,
                correctAnswer = msg.correctAnswer,
                explanation = msg.explanation
            };

            QuestionManager.Instance.SetQuestion(question);
            questionUI?.Render(question);
            AIHostManager.Instance?.Speak(question.question);
        }

        private async void OnDestroy()
        {
            if (cts != null) cts.Cancel();
            if (socket != null && socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
        }
    }
}
