namespace CongoGames.GameModes
{
    public interface IGameMode
    {
        string ModeId { get; }
        void Begin();
        void Tick(float deltaTime);
        void End();
    }
}
