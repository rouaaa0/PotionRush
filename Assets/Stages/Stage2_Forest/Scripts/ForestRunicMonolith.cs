using UnityEngine;

// ============================================================
//  ForestRunicMonolith  –  Stage 2 Scenario 3
//  Attach this to each runic pillar root.
//  When the player walks within activateRadius metres the pillar
//  "awakens": its point light changes colour and ForestQuestManager
//  is notified.  Once activated it cannot be deactivated.
// ============================================================
public class ForestRunicMonolith : MonoBehaviour
{
    [Header("Which monolith is this? (0, 1, 2 …)")]
    public int monolithIndex = 0;

    [Header("Activation distance in metres")]
    public float activateRadius = 2.5f;

    // Visual components – assigned in Start() from child objects.
    Light pointLight;
    Renderer[] stoneRenderers;
    PlayerController player;

    // State
    bool activated = false;

    // Colours
    static readonly Color DimColor   = new Color(0.1f, 0.4f, 0.2f);  // dark green
    static readonly Color BrightColor = new Color(0.2f, 1.0f, 0.4f); // vivid green

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();

        // Grab point light and all renderers on this pillar.
        pointLight    = GetComponentInChildren<Light>();
        stoneRenderers = GetComponentsInChildren<Renderer>();

        // Start dim.
        if (pointLight != null)
        {
            pointLight.color     = DimColor;
            pointLight.intensity = 1.0f;
            pointLight.range     = 3.0f;
        }
    }

    void Update()
    {
        if (activated) return;

        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
        }

        float dist = Vector3.Distance(transform.position, player.transform.position);
        if (dist <= activateRadius)
            Activate();
    }

    void Activate()
    {
        activated = true;

        // Light up the pillar.
        if (pointLight != null)
        {
            pointLight.color     = BrightColor;
            pointLight.intensity = 4.0f;
            pointLight.range     = 6.0f;
        }

        // Flash the stone mesh emission (if the material supports it).
        foreach (Renderer r in stoneRenderers)
        {
            // We use a property block so we don't create a material instance leak.
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            r.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", BrightColor * 2f);
            r.SetPropertyBlock(block);
        }

        // Notify the quest manager.
        if (ForestQuestManager.Instance != null)
            ForestQuestManager.Instance.MonolithActivated(monolithIndex);

        Debug.Log($"[ForestRunicMonolith] Monolith {monolithIndex} activated!");
    }

    // Draw a wire sphere in the editor so the designer can see the trigger zone.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = activated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activateRadius);
    }
}
