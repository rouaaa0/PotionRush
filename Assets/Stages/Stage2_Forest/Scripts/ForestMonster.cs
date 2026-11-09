using UnityEngine;

// ============================================================
//  ForestMonster  –  Stage 2 Scenario 2 (The Hedge Labyrinth)
//
//  A menacing forest stalker that pursues the wizard through
//  the labyrinth during Scenario 2.
//  If the monster catches (touches) the player, the stage is lost!
//
//  Features:
//    - Smooth path steering towards the player with hedge wall avoidance
//    - Menacing glowing red eyes & chest aura (point light)
//    - Procedural running bob & arm swing animation
//    - Triggers StageManager.FailStage("Attrapé par le Monstre !") on contact
// ============================================================
public class ForestMonster : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed = 3.6f;
    [SerializeField] float turnSpeed = 360f;
    [SerializeField] float catchRadius = 1.15f;

    [Header("Visuals")]
    [SerializeField] Light monsterLight;
    [SerializeField] Transform modelRoot;
    [SerializeField] Transform leftArm;
    [SerializeField] Transform rightArm;

    PlayerController player;
    bool caught = false;
    Vector3 startPos;
    float runCycle = 0f;

    void Start()
    {
        startPos = transform.position;
        player = FindFirstObjectByType<PlayerController>();

        if (modelRoot == null)
            modelRoot = transform.Find("Model");
    }

    void OnEnable()
    {
        caught = false;
        transform.position = startPos;
        runCycle = 0f;
    }

    void Update()
    {
        if (caught) return;

        // Check stage state
        if (StageManager.Instance != null && StageManager.Instance.State != StageState.Playing)
            return;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
        }

        Vector3 playerPos = player.transform.position;
        Vector3 myPos = transform.position;

        // Check if caught the player!
        float distToPlayer = Vector3.Distance(new Vector3(myPos.x, 0f, myPos.z),
                                              new Vector3(playerPos.x, 0f, playerPos.z));

        if (distToPlayer <= catchRadius)
        {
            CatchPlayer();
            return;
        }

        // Steer towards player with hedge obstacle avoidance
        Vector3 moveDir = CalculateMoveDirection(playerPos, myPos);

        // Move
        transform.position += moveDir * (speed * Time.deltaTime);

        // Rotate towards movement direction
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // Animate procedural running
        AnimateRun();
    }

    Vector3 CalculateMoveDirection(Vector3 targetPos, Vector3 currentPos)
    {
        Vector3 toTarget = (targetPos - currentPos);
        toTarget.y = 0f;
        Vector3 desiredDir = toTarget.normalized;

        // Cast rays to detect hedge walls ahead
        Vector3 origin = currentPos + Vector3.up * 0.8f;
        const float feelerDist = 1.6f;

        // Raycast straight ahead
        if (Physics.Raycast(origin, desiredDir, out RaycastHit hitCenter, feelerDist, ~0, QueryTriggerInteraction.Ignore))
        {
            if (!hitCenter.collider.isTrigger && !hitCenter.collider.CompareTag("Player"))
            {
                // Wall ahead: steer perpendicular to wall normal
                Vector3 wallNormal = hitCenter.normal;
                wallNormal.y = 0f;
                Vector3 tangent = Vector3.Cross(Vector3.up, wallNormal);

                // Choose the tangent that best aligns with target
                if (Vector3.Dot(tangent, desiredDir) < 0f)
                    tangent = -tangent;

                return (tangent * 0.8f + wallNormal * 0.3f).normalized;
            }
        }

        // Slight feelers to the left and right
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

    void AnimateRun()
    {
        runCycle += Time.deltaTime * 10f;

        // Bob up and down while running
        if (modelRoot != null)
        {
            float bob = Mathf.Abs(Mathf.Sin(runCycle)) * 0.12f;
            float tilt = Mathf.Sin(runCycle * 0.5f) * 3f;
            modelRoot.localPosition = new Vector3(0f, bob, 0f);
            modelRoot.localEulerAngles = new Vector3(tilt, 0f, -tilt * 0.5f);
        }

        // Swing arms
        if (leftArm != null && rightArm != null)
        {
            float armSwing = Mathf.Sin(runCycle) * 28f;
            leftArm.localEulerAngles = new Vector3(armSwing, 0f, 15f);
            rightArm.localEulerAngles = new Vector3(-armSwing, 0f, -15f);
        }

        // Pulse red aura
        if (monsterLight != null)
        {
            monsterLight.intensity = 2.5f + Mathf.Sin(Time.time * 6f) * 0.8f;
        }
    }

    void CatchPlayer()
    {
        if (caught) return;
        caught = true;

        Debug.Log("[ForestMonster] Player caught! Triggering game over!");

        // Freeze player
        if (player != null)
            player.CanMove = false;

        // Trigger fail state on StageManager
        if (StageManager.Instance != null)
        {
            StageManager.Instance.FailStage("Attrapé par le Monstre !");
        }
    }

    // Builder helper to attach components
    public void Setup(Light light, Transform model, Transform lArm, Transform rArm)
    {
        monsterLight = light;
        modelRoot = model;
        leftArm = lArm;
        rightArm = rArm;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
    }
}
