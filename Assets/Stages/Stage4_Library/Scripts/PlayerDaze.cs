using UnityEngine;

// Stage 4: what happens to the wizard when a flying book hits her (each hit costs a heart, see PlayerLives).
// She is pushed back, spins on the spot with stars over her head and cannot move for a moment.
// FlyingBook adds this to the player automatically, nothing to set up.
public class PlayerDaze : MonoBehaviour
{
    public float dazeTime = 1f;

    public bool Dazed { get { return dazeLeft > 0f; } }

    CharacterController controller;
    PlayerController playerController;
    AudioSource audioSource;
    Transform stars;

    Vector3 pushVelocity;
    float dazeLeft;
    float spin;

    static AudioClip hitClip;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerController = GetComponent<PlayerController>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        BuildStars();
    }

    public void Hit(Vector3 knock)
    {
        if (Dazed) return;

        const float decay = 6f; // the push slows down quickly; total distance = knock
        pushVelocity = knock * decay;
        dazeLeft = dazeTime;
        spin = 900f;
        if (playerController != null) playerController.CanMove = false;
        if (stars != null) stars.gameObject.SetActive(true);

        if (GameSettings.SoundOn) audioSource.PlayOneShot(HitClip());
        Haptics.Tap(120);

        // each hit costs a heart (the team's shared PlayerLives); the last one ends the stage
        PlayerLives lives = Lives();
        bool knockedOut = lives != null && lives.LoseOne();
        if (knockedOut) LibraryMessage.Show("Assommé !", 1.5f);
        else if (lives != null && lives.Left == 1) LibraryMessage.Show("Aïe ! Dernière vie !", 1.3f);
        else LibraryMessage.Show("Aïe !", 0.9f);
    }

    // The wizard's hearts, on the StageManager object like in stages 3 and 6.
    // If the scene has none yet, 3 are added (the hearts appear under the objectives).
    public static PlayerLives Lives()
    {
        StageManager stage = StageManager.Instance;
        if (stage == null) return null;

        PlayerLives lives = stage.GetComponent<PlayerLives>();
        if (lives == null)
        {
            lives = stage.gameObject.AddComponent<PlayerLives>();
            lives.Init(3, stage, FindAnyObjectByType<HUD>(), "Trop de coups !");
        }
        return lives;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        if (pushVelocity.sqrMagnitude > 0.0001f && controller != null && controller.enabled)
        {
            controller.Move(pushVelocity * dt);
            pushVelocity *= Mathf.Exp(-6f * dt);
        }

        if (!Dazed) return;

        dazeLeft -= dt;
        spin = Mathf.MoveTowards(spin, 0f, dt * 700f);
        transform.Rotate(0f, spin * dt, 0f, Space.World);

        if (stars != null)
        {
            stars.position = transform.position + Vector3.up * 2.25f;
            stars.Rotate(0f, 360f * dt, 0f, Space.World);
        }

        if (dazeLeft <= 0f) Recover();
    }

    void Recover()
    {
        dazeLeft = 0f;
        if (stars != null) stars.gameObject.SetActive(false);

        // do not give movement back if the cauldron locked the player in the meantime, or the stage is over
        StageManager stage = StageManager.Instance;
        bool locked = stage != null && (stage.Brewing || stage.State != StageState.Playing);
        if (playerController != null && !locked) playerController.CanMove = true;
    }

    // three little yellow stars turning above the head
    void BuildStars()
    {
        GameObject root = new GameObject("DazeStars");
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.up * 2.25f;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        Color yellow = new Color(1f, 0.85f, 0.3f);
        mat.SetColor("_BaseColor", yellow);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", yellow * 2f);

        for (int i = 0; i < 3; i++)
        {
            GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(s.GetComponent<Collider>());
            s.GetComponent<MeshRenderer>().sharedMaterial = mat;
            s.transform.SetParent(root.transform, false);
            float a = i * Mathf.PI * 2f / 3f;
            s.transform.localPosition = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.42f;
            s.transform.localScale = Vector3.one * 0.16f;
        }

        stars = root.transform;
        root.SetActive(false);
    }

    // A papery "thwack": a short noise burst on top of a low thump. Made from maths, no audio file needed.
    static AudioClip HitClip()
    {
        if (hitClip != null) return hitClip;

        const int rate = 22050;
        float[] data = new float[Mathf.CeilToInt(0.3f * rate)];
        System.Random rng = new System.Random(7);
        float phase = 0f;
        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)rate;
            phase += Mathf.PI * 2f * Mathf.Lerp(160f, 60f, t / 0.3f) / rate;
            float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 14f);
            float noise = ((float)rng.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 40f);
            data[i] = Mathf.Clamp(thump * 0.8f + noise * 0.5f, -1f, 1f);
        }

        hitClip = AudioClip.Create("BookHit", data.Length, 1, rate, false);
        hitClip.SetData(data, 0);
        return hitClip;
    }
}
