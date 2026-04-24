using UnityEngine;

namespace CongoGames.Audio
{
    /// <summary>
    /// Sons générés sans fichier (secours si pas d’assets dans StreamingAssets / Resources).
    /// </summary>
    public static class ProceduralClips
    {
        public static AudioClip BuildAmbientPadLoop()
        {
            int rate = 44100;
            const float dur = 2.8f;
            int n = (int)(rate * dur);
            float[] samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float ph = 2f * Mathf.PI * i / rate;
                float layer = Mathf.Sin(ph * 97f) * 0.045f
                    + Mathf.Sin(ph * 146f) * 0.032f
                    + Mathf.Sin(ph * 218f) * 0.022f;
                samples[i] = Mathf.Clamp(layer, -0.18f, 0.18f);
            }

            AudioClip clip = AudioClip.Create("cg_ambient_pad", n, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>Variation légère du pad selon le mode (quiz, semantic, etc.).</summary>
        public static AudioClip BuildAmbientPadForMode(string modeId)
        {
            int h = string.IsNullOrEmpty(modeId) ? 0 : Mathf.Abs(modeId.GetHashCode());
            float m1 = 0.85f + (h % 7) * 0.04f;
            float m2 = 1.1f + ((h >> 3) % 5) * 0.05f;
            float m3 = 0.95f + ((h >> 6) % 6) * 0.03f;

            int rate = 44100;
            const float dur = 2.8f;
            int n = (int)(rate * dur);
            float[] samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float ph = 2f * Mathf.PI * i / rate;
                float layer = Mathf.Sin(ph * 97f * m1) * 0.045f
                    + Mathf.Sin(ph * 146f * m2) * 0.032f
                    + Mathf.Sin(ph * 218f * m3) * 0.022f;
                float pulse = Mathf.Repeat(t * 1.85f + (h % 7) * 0.03f, 1f);
                float ndom = pulse < 0.12f ? Mathf.Sin(ph * 140f) * 0.09f : 0f;
                samples[i] = Mathf.Clamp(layer + ndom, -0.22f, 0.22f);
            }

            AudioClip clip = AudioClip.Create("cg_pad_" + (modeId ?? "x"), n, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static AudioClip BuildUiTap()
        {
            return BuildToneBurst(880f, 0.06f, 0.12f);
        }

        public static AudioClip BuildCorrectChime()
        {
            return BuildTwoTone(523f, 784f, 0.18f, 0.2f);
        }

        public static AudioClip BuildWrongBuzz()
        {
            return BuildToneBurst(140f, 0.22f, 0.18f);
        }

        /// <summary>Acclamation courte type « foule » (secours sans fichier).</summary>
        public static AudioClip BuildCrowdCheer()
        {
            int rate = 22050;
            float dur = 0.72f;
            int n = (int)(rate * dur);
            float[] samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.Sin(t * Mathf.PI);
                float ph = 2f * Mathf.PI * i / rate;
                float burst = Mathf.Sin(ph * 180f) * 0.14f
                    + Mathf.Sin(ph * 260f) * 0.11f
                    + Mathf.Sin(ph * 420f) * 0.08f;
                float noise = (Mathf.PerlinNoise(i * 0.08f, 1.7f) - 0.5f) * 0.18f * env;
                samples[i] = Mathf.Clamp((burst + noise) * env, -0.62f, 0.62f);
            }

            AudioClip c = AudioClip.Create("cg_cheer", n, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        /// <summary>Rire / moquerie procédural (secours sans fichier voix).</summary>
        public static AudioClip BuildMockingLaugh()
        {
            int rate = 22050;
            float dur = 0.78f;
            int n = (int)(rate * dur);
            float[] samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = Mathf.SmoothStep(0f, 1f, t * 3.2f) * Mathf.SmoothStep(1f, 0f, (t - 0.72f) * 6f);
                float ph = 2f * Mathf.PI * i / rate;
                float f0 = Mathf.Lerp(620f, 180f, t);
                float wobble = Mathf.Sin(ph * 12f) * 42f;
                float body = Mathf.Sin(ph * (f0 + wobble)) * 0.3f;
                float grunt = Mathf.Sin(ph * (f0 * 0.5f + wobble * 0.5f)) * 0.12f;
                float crowd = (Mathf.PerlinNoise(i * 0.04f, 2.3f) - 0.5f) * 0.14f * env;
                samples[i] = Mathf.Clamp((body + grunt + crowd) * env, -0.65f, 0.65f);
            }

            AudioClip c = AudioClip.Create("cg_laugh", n, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        /// <summary>Court motif type tam-tam (intro avant la musique de fond).</summary>
        public static AudioClip BuildTamTamIntro()
        {
            int rate = 44100;
            float dur = 1.35f;
            int n = (int)(rate * dur);
            float[] samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float ph = 2f * Mathf.PI * i / rate;
                float hit = 0f;
                foreach (float hitT in new[] { 0.08f, 0.38f, 0.68f })
                {
                    float dt = t - hitT;
                    if (dt >= 0f && dt < 0.18f)
                    {
                        float env = Mathf.Exp(-dt * 14f);
                        float boom = Mathf.Sin(ph * 62f) * 0.35f * env;
                        float slap = Mathf.Sin(ph * 180f) * 0.12f * env;
                        hit += boom + slap;
                    }
                }

                float rattle = Mathf.Sin(ph * 420f) * 0.04f * Mathf.Sin(t * 28f);
                samples[i] = Mathf.Clamp(hit + rattle, -0.55f, 0.55f);
            }

            AudioClip c = AudioClip.Create("cg_tamtam_intro", n, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        /// <summary>
        /// Boucle courte « extrait » pour blind test (pas de fichier musical) — rythme + mélodie procéduraux.
        /// </summary>
        public static AudioClip BuildBlindMusicStub(int seed)
        {
            int rate = 44100;
            // Boucle assez longue pour un extrait 30–60 s (fichier absent : la source boucle côté AudioSource).
            float dur = 96f;
            int n = (int)(rate * dur);
            float[] samples = new float[n];
            int h = Mathf.Abs(seed);
            float bpm = 96f + (h % 17);
            float beat = 60f / bpm;
            float f1 = 196f + (h % 9) * 4f;
            float f2 = 247f + (h % 11) * 3f;
            float f3 = 311f + (h % 7) * 2f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float step = Mathf.FloorToInt(t / beat);
                float ph = 2f * Mathf.PI * i / rate;
                float env = 0.55f + 0.45f * Mathf.Sin((t * bpm / 60f) * Mathf.PI * 2f * 0.25f);
                float mel = Mathf.Sin(ph * f1) * 0.11f;
                if ((step % 4) == 1)
                {
                    mel += Mathf.Sin(ph * f2) * 0.09f;
                }

                if ((step % 8) == 5)
                {
                    mel += Mathf.Sin(ph * f3) * 0.07f;
                }

                float kick = 0f;
                float bt = t % beat;
                if (bt < 0.06f)
                {
                    float ke = Mathf.Exp(-bt * 38f);
                    kick = Mathf.Sin(ph * 55f) * 0.22f * ke;
                }

                float hat = (bt > 0.12f && bt < 0.16f) ? Mathf.Sin(ph * 8000f) * 0.04f : 0f;
                float sway = Mathf.Sin(ph * 2.2f + h * 0.01f) * 0.03f;
                samples[i] = Mathf.Clamp((mel + kick + hat + sway) * env, -0.55f, 0.55f);
            }

            AudioClip c = AudioClip.Create("cg_blind_stub_" + seed, n, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        private static AudioClip BuildToneBurst(float freq, float dur, float volume)
        {
            int rate = 22050;
            int n = Mathf.Max(64, (int)(rate * dur));
            float[] samples = new float[n];
            for (int i = 0; i < n; i++)
            {
                float env = 1f - i / (float)n;
                samples[i] = Mathf.Sin(2f * Mathf.PI * freq * i / rate) * volume * env;
            }

            AudioClip c = AudioClip.Create("cg_sfx", n, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }

        private static AudioClip BuildTwoTone(float f1, float f2, float durEach, float volume)
        {
            int rate = 22050;
            int n1 = (int)(rate * durEach);
            int n2 = (int)(rate * durEach);
            float[] samples = new float[n1 + n2];
            for (int i = 0; i < n1; i++)
            {
                float env = Mathf.SmoothStep(1f, 0f, i / (float)n1);
                samples[i] = Mathf.Sin(2f * Mathf.PI * f1 * i / rate) * volume * env;
            }

            for (int i = 0; i < n2; i++)
            {
                float env = Mathf.SmoothStep(0f, 1f, i / (float)n2) * Mathf.SmoothStep(1f, 0f, i / (float)n2);
                samples[n1 + i] = Mathf.Sin(2f * Mathf.PI * f2 * i / rate) * volume * env;
            }

            AudioClip c = AudioClip.Create("cg_chime", samples.Length, 1, rate, false);
            c.SetData(samples, 0);
            return c;
        }
    }
}
