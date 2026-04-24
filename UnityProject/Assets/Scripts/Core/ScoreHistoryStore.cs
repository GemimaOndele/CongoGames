using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CongoGames.Core
{
    /// <summary>
    /// Meilleurs scores session (démo) par jour / semaine / mois / global — PlayerPrefs.
    /// </summary>
    public static class ScoreHistoryStore
    {
        private const string KDay = "cg_hi_day_";
        private const string KWeek = "cg_hi_week_";
        private const string KMonth = "cg_hi_month_";
        private const string KAll = "cg_hi_all";
        private const int MaxLog = 12;

        /// <summary>Enregistre un palier (ex. fin de manche) si le total courant est un record pour la période.</summary>
        public static void RegisterHighWaterIfNeeded(int currentTotal)
        {
            if (currentTotal < 0) return;
            string dk = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            Bump(KDay + dk, currentTotal);
            Bump(KWeek + WeekId(), currentTotal);
            Bump(KMonth + DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture), currentTotal);
            Bump(KAll, currentTotal);
            AppendLog(currentTotal);
        }

        private static void Bump(string key, int points)
        {
            int p = PlayerPrefs.GetInt(key, 0);
            if (points > p)
            {
                PlayerPrefs.SetInt(key, points);
                PlayerPrefs.Save();
            }
        }

        private static string WeekId()
        {
            DateTime d = DateTime.UtcNow;
            Calendar cal = CultureInfo.InvariantCulture.Calendar;
            int w = cal.GetWeekOfYear(d, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
            return d.Year + "W" + w.ToString("00", CultureInfo.InvariantCulture);
        }

        private static void AppendLog(int points)
        {
            string line = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "|" + points;
            string old = PlayerPrefs.GetString("cg_score_log", "");
            string all = line + (string.IsNullOrEmpty(old) ? "" : "\n" + old);
            string[] lines = all.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > MaxLog)
            {
                Array.Resize(ref lines, MaxLog);
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(lines[i]);
            }

            PlayerPrefs.SetString("cg_score_log", sb.ToString());
            PlayerPrefs.Save();
        }

        public static string BuildSummaryLine()
        {
            string dk = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            int bestDay = PlayerPrefs.GetInt(KDay + dk, 0);
            int bestWeek = PlayerPrefs.GetInt(KWeek + WeekId(), 0);
            int bestMonth = PlayerPrefs.GetInt(KMonth + DateTime.UtcNow.ToString("yyyyMM", CultureInfo.InvariantCulture), 0);
            int bestAll = PlayerPrefs.GetInt(KAll, 0);
            return "Records session — Jour: " + bestDay + "  Sem.: " + bestWeek + "  Mois: " + bestMonth + "  Global: " + bestAll;
        }
    }
}
