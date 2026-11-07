using UnityEngine;

// Stage 4: a small glowing bottle. Walking over it makes the wizard see-through for a few seconds,
// and the flying books ignore her (see PlayerInvisibility). It is a bonus, not an ingredient:
// it does not count for the recipe or the score.
public class InvisibilityPickup : MonoBehaviour
{
    public float duration = 5f;
    public float pickupRadius = 0.9f;
    [Tooltip("The see-through material put on the wizard (transparent).")]
    public Material ghostMaterial;

    [SerializeField] float spinSpeed = 70f;
    [SerializeField] float bobHeight = 0.15f;

    Transform player;
    Vector3 startPosition;
    bool taken;

    static AudioClip shimmer;

    void Start()
    {
        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null) player = pc.transform;
        startPosition = transform.position;
    }

    void Update()
    {
        if (taken) return;

        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * 2.4f) * bobHeight);

        StageManager stage = StageManager.Instance;
        if (player == null || (stage != null && stage.State != StageState.Playing)) return;

        Vector3 d = player.position - transform.position;
        d.y = 0f;
        if (d.magnitude <= pickupRadius) Take();
    }

    void Take()
    {
        taken = true;
        PlayerInvisibility.Begin(player.gameObject, ghostMaterial, duration);

        LibraryMessage.Show("Invisible pendant " + Mathf.RoundToInt(duration) + " s !", 2f, new Color(0.7f, 0.85f, 1f));
        Haptics.Tap(60);
        PlaySound();
        SpawnSparks();
        gameObject.SetActive(false);
    }

    void SpawnSparks()
    {
        Renderer source = GetComponentInChildren<Renderer>();
        if (source == null) return;

        Vector3 origin = transform.position + Vector3.up * 0.6f;
        for (int i = 0; i < 24; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spark.GetComponent<Collider>());
            spark.GetComponent<MeshRenderer>().sharedMaterial = source.sharedMaterial;
            spark.transform.position = origin;
            spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.13f);

            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(1.5f, 3.5f), Random.Range(-1f, 1f)) * 1.4f;
            spark.AddComponent<Spark>().Init(velocity, Random.Range(0.5f, 1f));
        }
    }

    // A rising magic shimmer, made from maths like the rest of the game's sounds.
    void PlaySound()
    {
        if (!GameSettings.SoundOn) return;

        if (shimmer == null)
        {
            const int rate = 22050;
            float[] notes = { 784f, 988f, 1175f, 1568f };
            float[] data = new float[Mathf.CeilToInt(0.8f * rate)];
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float s = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float tn = t - n * 0.07f;
                    if (tn < 0f) continue;
                    s += Mathf.Sin(Mathf.PI * 2f * notes[n] * tn) * Mathf.Exp(-tn * 6f) * (1f + 0.3f * Mathf.Sin(Mathf.PI * 2f * 7f * tn));
                }
                data[i] = Mathf.Clamp(s * 0.25f, -1f, 1f);
            }
            shimmer = AudioClip.Create("Invisibility", data.Length, 1, rate, false);
            shimmer.SetData(data, 0);
        }

        // a 2D sound (same volume wherever the camera is), removed once it has played
        GameObject go = new GameObject("InvisibilitySound");
        AudioSource source = go.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.PlayOneShot(shimmer);
        Destroy(go, shimmer.length + 0.1f);
    }
}
