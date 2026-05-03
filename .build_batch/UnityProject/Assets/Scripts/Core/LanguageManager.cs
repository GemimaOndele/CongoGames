using System.Collections.Generic;
using UnityEngine;

namespace CongoGames.Core
{
    public enum GameLanguage
    {
        FR,
        LN,
        KG
    }

    public class LanguageManager : MonoBehaviour
    {
        public static LanguageManager Instance { get; private set; }

        [SerializeField] private GameLanguage currentLanguage = GameLanguage.FR;

        private readonly Dictionary<string, string> fr = new Dictionary<string, string>
        {
            { "intro_1", "Bienvenue sur CongoGames — Congo, fierté et culture." },
            { "intro_2", "Réponds vite, monte au classement, gagne la battle." },
            { "intro_3", "CongoGames commence maintenant." }
        };

        private readonly Dictionary<string, string> ln = new Dictionary<string, string>
        {
            { "intro_1", "Boyei malamu na CongoGames — Kongo, culture mpe esengo." },
            { "intro_2", "Pesa eyano noki, mata na classement, longa battle." },
            { "intro_3", "CongoGames ebandi sikoyo." }
        };

        private readonly Dictionary<string, string> kg = new Dictionary<string, string>
        {
            { "intro_1", "Mbote na CongoGames — Kongo, culture mpé kéyíké." },
            { "intro_2", "Pesa mvutu nswalu, mata na classement, longa battle." },
            { "intro_3", "CongoGames yandi sikwé." }
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetLanguage(GameLanguage language)
        {
            currentLanguage = language;
        }

        public string T(string key)
        {
            Dictionary<string, string> table = currentLanguage switch
            {
                GameLanguage.LN => ln,
                GameLanguage.KG => kg,
                _ => fr
            };
            return table.TryGetValue(key, out string value) ? value : key;
        }
    }
}
