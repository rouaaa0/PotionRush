using System.Collections;
using UnityEngine;

// The danger of stage 5: lightning aims near the wizard. A ring on the floor warns the player,
// then the bolt falls. Anyone still inside the ring is stunned for a moment, which costs precious time.
// The storm stays calm while the player brews, so the brewing step is never interrupted.
public class TowerStorm : MonoBehaviour
{
    public Light mainLight;
    public Transform clouds;
    public Material boltMaterial;
    public Material warningMaterial;

    [Header("Lightning")]
    [SerializeField] float firstStrikeDelay = 5f;
    [SerializeField] float minDelay = 3.5f;
    [SerializeField] float maxDelay = 6f;
    [SerializeField] float warningTime = 1.2f;  // seconds the player has to leave the ring
    [SerializeField] float strikeRadius = 1.5f;
    [SerializeField] float stunTime = 1f;
    [SerializeField] float aimSpread = 1.5f;    // how far from the wizard a bolt can land
    [SerializeField] float arenaRadius = 7.8f;  // bolts stay inside the tower walls

    PlayerController player;
    Camera mainCamera;
    Color skyColor;
    float baseIntensity;
    float flash;
    AudioSource audioSource;
    AudioClip thunder;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        mainCamera = Camera.main;
        if (mainCamera != null) skyColor = mainCamera.backgroundColor;
        if (mainLight != null) baseIntensity = mainLight.intensity;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        thunder = MakeThunder();

        StartCoroutine(StormLoop());
    }

    void Update()
    {
        if (clouds != null) clouds.Rotate(0f, 1.5f * Time.deltaTime, 0f, Space.World);

        // every strike lights up the tower and the sky for an instant
        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 3.5f);
        if (mainLight != null) mainLight.intensity = baseIntensity + flash * 3f;
        if (mainCamera != null) mainCamera.backgroundColor = Color.Lerp(skyColor, Color.white, flash * 0.5f);
    }

    IEnumerator StormLoop()
    {
        yield return new WaitForSeconds(firstStrikeDelay);

        while (true)
        {
            StageManager stage = StageManager.Instance;
            if (stage == null || player == null || stage.State != StageState.Playing) yield break;

            if (stage.Brewing)
            {
                yield return null;
                continue;
            }

            yield return Strike(stage);
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }

    IEnumerator Strike(StageManager stage)
    {
        Vector3 spot = PickSpot();

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(ring.GetComponent<Collider>());
        ring.name = "LightningWarning";
        ring.GetComponent<MeshRenderer>().sharedMaterial = warningMaterial;
        ring.transform.position = spot + Vector3.up * 0.03f;

        // the ring grows until it reaches the size of the danger zone: full size = the bolt falls
        for (float t = 0f; t < warningTime; t += Time.deltaTime)
        {
            if (stage.Brewing || stage.State != StageState.Playing)
            {
                Destroy(ring);
                yield break;
            }

            float size = strikeRadius * 2f * Mathf.Lerp(0.25f, 1f, t / warningTime);
            ring.transform.localScale = new Vector3(size, 0.01f, size);
            yield return null;
        }
        Destroy(ring);

        flash = 1f;
        SpawnBolt(spot);
        SpawnSparks(spot + Vector3.up * 0.2f);
        if (GameSettings.SoundOn) audioSource.PlayOneShot(thunder);

        Vector3 toPlayer = player.transform.position - spot;
        toPlayer.y = 0f;
        if (toPlayer.magnitude <= strikeRadius)
        {
            Haptics.Tap(200);
            SpawnSparks(player.transform.position + Vector3.up * 1.2f);
            StartCoroutine(Stun(stage));
        }
    }

    Vector3 PickSpot()
    {
        Vector2 circle = Random.insideUnitCircle * aimSpread;
        Vector3 spot = player.transform.position + new Vector3(circle.x, 0f, circle.y);
        spot.y = 0f;
        return Vector3.ClampMagnitude(spot, arenaRadius);
    }

    IEnumerator Stun(StageManager stage)
    {
        player.CanMove = false;
        yield return new WaitForSeconds(stunTime);

        // the cauldron keeps the wizard in place while brewing and after the win, so only give control back during play
        if (stage.State == StageState.Playing && !stage.Brewing) player.CanMove = true;
    }

    // A zigzag of thin glowing boxes from the sky down to the spot. It only lasts a blink.
    void SpawnBolt(Vector3 spot)
    {
        GameObject bolt = new GameObject("LightningBolt");
        const int segments = 7;
        const float height = 14f;

        Vector3 from = spot + Vector3.up * height;
        for (int i = 1; i <= segments; i++)
        {
            Vector3 to = spot + Vector3.up * (height * (1f - i / (float)segments));
            if (i < segments) to += new Vector3(Random.Range(-0.8f, 0.8f), 0f, Random.Range(-0.8f, 0.8f));

            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(part.GetComponent<Collider>());
            part.GetComponent<MeshRenderer>().sharedMaterial = boltMaterial;
            part.transform.SetParent(bolt.transform, false);
            part.transform.position = (from + to) / 2f;
            part.transform.rotation = Quaternion.LookRotation(to - from);
            part.transform.localScale = new Vector3(0.16f, 0.16f, Vector3.Distance(from, to));

            from = to;
        }

        Destroy(bolt, 0.18f);
    }

    void SpawnSparks(Vector3 origin)
    {
        for (int i = 0; i < 14; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spark.GetComponent<Collider>());
            spark.GetComponent<MeshRenderer>().sharedMaterial = boltMaterial;
            spark.transform.position = origin;
            spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);

            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(1f, 2f), Random.Range(-1f, 1f)).normalized * Random.Range(2.5f, 5f);
            spark.AddComponent<Spark>().Init(velocity, Random.Range(0.3f, 0.6f));
        }
    }

    // Like the rest of the game's sounds, the thunder is made from maths: a sharp crack, then a low rumble.
    static AudioClip MakeThunder()
    {
        const int rate = 22050;
        System.Random random = new System.Random(11);
        float[] data = new float[Mathf.CeilToInt(1.4f * rate)];
        float lowpass = 0f;

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            lowpass += (noise - lowpass) * 0.06f;
            float s = noise * Mathf.Exp(-t * 30f) * 0.7f + lowpass * 3f * Mathf.Exp(-t * 3f);
            data[i] = Mathf.Clamp(s, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("thunder", data.Length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
