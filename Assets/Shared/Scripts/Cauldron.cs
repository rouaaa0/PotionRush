using UnityEngine;

// When the recipe is complete and the player walks up to it, the player is locked in place
// and each shake fills the brew until the potion is ready.
// It also animates itself: the liquid pulses and turns gold, bubbles rise and the light glows.
public class Cauldron : MonoBehaviour
{
    [SerializeField] float activateRadius = 2.2f;

    static readonly Color Green = new Color(0.37f, 0.89f, 0.63f);
    static readonly Color Gold = new Color(1f, 0.76f, 0.3f);
    static readonly Color Red = new Color(1f, 0.15f, 0.08f);

    PlayerController player;
    bool brewing;
    int shakes;

    Transform liquid;
    Material liquidMaterial;
    Light glow;
    float glowBase;
    Transform[] bubbles;
    float pulse;
    float progress;
    bool exploded;
    bool bursted;
    bool celebrated;
    float wonTime;
    Transform bottle;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (GameInput.Instance != null) GameInput.Instance.Shaken += OnShaken;

        liquid = transform.Find("Liquid");
        if (liquid != null) liquidMaterial = liquid.GetComponent<Renderer>().material; // a copy, the asset stays untouched
        glow = GetComponentInChildren<Light>();
        if (glow != null) glowBase = glow.intensity;

        bubbles = new Transform[10];
        for (int i = 0; i < bubbles.Length; i++)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(b.GetComponent<Collider>());
            if (liquidMaterial != null) b.GetComponent<MeshRenderer>().sharedMaterial = liquidMaterial;
            b.transform.SetParent(transform, false);
            b.SetActive(false);
            bubbles[i] = b.transform;
        }
    }

    void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.Shaken -= OnShaken;
    }

    void Update()
    {
        StageManager stage = StageManager.Instance;
        if (stage == null) return;

        if (!brewing && player != null && stage.State == StageState.Playing && stage.RecipeComplete &&
            Vector3.Distance(player.transform.position, transform.position) <= activateRadius)
        {
            brewing = true;
            shakes = 0;
            player.CanMove = false;
            stage.SetBrewing(true);
        }

        Animate(stage);
    }

    void Animate(StageManager stage)
    {
        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 3f);
        bool mixing = brewing || stage.State == StageState.Won;
        bool won = stage.State == StageState.Won;
        bool lost = stage.State == StageState.Lost;

        if (lost && !exploded)
        {
            exploded = true;
            pulse = 1.5f;
        }

        if (liquid != null)
        {
            float breathe = 1f + Mathf.Sin(Time.time * 3f) * 0.01f + pulse * 0.07f;
            liquid.localScale = new Vector3(1.3f * breathe, 0.01f, 1.3f * breathe);
        }

        Color tint = lost ? Red : Color.Lerp(Green, Gold, won ? 1f : progress);

        if (liquidMaterial != null)
        {
            liquidMaterial.SetColor("_BaseColor", tint);
            liquidMaterial.SetColor("_EmissionColor", tint * 0.6f);
        }

        if (glow != null)
        {
            glow.color = tint;
            glow.intensity = glowBase * (1f + Mathf.Sin(Time.time * 2f) * 0.1f) + pulse * 4f + (won ? 3f : 0f);
            if (lost) glow.intensity = glowBase * (0.6f + Mathf.Sin(Time.time * 18f) * 0.4f) + pulse * 6f; // angry flicker
        }

        if (lost && exploded && !bursted)
        {
            bursted = true;
            Burst();
        }

        if (won)
        {
            if (!celebrated)
            {
                celebrated = true;
                Celebrate();
            }
            AnimateBottle();
        }

        for (int i = 0; i < bubbles.Length; i++)
        {
            bubbles[i].gameObject.SetActive(mixing);
            if (!mixing) continue;

            float u = (Time.time * (0.9f + (i % 4) * 0.25f) + i * 0.37f) % 1f;
            float angle = i * 2.4f;
            float radius = 0.15f + (i * 0.137f) % 0.4f;
            bubbles[i].localPosition = new Vector3(Mathf.Cos(angle) * radius, 1.06f + u * (0.35f + pulse * 0.4f), Mathf.Sin(angle) * radius);
            bubbles[i].localScale = Vector3.one * (0.1f + (i % 3) * 0.035f) * (1f - u * 0.7f);
        }
    }

    // The potion is ready: confetti, a shockwave along the floor and the finished bottle rising out of the cauldron.
    void Celebrate()
    {
        pulse = 1.5f;
        Vector3 origin = transform.position + Vector3.up * 1.1f;

        Color[] palette =
        {
            Gold, new Color(1f, 0.42f, 0.6f), new Color(0.44f, 0.83f, 1f), new Color(0.62f, 1f, 0.42f), new Color(0.77f, 0.55f, 1f),
        };
        Material[] confetti = new Material[palette.Length];
        for (int i = 0; i < palette.Length; i++) confetti[i] = Tinted(palette[i], 0.8f);

        for (int i = 0; i < 70; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spark.GetComponent<Collider>());
            spark.GetComponent<MeshRenderer>().sharedMaterial = confetti[i % confetti.Length];
            spark.transform.position = origin;
            spark.transform.localScale = Vector3.one * Random.Range(0.08f, 0.17f);

            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(1.6f, 2.6f), Random.Range(-1f, 1f)).normalized * Random.Range(4f, 8f);
            spark.AddComponent<Spark>().Init(velocity, Random.Range(1.2f, 2f));
        }

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(ring.GetComponent<Collider>());
        ring.GetComponent<MeshRenderer>().sharedMaterial = Tinted(Gold, 1.2f);
        ring.transform.position = transform.position + Vector3.up * 0.06f;
        ring.AddComponent<Shockwave>();

        GameObject b = new GameObject("PotionBottle");
        b.transform.SetParent(transform, false);
        b.transform.localPosition = Vector3.up * 1.1f;
        Material glass = Tinted(Gold, 0.9f);
        Part(b, PrimitiveType.Sphere, new Vector3(0f, 0f, 0f), Vector3.one * 0.5f, glass);
        Part(b, PrimitiveType.Cylinder, new Vector3(0f, 0.32f, 0f), new Vector3(0.16f, 0.14f, 0.16f), glass);
        Part(b, PrimitiveType.Cylinder, new Vector3(0f, 0.5f, 0f), new Vector3(0.2f, 0.06f, 0.2f), Tinted(new Color(0.45f, 0.27f, 0.15f), 0f));
        bottle = b.transform;
        bottle.localScale = Vector3.zero;
    }

    void AnimateBottle()
    {
        if (bottle == null) return;

        wonTime += Time.deltaTime;
        float k = Mathf.Clamp01(wonTime / 0.8f);
        float x = k - 1f;
        const float c1 = 1.70158f;
        float pop = 1f + (c1 + 1f) * x * x * x + c1 * x * x;

        float rise = 1f - (1f - k) * (1f - k);
        float y = Mathf.Lerp(1.1f, 2.7f, rise) + Mathf.Sin(wonTime * 3f) * 0.08f * k;
        bottle.localPosition = new Vector3(0f, y, 0f);
        bottle.localScale = Vector3.one * pop;
        bottle.localRotation = Quaternion.Euler(0f, wonTime * 180f, Mathf.Sin(wonTime * 2f) * 12f);
    }

    Material Tinted(Color color, float emission)
    {
        Material m = liquidMaterial != null ? new Material(liquidMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", color);
        m.SetColor("_EmissionColor", color * emission);
        return m;
    }

    static void Part(GameObject parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
    }

    // The potion blows up: a big spray of red sparks.
    void Burst()
    {
        Vector3 origin = transform.position + Vector3.up * 1.1f;
        for (int i = 0; i < 45; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spark.GetComponent<Collider>());
            if (liquidMaterial != null) spark.GetComponent<MeshRenderer>().sharedMaterial = liquidMaterial;
            spark.transform.position = origin;
            spark.transform.localScale = Vector3.one * Random.Range(0.1f, 0.22f);

            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 1.6f), Random.Range(-1f, 1f)).normalized * Random.Range(3f, 7f);
            spark.AddComponent<Spark>().Init(velocity, Random.Range(0.8f, 1.5f));
        }
    }

    void OnShaken()
    {
        if (!brewing) return;

        StageManager stage = StageManager.Instance;
        shakes++;
        pulse = 1f;
        progress = (float)shakes / stage.config.shakesRequired;
        stage.ReportBrewProgress(progress);

        if (shakes >= stage.config.shakesRequired)
        {
            brewing = false;
            stage.SetBrewing(false);
            stage.CompleteBrew();
        }
    }
}
