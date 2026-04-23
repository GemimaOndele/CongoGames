using UnityEngine;
using CongoGames.Presentation;

namespace CongoGames.GameModes
{
    public class SemanticMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "semantic";
        public void Begin() { }

        public void Tick(float deltaTime) { }
        public void End() { }
    }

    public class WordScrambleMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "word-scramble";
        public void Begin() { }

        public void Tick(float deltaTime) { }
        public void End() { }
    }

    public class CrosswordLiteMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "crossword-lite";
        public void Begin() { }

        public void Tick(float deltaTime) { }
        public void End() { }
    }

    public class BlindTestMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "blind-test";
        public void Begin()
        {
        }

        public void Tick(float deltaTime) { }

        public void End()
        {
            GameSfxHub.Instance?.StopBlindDemoMusic();
        }
    }

    public class MysteryWordMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "mystery-word";
        public void Begin() { }

        public void Tick(float deltaTime) { }
        public void End() { }
    }

    public class MemoryMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "memory";
        public void Begin() { }

        public void Tick(float deltaTime) { }
        public void End() { }
    }

    public class SpeedChronoMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "speed-chrono";
        public void Begin() { }

        public void Tick(float deltaTime) { }
        public void End() { }
    }

    public class ImageGuessMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "image-guess";
        public void Begin() { }

        public void Tick(float deltaTime) { }
        public void End() { }
    }
}
