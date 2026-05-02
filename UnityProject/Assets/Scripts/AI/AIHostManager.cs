using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CongoGames.Core;
using CongoGames.Network;
using CongoGames.Perf;

namespace CongoGames.AI
{
    [RequireComponent(typeof(AudioSource))]
    public class AIHostManager : MonoBehaviour
    {
        public static AIHostManager Instance { get; private set; }

        public static event System.Action<bool> OnSpeakingChanged;

        [SerializeField] private string ttsHttpBase = "http://127.0.0.1:3000";
        [SerializeField] private bool autoDiscoverLocalHttp = true;
        [SerializeField] private int discoverPortMin = 3000;
        [SerializeField] private int discoverPortMax = 3010;
        [SerializeField] private bool preferTtsOverLogs = true;
        [SerializeField] private int maxQueuedLines = 48;
        [Tooltip("Si faux, les répliques de l’animateur ne polluent pas la Console (recommandé).")]
        [SerializeField] private bool logHostLinesToConsole = false;
        [SerializeField] private float ttsQuotaCooldownSec = 300f;
        [Tooltip("Optionnel : URL directe vers un .wav / .ogg / .mp3 pour « Oza zoba » (pas de TTS, pas de quota).")]
        [SerializeField] private string ozaZobaAudioUrl = "";

        private AudioSource audioSource;
        private AudioCacheManager cache;
        private readonly Queue<string> speechQueue = new Queue<string>();
        private readonly object speechQueueLock = new object();
        private Coroutine processCo;
        private bool speaking;
        private bool ttsProbeDone;
        private bool ttsAvailable;
        private float ttsSuspendedUntil;
        private string lastTtsThrottleKey = "";
        private float lastTtsThrottleTime;
        private static bool speechRulesLoaded;
        private static SpeechReplaceRule[] externalSpeechRules = Array.Empty<SpeechReplaceRule>();

        [Serializable]
        private sealed class SpeechReplaceRule
        {
            public string find;
            public string replace;
        }

        [Serializable]
        private sealed class SpeechReplaceFile
        {
            public SpeechReplaceRule[] items;
        }

        private void Awake()
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
            cache = GetComponent<AudioCacheManager>();
            if (cache == null)
            {
                cache = gameObject.AddComponent<AudioCacheManager>();
            }
        }

        private void Start()
        {
            StartCoroutine(BootstrapTts());
        }

        private void OnDestroy()
        {
            InterruptSpeech();
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }

            cache?.ClearAndRelease();
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }
        }

        public void ApplyLiveServerHints(string httpApiBase, int httpPort)
        {
            if (!string.IsNullOrWhiteSpace(httpApiBase))
            {
                ttsHttpBase = httpApiBase.TrimEnd('/');
            }
            else if (httpPort > 0)
            {
                ttsHttpBase = "http://127.0.0.1:" + httpPort;
            }
            else
            {
                return;
            }

            StartCoroutine(ReprobeTts());
        }

        public string TtsHttpBase => ttsHttpBase;
        public bool IsSpeakingNow
        {
            get
            {
                lock (speechQueueLock)
                {
                    return speaking || (audioSource != null && audioSource.isPlaying) || speechQueue.Count > 0;
                }
            }
        }

        private IEnumerator BootstrapTts()
        {
            WebGlCloudEndpoints.TryLoadAndApply();
            if (Application.platform == RuntimePlatform.WebGLPlayer && !string.IsNullOrEmpty(WebGlCloudEndpoints.LoadedTtsBase))
            {
                ttsHttpBase = WebGlCloudEndpoints.LoadedTtsBase;
                autoDiscoverLocalHttp = false;
            }
            else if (autoDiscoverLocalHttp)
            {
                string found = null;
                yield return TtsClient.DiscoverLocalHttpBase("127.0.0.1", discoverPortMin, discoverPortMax, b => found = b);
                if (!string.IsNullOrEmpty(found))
                {
                    ttsHttpBase = found;
                    if (logHostLinesToConsole)
            {
                Debug.Log("CongoGames TTS: HTTP détecté → " + ttsHttpBase);
            }
                }
            }

            yield return TtsClient.ProbeEnabledEnum(ttsHttpBase, v => ttsAvailable = v);
            ttsProbeDone = true;
            if (logHostLinesToConsole)
            {
                Debug.Log("CongoGames TTS: " + (ttsAvailable ? "disponible (HTTP)" : "indisponible (bip de secours)"));
            }
        }

        private IEnumerator ReprobeTts()
        {
            ttsProbeDone = false;
            yield return TtsClient.ProbeEnabledEnum(ttsHttpBase, v => ttsAvailable = v);
            ttsProbeDone = true;
            if (logHostLinesToConsole)
            {
                Debug.Log("CongoGames TTS: base mise à jour → " + ttsHttpBase + " | actif=" + ttsAvailable);
            }
        }

        public void Speak(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            string normalized = NormalizeForSpeech(line);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            if (logHostLinesToConsole)
            {
                Debug.Log("CongoGames host: " + normalized);
            }

            lock (speechQueueLock)
            {
                if (speechQueue.Count >= maxQueuedLines)
                {
                    speechQueue.Dequeue();
                }

                speechQueue.Enqueue(normalized);
            }
            // Évite de lancer plusieurs coroutines ProcessQueue en parallèle.
            if (processCo == null)
            {
                processCo = StartCoroutine(ProcessQueue());
            }
        }

        private static string NormalizeForSpeech(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            string s = input.Trim();

            // Connecteurs fréquents dans les titres/artistes pour une prononciation plus naturelle.
            s = s.Replace(" feat. ", " featuring ");
            s = s.Replace(" feat ", " featuring ");
            s = s.Replace(" ft. ", " featuring ");
            s = s.Replace(" ft ", " featuring ");
            s = s.Replace(" & ", " et ");
            s = s.Replace(" x ", " avec ");
            s = s.Replace(" vs ", " versus ");

            // Remplacement des séparateurs techniques utilisés dans les noms de fichiers.
            s = s.Replace(" - ", ", ");
            s = s.Replace("_", " ");
            s = s.Replace("/", " ");
            s = s.Replace("\\", " ");

            // Nettoyage ponctuation répétée qui dégrade parfois la TTS.
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            s = s.Replace("..", ".");
            s = s.Replace(",,", ",");
            s = s.Replace(" ;", ",");
            s = s.Replace(" :", ",");

            // Mini dictionnaire local: prononciation plus stable des noms propres congolais.
            s = ApplySpeechDictionary(s);

            return s.Trim();
        }

        private static string ApplySpeechDictionary(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";

            string[,] dict =
            {
                { "Roga Roga", "Roga Roga" },
                { "Extra Musica", "Extra Moussica" },
                { "Nzungou", "Ndzoungou" },
                { "Bokoko", "Bokoko" },
                { "Pozo Charabia", "Pozo Charabia" },
                { "MJ Nader", "Em Jay Nader" },
                { "Adou Danga", "Adou Danga" },
                { "Sassou", "Sassou" },
                { "Pointe-Noire", "Pointe Noire" },
                { "Kouilou", "Kouilou" },
                { "Likouala", "Likouala" },
                { "Sangha", "Sangha" },
                { "Odzala", "Odzala" },
                { "Mbochi", "Mbochi" },
                { "Mboshi", "Mbochi" },
                { "Lingala", "LiNgala" },
                { "Kituba", "Kitouba" },
                { "MBOTE", "M'boté" },
                { "Mbote", "M'boté" },
                { "Brazzaville", "Brazzaville" },
                { "Ndombolo", "Ndom bolo" },
                { "Nzoto", "Nzoto" },

                // Passe 2 ultra-ciblée (playlist blind test).
                { "CARINE FLEUR EDOUARE", "Carine Fleur Édouard" },
                { "Casimir Zao", "Casimir Zao" },
                { "Cino black", "Cino Black" },
                { "Zaparo de guerre", "Zaparo de guerre" },
                { "Davy Kassa", "Davy Kassa" },
                { "Elveronne Ndinga", "Elveronne Ndinga" },
                { "Franklin Boukaka", "Franklin Boukaka" },
                { "Groupe AVB", "Groupe A V B" },
                { "Kedjevara", "Kédjévara" },
                { "Nouvel Horizon", "Nouvel Horizon" },
                { "Les Bantous de la Capitale", "Les Bantous de la Capitale" },
                { "Louz Baby", "Louz Baby" },
                { "Paterne Maestro", "Paterne Maestro" },
                { "Michelle Moutouari", "Michelle Moutouari" },
                { "Norbat de Paris", "Norbat de Paris" },
                { "Pamelo Mounka", "Pamelo Mounka" },
                { "Pierre Moutouari", "Pierre Moutouari" },
                { "Pierrette Adams", "Pierrette Adams" },
                { "Teddy Benzo Mixton", "Teddy Benzo Mixton" },
                { "Spinho", "Spin ho" },
                { "Tidiane Mario", "Tidiane Mario" },
                { "Tété Ketch", "Tété Ketch" },
                { "Vinny Baltazard", "Vini Baltazard" },
                { "Wayé", "Wayé" },
                { "YOULOU MABIALA", "Youlou Mabiala" },
                { "Zitany Neil", "Zitany Neil" },
                { "KILOMBO CONGO", "Kilombo Congo" },
                { "LOUFOULAKARI", "Loufoulakari" },
                { "MBONGO", "Mbongo" },
                { "TIA LOKOLO", "Tia Lokolo" },
                { "Moselebende", "Moselebendé" },
                { "Ya Nga Bébé", "Ya Nga Bébé" }
            };

            string result = text;
            int n = dict.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                string from = dict[i, 0];
                string to = dict[i, 1];
                result = ReplaceInsensitive(result, from, to);
            }

            SpeechReplaceRule[] rules = LoadSpeechRulesFromResources();
            for (int i = 0; i < rules.Length; i++)
            {
                SpeechReplaceRule r = rules[i];
                if (r == null || string.IsNullOrWhiteSpace(r.find) || string.IsNullOrWhiteSpace(r.replace))
                {
                    continue;
                }

                result = ReplaceInsensitive(result, r.find.Trim(), r.replace.Trim());
            }

            return result;
        }

        private static SpeechReplaceRule[] LoadSpeechRulesFromResources()
        {
            if (speechRulesLoaded)
            {
                return externalSpeechRules ?? Array.Empty<SpeechReplaceRule>();
            }

            speechRulesLoaded = true;
            try
            {
                TextAsset ta = Resources.Load<TextAsset>("Datasets/tts_pronunciation_overrides");
                if (ta == null || string.IsNullOrWhiteSpace(ta.text))
                {
                    externalSpeechRules = Array.Empty<SpeechReplaceRule>();
                    return externalSpeechRules;
                }

                SpeechReplaceFile parsed = JsonUtility.FromJson<SpeechReplaceFile>(ta.text);
                externalSpeechRules = parsed?.items ?? Array.Empty<SpeechReplaceRule>();
            }
            catch (Exception e)
            {
                externalSpeechRules = Array.Empty<SpeechReplaceRule>();
                Debug.LogWarning("tts_pronunciation_overrides : " + e.Message);
            }

            return externalSpeechRules;
        }

        private static string ReplaceInsensitive(string input, string find, string replace)
        {
            if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(find)) return input ?? "";
            int idx = input.IndexOf(find, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return input;

            var sb = new System.Text.StringBuilder(input.Length + 24);
            int start = 0;
            while (idx >= 0)
            {
                sb.Append(input, start, idx - start);
                sb.Append(replace);
                start = idx + find.Length;
                idx = input.IndexOf(find, start, StringComparison.OrdinalIgnoreCase);
            }

            sb.Append(input, start, input.Length - start);
            return sb.ToString();
        }

        /// <summary>Stop la voix en cours, vide la file — pour éviter chevauchement avec SFX d’action (quiz, chrono, etc.).</summary>
        public void InterruptSpeech()
        {
            lock (speechQueueLock)
            {
                speechQueue.Clear();
            }
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            if (processCo != null)
            {
                StopCoroutine(processCo);
                processCo = null;
            }

            speaking = false;
            SetSpeakingVisual(false);
        }

        private IEnumerator ProcessQueue()
        {
            yield return WebAudioGestureGate.CoWaitForUnlock();

            // « speaking » = file active (IsSpeakingNow). Le duck broadcast ne doit pas couper la musique
            // pendant l’attente HTTP/TTS — seulement pendant la lecture audio réelle (sinon la BGM reste à 0 trop longtemps).
            speaking = true;
            if (logHostLinesToConsole)
            {
                Debug.Log("CongoGames TTS queue: démarrage");
            }

            float wait = 0f;
            while (!ttsProbeDone && wait < 5f)
            {
                wait += Time.deltaTime;
                yield return null;
            }

            while (true)
            {
                if (!TryDequeueSpeech(out string line)) break;

                if (!ttsAvailable && Time.unscaledTime >= ttsSuspendedUntil)
                {
                    yield return ReprobeTts();
                }
                bool playedAudio = false;

                bool ttsMayCall = preferTtsOverLogs && ttsProbeDone && ttsAvailable && Time.unscaledTime >= ttsSuspendedUntil;
                if (ttsMayCall)
                {
                    string key = "tts:" + line.GetHashCode();
                    if (cache != null && cache.TryGetClip(key, out AudioClip cached))
                    {
                        SetSpeakingVisual(true);
                        audioSource.clip = cached;
                        audioSource.Play();
                        playedAudio = true;
                        yield return new WaitWhile(() => audioSource.isPlaying);
                        SetSpeakingVisual(false);
                    }
                    else
                    {
                        AudioClip loaded = null;
                        string err = null;
                        yield return TtsClient.FetchClip(ttsHttpBase, line, c => loaded = c, e => err = e);

                        if (loaded != null)
                        {
                            if (cache != null)
                            {
                                cache.SetClip(key, loaded);
                            }

                            SetSpeakingVisual(true);
                            audioSource.clip = loaded;
                            audioSource.Play();
                            playedAudio = true;
                            yield return new WaitWhile(() => audioSource.isPlaying);
                            SetSpeakingVisual(false);
                        }
                        else if (!string.IsNullOrEmpty(err))
                        {
                            LogTtsFailureThrottled(err);
                        }
                    }
                }

                if (!playedAudio)
                {
                    SetSpeakingVisual(true);
                    yield return PlayFallbackBeep(line.Length);
                    SetSpeakingVisual(false);
                }
            }

            speaking = false;
            SetSpeakingVisual(false);
            processCo = null;
            if (logHostLinesToConsole)
            {
                Debug.Log("CongoGames TTS queue: arrêt (file vide)");
            }
        }

        private bool TryDequeueSpeech(out string line)
        {
            lock (speechQueueLock)
            {
                if (speechQueue.Count > 0)
                {
                    line = speechQueue.Dequeue();
                    return true;
                }
            }

            line = null;
            return false;
        }

        private sealed class PhrasePlayState
        {
            public bool Played;
        }

        /// <summary>
        /// Repliques connues : Resources/Audio/host_oza_zoba ou URL (champ ozaZobaAudioUrl), avant tout appel TTS.
        /// </summary>
        private IEnumerator TryPhraseAudioCoroutine(string line, PhrasePlayState state)
        {
            state.Played = false;
            string t = (line ?? "").Trim();
            if (t.Length == 0)
            {
                yield break;
            }

            if (t.IndexOf("zoba", StringComparison.OrdinalIgnoreCase) < 0)
            {
                yield break;
            }

            const string cacheKey = "phrase:url:oza_zoba";
            if (cache != null && cache.TryGetClip(cacheKey, out AudioClip cached))
            {
                audioSource.clip = cached;
                audioSource.Play();
                yield return new WaitWhile(() => audioSource.isPlaying);
                state.Played = true;
                yield break;
            }

            AudioClip local = Resources.Load<AudioClip>("Audio/host_oza_zoba");
            if (local != null)
            {
                audioSource.PlayOneShot(local);
                yield return new WaitForSeconds(local.length);
                state.Played = true;
                yield break;
            }

            string url = (ozaZobaAudioUrl ?? "").Trim();
            if (url.Length > 0)
            {
                AudioClip remote = null;
                string err = null;
                yield return TtsClient.FetchAudioClipFromUrl(url, c => remote = c, e => err = e);
                if (remote != null)
                {
                    cache?.SetClip(cacheKey, remote);
                    audioSource.clip = remote;
                    audioSource.Play();
                    yield return new WaitWhile(() => audioSource.isPlaying);
                    state.Played = true;
                }
                else if (logHostLinesToConsole && !string.IsNullOrEmpty(err))
                {
                    Debug.Log("CongoGames phrase URL: " + err);
                }
            }
        }

        private IEnumerator PlayFallbackBeep(int lengthHint)
        {
            float dur = Mathf.Clamp(0.08f + lengthHint * 0.012f, 0.12f, 0.45f);
            int rate = 22050;
            int n = Mathf.Max(256, (int)(rate * dur));
            float[] samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                samples[i] = Mathf.Sin(2f * Mathf.PI * 520f * i / rate) * 0.22f;
            }

            AudioClip beep = AudioClip.Create("host_beep", n, 1, rate, false);
            beep.SetData(samples, 0);
            audioSource.clip = beep;
            audioSource.Play();
            yield return new WaitForSeconds(dur);
        }

        private void SetSpeakingVisual(bool value)
        {
            OnSpeakingChanged?.Invoke(value);
        }

        private void LogTtsFailureThrottled(string err)
        {
            bool quota = err.IndexOf("429", StringComparison.Ordinal) >= 0
                || err.IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("crédits", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("credits", StringComparison.OrdinalIgnoreCase) >= 0;
            bool networkReset = err.IndexOf("Connection was reset", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("Recv failure", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("Failed to receive data", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("Curl error 56", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("Unable to read data", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("Décodage MP3 impossible", StringComparison.OrdinalIgnoreCase) >= 0
                || err.IndexOf("Lecture MP3:", StringComparison.OrdinalIgnoreCase) >= 0;

            if (quota)
            {
                ttsSuspendedUntil = Time.unscaledTime + Mathf.Max(30f, ttsQuotaCooldownSec);
            }
            else if (networkReset)
            {
                // Backend local instable : on évite les retries agressifs pendant un court laps de temps.
                ttsSuspendedUntil = Time.unscaledTime + 300f;
                ttsAvailable = false;
            }

            string kind = quota ? "quota" : (networkReset ? "net" : "err");
            if (kind == lastTtsThrottleKey && Time.unscaledTime - lastTtsThrottleTime < 45f)
            {
                return;
            }

            lastTtsThrottleKey = kind;
            lastTtsThrottleTime = Time.unscaledTime;

            if (quota)
            {
                Debug.LogWarning(
                    "CongoGames — quota TTS API dépassé (ex. OpenAI) : la voix est suspendue quelques minutes. " +
                    "En local, le backend peut utiliser le TTS Edge gratuit (voir docs/TTS_LOCAL.md) — le jeu continue avec des bips.");
            }
            else if (networkReset)
            {
                if (logHostLinesToConsole)
                {
                    Debug.Log(
                        "CongoGames — connexion TTS instable (backend local interrompu/réinitialisé). " +
                        "Passage temporaire en bip de secours pendant ~5 min, puis reprise auto.");
                }
            }
            else
            {
                Debug.LogWarning("TTS: " + err);
            }
        }
    }
}
