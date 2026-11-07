using UnityEngine;

// Stage 4: a floating candle. It bobs gently in the air and its flame and light flicker.
public class CandleFlicker : MonoBehaviour
{
    Light candleLight;
    Transform flame;
    Vector3 flameScale;
    Vector3 startPosition;
    float baseIntensity;
    float seed;

    void Start()
    {
        candleLight = GetComponentInChildren<Light>();
        if (candleLight != null) baseIntensity = candleLight.intensity;
        flame = transform.Find("Flame");
        if (flame != null) flameScale = flame.localScale;
        startPosition = transform.position;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * 6f);

        if (candleLight != null) candleLight.intensity = baseIntensity * (0.75f + n * 0.5f);
        if (flame != null) flame.localScale = new Vector3(flameScale.x, flameScale.y * (0.8f + n * 0.4f), flameScale.z);

        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * 1.3f + seed) * 0.08f);
    }
}
