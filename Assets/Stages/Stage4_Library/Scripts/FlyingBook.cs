using UnityEngine;

// Stage 4 twist: a haunted book that flies along a route, flapping its covers like wings.
// If it touches the wizard she is knocked back and dazed for a moment (see PlayerDaze).
//
// The route is a list of points. Only x and z are used: the book keeps its own flying height.
// It flies from point to point and loops back to the first one, so 2 points = back and forth.
// Select a book in the Scene view to see its route drawn in purple; you can edit the points in the Inspector.
// In the last seconds of the stage the books get angry: they fly faster and glow red.
// They ignore the wizard while she is invisible (see PlayerInvisibility).
public class FlyingBook : MonoBehaviour
{
    [Header("Flight")]
    public Vector3[] route = { new Vector3(-3f, 0f, 0f), new Vector3(3f, 0f, 0f) };
    public float speed = 3f;
    public float height = 1.1f;
    [Tooltip("Seconds after the stage starts before the book wakes up. Until then it dozes and does no harm.")]
    public float wakeDelay = 0f;

    [Header("Hit")]
    public float hitRadius = 0.75f;
    [Tooltip("How far (in metres) the wizard is pushed when hit.")]
    public float knockback = 2.5f;

    [Header("Angry")]
    [Tooltip("When this many seconds are left, the book gets angry.")]
    public float angryBelowSeconds = 30f;
    public float angrySpeedMultiplier = 1.6f;

    const float SleepHeight = 0.35f;

    Transform coverLeft;
    Transform coverRight;
    Transform glow;
    Vector3 glowScale;

    Transform player;
    PlayerDaze daze;

    Vector3 basePosition; // position without the bobbing
    float currentHeight;
    int next;
    float flapPhase;
    float bobPhase;
    float flapBoost;
    float cooldown;
    float wing; // current wing angle, so the covers move smoothly between states
    float anger; // 0 = calm, 1 = angry

    static readonly Color AngryRed = new Color(1f, 0.12f, 0.08f);
    Material glowMaterial; // this book's own copy, so only its colour changes
    Color glowColor;
    Color glowEmission;
    Material eyeMaterial;
    Color eyeColor;
    Color eyeEmission;

    void Start()
    {
        coverLeft = transform.Find("Body/CoverLeft");
        coverRight = transform.Find("Body/CoverRight");
        glow = transform.Find("Glow");
        if (glow != null) glowScale = glow.localScale;
        SetupColors();

        PlayerController pc = FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            player = pc.transform;
            daze = pc.GetComponent<PlayerDaze>();
            if (daze == null) daze = pc.gameObject.AddComponent<PlayerDaze>();
        }

        PlayerDaze.Lives(); // makes sure the hearts are on screen from the start

        if (route == null || route.Length == 0) route = new[] { transform.position };

        currentHeight = SleepHeight;
        basePosition = Flat(route[0]) + Vector3.up * currentHeight;
        next = route.Length > 1 ? 1 : 0;
        flapPhase = Random.value * 10f;
        bobPhase = Random.value * 10f;
        wing = -85f;

        Vector3 first = Flat(route[next]) - Flat(route[0]);
        if (first.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(first);
        transform.position = basePosition;
    }

    void Update()
    {
        StageManager stage = StageManager.Instance;
        float dt = Time.deltaTime;
        cooldown -= dt;
        flapBoost = Mathf.MoveTowards(flapBoost, 0f, dt * 2f);

        bool won = stage != null && stage.State == StageState.Won;

        // angry when time is running out (calms down again when the stage is over)
        bool angry = stage != null && stage.State == StageState.Playing && stage.TimeLeft <= angryBelowSeconds;
        anger = Mathf.MoveTowards(anger, angry ? 1f : 0f, dt * 0.8f);
        UpdateColors();
        bool awake = Time.timeSinceLevelLoad >= wakeDelay;

        if (won)
            Settle(dt);          // the potion is done: the curse is lifted and the books drop
        else if (!awake)
            Doze(dt);
        else
            Fly(dt, stage);

        UpdateGlow(awake && !won);
    }

    // Before waking: closed, hovering low, and shivering just before it takes off.
    void Doze(float dt)
    {
        float timeToWake = wakeDelay - Time.timeSinceLevelLoad;
        wing = Mathf.MoveTowards(wing, -85f, dt * 200f);
        SetWings(wing);

        Vector3 p = basePosition + Vector3.up * (Mathf.Sin(Time.time * 1.5f + bobPhase) * 0.04f);
        if (timeToWake < 1.5f)
        {
            float shiver = (1.5f - timeToWake) / 1.5f;
            p += new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * 0.04f * shiver;
        }
        transform.position = p;
    }

    void Fly(float dt, StageManager stage)
    {
        // rise from the doze height up to the flying height
        currentHeight = Mathf.MoveTowards(currentHeight, height, dt * 2f);

        Vector3 target = Flat(route[next]) + Vector3.up * currentHeight;
        Vector3 flatBase = Flat(basePosition) + Vector3.up * currentHeight;
        float currentSpeed = speed * Mathf.Lerp(1f, angrySpeedMultiplier, anger);
        basePosition = Vector3.MoveTowards(flatBase, target, currentSpeed * dt);

        Vector3 toTarget = Flat(target) - Flat(basePosition);
        if (toTarget.sqrMagnitude < 0.0004f)
            next = (next + 1) % route.Length;
        else
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toTarget), 1f - Mathf.Exp(-6f * dt));

        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * 3f + bobPhase) * 0.15f);

        // flapping: faster right after hitting someone
        flapPhase += dt * (11f + flapBoost * 10f + anger * 7f);
        float target_wing = -20f + Mathf.Sin(flapPhase) * 45f;
        wing = Mathf.MoveTowards(wing, target_wing, dt * 600f);
        SetWings(wing);

        TryHit(stage);
    }

    void TryHit(StageManager stage)
    {
        if (player == null || daze == null || cooldown > 0f || daze.Dazed) return;
        if (PlayerInvisibility.Active) return; // the books cannot see an invisible wizard
        if (stage != null && (stage.State != StageState.Playing || stage.Brewing)) return;

        Vector3 away = Flat(player.position) - Flat(transform.position);
        if (away.magnitude > hitRadius) return;

        // push the wizard away from the book, a bit in the direction the book is flying
        Vector3 push = (away.sqrMagnitude > 0.001f ? away.normalized : transform.forward) + transform.forward * 0.6f;
        push.y = 0f;
        daze.Hit(push.normalized * knockback);

        cooldown = 1.5f;
        flapBoost = 1f;
    }

    void Settle(float dt)
    {
        currentHeight = Mathf.MoveTowards(currentHeight, 0.32f, dt * 3f);
        basePosition = Flat(basePosition) + Vector3.up * currentHeight;
        transform.position = basePosition;
        wing = Mathf.MoveTowards(wing, -85f, dt * 200f);
        SetWings(wing);
    }

    void SetWings(float angle)
    {
        if (coverLeft != null) coverLeft.localRotation = Quaternion.Euler(0f, 0f, -angle);
        if (coverRight != null) coverRight.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // The purple glow on the floor shows where the book is, even from the camera high above.
    void UpdateGlow(bool active)
    {
        if (glow == null) return;

        glow.position = new Vector3(transform.position.x, 0.03f, transform.position.z);
        glow.rotation = Quaternion.identity;
        float size = active ? 1f + Mathf.Sin(Time.time * 4f + bobPhase) * 0.1f + flapBoost * 0.4f : 0.45f;
        Vector3 wanted = new Vector3(glowScale.x * size, glowScale.y, glowScale.z * size);
        glow.localScale = Vector3.Lerp(glow.localScale, wanted, 1f - Mathf.Exp(-8f * Time.deltaTime));
    }

    void SetupColors()
    {
        Renderer glowRenderer = glow != null ? glow.GetComponent<Renderer>() : null;
        if (glowRenderer != null)
        {
            glowMaterial = glowRenderer.material; // a copy for this book
            glowColor = glowMaterial.GetColor("_BaseColor");
            glowEmission = glowMaterial.GetColor("_EmissionColor");
        }

        Transform eyeL = transform.Find("Body/EyeL");
        Transform eyeR = transform.Find("Body/EyeR");
        if (eyeL != null)
        {
            eyeMaterial = new Material(eyeL.GetComponent<Renderer>().sharedMaterial);
            eyeColor = eyeMaterial.GetColor("_BaseColor");
            eyeEmission = eyeMaterial.GetColor("_EmissionColor");
            eyeL.GetComponent<Renderer>().sharedMaterial = eyeMaterial;
            if (eyeR != null) eyeR.GetComponent<Renderer>().sharedMaterial = eyeMaterial;
        }
    }

    // purple glow and yellow eyes slowly turn red as the book gets angry
    void UpdateColors()
    {
        if (glowMaterial != null)
        {
            glowMaterial.SetColor("_BaseColor", Color.Lerp(glowColor, AngryRed, anger));
            glowMaterial.SetColor("_EmissionColor", Color.Lerp(glowEmission, AngryRed * 2.2f, anger));
        }
        if (eyeMaterial != null)
        {
            eyeMaterial.SetColor("_BaseColor", Color.Lerp(eyeColor, AngryRed, anger));
            eyeMaterial.SetColor("_EmissionColor", Color.Lerp(eyeEmission, AngryRed * 3f, anger));
        }
    }

    static Vector3 Flat(Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    void OnDrawGizmos()
    {
        if (route == null || route.Length == 0) return;

        Gizmos.color = new Color(0.75f, 0.4f, 1f, 0.8f);
        for (int i = 0; i < route.Length; i++)
        {
            Vector3 a = Flat(route[i]) + Vector3.up * height;
            Vector3 b = Flat(route[(i + 1) % route.Length]) + Vector3.up * height;
            Gizmos.DrawSphere(a, 0.12f);
            if (route.Length > 1) Gizmos.DrawLine(a, b);
        }
    }
}
