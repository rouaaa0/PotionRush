using System.Collections;
using UnityEngine;

// The danger of stage 5: lightning aims near the wizard. A ring on the floor warns the player,
// then the bolt falls. Anyone still inside the ring is stunned, which costs precious time;
// escaping at the last moment earns bonus points. The storm gets angrier as the potion comes together,
// but stays calm while the player brews, so the brewing step is never interrupted.
// It also owns the storm's look and sound: flashes, camera shake, distant bolts, wind and thunder.
[DefaultExecutionOrder(200)] // runs after the camera follow, so the camera shake is added on top of it
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

    [Header("Near miss")]
    [SerializeField] int dodgeBonus = 5;
    [SerializeField] float dodgeMargin = 1.5f;  // how close to the ring still counts as a near miss

    // The finale changes these to clear the sky.
    public Color SkyColor { get; set; }
    public float LightLevel { get; set; }
    public bool Calm { get; set; }

    PlayerController player;
    Camera mainCamera;
    float flash;
    float shake;
    float stunUntil;
    bool stunned;
    AudioSource audioSource;
    AudioSource windSource;
    AudioClip thunder;
    Material scorchMaterial;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        mainCamera = Camera.main;
        if (mainCamera != null) SkyColor = mainCamera.backgroundColor;
        if (mainLight != null) LightLevel = mainLight.intensity;

        scorchMaterial = new Material(boltMaterial);
        scorchMaterial.SetColor("_BaseColor", new Color(0.05f, 0.05f, 0.06f));
        scorchMaterial.SetColor("_EmissionColor", new Color(0.5f, 0.2f, 0.05f));

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        thunder = MakeThunder();

        windSource = gameObject.AddComponent<AudioSource>();
        windSource.clip = MakeWind();
        windSource.loop = true;
        windSource.volume = 0.35f;
        windSource.mute = !GameSettings.SoundOn;
        windSource.Play();

        gameObject.AddComponent<TowerSpectacle>();

        StartCoroutine(StormLoop());
        StartCoroutine(DistantLightning());
    }

    void Update()
    {
        if (clouds != null) clouds.Rotate(0f, 6f * Time.deltaTime, 0f, Space.World);

        flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * 3.5f);
        shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 1.2f);
        if (Calm) windSource.volume = Mathf.MoveTowards(windSource.volume, 0f, Time.deltaTime * 0.2f);

        if (mainLight != null) mainLight.intensity = LightLevel + flash * 3f;
        if (mainCamera != null) mainCamera.backgroundColor = Color.Lerp(SkyColor, Color.white, flash * 0.5f);
    }

    void LateUpdate()
    {
        if (mainCamera != null && shake > 0f && Time.timeScale > 0f)
            mainCamera.transform.position += Random.insideUnitSphere * shake;
    }

    // Lights up the tower and the sky for an instant and shakes the camera.
    public void Flash(float amount, float shakeAmount)
    {
        flash = Mathf.Max(flash, amount);
        shake = Mathf.Max(shake, shakeAmount);
    }

    public void PlayThunder(float volume, float pitch)
    {
        if (!GameSettings.SoundOn) return;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(thunder, volume);
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

            // the first bolt hunts the wizard, the others land anywhere on the tower
            int bolts = Rage(stage);
            for (int i = 0; i < bolts; i++)
            {
                StartCoroutine(Strike(stage, i == 0));
                yield return new WaitForSeconds(0.3f);
            }

            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(bolts >= 3 ? delay * 0.75f : delay);
        }
    }

    // How many bolts fall at once: 1 at the start, 2 from half the recipe, 3 for the final dash.
    static int Rage(StageManager stage)
    {
        if (stage.RecipeComplete || stage.TimeLeft <= 20f) return 3;

        int have = 0;
        int need = 0;
        foreach (RecipeEntry entry in stage.config.recipe)
        {
            have += stage.Collected(entry.type);
            need += entry.amount;
        }
        return have * 2 >= need ? 2 : 1;
    }

    IEnumerator Strike(StageManager stage, bool aimed)
    {
        Vector3 spot = aimed ? PickSpotNearPlayer() : PickRandomSpot();

        GameObject ring = Visual(PrimitiveType.Cylinder, warningMaterial, null);
        ring.name = "LightningWarning";
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

        SpawnBolt(spot + Vector3.up * 16f, spot, 0.2f, true, true);
        SpawnSparks(spot + Vector3.up * 0.2f, 16);
        Flash(1f, 0.22f);
        PlayThunder(1f, Random.Range(0.9f, 1.15f));
        StartCoroutine(Scorch(spot));

        Vector3 toPlayer = player.transform.position - spot;
        toPlayer.y = 0f;
        float distance = toPlayer.magnitude;

        if (distance <= strikeRadius)
        {
            Haptics.Tap(200);
            Flash(1f, 0.4f);
            SpawnSparks(player.transform.position + Vector3.up * 1.2f, 16);
            stunUntil = Time.time + stunTime;
            if (!stunned) StartCoroutine(Stun(stage));
        }
        else if (aimed && distance <= strikeRadius + dodgeMargin)
        {
            stage.AddBonus(dodgeBonus);
        }
    }

    Vector3 PickSpotNearPlayer()
    {
        Vector2 circle = Random.insideUnitCircle * aimSpread;
        Vector3 spot = player.transform.position + new Vector3(circle.x, 0f, circle.y);
        spot.y = 0f;
        return Vector3.ClampMagnitude(spot, arenaRadius);
    }

    Vector3 PickRandomSpot()
    {
        Vector2 circle = Random.insideUnitCircle * arenaRadius;
        return new Vector3(circle.x, 0f, circle.y);
    }

    // The wizard cannot move and little stars circle above the hat.
    IEnumerator Stun(StageManager stage)
    {
        stunned = true;
        player.CanMove = false;

        GameObject stars = new GameObject("StunStars");
        stars.transform.SetParent(player.transform, false);
        stars.transform.localPosition = Vector3.up * 2.3f;
        for (int i = 0; i < 3; i++)
        {
            float angle = i * Mathf.PI * 2f / 3f;
            GameObject star = Visual(PrimitiveType.Sphere, warningMaterial, stars.transform);
            star.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.45f;
            star.transform.localScale = Vector3.one * 0.16f;
        }

        while (Time.time < stunUntil)
        {
            stars.transform.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }

        Destroy(stars);
        stunned = false;

        // the cauldron keeps the wizard in place while brewing and after the win, so only give control back during play
        if (stage.State == StageState.Playing && !stage.Brewing) player.CanMove = true;
    }

    // A burnt mark stays on the floor for a few seconds, then shrinks away.
    IEnumerator Scorch(Vector3 spot)
    {
        GameObject mark = Visual(PrimitiveType.Cylinder, scorchMaterial, null);
        mark.name = "Scorch";
        mark.transform.position = spot + Vector3.up * Random.Range(0.024f, 0.028f);
        Vector3 size = new Vector3(1.7f, 0.002f, 1.7f);
        mark.transform.localScale = size;

        yield return new WaitForSeconds(5f);

        for (float t = 0f; t < 1f; t += Time.deltaTime)
        {
            mark.transform.localScale = new Vector3(size.x * (1f - t), size.y, size.z * (1f - t));
            yield return null;
        }
        Destroy(mark);
    }

    // Far-away bolts around the tower, so the storm feels huge even when nothing is aimed at the player.
    IEnumerator DistantLightning()
    {
        while (!Calm)
        {
            yield return new WaitForSeconds(Random.Range(2f, 5f));
            if (Calm) yield break;

            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(15f, 24f);
            Vector3 foot = new Vector3(Mathf.Sin(angle) * radius, -7f, Mathf.Cos(angle) * radius);

            SpawnBolt(foot + Vector3.up * 16f, foot, 0.1f, true, false);
            Flash(0.2f, 0f);
            PlayThunder(0.2f, 0.7f);
        }
    }

    // A glowing zigzag between two points that flickers and disappears in a blink.
    public void SpawnBolt(Vector3 from, Vector3 to, float thickness, bool branches, bool withLight)
    {
        GameObject bolt = new GameObject("LightningBolt");
        Zigzag(bolt.transform, from, to, 8, thickness);

        if (branches)
        {
            for (int i = 0; i < 2; i++)
            {
                Vector3 start = Vector3.Lerp(from, to, Random.Range(0.3f, 0.7f));
                Vector3 end = start + (to - from).normalized * Random.Range(2f, 4f) + Random.onUnitSphere * 2.5f;
                Zigzag(bolt.transform, start, end, 3, thickness * 0.5f);
            }
        }

        if (withLight)
        {
            Light light = new GameObject("BoltLight").AddComponent<Light>();
            light.transform.SetParent(bolt.transform, false);
            light.transform.position = to + Vector3.up * 1.5f;
            light.type = LightType.Point;
            light.color = new Color(0.8f, 0.9f, 1f);
            light.range = 14f;
            light.intensity = 8f;
        }

        StartCoroutine(FlickerBolt(bolt));
    }

    void Zigzag(Transform parent, Vector3 from, Vector3 to, int segments, float thickness)
    {
        float jitter = Vector3.Distance(from, to) * 0.06f;
        Vector3 previous = from;

        for (int i = 1; i <= segments; i++)
        {
            Vector3 next = Vector3.Lerp(from, to, i / (float)segments);
            if (i < segments) next += Random.insideUnitSphere * jitter;

            Vector3 step = next - previous;
            if (step.sqrMagnitude > 0.0001f)
            {
                GameObject part = Visual(PrimitiveType.Cube, boltMaterial, parent);
                part.transform.position = (previous + next) / 2f;
                part.transform.rotation = Quaternion.LookRotation(step);
                part.transform.localScale = new Vector3(thickness, thickness, step.magnitude);
            }
            previous = next;
        }
    }

    IEnumerator FlickerBolt(GameObject bolt)
    {
        yield return new WaitForSeconds(0.06f);
        bolt.SetActive(false);
        yield return new WaitForSeconds(0.03f);
        bolt.SetActive(true);
        yield return new WaitForSeconds(0.09f);
        Destroy(bolt);
    }

    public void SpawnSparks(Vector3 origin, int count)
    {
        SpawnSparks(origin, count, boltMaterial);
    }

    public static void SpawnSparks(Vector3 origin, int count, Material material)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject spark = Visual(PrimitiveType.Sphere, material, null);
            spark.transform.position = origin;
            spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);

            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(1f, 2f), Random.Range(-1f, 1f)).normalized * Random.Range(2.5f, 5f);
            spark.AddComponent<Spark>().Init(velocity, Random.Range(0.3f, 0.6f));
        }
    }

    // A simple shape used only for looks: no collider, so it never gets in the wizard's way.
    public static GameObject Visual(PrimitiveType type, Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
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

    // 6 seconds of soft howling wind. The end fades into the start, so the loop has no click.
    static AudioClip MakeWind()
    {
        const int rate = 22050;
        const float Tau = Mathf.PI * 2f;
        int length = 6 * rate;
        int fade = rate / 2;

        System.Random random = new System.Random(23);
        float[] raw = new float[length + fade];
        float lowpass = 0f;
        for (int i = 0; i < raw.Length; i++)
        {
            float t = i / (float)rate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            lowpass += (noise - lowpass) * 0.035f;
            raw[i] = lowpass * (1.6f + Mathf.Sin(Tau * t / 3f) * 0.6f);
        }

        float[] data = new float[length];
        for (int i = 0; i < length; i++)
            data[i] = Mathf.Clamp(i < fade ? Mathf.Lerp(raw[length + i], raw[i], i / (float)fade) : raw[i], -1f, 1f);

        AudioClip clip = AudioClip.Create("wind", length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
