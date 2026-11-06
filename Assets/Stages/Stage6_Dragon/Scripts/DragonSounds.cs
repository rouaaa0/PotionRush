using System.Collections.Generic;
using UnityEngine;

// The dragon's sounds, made from maths like the shared SoundBank, so there are no audio files.
public static class DragonSounds
{
    const int Rate = 22050;
    const float Tau = Mathf.PI * 2f;

    // In the editor this list can outlive its sounds between two Play sessions, so a sound that is gone gets remade.
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    // A low growl that rises and falls.
    public static AudioClip Roar()
    {
        AudioClip clip;
        if (cache.TryGetValue("roar", out clip) && clip != null) return clip;

        const float length = 1.3f;
        System.Random random = new System.Random(6);
        float[] data = new float[Mathf.CeilToInt(length * Rate)];
        float phase = 0f;
        float rough = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            float k = t / length;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            rough += (noise - rough) * 0.12f;

            phase += Tau * (70f + Mathf.Sin(k * Mathf.PI) * 75f + rough * 25f) / Rate;
            float saw = (phase / Tau) % 1f * 2f - 1f;
            float envelope = Mathf.Min(1f, t * 8f) * Mathf.Min(1f, (length - t) * 3f);
            data[i] = Mathf.Clamp((saw * 0.55f + rough * 1.4f) * envelope, -1f, 1f) * 0.8f;
        }
        return Store("roar", data);
    }

    // A fireball leaving the mouth.
    public static AudioClip Whoosh()
    {
        AudioClip clip;
        if (cache.TryGetValue("whoosh", out clip) && clip != null) return clip;

        const float length = 0.5f;
        System.Random random = new System.Random(3);
        float[] data = new float[Mathf.CeilToInt(length * Rate)];
        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            low += (noise - low) * Mathf.Lerp(0.05f, 0.3f, Mathf.Sin(t / length * Mathf.PI));
            data[i] = low * Mathf.Sin(t / length * Mathf.PI) * 1.2f;
        }
        return Store("whoosh", data);
    }

    // The steady roar of the fire breath.
    public static AudioClip Breath()
    {
        AudioClip clip;
        if (cache.TryGetValue("breath", out clip) && clip != null) return clip;

        const float length = 2.2f;
        System.Random random = new System.Random(9);
        float[] data = new float[Mathf.CeilToInt(length * Rate)];
        float low = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            low += (noise - low) * 0.1f;
            float envelope = Mathf.Min(1f, t * 6f) * Mathf.Min(1f, (length - t) * 2f);
            data[i] = Mathf.Clamp(low * 1.8f * envelope, -1f, 1f);
        }
        return Store("breath", data);
    }

    // A fireball hitting the floor.
    public static AudioClip Boom()
    {
        AudioClip clip;
        if (cache.TryGetValue("boom", out clip) && clip != null) return clip;

        const float length = 0.6f;
        System.Random random = new System.Random(5);
        float[] data = new float[Mathf.CeilToInt(length * Rate)];
        float low = 0f;
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)Rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            low += (noise - low) * 0.15f;
            phase += Tau * Mathf.Max(35f, 120f - 170f * t) / Rate;
            data[i] = Mathf.Clamp(low * Mathf.Exp(-t * 9f) * 1.6f + Mathf.Sin(phase) * Mathf.Exp(-t * 6f) * 0.8f, -1f, 1f);
        }
        return Store("boom", data);
    }

    static AudioClip Store(string key, float[] data)
    {
        AudioClip clip = AudioClip.Create("dragon_" + key, data.Length, 1, Rate, false);
        clip.SetData(data, 0);
        cache[key] = clip;
        return clip;
    }
}
