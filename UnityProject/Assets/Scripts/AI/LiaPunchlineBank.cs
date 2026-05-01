using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using CongoGames.Core;

namespace CongoGames.AI
{
    /// <summary>
    /// Répliques Lia — Resources/LiaPunchlines.txt avec blocs : &lt;c&gt; &lt;w&gt; &lt;b&gt; &lt;t&gt; &lt;v&gt; &lt;cta&gt; (ou tout dans &lt;c&gt; = mélangé).
    /// Anti-répétition : file d’attente des N derniers index par catégorie.
    /// </summary>
    public static class LiaPunchlineBank
    {
        private const int AvoidRecent = 6;
        private static string[] _correct;
        private static string[] _wrong;
        private static string[] _battle;
        private static string[] _trans;
        private static string[] _viral;
        private static string[] _cta;
        private static readonly Queue<string> _rc = new Queue<string>(AvoidRecent + 1);
        private static readonly Queue<string> _rw = new Queue<string>(AvoidRecent + 1);
        private static readonly Queue<string> _rb = new Queue<string>(AvoidRecent + 1);
        private static readonly Queue<string> _rt = new Queue<string>(AvoidRecent + 1);
        private static readonly Queue<string> _rv = new Queue<string>(AvoidRecent + 1);
        private static readonly Queue<string> _rcta = new Queue<string>(AvoidRecent + 1);

        private static void EnsureLoaded()
        {
            if (_correct != null && _correct.Length > 0) return;
            TextAsset ta = Resources.Load<TextAsset>("LiaPunchlines");
            if (ta != null && !string.IsNullOrEmpty(ta.text))
            {
                var c = new List<string>();
                var w = new List<string>();
                var b = new List<string>();
                var t = new List<string>();
                var v = new List<string>();
                var cta = new List<string>();
                int section = 0;
                foreach (string raw in ta.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string line0 = raw.Trim();
                    if (line0.Length == 0) continue;
                    if (line0.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line0.Equals("<c>", StringComparison.OrdinalIgnoreCase)) { section = 1; continue; }
                    if (line0.Equals("<w>", StringComparison.OrdinalIgnoreCase)) { section = 2; continue; }
                    if (line0.Equals("<b>", StringComparison.OrdinalIgnoreCase)) { section = 3; continue; }
                    if (line0.Equals("<t>", StringComparison.OrdinalIgnoreCase)) { section = 4; continue; }
                    if (line0.Equals("<v>", StringComparison.OrdinalIgnoreCase)) { section = 5; continue; }
                    if (line0.Equals("<cta>", StringComparison.OrdinalIgnoreCase)) { section = 6; continue; }
                    if (line0.Length >= 2 && char.IsLetter(line0[0]) && line0[1] == ':')
                    {
                        char k = char.ToLowerInvariant(line0[0]);
                        string rest = line0.Substring(2).Trim();
                        rest = CleanForTts(rest);
                        if (string.IsNullOrEmpty(rest)) continue;
                        if (k == 'c') c.Add(rest);
                        else if (k == 'w') w.Add(rest);
                        else if (k == 'b') b.Add(rest);
                        else if (k == 't') t.Add(rest);
                        else if (k == 'v') v.Add(rest);
                        else if (k == 'a') cta.Add(rest);
                        continue;
                    }

                    string line = CleanForTts(line0);
                    if (string.IsNullOrEmpty(line)) continue;
                    if (section == 1) c.Add(line);
                    else if (section == 2) w.Add(line);
                    else if (section == 3) b.Add(line);
                    else if (section == 4) t.Add(line);
                    else if (section == 5) v.Add(line);
                    else if (section == 6) cta.Add(line);
                }

                if (c.Count == 0 && w.Count == 0)
                {
                    _correct = _wrong = BuildFallbackCorrect();
                }
                else
                {
                    _correct = c.Count > 0 ? c.ToArray() : BuildFallbackCorrect();
                    _wrong = w.Count > 0 ? w.ToArray() : BuildFallbackWrong();
                }

                _battle = b.Count > 0 ? b.ToArray() : new[] { "Combat en place — chaud chaud !" };
                _trans = t.Count > 0 ? t.ToArray() : new[] { "On enchaîne !" };
                _viral = v.Count > 0 ? v.ToArray() : new[] { "C’est chaud ici, fais un effort !" };
                _cta = cta.Count > 0 ? cta.ToArray() : BuildDefaultCta();
            }
            else
            {
                _correct = BuildFallbackCorrect();
                _wrong = BuildFallbackWrong();
                _battle = new[] { "Duel en cours !" };
                _trans = new[] { "Nouveau défi !" };
                _viral = new[] { "Ici, on joue sérieux !" };
                _cta = BuildDefaultCta();
            }

            Shuffle(_correct);
            Shuffle(_wrong);
            Shuffle(_battle);
            Shuffle(_trans);
            Shuffle(_viral);
            Shuffle(_cta);
        }

        public static string CleanForTts(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim();
            s = Regex.Replace(s, "^[0-9]+[.)]\\s*", "");
            s = s.Trim(' ', '"', '«', '»', '“', '”', '\'');
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length;)
            {
                int cp = char.ConvertToUtf32(s, i);
                int adv = char.IsHighSurrogate(s[i]) && i + 1 < s.Length ? 2 : 1;
                if (IsEmojiOrPicSymbol(cp)) { i += adv; continue; }

                sb.Append(char.ConvertFromUtf32(cp));
                i += adv;
            }

            s = sb.ToString().Trim();
            s = Regex.Replace(s, @"\s+", " ");
            return s;
        }

        private static bool IsEmojiOrPicSymbol(int cp)
        {
            return (cp is >= 0x1f300 and <= 0x1faff) || (cp is >= 0x2600 and <= 0x27bf) || (cp is >= 0xfe00 and <= 0xfe0f);
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

        private static string[] BuildFallbackCorrect() =>
            new[] { "Bien vu !", "C'est validé !", "Très propre !", "Malamu !" };

        private static string[] BuildFallbackWrong() =>
            new[] { "Aïe, pas ça !", "Essaie encore !", "Raté, on continue !" };

        private static string[] BuildDefaultCta() => new[]
        {
            "Pense à t'abonner et à partager le live, ça booste le CongoGames !",
            "Si t'aimes le show, abonne-toi, republie, et envoie un petit cadeau pour soutenir !",
            "N'oublie pas de suivre, partager et repartager, et d'offrir un cadeau si tu kiffes l'énergie !"
        };

        private static string PickFromPool(string[] pool, Queue<string> recent)
        {
            EnsureLoaded();
            if (pool == null || pool.Length == 0) return "";
            for (int attempt = 0; attempt < 48; attempt++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Length);
                string s = (pool[idx] ?? "").Trim();
                if (s.Length < 1) continue;
                if (IsRecent(recent, s)) continue;
                EnqueueRecent(recent, s);
                return s;
            }

            int fb = UnityEngine.Random.Range(0, pool.Length);
            string t = (pool[fb] ?? "").Trim();
            EnqueueRecent(recent, t);
            return t;
        }

        private static bool IsRecent(Queue<string> recent, string s)
        {
            if (recent == null || s == null) return false;
            foreach (string x in recent)
            {
                if (string.Equals(x, s, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static void EnqueueRecent(Queue<string> recent, string s)
        {
            if (recent == null || AvoidRecent <= 0) return;
            while (recent.Count >= AvoidRecent) recent.Dequeue();
            recent.Enqueue(s);
        }

        public static string PickCorrect() => PickFromPool(_correct, _rc);
        public static string PickWrong() => PickFromPool(_wrong, _rw);
        public static string PickBattle() => PickFromPool(_battle, _rb);
        public static string PickTransition() => PickFromPool(_trans, _rt);
        public static string PickViral() => PickFromPool(_viral, _rv);
        public static string PickCta() => PickFromPool(_cta, _rcta);

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
            return "On laisse " + a + " — place à : " + b + " ! " + PickTransition();
        }

        public static string BuildTransitionWithRules(string fromId, string toId)
        {
            string name = GameModeManager.GetModeDisplayName(toId);
            string r = ModeRulesOneLiner(toId);
            if (string.IsNullOrEmpty(fromId) || fromId == toId)
            {
                return "Question suivante, mode " + name + ". " + r;
            }

            return "Question suivante, mode " + name + ". " + r;
        }

        public static string BuildNextQuestionLine(string modeId)
        {
            string name = GameModeManager.GetModeDisplayName(modeId);
            string r = ModeRulesOneLiner(modeId);
            return "Question suivante, mode " + name + ". " + r;
        }

        public static string ModeRulesOneLiner(string modeId)
        {
            switch (modeId)
            {
                case "quiz": return "Question : choisis A, B, C ou D en bas.";
                case "semantic": return "Tape le mot, puis Valider.";
                case "word-scramble": return "Trouve les mots du thème — Valider.";
                case "crossword-lite": return "Trouve des mots dans la grille — Valider.";
                case "blind-test": return "Écoute, puis A à D.";
                case "mystery-word": return "Tape le mot, Valider.";
                case "memory": return "Deux cartes identiques = paire.";
                case "speed-chrono": return "3, 2, 1, GO : touche 1 à 4 vite !";
                case "image-guess": return "Regarde l’image, tape un mot, Valider.";
                default: return "Suis l’écran, Valider.";
            }
        }

        public static void SpeakResultReaction(bool correct)
        {
            AIHostManager.Instance?.Speak(correct ? PickCorrect() : PickWrong());
        }
    }
}
