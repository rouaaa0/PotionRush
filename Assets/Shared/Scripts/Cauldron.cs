using UnityEngine;

// When the recipe is complete and the player walks up to it, the player is locked in place
// and each shake fills the brew until the potion is ready.
// It also animates itself: the liquid pulses and turns gold, bubbles rise and the light glows.
public class Cauldron : MonoBehaviour
{
    [SerializeField] float activateRadius = 2.2f;

    static readonly Color Green = new Color(0.37f, 0.89f, 0.63f);
    static readonly Color Gold = new Color(1f, 0.76f, 0.3f);

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

        if (liquid != null)
        {
            float breathe = 1f + Mathf.Sin(Time.time * 3f) * 0.01f + pulse * 0.07f;
            liquid.localScale = new Vector3(1.3f * breathe, 0.01f, 1.3f * breathe);
        }

        if (liquidMaterial != null)
        {
            Color c = Color.Lerp(Green, Gold, won ? 1f : progress);
            liquidMaterial.SetColor("_BaseColor", c);
            liquidMaterial.SetColor("_EmissionColor", c * 0.6f);
        }

        if (glow != null)
        {
            glow.color = Color.Lerp(Green, Gold, won ? 1f : progress);
            glow.intensity = glowBase * (1f + Mathf.Sin(Time.time * 2f) * 0.1f) + pulse * 4f + (won ? 3f : 0f);
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
