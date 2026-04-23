namespace CongoGames.Core
{
    /// <summary>Identifiants de mini-jeux reconnus par le serveur et l’UI.</summary>
    public static class ModeIdCatalog
    {
        public static string NormalizeOrNull(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            string s = raw.Trim().ToLowerInvariant();
            switch (s)
            {
                case "quiz":
                case "semantic":
                case "word-scramble":
                case "crossword-lite":
                case "blind-test":
                case "mystery-word":
                case "memory":
                case "speed-chrono":
                case "image-guess":
                    return s;
                default:
                    return null;
            }
        }
    }
}
