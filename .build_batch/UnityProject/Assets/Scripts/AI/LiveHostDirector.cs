using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CongoGames.Network;

namespace CongoGames.AI
{
    /// <summary>
    /// Programme type « 1 h de live » : fichier TextAsset (dossier Resources) format <c>seconde|ligne TTS</c> + CTA
    /// périodiques en live. Impro type LLM = backend (OpenAI) ou autre service — ici aléa + texte
    /// planifié + <see cref="LiaPunchlineBank"/> (anti-répétition TTS côté Unity).
    /// </summary>
    public class LiveHostDirector : MonoBehaviour
    {
        [SerializeField] private bool enableSchedule = true;
        [Tooltip("Si faux, le script 1 h ne se lit qu’en live TikTok (WS connecté) — recommandé en local pour éviter le spam TTS.")]
        [SerializeField] private bool playScriptWhenOffline;
        [Tooltip("Fichier Resources sans extension (ex. LiveHost_1h_Script)")]
        [SerializeField] private string scriptResourceName = "LiveHost_1h_Script";
        [SerializeField] private float ctaMinIntervalSec = 95f;
        [SerializeField] private float ctaMaxIntervalSec = 200f;
        [SerializeField] [Range(0f, 1f)] private float ctaChanceOnTick = 0.35f;

        private float nextCta;
        private float nextCtaTime;
        private readonly List<ScheduledLine> _schedule = new List<ScheduledLine>(64);
        private int _nextScheduleIndex;
        private float _t0;
        private Coroutine _co;

        private struct ScheduledLine
        {
            public float AtSec;
            public string Text;
        }

        private void Start()
        {
            _t0 = Time.unscaledTime;
            nextCta = UnityEngine.Random.Range(ctaMinIntervalSec, ctaMaxIntervalSec);
            nextCtaTime = _t0 + nextCta;
            LoadScript();
            TryStartSchedule();
        }

        private void OnDestroy()
        {
            if (_co != null) StopCoroutine(_co);
        }

        private void TryStartSchedule()
        {
            if (!enableSchedule || _schedule.Count == 0 || _co != null) return;
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            bool ok = playScriptWhenOffline || (live != null && live.IsConnected);
            if (!ok) return;
            _co = StartCoroutine(CoRunSchedule());
        }

        private void Update()
        {
            if (enableSchedule && _co == null && !playScriptWhenOffline)
            {
                TryStartSchedule();
            }

            if (!enableSchedule) return;
            LiveEventClient live = FindAnyObjectByType<LiveEventClient>();
            if (live == null || !live.IsConnected) return;
            if (Time.unscaledTime < nextCtaTime) return;
            nextCta = UnityEngine.Random.Range(ctaMinIntervalSec, ctaMaxIntervalSec);
            nextCtaTime = Time.unscaledTime + nextCta;
            if (UnityEngine.Random.value < ctaChanceOnTick)
            {
                AIHostManager.Instance?.Speak(LiaPunchlineBank.PickCta());
            }
        }

        private void LoadScript()
        {
            _schedule.Clear();
            TextAsset ta = Resources.Load<TextAsset>(scriptResourceName);
            if (ta == null) return;
            foreach (string raw in ta.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                int pipe = line.IndexOf('|', StringComparison.Ordinal);
                if (pipe <= 0) continue;
                if (!float.TryParse(line.Substring(0, pipe).Trim(), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float sec)) continue;
                string msg = line.Substring(pipe + 1).Trim();
                if (string.IsNullOrEmpty(msg)) continue;
                _schedule.Add(new ScheduledLine { AtSec = sec, Text = LiaPunchlineBank.CleanForTts(msg) });
            }

            _schedule.Sort((a, b) => a.AtSec.CompareTo(b.AtSec));
        }

        private IEnumerator CoRunSchedule()
        {
            while (true)
            {
                if (_nextScheduleIndex >= _schedule.Count)
                {
                    yield return new WaitForSeconds(30f);
                    continue;
                }

                float now = Time.unscaledTime - _t0;
                float wait = _schedule[_nextScheduleIndex].AtSec - now;
                if (wait > 0.1f) yield return new WaitForSeconds(wait);
                string t = _schedule[_nextScheduleIndex].Text;
                _nextScheduleIndex++;
                if (!string.IsNullOrEmpty(t)) AIHostManager.Instance?.Speak(t);
            }
        }
    }
}
