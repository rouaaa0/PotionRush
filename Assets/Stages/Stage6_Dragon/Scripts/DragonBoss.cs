using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// The boss of the last stage. It stays at the back of the lair and attacks the wizard:
// - fireballs: a circle of fire on the floor shows where each one will land;
// - fire breath: a slow sweep across the room, the pillars give cover.
// A hit knocks the wizard back and stuns it, which costs precious seconds.
// Every dragon scale taken makes it angrier: it attacks faster and with more fireballs.
// While the wizard brews, fireballs rain on the room but never on the cauldron.
// When the potion is ready, the dragon shrinks into a tiny harmless one.
// It only listens to the StageManager, so the shared scripts stay untouched.
public class DragonBoss : MonoBehaviour
{
    [Header("Attacks")]
    public float introTime = 2.5f;
    public float[] attackPause = { 2.6f, 2.1f, 1.7f, 1.3f };   // seconds between attacks, per anger level
    public int[] fireballCount = { 1, 2, 2, 3 };
    public float[] sweepSpeed = { 42f, 50f, 58f, 66f };        // fire breath, degrees per second
    public float fireballFallTime = 1.15f;
    public float fireballRadius = 1.5f;
    public float breathRange = 9f;
    public float breathHalfArc = 60f;
    public float breathHitAngle = 11f;
    public float breathWarning = 1.2f;
    public int lives = 3;       // hits the wizard can take before the dragon wins
    public float stunTime = 1.1f;
    public float immuneAfterHit = 1.2f; // so the wizard can't be stunned again and again without a chance to move

    [Header("Parts (filled in by the builder)")]
    public Transform chest;
    public Transform head;
    public Transform jaw;
    public Transform mouth;
    public Transform wingLeft;
    public Transform wingRight;
    public Transform[] neck;
    public Transform[] tail;
    public ParticleSystem breathFlames;
    public Light mouthGlow;
    public Transform[] cover;
    public float coverRadius = 0.85f;
    public Material fireballMaterial;
    public Material dangerMaterial;
    public Material sootMaterial;

    const float RoomEdge = 6.6f;

    StageManager stage;
    PlayerController player;
    CharacterController playerBody;
    HUD hud;
    PlayerLives wizardLives;
    AudioSource voice;
    Mesh ringMesh;
    Mesh discMesh;
    GameObject breathFan;

    Vector3 chestBase;
    Vector3 lastPlayerPosition;
    Vector3 playerVelocity;
    Vector3[] tailBase;

    float yaw;          // where the head looks, 0 = straight at the camera
    float yawTarget;
    float rear;         // 0 = resting, 1 = head thrown back
    float rearTarget;
    float jawTarget;
    float jawOpen;
    float pitch;
    float pitchTarget;
    float wingBeat = 1f;
    bool trackPlayer = true;
    float angerFlash;
    float immuneUntil;
    bool stunned;
    string hint = "";
    float hintUntil;

    // How angry the dragon is: the number of scales taken so far, 0 to 3.
    int Anger
    {
        get
        {
            int scales = stage != null ? stage.Collected(IngredientType.DragonScale) : 0;
            return Mathf.Clamp(scales, 0, attackPause.Length - 1);
        }
    }

    void Start()
    {
        stage = StageManager.Instance;
        player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerBody = player.GetComponent<CharacterController>();
            lastPlayerPosition = player.transform.position;
        }
        hud = FindFirstObjectByType<HUD>();

        wizardLives = gameObject.AddComponent<PlayerLives>();
        wizardLives.Init(lives, stage, hud, "Vaincu par le dragon");

        voice =gameObject.AddComponent<AudioSource>();
        voice.playOnAwake = false;

        ringMesh = DragonMeshes.Ring(0.86f, 1f);
        discMesh = DragonMeshes.Fan(1f, 360f);
        BuildBreathFan();

        chestBase = chest.localPosition;
        tailBase = new Vector3[tail.Length];
        for (int i = 0; i < tail.Length; i++) tailBase[i] = tail[i].localPosition;

        if (stage != null)
        {
            stage.IngredientCollected += OnCollected;
            stage.StageWon += OnWon;
            stage.StageLost += OnLost;
        }
        StartCoroutine(AttackLoop());
    }

    void OnDestroy()
    {
        if (stage == null) return;
        stage.IngredientCollected -= OnCollected;
        stage.StageWon -= OnWon;
        stage.StageLost -= OnLost;
    }

    bool Playing
    {
        get { return stage != null && stage.State == StageState.Playing; }
    }

    // ---------- attacks ----------

    IEnumerator AttackLoop()
    {
        // the dragon wakes up slowly and gives the player time to look around
        trackPlayer = false;
        Hint("Le dragon se réveille...", introTime);
        rearTarget = 0.6f;
        jawTarget = 1f;
        Play(DragonSounds.Roar(), 1f);
        ScreenRumble.Shake(0.25f);
        yield return new WaitForSeconds(introTime);
        rearTarget = 0f;
        jawTarget = 0f;
        trackPlayer = true;

        bool firstFireball = true;
        int attack = 0;

        while (Playing)
        {
            if (stage.Brewing)
            {
                yield return RainDuringBrew();
                continue;
            }

            attack++;
            if (attack % 3 == 0)
            {
                yield return FireBreath();
            }
            else
            {
                if (firstFireball) Hint("Évitez les cercles de feu !", 2.5f);
                firstFireball = false;
                yield return Fireballs();
            }

            yield return WaitWhilePlaying(attackPause[Anger]);
        }
    }

    IEnumerator WaitWhilePlaying(float seconds)
    {
        for (float t = 0f; t < seconds && Playing && !stage.Brewing; t += Time.deltaTime) yield return null;
    }

    IEnumerator Fireballs()
    {
        rearTarget = 0.35f;
        jawTarget = 1f;
        yield return new WaitForSeconds(0.25f);
        if (!Playing) yield break;

        // the first one aims a little ahead of the wizard, the others land nearby
        int count = fireballCount[Anger];
        for (int i = 0; i < count && player != null; i++)
        {
            Vector3 target = player.transform.position + playerVelocity * 0.55f;
            if (i > 0)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.8f, 3.2f);
                target += new Vector3(offset.x, 0f, offset.y);
            }
            SpawnFireball(target);
            yield return new WaitForSeconds(0.14f);
        }

        Play(DragonSounds.Whoosh(), 1.2f);
        rearTarget = 0f;
        jawTarget = 0f;
    }

    void SpawnFireball(Vector3 target)
    {
        target.x = Mathf.Clamp(target.x, -RoomEdge, RoomEdge);
        target.z = Mathf.Clamp(target.z, -RoomEdge, RoomEdge);
        target.y = 0f;

        GameObject go = new GameObject("DragonFireball");
        go.AddComponent<DragonFireball>().Launch(this, mouth.position, target, fireballFallTime, fireballRadius, ringMesh, discMesh);
    }

    // Called by a fireball when it lands.
    public void Explode(Vector3 position, float radius)
    {
        Play(DragonSounds.Boom(), Random.Range(0.9f, 1.15f));
        ScreenRumble.Shake(0.18f);

        for (int i = 0; i < 12; i++)
            SpawnSpark(position + Vector3.up * 0.3f, fireballMaterial, Random.Range(2.5f, 5f));

        if (player == null) return;
        Vector3 away = player.transform.position - position;
        away.y = 0f;
        if (away.sqrMagnitude <= radius * radius) Burn(away);
    }

    IEnumerator FireBreath()
    {
        Vector3 origin = BreathOrigin();
        Vector3 toPlayer = player != null ? player.transform.position - origin : Vector3.forward;

        // start from the side the wizard is on, so the fire sweeps towards it and across the room
        float side = Mathf.DeltaAngle(0f, Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg) >= 0f ? 1f : -1f;
        float from = side * breathHalfArc;
        float to = -from;

        trackPlayer = false;
        yawTarget = from;
        rearTarget = 1f;
        jawTarget = 0.4f;
        pitchTarget = -12f;
        Hint("Cachez-vous derrière un pilier !", breathWarning + 1f);
        Play(DragonSounds.Roar(), 1.15f);

        breathFan.transform.position = new Vector3(origin.x, 0.05f, origin.z);
        breathFan.SetActive(true);

        for (float t = 0f; t < breathWarning && Playing && !stage.Brewing; t += Time.deltaTime)
        {
            mouthGlow.intensity = Mathf.Lerp(0f, 5f, t / breathWarning);
            yield return null;
        }

        if (Playing && !stage.Brewing)
        {
            rearTarget = 0.1f;
            jawTarget = 1f;
            pitchTarget = 24f;
            breathFlames.Play();
            Play(DragonSounds.Breath(), 1f);
            ScreenRumble.Shake(0.12f);

            float speed = sweepSpeed[Anger];
            float angle = from;
            float direction = Mathf.Sign(to - from);

            while (Playing && !stage.Brewing && (to - angle) * direction > 0f)
            {
                angle += direction * speed * Time.deltaTime;
                yawTarget = angle;
                yaw = angle; // the head follows the fire exactly
                if (BreathHits(angle))
                {
                    Vector3 away = player.transform.position - origin;
                    away.y = 0f;
                    Burn(away);
                }
                yield return null;
            }
            breathFlames.Stop();
        }

        breathFan.SetActive(false);
        mouthGlow.intensity = 0f;
        rearTarget = 0f;
        jawTarget = 0f;
        pitchTarget = 0f;
        trackPlayer = true;
    }

    Vector3 BreathOrigin()
    {
        return chest.position;
    }

    bool BreathHits(float angle)
    {
        if (player == null) return false;

        Vector3 origin = BreathOrigin();
        Vector3 to = player.transform.position - origin;
        to.y = 0f;
        if (to.magnitude > breathRange) return false;

        float toPlayer = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
        if (Mathf.Abs(Mathf.DeltaAngle(angle, toPlayer)) > breathHitAngle) return false;

        return !BehindCover(origin, player.transform.position);
    }

    // True when a pillar stands on the straight line between the dragon and the wizard.
    bool BehindCover(Vector3 from, Vector3 to)
    {
        Vector2 a = new Vector2(from.x, from.z);
        Vector2 b = new Vector2(to.x, to.z);
        Vector2 line = b - a;
        foreach (Transform pillar in cover)
        {
            if (pillar == null) continue;
            Vector2 c = new Vector2(pillar.position.x, pillar.position.z);
            float k = Mathf.Clamp01(Vector2.Dot(c - a, line) / line.sqrMagnitude);
            if ((a + line * k - c).sqrMagnitude < coverRadius * coverRadius) return true;
        }
        return false;
    }

    // The wizard can't move while brewing, so nothing lands near it: this is only for the drama.
    IEnumerator RainDuringBrew()
    {
        trackPlayer = false;
        yawTarget = 0f;
        rearTarget = 0.8f;
        jawTarget = 1f;
        Play(DragonSounds.Roar(), 0.9f);

        while (Playing && stage.Brewing)
        {
            Vector3 safe = player != null ? player.transform.position : Vector3.zero;
            for (int tries = 0; tries < 8; tries++)
            {
                Vector3 target = new Vector3(Random.Range(-RoomEdge, RoomEdge), 0f, Random.Range(-2f, RoomEdge)); // never under the dragon
                if ((target - safe).magnitude < 4.5f) continue;
                SpawnFireball(target);
                break;
            }
            yield return new WaitForSeconds(0.9f);
        }

        rearTarget = 0f;
        jawTarget = 0f;
        trackPlayer = true;
    }

    void Burn(Vector3 away)
    {
        if (!Playing || stage.Brewing || stunned || Time.time < immuneUntil || player == null || !player.CanMove) return;

        bool dead = wizardLives.LoseOne();
        if (!dead) Hint(wizardLives.Left == 1 ? "Attention, dernière vie !" : "Touché ! Plus que " + wizardLives.Left + " vies", 2f);
        StartCoroutine(Stun(away));
    }

    IEnumerator Stun(Vector3 away)
    {
        stunned = true;
        player.CanMove = false;
        Haptics.Tap(120);
        ScreenRumble.Shake(0.3f);

        Vector3 at = player.transform.position + Vector3.up;
        for (int i = 0; i < 8; i++) SpawnSpark(at, sootMaterial, Random.Range(1.5f, 3f));
        for (int i = 0; i < 6; i++) SpawnSpark(at, fireballMaterial, Random.Range(1.5f, 3f));

        // pushed back by the blast
        Vector3 push = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime)
        {
            if (playerBody != null && playerBody.enabled) playerBody.Move((push * 7f * (1f - t / 0.3f) + Vector3.down) * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, stunTime - 0.3f));

        // the cauldron also locks the wizard while brewing: leave that lock alone
        if (Playing && !stage.Brewing) player.CanMove = true;
        stunned = false;
        immuneUntil = Time.time + immuneAfterHit;
    }

    void SpawnSpark(Vector3 position, Material material, float speed)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(spark.GetComponent<Collider>());
        spark.GetComponent<MeshRenderer>().sharedMaterial = material;
        spark.transform.position = position;
        spark.transform.localScale = Vector3.one * Random.Range(0.08f, 0.2f);

        Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 1.8f), Random.Range(-1f, 1f)).normalized * speed;
        spark.AddComponent<Spark>().Init(velocity, Random.Range(0.45f, 0.9f));
    }

    void BuildBreathFan()
    {
        breathFan = new GameObject("BreathDangerZone");
        breathFan.transform.rotation = Quaternion.identity;
        breathFan.AddComponent<MeshFilter>().sharedMesh = DragonMeshes.Fan(breathRange, breathHalfArc * 2f);
        breathFan.AddComponent<MeshRenderer>().sharedMaterial = dangerMaterial;
        breathFan.SetActive(false);
    }

    // ---------- reactions ----------

    void OnCollected(IngredientType type)
    {
        if (type != IngredientType.DragonScale) return;

        angerFlash = 1f;
        Play(DragonSounds.Roar(), 1f + Anger * 0.1f);
        ScreenRumble.Shake(0.2f);
        Hint(stage.RecipeComplete ? "Courez au chaudron !" : "Le dragon s'énerve !", 2f);
    }

    void OnWon(int score)
    {
        StopAllCoroutines();
        if (breathFan != null) breathFan.SetActive(false);
        breathFlames.Stop();
        mouthGlow.intensity = 0f;
        StartCoroutine(ShrinkAway());
    }

    // The elixir of power was stronger: the dragon shrinks into a tiny one that hops about.
    IEnumerator ShrinkAway()
    {
        trackPlayer = false;
        yawTarget = 0f;
        pitchTarget = 0f;
        rearTarget = 1f;
        jawTarget = 1f;
        Play(DragonSounds.Roar(), 1.6f);

        Vector3 big = transform.localScale;
        Vector3 ground = transform.position;
        for (float t = 0f; t < 1.3f; t += Time.deltaTime)
        {
            float k = t / 1.3f;
            transform.localScale = big * Mathf.Lerp(1f, 0.22f, k * k) * (1f + Mathf.Sin(t * 30f) * 0.03f);
            yield return null;
        }

        for (int i = 0; i < 26; i++) SpawnSpark(ground + Vector3.up * 0.7f, fireballMaterial, Random.Range(2f, 5f));

        rearTarget = 0f;
        jawTarget = 0.5f;
        while (true)
        {
            transform.position = ground + Vector3.up * Mathf.Abs(Mathf.Sin(Time.time * 7f)) * 0.3f;
            yield return null;
        }
    }

    void OnLost()
    {
        StopAllCoroutines();
        if (breathFan != null) breathFan.SetActive(false);
        mouthGlow.intensity = 3f;

        // it roars and shoots flames at the ceiling: the dragon wins
        trackPlayer = false;
        yawTarget = 0f;
        rearTarget = 1f;
        jawTarget = 1f;
        pitchTarget = -55f;
        breathFlames.Play();
        Play(DragonSounds.Roar(), 0.8f);
        ScreenRumble.Shake(0.35f);
    }

    void Play(AudioClip clip, float pitchShift)
    {
        if (!GameSettings.SoundOn || voice == null) return;
        voice.pitch = pitchShift;
        voice.PlayOneShot(clip);
    }

    void Hint(string text, float seconds)
    {
        hint = text;
        hintUntil = Time.time + seconds;
    }

    // ---------- animation ----------

    void Update()
    {
        if (player != null && Time.deltaTime > 0f)
        {
            Vector3 v = (player.transform.position - lastPlayerPosition) / Time.deltaTime;
            v.y = 0f;
            playerVelocity = Vector3.Lerp(playerVelocity, Vector3.ClampMagnitude(v, 6f), 1f - Mathf.Exp(-6f * Time.deltaTime));
            lastPlayerPosition = player.transform.position;

            if (trackPlayer)
            {
                Vector3 to = player.transform.position - chest.position;
                yawTarget = Mathf.Clamp(Mathf.DeltaAngle(0f, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg), -70f, 70f);
            }
        }

        float dt = Time.deltaTime;
        yaw = Mathf.Lerp(yaw, yawTarget, 1f - Mathf.Exp(-5f * dt));
        rear = Mathf.Lerp(rear, rearTarget, 1f - Mathf.Exp(-6f * dt));
        jawOpen = Mathf.Lerp(jawOpen, jawTarget, 1f - Mathf.Exp(-12f * dt));
        pitch = Mathf.Lerp(pitch, pitchTarget, 1f - Mathf.Exp(-6f * dt));
        angerFlash = Mathf.MoveTowards(angerFlash, 0f, dt * 2f);
        wingBeat = Mathf.Lerp(wingBeat, 1f + Anger * 0.45f + angerFlash * 2f, 1f - Mathf.Exp(-3f * dt));

        AnimateBody();
    }

    void AnimateBody()
    {
        float breathe = Mathf.Sin(Time.time * 1.6f);

        chest.localPosition = chestBase + Vector3.up * (breathe * 0.04f);

        // the head moves on a circle around the chest; the neck is a smooth curve between the two
        Vector3 reach = new Vector3(0f, Mathf.Lerp(1.7f, 2.5f, rear) + breathe * 0.06f, Mathf.Lerp(1.7f, 0.9f, rear));
        Vector3 headLocal = chest.localPosition + Quaternion.Euler(0f, yaw * 0.85f, 0f) * reach;
        head.localPosition = headLocal;
        head.localRotation = Quaternion.Euler(pitch + rear * -18f, yaw, 0f);

        Vector3 bend = chest.localPosition + Vector3.up * (reach.y * 0.85f);
        for (int i = 0; i < neck.Length; i++)
        {
            float k = (i + 1f) / (neck.Length + 1f);
            neck[i].localPosition = Vector3.Lerp(Vector3.Lerp(chest.localPosition, bend, k), Vector3.Lerp(bend, headLocal, k), k);
        }

        jaw.localRotation = Quaternion.Euler(jawOpen * 34f, 0f, 0f);

        float flap = Mathf.Sin(Time.time * 1.8f * wingBeat) * (6f + (wingBeat - 1f) * 12f);
        wingRight.localRotation = Quaternion.Euler(0f, 0f, flap);
        wingLeft.localRotation = Quaternion.Euler(0f, 0f, -flap);

        for (int i = 0; i < tail.Length; i++)
        {
            float sway = Mathf.Sin(Time.time * 1.3f * wingBeat - i * 0.6f) * i * 0.05f;
            tail[i].localPosition = tailBase[i] + new Vector3(sway * 0.5f, 0f, sway);
        }
    }

    // Runs after the HUD has set its own hint this frame, so this one stays on screen.
    void LateUpdate()
    {
        if (hud == null || hud.hintRoot == null || !Playing) return;

        string text = stage.Brewing ? (stage.config.tapToBrew ? "Touchez les cercles !" : "Secouez votre téléphone !") : Time.time < hintUntil ? hint : "";
        if (text.Length == 0) return;

        hud.hintRoot.SetActive(true);
        Text label = hud.hintText;
        if (label != null) label.text = text;
    }
}
