using System;
using UnityEngine;

namespace CongoGames.Audio
{
    public static class PcmToAudioClip
    {
        public static AudioClip From16BitMono(byte[] pcmBytes, int sampleRate)
        {
            if (pcmBytes == null || pcmBytes.Length < 2)
            {
                return null;
            }

            int sampleCount = pcmBytes.Length / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short s = (short)(pcmBytes[i * 2] | (pcmBytes[i * 2 + 1] << 8));
                samples[i] = s / 32768f;
            }

            AudioClip clip = AudioClip.Create("tts_pcm", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static byte[] FromBase64(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return Array.Empty<byte>();
            try
            {
                string s = b64.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");
                byte[] raw = Convert.FromBase64String(s);
                if ((raw.Length & 1) == 1)
                {
                    byte[] even = new byte[raw.Length - 1];
                    Buffer.BlockCopy(raw, 0, even, 0, even.Length);
                    return even;
                }

                return raw;
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }
    }
}
