using UnityEngine;

// ============================================================
//  ForestMonster  –  Stage 2 (Scenarios 2 & 3)
//  A small, slow, floating spectral ghost / wisp.
//
//  Moves slowly:
//    - Patrol speed: 1.35 m/s (slow, gentle floating drift)
//    - Chase speed:  2.15 m/s (slow eerie pursuit, wizard can easily outrun)
//
//  1. GuardCauldron (Scenario 2):
//     Slowly floats around the cauldron shrine.
//     If the wizard approaches, it slowly pursues!
//
//  2. GuardObjectives (Scenario 3):
//     Slowly floats between the 3 rune monoliths, haunting each one.
//     The wizard must activate all 3 monoliths without touching the ghost!
//
//  Contact (<= 0.9m) triggers:
//     StageManager.FailStage("Attrapé par le Fantôme !")
// ============================================================
public enum MonsterMode
{
    GuardCauldron,
    GuardObjectives,
    ChasePlayer
}

public class ForestMonster : MonoBehaviour
{
    [Header("Behavior Mode")]
    public MonsterMode currentMode = MonsterMode.GuardCauldron;

    [Header("Speeds (Slow Floating Ghost)")]
    [SerializeField] float patrolSpeed = 1.35f; // Slow floating patrol
    [SerializeField] float chaseSpeed  = 2.15f; // Slow eerie pursuit
    [SerializeField] float turnSpeed   = 180f;  // Gentle floating turn
    [SerializeField] float catchRadius = 0.90f; // Small ghost collision radius

    [Header("Detection")]
    [SerializeField] float detectRange = 6.0f;
    [SerializeField] float loseRange   = 10.5f;

    [Header("Visuals")]
    [SerializeField] Light monsterLight;
    [SerializeField] Transform modelRoot;
    [SerializeField] Transform headRoot;
    [SerializeField] Transform leftArm;
    [SerializeField] Transform rightArm;

    PlayerController player;
    bool caught = false;
    float floatTimer = 0f;

    // Cauldron guard patrol waypoints (around cauldron at (0, 0, -2.5))
    Vector3 cauldronPos = new Vector3(0f, 0f, -2.5f);
    Vector3[] cauldronWaypoints;
    int currentCauldronIndex = 0;

    // Objectives guard waypoints (Scenario 3 monoliths)
    Vector3[] objectiveWaypoints;
    int currentObjectiveIndex = 0;
    float waitTimer = 0f;
    bool isWaitingAtObjective = false;

    void Awake()
    {
        // Define circular patrol points around the cauldron shrine
        cauldronWaypoints = new Vector3[]
        {
            cauldronPos + new Vector3(-3.2f, 0f,  0f),
            cauldronPos + new Vector3( 0.0f, 0f, -3.2f),
            cauldronPos + new Vector3( 3.2f, 0f,  0f),
            cauldronPos + new Vector3( 0.0f, 0f,  2.8f)
        };
    }

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        if (modelRoot == null) modelRoot = transform.Find("Model");
    }

    void OnEnable()
    {
        caught = false;
        floatTimer = 0f;
        waitTimer = 0f;
        isWaitingAtObjective = false;
        currentCauldronIndex = 0;
        currentObjectiveIndex = 0;
    }

    public void SetMode(MonsterMode mode, Vector3 guardCenter, Vector3[] waypoints = null)
    {
        currentMode = mode;
        caught = false;

        if (mode == MonsterMode.GuardCauldron)
        {
            cauldronPos = guardCenter;
            cauldronWaypoints = new Vector3[]
            {
                cauldronPos + new Vector3(-3.2f, 0f,  0f),
                cauldronPos + new Vector3( 0.0f, 0f, -3.2f),
                cauldronPos + new Vector3( 3.2f, 0f,  0f),
                cauldronPos + new Vector3( 0.0f, 0f,  2.8f)
            };
            transform.position = cauldronWaypoints[0];
        }
        else if (mode == MonsterMode.GuardObjectives)
        {
            if (waypoints != null && waypoints.Length > 0)
                objectiveWaypoints = waypoints;
            else
                objectiveWaypoints = new Vector3[]
                {
                    new Vector3( 0f, 0f, -6f),
                    new Vector3(-6f, 0f,  4f),
                    new Vector3( 6f, 0f,  4f)
                };

            transform.position = objectiveWaypoints[0];
        }
    }

    void Update()
    {
        if (caught) return;

        // Ensure game is actively playing
        if (StageManager.Instance != null && StageManager.Instance.State != StageState.Playing)
            return;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
        }

        // ── CAULDRON SAFE ZONE ───────────────────────────────────────
        // When the wizard is brewing at the cauldron, the ghost CANNOT
        // catch him. It backs off and returns to its patrol behavior.
        if (StageManager.Instance != null && StageManager.Instance.Brewing)
        {
            // If we were chasing, cancel the chase and go back to guard patrol
            if (currentMode == MonsterMode.ChasePlayer)
                currentMode = MonsterMode.GuardCauldron;

            // Keep floating animation but NOT the aggressive chase animation
            AnimateVisuals(false);
            return; // skip catch-check and pursuit entirely
        }
        // ─────────────────────────────────────────────────────────────

        Vector3 playerPos = player.transform.position;
        Vector3 myPos = transform.position;

        // 1. Check if caught the player
        float distToPlayer = Vector3.Distance(new Vector3(myPos.x, 0f, myPos.z),
                                              new Vector3(playerPos.x, 0f, playerPos.z));

        if (distToPlayer <= catchRadius)
        {
            CatchPlayer();
            return;
        }

        // 2. State machine based on currentMode
        switch (currentMode)
        {
            case MonsterMode.GuardCauldron:
                UpdateGuardCauldron(playerPos, myPos, distToPlayer);
                break;

            case MonsterMode.GuardObjectives:
                UpdateGuardObjectives(playerPos, myPos, distToPlayer);
                break;

            case MonsterMode.ChasePlayer:
                UpdateChasePlayer(playerPos, myPos, distToPlayer);
                break;
        }

        // 3. Procedural run & breathing animation
        AnimateVisuals(currentMode == MonsterMode.ChasePlayer);
    }

    // ── Scenario 2: Guard Cauldron Area ────────────────────────

    void UpdateGuardCauldron(Vector3 playerPos, Vector3 myPos, float distToPlayer)
    {
        float distPlayerToCauldron = Vector3.Distance(new Vector3(playerPos.x, 0f, playerPos.z),
                                                      new Vector3(cauldronPos.x, 0f, cauldronPos.z));

        // Player entered cauldron area or came too close to the monster!
        // EXCEPTION: If the wizard is safely brewing at the cauldron, the ghost
        // must NOT start chasing him — the cauldron is a sanctuary.
        bool wizardIsSafe = StageManager.Instance != null && StageManager.Instance.Brewing;
        if (!wizardIsSafe && (distToPlayer <= detectRange || distPlayerToCauldron <= 5.5f))
        {
            currentMode = MonsterMode.ChasePlayer;
            return;
        }

        // Patrol smoothly around cauldron
        Vector3 targetPoint = cauldronWaypoints[currentCauldronIndex];
        Vector3 toPoint = targetPoint - myPos;
        toPoint.y = 0f;

        if (toPoint.magnitude < 0.9f)
        {
            currentCauldronIndex = (currentCauldronIndex + 1) % cauldronWaypoints.Length;
            targetPoint = cauldronWaypoints[currentCauldronIndex];
        }

        MoveTowards(targetPoint, patrolSpeed);
    }

    // ── Scenario 3: Guard Monolith Objectives ──────────────────

    void UpdateGuardObjectives(Vector3 playerPos, Vector3 myPos, float distToPlayer)
    {
        // If player gets near, start chasing!
        if (distToPlayer <= detectRange)
        {
            currentMode = MonsterMode.ChasePlayer;
            return;
        }

        if (objectiveWaypoints == null || objectiveWaypoints.Length == 0) return;

        // If waiting and inspecting a monolith
        if (isWaitingAtObjective)
        {
            waitTimer -= Time.deltaTime;
            // Look around slowly
            if (headRoot != null)
                headRoot.localEulerAngles = new Vector3(12f, Mathf.Sin(Time.time * 2f) * 35f, 0f);

            if (waitTimer <= 0f)
            {
                isWaitingAtObjective = false;
                currentObjectiveIndex = (currentObjectiveIndex + 1) % objectiveWaypoints.Length;
            }
            return;
        }

        // Walk to current objective
        Vector3 targetObjective = objectiveWaypoints[currentObjectiveIndex];
        Vector3 toObj = targetObjective - myPos;
        toObj.y = 0f;

        if (toObj.magnitude < 1.3f)
        {
            // Arrived at monolith! Pause and inspect for 2.5 seconds
            isWaitingAtObjective = true;
            waitTimer = 2.5f;
            return;
        }

        MoveTowards(targetObjective, patrolSpeed);
    }

    // ── Active Pursuit ─────────────────────────────────────────

    void UpdateChasePlayer(Vector3 playerPos, Vector3 myPos, float distToPlayer)
    {
        // Player managed to escape far into the hedges!
        if (distToPlayer > loseRange)
        {
            // Return to appropriate guard behavior
            if (ForestQuestManager.Instance != null && ForestQuestManager.Instance.CurrentScenario == 3)
                currentMode = MonsterMode.GuardObjectives;
            else
                currentMode = MonsterMode.GuardCauldron;
            return;
        }

        // Aggressively pursue player
        MoveTowards(playerPos, chaseSpeed);
    }

    // ── Movement & Path Steering ───────────────────────────────

    void MoveTowards(Vector3 targetPos, float moveSpeed)
    {
        Vector3 moveDir = CalculateMoveDirection(targetPos, transform.position);

        transform.position += moveDir * (moveSpeed * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }
    }

    Vector3 CalculateMoveDirection(Vector3 targetPos, Vector3 currentPos)
    {
        Vector3 toTarget = targetPos - currentPos;
        toTarget.y = 0f;
        Vector3 desiredDir = toTarget.normalized;

        Vector3 origin = currentPos + Vector3.up * 0.8f;
        const float feelerDist = 1.6f;

        if (Physics.Raycast(origin, desiredDir, out RaycastHit hitCenter, feelerDist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hitCenter.collider.isTrigger && !hitCenter.collider.CompareTag("Player"))
            {
                Vector3 wallNormal = hitCenter.normal;
                wallNormal.y = 0f;
                Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal);

                if (Vector3.Dot(tangent, desiredDir) < 0f)
                    tangent = -tangent;

                return (tangent * 0.8f + wallNormal * 0.3f).normalized;
            }
        }

        Vector3 leftFeeler = Quaternion.Euler(0f, -35f, 0f) * desiredDir;
        Vector3 rightFeeler = Quaternion.Euler(0f, 35f, 0f) * desiredDir;

        if (Physics.Raycast(origin, leftFeeler, feelerDist * 0.8f, ~0, QueryTriggerInteraction.Ignore))
        {
            desiredDir = (desiredDir + rightFeeler * 0.5f).normalized;
        }
        else if (Physics.Raycast(origin, rightFeeler, feelerDist * 0.8f, ~0, QueryTriggerInteraction.Ignore))
        {
            desiredDir = (desiredDir + leftFeeler * 0.5f).normalized;
        }

        return desiredDir;
    }

    void AnimateVisuals(bool isChasing)
    {
        floatTimer += Time.deltaTime;

        // Smooth ghostly hover bobbing up and down (slow floating motion)
        if (modelRoot != null)
        {
            float hoverY = Mathf.Sin(floatTimer * 3.0f) * 0.12f;
            float swayZ  = Mathf.Sin(floatTimer * 2.2f) * 4.0f;
            modelRoot.localPosition = new Vector3(0f, hoverY, 0f);
            modelRoot.localEulerAngles = new Vector3(0f, 0f, swayZ);
        }

        // Floating little ghost hands
        if (leftArm != null && rightArm != null)
        {
            float handFloat = Mathf.Sin(floatTimer * 3.6f) * 12f;
            leftArm.localEulerAngles  = new Vector3(handFloat, 0f, 18f);
            rightArm.localEulerAngles = new Vector3(-handFloat, 0f, -18f);
        }

        // Soft spectral cyan/blue light pulse
        if (monsterLight != null)
        {
            float baseIntensity = isChasing ? 3.0f : 2.2f;
            float pulse = Mathf.Sin(Time.time * 4f) * 0.45f;
            monsterLight.intensity = baseIntensity + pulse;
        }
    }

    void CatchPlayer()
    {
        if (caught) return;

        // Double-check: wizard is safe if he's currently brewing at the cauldron
        if (StageManager.Instance != null && StageManager.Instance.Brewing)
            return;

        caught = true;

        Debug.Log("[ForestMonster] Player caught by ghost! Defeat!");

        if (player != null)
            player.CanMove = false;

        if (StageManager.Instance != null)
            StageManager.Instance.FailStage("Attrapé par le Fantôme !");
    }

    public void Setup(Light light, Transform model, Transform head, Transform lArm, Transform rArm)
    {
        monsterLight = light;
        modelRoot = model;
        headRoot = head;
        leftArm = lArm;
        rightArm = rArm;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
    }
}
