using System;
using System.Collections.Generic;
using UnityEngine;
using CongoGames.Core;

namespace CongoGames.AI
{
    /// <summary>
    /// Répliques aléatoires pour Lia (hôte) — fiche virale, sans SFX de rire (voir GameSfxHub).
    /// Resources/LiaPunchlines.txt : blocs <c> / <w> (une phrase par ligne), ou tout dans &lt;c&gt; seul = tout mélangé.
    /// </summary>
    public static class LiaPunchlineBank
    {
        private static string[] _correct;
        private static string[] _wrong;
        private static int _iCorrect;
        private static int _iWrong;
        private static int _iTrans;

        private static void EnsureLoaded()
        {
            if (_correct != null && _correct.Length > 0 && _wrong != null && _wrong.Length > 0) return;
            TextAsset ta = Resources.Load<TextAsset>("LiaPunchlines");
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                var c = new List<string>();
                var w = new List<string>();
                var mixed = new List<string>();
                int section = 0; // 0=mixed, 1=c, 2=w
                foreach (string line in ta.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string t = line.Trim();
                    if (t.Length == 0) continue;
                    if (t.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (t.Equals("<c>", StringComparison.OrdinalIgnoreCase)) { section = 1; continue; }
                    if (t.Equals("<w>", StringComparison.OrdinalIgnoreCase)) { section = 2; continue; }
                    if (section == 1) c.Add(t);
                    else if (section == 2) w.Add(t);
                    else mixed.Add(t);
                }

                if (c.Count == 0 && w.Count == 0 && mixed.Count > 0)
                {
                    _correct = _wrong = mixed.ToArray();
                }
                else
                {
                    _correct = c.Count > 0 ? c.ToArray() : BuildFallbackCorrect();
                    _wrong = w.Count > 0 ? w.ToArray() : BuildFallbackWrong();
                }
            }
            else
            {
                _correct = BuildFallbackCorrect();
                _wrong = BuildFallbackWrong();
            }

            Shuffle(_correct);
            Shuffle(_wrong);
        }

        private static void Shuffle(string[] a)
        {
            if (a == null) return;
            for (int i = 0; i < a.Length; i++)
            {
                int j = UnityEngine.Random.Range(i, a.Length);
                (a[i], a[j]) = (a[j], a[i]);
            }
        }

        private static string[] BuildFallbackCorrect()
        {
            return new[] { "Bien vu !", "C’est validé !", "Tu t’y connais !", "Yes !", "Boom, c’est bon !", "Malamu !" };
        }

        private static string[] BuildFallbackWrong()
        {
            return new[] { "Aïe, pas ça !", "Essaie encore !", "Presque, mais non !", "Raté, on continue !", "Bof bof…", "Même moi j’ai mal !" };
        }

        public static string PickCorrect()
        {
            EnsureLoaded();
            return _correct[(_iCorrect++) % _correct.Length];
        }

        public static string PickWrong()
        {
            EnsureLoaded();
            return _wrong[(_iWrong++) % _wrong.Length];
        }

        public static string BuildModeIntroLine(string modeId)
        {
            if (string.IsNullOrEmpty(modeId)) return "On lance le jeu — bonne chance !";
            return GameModeManager.GetModeDisplayName(modeId) + " : " + ModeRulesOneLiner(modeId);
        }

        public static string BuildTransitionLine(string fromId, string toId)
        {
            EnsureLoaded();
            string a = string.IsNullOrEmpty(fromId) ? "ici" : GameModeManager.GetModeDisplayName(fromId);
            string b = GameModeManager.GetModeDisplayName(toId);
            return "On laisse " + a + " — place à : " + b + " ! " + _correct[_iTrans++ % _correct.Length];
        }

        /// <summary>Règles + transition — une seule phrase TTS au début de chaque mode.</summary>
        public static string BuildTransitionWithRules(string fromId, string toId)
        {
            string name = GameModeManager.GetModeDisplayName(toId);
            string r = ModeRulesOneLiner(toId);
            if (string.IsNullOrEmpty(fromId) || fromId == toId)
            {
                return name + " — " + r;
            }

            return "On quitte " + GameModeManager.GetModeDisplayName(fromId) + " pour " + name + ". " + r;
        }

        public static string ModeRulesOneLiner(string modeId)
        {
            switch (modeId)
            {
                case "quiz": return "lis la question, appuie sur A, B, C ou D (bandeau du bas), puis écoute Lia.";
                case "semantic": return "relie l’idée, tape le mot clé, Valider.";
                case "word-scramble": return "thème affiché : trouve les mots (ordre libre) — la ligne jaune t’aide ; tape ou clique les lettres, Valider.";
                case "crossword-lite": return "mots cachés dans la grande grille (H/V/diagonales) — cherche n’importe lequel de la liste, tape le mot, Valider.";
                case "blind-test": return "écoutes la musique, puis choisis A–D quand c’est actif.";
                case "mystery-word": return "devine le mot complet : champ en bas, Valider.";
                case "memory": return "ouvre deux cartes : s’il y a la même lettre, la paire reste.";
                case "speed-chrono": return "3-2-1 — une cible 1/2/3/4 cachée : au GO, appuie vite sur la touche 1, 2, 3 ou 4 (même chiffre en live dans le chat).";
                case "image-guess": return "l’image se révèle : écris ce que tu vois, Valider.";
                default: return "suis l’indication à l’écran, Valider si besoin.";
            }
        }

        /// <summary>Voix courte de Lia après bon / mauvais choix (TTS — la musique baisse via ducking).</summary>
        public static void SpeakResultReaction(bool correct)
        {
            AIHostManager.Instance?.Speak(correct ? PickCorrect() : PickWrong());
        }
    }
}
