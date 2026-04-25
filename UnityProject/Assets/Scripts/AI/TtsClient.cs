using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using CongoGames.Audio;

namespace CongoGames.AI
{
    [Serializable]
    public class TtsStatusDto
    {
        public bool ok;
        public bool enabled;
    }

    [Serializable]
    public class HealthDto
    {
        public bool ok;
        public string service;
        public int httpPort;
        public int wsPort;
        public bool ttsEnabled;
    }

    [Serializable]
    public class TtsResponseDto
    {
        public bool ok;
        public string format;
        public int sampleRate;
        public int channels;
        public string pcmBase64;
        public string mp3Base64;
        public string error;
        public string code;
    }

    public static class TtsClient
    {
        public static IEnumerator FetchClip(string httpBase, string text, Action<AudioClip> onSuccess, Action<string> onError)
        {
            string url = httpBase.TrimEnd('/') + "/tts";
            string form = "text=" + UnityWebRequest.EscapeURL(text ?? "") + "&prefer_pcm=1";
            byte[] body = Encoding.UTF8.GetBytes(form);

            using UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            req.timeout = 45;

            yield return req.SendWebRequest();

            string bodyText = req.downloadHandler != null ? req.downloadHandler.text : "";
            long httpCode = req.responseCode;

            if (req.result != UnityWebRequest.Result.Success)
            {
                string detail = ExtractJsonError(bodyText);
                if (string.IsNullOrEmpty(detail) && bodyText.Length > 0)
                {
                    detail = bodyText.Substring(0, Mathf.Min(400, bodyText.Length));
                }

                if (httpCode == 429 || (!string.IsNullOrEmpty(detail) && detail.IndexOf("429", StringComparison.Ordinal) >= 0))
                {
                    detail = "Quota TTS dépassé (OpenAI ou ElevenLabs) — ajoute des crédits, ou configure ELEVENLABS_API_KEY + ELEVENLABS_VOICE_ID dans Backend/.env.";
                }

                string webErr = req.error != null ? req.error.Trim() : "";
                string d = detail != null ? detail.Trim() : "";
                string combined;
                if (string.IsNullOrEmpty(webErr))
                {
                    combined = string.IsNullOrEmpty(d) ? "Erreur réseau TTS" : d;
                }
                else if (string.IsNullOrEmpty(d) || string.Equals(webErr, d, StringComparison.Ordinal))
                {
                    combined = webErr;
                }
                else if (d.IndexOf(webErr, StringComparison.Ordinal) >= 0)
                {
                    combined = d;
                }
                else
                {
                    combined = webErr + " — " + d;
                }

                onError?.Invoke(combined);
                yield break;
            }

            TtsResponseDto dto = JsonUtility.FromJson<TtsResponseDto>(bodyText);
            if (dto == null || !dto.ok)
            {
                onError?.Invoke(dto != null ? dto.error : "Réponse TTS invalide");
                yield break;
            }

            byte[] pcm = PcmToAudioClip.FromBase64(dto.pcmBase64 ?? "");
            if (pcm.Length >= 2)
            {
                int rate = dto.sampleRate > 0 ? dto.sampleRate : 24000;
                AudioClip clip = PcmToAudioClip.From16BitMono(pcm, rate);
                if (clip == null || clip.samples < 1)
                {
                    onError?.Invoke("AudioClip null (PCM) ou trop court");
                    yield break;
                }

                onSuccess?.Invoke(clip);
                yield break;
            }

            onError?.Invoke("PCM vide ou invalide (TTS attendu en PCM uniquement).");
        }

        private static string ExtractJsonError(string json)
        {
            if (string.IsNullOrEmpty(json) || !json.Contains("\"error\""))
            {
                return null;
            }

            try
            {
                TtsResponseDto dto = JsonUtility.FromJson<TtsResponseDto>(json);
                return dto != null ? dto.error : null;
            }
            catch
            {
                return null;
            }
        }

        public static IEnumerator ProbeEnabledEnum(string httpBase, Action<bool> onResult)
        {
            string url = httpBase.TrimEnd('/') + "/tts/status";
            using UnityWebRequest req = UnityWebRequest.Get(url);
            req.timeout = 5;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                onResult?.Invoke(false);
                yield break;
            }

            TtsStatusDto dto = JsonUtility.FromJson<TtsStatusDto>(req.downloadHandler.text);
            onResult?.Invoke(dto != null && dto.enabled);
        }

        public static IEnumerator DiscoverLocalHttpBase(string host, int portMin, int portMax, Action<string> onFound)
        {
            for (int p = portMin; p <= portMax; p++)
            {
                string url = $"http://{host}:{p}/health";
                using UnityWebRequest req = UnityWebRequest.Get(url);
                req.timeout = 2;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    continue;
                }

                HealthDto dto = JsonUtility.FromJson<HealthDto>(req.downloadHandler.text);
                if (dto == null || !dto.ok)
                {
                    continue;
                }

                int port = dto.httpPort > 0 ? dto.httpPort : p;
                onFound?.Invoke($"http://{host}:{port}");
                yield break;
            }

            onFound?.Invoke(null);
        }

        /// <summary>
        /// Charge un clip depuis une URL HTTPS (wav, ogg, mp3). À utiliser pour des fichiers hébergés légalement (CDN perso, Wikimedia, etc.).
        /// </summary>
        public static IEnumerator FetchAudioClipFromUrl(string absoluteUrl, Action<AudioClip> onSuccess, Action<string> onError)
        {
            if (string.IsNullOrWhiteSpace(absoluteUrl))
            {
                onError?.Invoke("URL vide");
                yield break;
            }

            string u = absoluteUrl.Trim();
            AudioType type = AudioType.UNKNOWN;
            string low = u.ToLowerInvariant();
            if (low.Contains(".ogg")) type = AudioType.OGGVORBIS;
            else if (low.Contains(".mp3")) type = AudioType.MPEG;
            else if (low.Contains(".wav")) type = AudioType.WAV;

            using UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(u, type);
            req.timeout = 30;
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(req.error ?? "Téléchargement audio");
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                onError?.Invoke(
                    "Impossible de décoder l’audio depuis l’URL (format non reconnu ou flux illisible). Utilise un lien .wav / .ogg direct.");
                yield break;
            }

            onSuccess?.Invoke(clip);
        }

    }
}
