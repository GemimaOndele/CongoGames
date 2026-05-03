namespace CongoGames.Core
{
    /// <summary>
    /// Durées de référence partagées (structure uniforme des manches).
    /// Les composants peuvent les copier en valeurs par défaut dans l’inspecteur.
    /// </summary>
    public static class GameFlowDurations
    {
        public const float QuizReadQuestion = 0f;
        public const float QuizPickAnswer = 86400f;
        public const float QuizShowResult = 0.4f;
        public const float BetweenQuestionsGap = 0.28f;
    }
}
