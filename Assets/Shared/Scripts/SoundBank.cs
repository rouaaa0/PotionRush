using System.Collections.Generic;
using UnityEngine;

// All the game's sounds, made from maths when the game starts. There are no audio files to import,
// so every stage and every teammate gets the same sounds for free.
public static class SoundBank
{
    const int Rate = 22050;
    const float Tau = Mathf.PI * 2f;

    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    // A bright two-note "ping". Each ingredient has its own pitch.
    public static AudioClip Collect(IngredientType type)
    {
        string key = "collect" + (int)type;
        AudioClip clip;
        if (cache.TryGetValue(key, out clip)) return clip;

        float f = 660f;
        switch (type)
        {
            case IngredientType.Mushroom: f = 587f; break;
            case IngredientType.Crystal: f = 880f; break;
            case IngredientType.Herb: f = 659f; break;
            case IngredientType.IceFlower: f = 988f; break;
            case IngredientType.Scroll: f = 523f; break;
            case IngredientType.DragonScale: f = 392f; break;
        }

        float[] data = new float[Mathf.CeilToInt(0.35f * Rate)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            data[i] = Mathf.Clamp((Tone(f, t, 11f) + Tone(f * 1.5f, t - 0.09f, 11f)) * 0.5f, -1f, 1f);
        }
        return Store(key, data);
    }

    // A "bloop" for every shake while brewing. The pitch is raised in the code as the potion fills up.
    public static AudioClip Brew()
    {
        AudioClip clip;
        if (cache.TryGetValue("brew", out clip)) return clip;

        float[] data = new float[Mathf.CeilToInt(0.24f * Rate)];
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            phase += Tau * (250f + 700f * (t / 0.24f)) / Rate;
            data[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 13f) * 0.8f;
        }
        return Store("brew", data);
    }

    // A rising fanfare for the finished potion.
    public static AudioClip Win()
    {
        AudioClip clip;
        if (cache.TryGetValue("win", out clip)) return clip;

        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
        float[] starts = { 0f, 0.12f, 0.24f, 0.36f, 0.54f };

        float[] data = new float[Mathf.CeilToInt(1.6f * Rate)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            float s = 0f;
            for (int n = 0; n < notes.Length; n++) s += Tone(notes[n], t - starts[n], 3.5f);
            data[i] = Mathf.Clamp(s * 0.3f, -1f, 1f);
        }
        return Store("win", data);
    }

    // A low boom with a crackle for the exploding cauldron.
    public static AudioClip Lose()
    {
        AudioClip clip;
        if (cache.TryGetValue("lose", out clip)) return clip;

        System.Random random = new System.Random(7);
        float[] data = new float[Mathf.CeilToInt(1.8f * Rate)];
        float lowpass = 0f;
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            lowpass += (noise - lowpass) * 0.08f;
            phase += Tau * Mathf.Max(25f, 90f - 40f * t) / Rate;

            float s = noise * Mathf.Exp(-t * 28f) * 0.8f;          // the crack
            s += lowpass * 3f * Mathf.Exp(-t * 2.6f);              // the rumble
            s += Mathf.Sin(phase) * Mathf.Exp(-t * 2.2f) * 0.7f;   // the boom
            data[i] = Mathf.Clamp(s, -1f, 1f);
        }
        return Store("lose", data);
    }

    public static AudioClip Click()
    {
        AudioClip clip;
        if (cache.TryGetValue("click", out clip)) return clip;

        float[] data = new float[Mathf.CeilToInt(0.06f * Rate)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            data[i] = Mathf.Sin(Tau * 1200f * t) * Mathf.Exp(-t * 60f) * 0.6f;
        }
        return Store("click", data);
    }

    // 20 seconds of calm, magical music that loops: a pad, a bass note and a plucked melody
    // over four chords (A minor, F, C, G).
    public static AudioClip Music()
    {
        AudioClip clip;
        if (cache.TryGetValue("music", out clip)) return clip;

        float[][] pad =
        {
            new[] { 220f, 261.63f, 329.63f },
            new[] { 174.61f, 220f, 261.63f },
            new[] { 261.63f, 329.63f, 392f },
            new[] { 196f, 246.94f, 293.66f },
        };
        float[][] melody =
        {
            new[] { 440f, 523.25f, 659.25f, 880f },
            new[] { 349.23f, 440f, 523.25f, 698.46f },
            new[] { 523.25f, 659.25f, 784f, 1046.5f },
            new[] { 392f, 493.88f, 587.33f, 783.99f },
        };
        float[] bass = { 110f, 87.31f, 130.81f, 98f };
        int[] pattern = { 0, 2, 1, 3, 2, 1, 3, 2 };

        const float bar = 2.5f;       // 4 beats at 96 bpm
        const float eighth = 0.3125f;
        const float chordLength = bar * 2f;

        float[] data = new float[Mathf.CeilToInt(bar * 8f * Rate)];
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            int chord = Mathf.Min(3, (int)(t / chordLength));
            float inChord = t - chord * chordLength;

            // pad: fades in and out inside each chord so the changes do not click
            float fade = Mathf.Min(1f, Mathf.Min(inChord / 0.3f, (chordLength - inChord) / 0.3f));
            float s = 0f;
            foreach (float f in pad[chord]) s += Mathf.Sin(Tau * f * t) * 0.05f * fade;

            // bass on the first beat of every bar
            float inBar = t - Mathf.Floor(t / bar) * bar;
            s += Mathf.Sin(Tau * bass[chord] * inBar) * Mathf.Exp(-inBar * 2f) * 0.16f;

            // plucked melody, one note every eighth
            int step = (int)(t / eighth);
            float inNote = t - step * eighth;
            float mf = melody[chord][pattern[step % 8]];
            s += (Mathf.Sin(Tau * mf * inNote) + 0.35f * Mathf.Sin(Tau * mf * 2f * inNote)) * Mathf.Exp(-inNote * 7f) * 0.12f * Mathf.Min(1f, inNote * 300f);

            data[i] = Mathf.Clamp(s, -1f, 1f);
        }
        return Store("music", data);
    }

    // A note that starts at time 0 (use a negative time to delay it) and fades away.
    static float Tone(float frequency, float t, float decay)
    {
        if (t < 0f) return 0f;
        float attack = Mathf.Min(1f, t * 250f);
        float wave = Mathf.Sin(Tau * frequency * t) + 0.3f * Mathf.Sin(Tau * frequency * 2f * t);
        return wave * Mathf.Exp(-t * decay) * attack;
    }

    static AudioClip Store(string key, float[] data)
    {
        AudioClip clip = AudioClip.Create(key, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        cache[key] = clip;
        return clip;
    }
}
