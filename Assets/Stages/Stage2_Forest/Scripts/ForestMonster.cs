using UnityEngine;

// ============================================================
//  ForestMonster  –  Stage 2 (Scenarios 2 & 3)
//
//  A menacing forest stalker with two specialized guard behaviors:
//
//  1. GuardCauldron (Scenario 2):
//     Patrols around the Cauldron (where the wizard must empty herbs).
//     If the player approaches within detection range, the monster
//     chases aggressively! If the wizard flees into the hedges,
//     the monster returns to guard the cauldron.
//
//  2. GuardObjectives (Scenario 3):
//     Patrols between the 3 rune monoliths / relics.
//     Pauses at each to inspect it, then marches to the next.
//     If the player gets near, it chases! The player must collect
//     all monoliths without touching the monster.
//
//  Contact (<= 1.2m) triggers:
//     StageManager.FailStage("Attrapé par le Monstre !")
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

    [Header("Speeds")]
    [SerializeField] float patrolSpeed = 2.3f;
    [SerializeField] float chaseSpeed = 3.65f;
    [SerializeField] float turnSpeed = 360f;
    [SerializeField] float catchRadius = 1.2f;

    [Header("Detection")]
    [SerializeField] float detectRange = 6.5f;
    [SerializeField] float loseRange = 11.5f;

    [Header("Visuals")]
    [SerializeField] Light monsterLight;
    [SerializeField] Transform modelRoot;
    [SerializeField] Transform headRoot;
    [SerializeField] Transform leftArm;
    [SerializeField] Transform rightArm;

    PlayerController player;
    bool caught = false;
    float runCycle = 0f;

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
        runCycle = 0f;
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
        if (distToPlayer <= detectRange || distPlayerToCauldron <= 5.5f)
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
        float animSpeed = isChasing ? 12f : 6f;
        runCycle += Time.deltaTime * animSpeed;

        if (modelRoot != null)
        {
            float bob = Mathf.Abs(Mathf.Sin(runCycle)) * (isChasing ? 0.14f : 0.07f);
            float tilt = Mathf.Sin(runCycle * 0.5f) * (isChasing ? 4f : 2f);
            modelRoot.localPosition = new Vector3(0f, bob, 0f);
            modelRoot.localEulerAngles = new Vector3(tilt, 0f, -tilt * 0.5f);
        }

        if (leftArm != null && rightArm != null)
        {
            float armSwing = Mathf.Sin(runCycle) * (isChasing ? 32f : 16f);
            leftArm.localEulerAngles = new Vector3(armSwing, 0f, 15f);
            rightArm.localEulerAngles = new Vector3(-armSwing, 0f, -15f);
        }

        if (monsterLight != null)
        {
            float baseIntensity = isChasing ? 4.5f : 2.5f;
            float pulse = Mathf.Sin(Time.time * (isChasing ? 10f : 4f)) * (isChasing ? 1.2f : 0.5f);
            monsterLight.intensity = baseIntensity + pulse;
        }
    }

    void CatchPlayer()
    {
        if (caught) return;
        caught = true;

        Debug.Log("[ForestMonster] Player caught! Defeat!");

        if (player != null)
            player.CanMove = false;

        if (StageManager.Instance != null)
            StageManager.Instance.FailStage("Attrapé par le Monstre !");
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
