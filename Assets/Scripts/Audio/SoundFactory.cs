using UnityEngine;

namespace IdleMiningEmpire
{
    public enum Sfx { Hit, Crit, Break, Pickup, Sell, Buy, Error, LevelUp, Hatch, Quest, Achievement, Jump, Dash, Click, Prestige, Unlock, Boss, Full }

    /// <summary>Synthesises every sound effect and a unique chiptune loop per zone at runtime.</summary>
    public static class SoundFactory
    {
        public const int Rate = 22050;

        static readonly int[][] Scales =
        {
            new[] { 0, 2, 4, 7, 9 },          // major pentatonic (surface)
            new[] { 0, 3, 5, 7, 10 },         // minor pentatonic (cave)
            new[] { 0, 2, 3, 5, 7, 9, 10 },   // dorian (deep)
            new[] { 0, 1, 3, 5, 7, 8, 10 },   // phrygian (mantle)
            new[] { 0, 2, 3, 5, 7, 8, 11 },   // harmonic minor (core)
            new[] { 0, 2, 4, 6, 7, 9, 11 },   // lydian (ruins)
            new[] { 0, 2, 4, 6, 8, 10 },      // whole tone (abyss)
        };

        static float Freq(float midi) => 440f * Mathf.Pow(2f, (midi - 69f) / 12f);

        static float Wave(int type, float phase)
        {
            phase -= Mathf.Floor(phase);
            switch (type)
            {
                case 0: return Mathf.Sin(phase * Mathf.PI * 2f);
                case 1: return phase < 0.5f ? 0.6f : -0.6f;                // square
                case 2: return (phase * 2f - 1f) * 0.7f;                   // saw
                case 3: return (phase < 0.5f ? phase * 4f - 1f : 3f - phase * 4f); // triangle
                default: return 0;
            }
        }

        static AudioClip Make(string name, float seconds, System.Func<float, float, float> fn)
        {
            int n = Mathf.Max(1, Mathf.CeilToInt(seconds * Rate));
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                data[i] = Mathf.Clamp(fn(t, t / seconds), -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Noise(int seed) { uint x = (uint)seed * 747796405u + 2891336453u; x = ((x >> (int)((x >> 28) + 4u)) ^ x) * 277803737u; return ((x >> 22) ^ x) / (float)uint.MaxValue * 2f - 1f; }

        public static AudioClip Create(Sfx s)
        {
            switch (s)
            {
                case Sfx.Hit:
                    return Make("hit", 0.09f, (t, p) =>
                        (Noise((int)(t * Rate)) * 0.5f + Mathf.Sin(t * 2 * Mathf.PI * (180 - 900 * t)) * 0.6f) * (1 - p) * (1 - p));
                case Sfx.Crit:
                    return Make("crit", 0.22f, (t, p) =>
                        (Wave(1, t * 1320) * 0.25f + Wave(0, t * 1980) * 0.25f + Noise((int)(t * Rate)) * 0.3f * (1 - p * 4)) * Mathf.Pow(1 - p, 2));
                case Sfx.Break:
                    return Make("break", 0.32f, (t, p) =>
                    {
                        float n = Noise((int)(t * Rate / 2)) * 0.6f;
                        float thump = Mathf.Sin(t * 2 * Mathf.PI * (110 - 60 * p)) * 0.7f * Mathf.Max(0, 1 - p * 3);
                        return (n + thump) * (1 - p);
                    });
                case Sfx.Pickup:
                    return Make("pickup", 0.08f, (t, p) => Wave(1, t * (p < 0.5f ? 1046 : 1568)) * 0.35f * (1 - p));
                case Sfx.Sell:
                    return Make("sell", 0.45f, (t, p) =>
                    {
                        int step = Mathf.Min(3, (int)(p * 4));
                        float f = Freq(72 + new[] { 0, 4, 7, 12 }[step]);
                        return (Wave(1, t * f) * 0.3f + Wave(0, t * f * 2) * 0.15f) * (1 - (p * 4 - step) * 0.5f) * (1 - p * 0.5f);
                    });
                case Sfx.Buy:
                    return Make("buy", 0.2f, (t, p) => Wave(3, t * Freq(p < 0.5f ? 76 : 83)) * 0.45f * (1 - p));
                case Sfx.Error:
                    return Make("error", 0.18f, (t, p) => Wave(1, t * 120) * 0.35f * (1 - p));
                case Sfx.LevelUp:
                case Sfx.Quest:
                    return Make(s.ToString(), 0.6f, (t, p) =>
                    {
                        int[] notes = s == Sfx.LevelUp ? new[] { 0, 4, 7, 12, 16, 19 } : new[] { 0, 7, 12, 7, 12, 19 };
                        int step = Mathf.Min(notes.Length - 1, (int)(p * notes.Length));
                        float f = Freq(67 + notes[step]);
                        return (Wave(1, t * f) * 0.25f + Wave(3, t * f * 0.5f) * 0.25f) * (1 - p * 0.7f);
                    });
                case Sfx.Achievement:
                    return Make("ach", 0.8f, (t, p) =>
                    {
                        float f1 = Freq(79), f2 = Freq(83), f3 = Freq(86);
                        float env = Mathf.Min(1, t * 20) * (1 - p);
                        return (Wave(0, t * f1) + Wave(0, t * f2) * (p > 0.15f ? 1 : 0) + Wave(0, t * f3) * (p > 0.3f ? 1 : 0)) * 0.22f * env;
                    });
                case Sfx.Hatch:
                    return Make("hatch", 0.5f, (t, p) =>
                        (Wave(3, t * Freq(72 + (int)(p * 16) % 12 * 2)) * 0.3f + Noise((int)(t * Rate)) * 0.15f * (1 - p)) * (1 - p));
                case Sfx.Jump:
                    return Make("jump", 0.15f, (t, p) => Wave(1, t * (300 + 500 * p)) * 0.22f * (1 - p));
                case Sfx.Dash:
                    return Make("dash", 0.2f, (t, p) => Noise((int)(t * Rate / (1 + p * 4))) * 0.35f * Mathf.Sin(p * Mathf.PI));
                case Sfx.Click:
                    return Make("click", 0.04f, (t, p) => Wave(1, t * 1800) * 0.2f * (1 - p));
                case Sfx.Prestige:
                    return Make("prestige", 1.6f, (t, p) =>
                    {
                        float sweep = Freq(48 + p * 36);
                        return (Wave(2, t * sweep) * 0.15f + Wave(0, t * sweep * 1.5f) * 0.2f + Noise((int)(t * Rate)) * 0.05f) * Mathf.Sin(p * Mathf.PI);
                    });
                case Sfx.Unlock:
                    return Make("unlock", 0.7f, (t, p) =>
                    {
                        int step = Mathf.Min(4, (int)(p * 5));
                        float f = Freq(60 + new[] { 0, 5, 7, 12, 24 }[step]);
                        return (Wave(3, t * f) * 0.35f + Wave(1, t * f * 0.5f) * 0.1f) * (1 - p * 0.6f);
                    });
                case Sfx.Boss:
                    return Make("boss", 1.0f, (t, p) =>
                        (Wave(2, t * Freq(36 + Mathf.Sin(t * 12) * 0.6f)) * 0.4f + Noise((int)(t * Rate / 3)) * 0.15f) * Mathf.Sin(p * Mathf.PI));
                case Sfx.Full:
                    return Make("full", 0.25f, (t, p) => Wave(1, t * (p < 0.5f ? 300 : 220)) * 0.3f * (1 - p));
                default:
                    return Make("none", 0.01f, (t, p) => 0);
            }
        }

        /// <summary>Generates a looping 8-bar chiptune for a zone (bass + arpeggio + lead + drums).</summary>
        public static AudioClip Music(ZoneDefinition z)
        {
            var scale = Scales[Mathf.Clamp(z.scale, 0, Scales.Length - 1)];
            var rnd = new System.Random(z.musicSeed);
            float beat = 60f / Mathf.Max(60f, z.bpm);
            int bars = 8, steps = bars * 8; // eighth notes
            float step = beat / 2f;
            float len = steps * step;
            int n = Mathf.CeilToInt(len * Rate);
            var data = new float[n];

            int[] chordRoots = { 0, 3, 4, 2, 0, 5, 3, 4 };
            int[] lead = new int[steps];
            for (int i = 0; i < steps; i++) lead[i] = rnd.NextDouble() < 0.55 ? rnd.Next(scale.Length + 3) : -1;
            int leadWave = z.index % 2 == 0 ? 1 : 3;

            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                int s = Mathf.Min(steps - 1, (int)(t / step));
                float st = t - s * step;
                int bar = s / 8;
                int deg = chordRoots[bar % chordRoots.Length] % scale.Length;
                float v = 0;

                // bass
                int bassNote = z.rootMidi - 24 + scale[deg];
                float bassEnv = Mathf.Exp(-st * 6f);
                v += Wave(3, t * Freq(bassNote + (s % 2 == 1 ? 12 : 0))) * 0.22f * bassEnv;

                // arpeggio
                int arpIdx = (deg + (s % 3) * 2) % scale.Length;
                int arpOct = (deg + (s % 3) * 2) / scale.Length;
                float arpF = Freq(z.rootMidi + scale[arpIdx] + arpOct * 12);
                v += Wave(1, t * arpF) * 0.06f * Mathf.Exp(-st * 10f);

                // lead
                if (lead[s] >= 0 && (bar % 4) >= 1)
                {
                    int li = lead[s];
                    float lf = Freq(z.rootMidi + 12 + scale[li % scale.Length] + (li / scale.Length) * 12);
                    float vib = 1 + Mathf.Sin(t * 30f) * 0.004f;
                    v += Wave(leadWave, t * lf * vib) * 0.07f * Mathf.Exp(-st * 4f);
                }

                // drums
                if (s % 4 == 0) v += Mathf.Sin(st * 2 * Mathf.PI * (120 - st * 600)) * 0.35f * Mathf.Exp(-st * 25f);
                if (s % 8 == 4) v += Noise(i) * 0.18f * Mathf.Exp(-st * 18f);
                v += Noise(i * 3) * 0.04f * Mathf.Exp(-st * 60f);

                // short fade at loop edges to avoid clicks
                float edge = Mathf.Min(1f, Mathf.Min(t, len - t) * 40f);
                data[i] = Mathf.Clamp(v * edge, -1f, 1f);
            }
            var clip = AudioClip.Create("music_" + z.id, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
