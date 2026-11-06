using UnityEngine;

// Makes a light near the lava flicker softly, like fire.
[RequireComponent(typeof(Light))]
public class LavaGlow : MonoBehaviour
{
    Light glow;
    float baseIntensity;
    float seed;

    void Start()
    {
        glow = GetComponent<Light>();
        baseIntensity = glow.intensity;
        seed = Random.value * 10f;
    }

    void Update()
    {
        glow.intensity = baseIntensity * (0.8f + Mathf.PerlinNoise(Time.time * 1.7f, seed) * 0.4f);
    }
}
