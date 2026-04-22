using UnityEngine;

namespace CongoGames.GameModes
{
    public class SemanticMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "semantic";
        public void Begin() { Debug.Log("Semantic mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Semantic mode ended."); }
    }

    public class WordScrambleMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "word-scramble";
        public void Begin() { Debug.Log("Word scramble mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Word scramble mode ended."); }
    }

    public class CrosswordLiteMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "crossword-lite";
        public void Begin() { Debug.Log("Crossword-lite mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Crossword-lite mode ended."); }
    }

    public class BlindTestMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "blind-test";
        public void Begin() { Debug.Log("Blind test mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Blind test mode ended."); }
    }

    public class MysteryWordMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "mystery-word";
        public void Begin() { Debug.Log("Mystery word mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Mystery word mode ended."); }
    }

    public class MemoryMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "memory";
        public void Begin() { Debug.Log("Memory mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Memory mode ended."); }
    }

    public class SpeedChronoMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "speed-chrono";
        public void Begin() { Debug.Log("Speed chrono mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Speed chrono mode ended."); }
    }

    public class ImageGuessMode : MonoBehaviour, IGameMode
    {
        public string ModeId => "image-guess";
        public void Begin() { Debug.Log("Image guess mode started."); }
        public void Tick(float deltaTime) { }
        public void End() { Debug.Log("Image guess mode ended."); }
    }
}
