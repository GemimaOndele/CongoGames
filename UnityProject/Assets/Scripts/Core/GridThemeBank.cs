using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CongoGames.Core
{
    /// <summary>
    /// Thèmes pour mots mélangés / mots croisés : listes de mots liés au Congo (pas RDC) — mélange et tirage 5–15 mots.
    /// Référentiel de validation scolaire: docs/grid-theme-bank-validation-fr.md
    /// </summary>
    public static class GridThemeBank
    {
        public readonly struct Theme
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string[] Words;

            public Theme(string id, string label, string[] words)
            {
                Id = id;
                Label = label;
                Words = words;
            }
        }

        private static readonly Theme[] Themes =
        {
            new Theme("geo", "Géo & lieux (Congo)", new[]
            {
                "KINTELE", "POINTENOIRE", "CUVETTE", "LIKOUALA", "SANGHA", "POOL",
                "LEFINI", "NIARI", "LOANGO", "OUESSO", "BETOU", "IMPFONDO",
                "MAYOMBE", "ESTUAIRE", "BRAZZAVILLE", "DJAMBALA", "OWANDO", "DOLISIE"
            }),
            new Theme("departements", "Départements du Congo", new[]
            {
                "BOUENZA", "BRAZZAVILLE", "CUVETTE", "CUVETTEOUEST", "KOUILOU", "LEKOUMOU",
                "LIKOUALA", "NIARI", "PLATEAUX", "POINTENOIRE", "POOL", "SANGHA"
            }),
            new Theme("affluents", "Affluents & rivières (Congo)", new[]
            {
                "ALIMA", "LIKOUALA", "MOSSAKA", "SANGHA", "NGOKO", "LEFINI",
                "DJOUE", "FOULAKARI", "LOUFIKA", "BOUENZA", "NIARI", "KOUILOU",
                "MPASSA", "LOUBILIKA", "MOTABA", "FAYA", "LIKOUALAUX"
            }),
            new Theme("villes", "Villes & préfectures (Congo)", new[]
            {
                "BRAZZAVILLE", "POINTENOIRE", "DOLISIE", "SIBITI", "MADINGOU", "OWANDO",
                "EWO", "IMPFONDO", "DJAMBALA", "KINKALA", "OUESSO", "LOANGO"
            }),
            new Theme("nature", "Forêts, fleuve, faune", new[]
            {
                "FLEUVE", "CONGO", "GORILLE", "SANGHA", "FORET", "PARC", "EQUATEUR",
                "PLUIE", "OCEAN", "RIVIERE", "COCOTIER", "MANGROVE", "BASSIN",
                "FLORE", "FAUNE", "HERBE", "BOISE", "PLAGE", "CRABE"
            }),
            new Theme("culture", "Culture, fête, drapeau", new[]
            {
                "BANTOU", "RUMBA", "NGOMA", "NDOMBOLO", "SEBEN", "DANSE",
                "CHANT", "CONGO", "MBOTE", "FETE", "AOUT", "CULTURE",
                "FOULE", "FOYER", "GLOIRE", "HONNEUR", "SCENE", "HABIT"
            }),
            new Theme("lingala", "Lingala — vocabulaire (mots à retrouver)", new[]
            {
                "MELESI", "MBOTE", "MWANA", "MANGI", "TATA", "MAMA", "BISO", "KOKO", "MOTO", "MOKO", "PONA", "SUKA", "SIMBA", "BOKO", "BANDA", "KATI", "PEKO", "SUKI", "POTO", "MUNA", "MOKI", "BANA", "LISI", "NZOTO", "MOTI", "YAYA", "BUKA", "MELI", "MOSI", "LIBO"
            }),
            new Theme("kituba", "Kitouba — vocabulaire (mots à retrouver)", new[]
            {
                "MALAMU", "SANTU", "MUNTU", "BANTU", "BOKO", "DIBU", "KUTA", "BUKA", "SUKA", "KUKU", "KISI", "MUKA", "TUKA", "DUKA", "ZONI", "TONI", "MUKO", "MBOA", "BUKI", "KONO", "TUKO", "BUKO", "KITA", "BUKU", "DUKO", "TATA", "MAMA", "MUNA", "SUKI", "MPASI"
            }),
            new Theme("sport", "Stade, foot, ferveur", new[]
            {
                "BUTEUR", "STADE", "EQUIPE", "BALLON", "VICTO", "COUPE", "FOULE", "CONGO", "BANC", "TALENT", "ESSAI", "JOUER", "MATCH", "GAGNE", "BUTS", "BLESS", "TIR", "BUT", "CARTO", "MEDAL"
            }),
            new Theme("histoire", "Indépendance, symboles", new[]
            {
                "AOUT", "ANNEE", "DROIT", "LIBRE", "PAIX", "UNION", "ORDRE", "TRAVAI", "PROGRE", "ECOLE", "VOTER", "LECONS", "TEMPO", "CONGO", "AVANT", "APRES", "EPOPEE", "PATRIO", "STATUE", "GLOIRE"
            }),
            new Theme("eco", "Commerce & vie quotidienne", new[]
            {
                "MARCHE", "VENTE", "POIDS", "PRIX", "KIOSK", "CAFE", "PAIN", "SABLE", "ROUTE", "GARES", "TAXES", "PRIME", "SALAIRE", "OFFRE", "ACHAT", "BUDGET", "CREDIT", "STOCKS", "BOUTIK", "CAISSE"
            }),
        };

        public static int MinWords => 5;
        public static int MaxWords => 21;

        /// <summary>Tirage d’un thème + N mots uniques, longueur 4–12, upper sans accents pour les grilles.</summary>
        public static void DrawSessionWords(out string themeLabel, out List<string> words, int? wordCount = null)
        {
            int n = wordCount ?? Random.Range(MinWords, MaxWords + 1);
            n = Mathf.Clamp(n, MinWords, MaxWords);
            const int maxLenForGrid = 18;
            Theme t = Themes[Random.Range(0, Themes.Length)];
            themeLabel = t.Label;
            var pool = new List<string>(t.Words.Length);
            foreach (string w in t.Words)
            {
                if (string.IsNullOrEmpty(w)) continue;
                string u = SanitizeForGrid(w);
                if (u.Length >= 4 && u.Length <= maxLenForGrid) pool.Add(u);
            }

            if (pool.Count < n)
            {
                for (int k = 0; k < 40 && pool.Count < n; k++)
                {
                    pool.Add("CONGO" + (char)('A' + (k % 26)));
                }
            }

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            var picked = new List<string>(n);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < pool.Count && picked.Count < n; i++)
            {
                if (seen.Add(pool[i])) picked.Add(pool[i]);
            }

            while (picked.Count < n)
            {
                string fallback = "PAYS" + picked.Count;
                if (seen.Add(fallback)) picked.Add(fallback);
            }

            for (int i = picked.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (picked[i], picked[j]) = (picked[j], picked[i]);
            }

            words = picked;
        }

        public static string SanitizeForGrid(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Trim().ToUpperInvariant();
            s = s.Replace("É", "E").Replace("È", "E").Replace("Ê", "E")
                .Replace("À", "A").Replace("Ô", "O")
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("'", "");
            return s;
        }

        /// <summary>
        /// Affichage UI (liste des mots trouvés) : remet des tirets pour les mots composés.
        /// </summary>
        public static string ToUiDisplayWord(string word)
        {
            string w = SanitizeForGrid(word ?? "");
            switch (w)
            {
                case "POINTENOIRE": return "POINTE-NOIRE";
                case "CUVETTEOUEST": return "CUVETTE-OUEST";
                case "LIKOUALAUX": return "LIKOUALA-AUX-HERBES";
                default: return w;
            }
        }

        /// <summary>Glose FR optionnelle (thème langues / démo) — vide si inconnu.</summary>
        public static string TryGlossFr(string word)
        {
            string k = SanitizeForGrid(word ?? "");
            switch (k)
            {
                case "MELESI": return "merci";
                case "MBOTE": return "salut";
                case "MALAMU": return "bien";
                case "MOTEMA": return "cœur";
                case "MWANA": return "enfant";
                case "MAIE": return "eau";
                case "KOKO": return "grand-père";
                case "BISO": return "nous";
                case "MANGI": return "faim";
                case "LIBOSO": return "devant";
                case "SIMBA": return "lion";
                case "MOSALA": return "travail";
                case "NDAKISA": return "aider";
                case "NZOTO": return "corps";
                case "MABOKO": return "mains";
                case "PEKO": return "un peu";
                case "ZALA": return "être";
                case "SALUT": return "salut";
                case "SANTU": return "saint / sainte (souvent)";
                case "MUNTU": return "personne / être humain (souvent)";
                case "BANTU": return "pluriel de muntu (souvent)";
                case "MPASI": return "souffrance / peine (souvent)";
                case "MOTI": return "mort (souvent)";
                case "BANDA": return "tribu / origine (souvent)";
                case "PONA": return "pour / afin de (souvent)";
                case "POTO": return "port (souvent)";
                case "MOKI": return "regard / œil (souvent)";
                case "BANA": return "enfants (souvent)";
                case "LISI": return "yeux (souvent)";
                case "LIBO": return "intestins (souvent)";
                case "YAYA": return "oncle (souvent)";
                case "MELI": return "poisson (souvent)";
                case "MOSI": return "fumée (souvent)";
                case "BUKA": return "ouvrir (souvent)";
                default: return "";
            }
        }

        /// <summary>
        /// Indice joueur (thème Congo), sans jargon « prompt » ni méta de dev.
        /// </summary>
        public static string InGameThemeHintFr(string themeLabel)
        {
            string s = (themeLabel ?? "").Trim();
            if (string.IsNullOrEmpty(s))
            {
                return "Tous les mots à trouver vont dans le sens du thème affiché dans le titre.";
            }

            if (s.IndexOf("Géo", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("lieux", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : cartes, fleuves, villes et régions du Congo.";
            }

            if (s.IndexOf("Départements", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : divisions administratives de la République du Congo.";
            }

            if (s.IndexOf("Affluents", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("rivières", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : rivières et affluents du bassin du Congo (côté Congo).";
            }

            if (s.IndexOf("Villes", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("préfectures", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("prefectures", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : villes congolaises et chefs-lieux départementaux.";
            }

            if (s.IndexOf("Forêt", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("fleuve", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("faune", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : nature, eau, animaux et paysages.";
            }

            if (s.IndexOf("Culture", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("fête", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("drapeau", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : fêtes, musique, public et symboles nationaux.";
            }

            if (s.IndexOf("Lingala", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : mots d'usage courant en Lingala (salutations, famille...).";
            }

            if (s.IndexOf("Kituba", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("Kitouba", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : mots d'usage courant en Kitouba (politesse, maison...).";
            }

            if (s.IndexOf("Stade", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("foot", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : ballon, équipe, stade et ambiance match.";
            }

            if (s.IndexOf("Indépendance", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("symboles", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : dates fortes, droits, fierté et symboles du pays.";
            }

            if (s.IndexOf("Commerce", StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("quotidien", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Indice : marché, prix, courses, travail et vie de tous les jours.";
            }

            return "Indice : reste dans le thème du titre — lieux, habitudes et mots du quotidien.";
        }

        /// <summary>Définition courte pour grille mots croisés.</summary>
        public static string BuildCrosswordDefinitionFr(string word, string themeLabel)
        {
            string w = SanitizeForGrid(word ?? "");
            if (string.IsNullOrEmpty(w))
            {
                return "Mot du thème.";
            }

            string gloss = TryGlossFr(w);
            if (!string.IsNullOrEmpty(gloss))
            {
                return "Terme courant : " + gloss + ".";
            }

            if (w.EndsWith("E", StringComparison.Ordinal))
            {
                return "Nom du thème (" + w.Length + " lettres), souvent utilisé au quotidien.";
            }

            string hint = InGameThemeHintFr(themeLabel);
            return hint.Replace("Indice : ", "") + " (" + w.Length + " lettres).";
        }
    }
}
