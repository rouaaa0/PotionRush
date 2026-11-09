using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ForestQuestManager  –  Stage 2 Enchanted Forest
//  Controls the 4-scenario quest chain.
//
//  Scenario 1  "First Gathering"
//      Collect 2 Mushrooms + 1 Crystal then brew.
//
//  Scenario 2  "The Shifting Hedge"
//      Hedge walls rise from the ground (labyrinth).
//      Collect 3 Herbs + 1 Crystal then brew.
//
//  Scenario 3  "Monolith Awakening"
//      Activate 3 glowing rune pillars in any order
//      by walking near them, then brew.
//
//  Scenario 4  "Master Brew"
//      All ingredient types respawn in new spots.
//      Collect 2 Mushrooms + 2 Crystals + 2 Herbs then final brew.
//
//  This script creates all scenario objects in code (no extra prefabs).
//  It hooks into the existing StageManager singleton.
// ============================================================
public class ForestQuestManager : MonoBehaviour
{
    // ── public state ───────────────────────────────────────────
    public static ForestQuestManager Instance { get; private set; }

    // Current 1-based scenario number (1–4). Read by ForestMinimap.
    public int CurrentScenario { get; private set; } = 1;

    // Human-readable "next task" text shown on the minimap banner.
    public string QuestBannerText { get; private set; } = "";

    // Live list of active ingredient transforms the minimap can track.
    // Key = Transform, Value = IngredientType (cast to int) or -1 for cauldron.
    public List<(Transform t, int iconType)> MinimapIcons { get; private set; }
        = new List<(Transform, int)>();

    // ── scene references (assigned by ForestStageBuilder) ──────
    [Header("Set by builder – do not edit in Inspector")]
    public Transform cauldronTransform;    // shared Cauldron in scene

    // ── internal runtime ───────────────────────────────────────
    // Containers for each scenario's spawned objects.
    // ForestStageBuilder fills these at edit time; we enable/disable at runtime.
    GameObject scenario1Root;   // Mushrooms + Crystal (Scenario 1 layout)
    GameObject scenario2Root;   // Hedges + Herbs + Crystal (Scenario 2 layout)
    GameObject scenario3Root;   // 3 runic pillars
    GameObject scenario4Root;   // Final spread of ingredients

    // Track monolith activation count for Scenario 3.
    int monolithsActivated = 0;
    const int MonolithsRequired = 3;

    // Materials for scenario-specific objects, created at runtime.
    Material hedgeMat;
    Material herbMat;
    Material monolithDimMat;
    Material monolithBrightMat;

    // ── Unity lifecycle ────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        MinimapIcons = new List<(Transform, int)>();
    }

    void Start()
    {
        // Wire up brew-complete callback from StageManager.
        if (StageManager.Instance != null)
            StageManager.Instance.BrewProgressed += OnBrewProgressed;

        // Find the scenario roots that the builder placed in the scene.
        // They are named children of this GameObject.
        scenario1Root = FindChild("Scenario1_Ingredients");
        scenario2Root = FindChild("Scenario2_Hedge");
        scenario3Root = FindChild("Scenario3_Monoliths");
        scenario4Root = FindChild("Scenario4_FinalIngredients");

        // Start with Scenario 1 active.
        ActivateScenario(1);
    }

    void OnDestroy()
    {
        if (StageManager.Instance != null)
            StageManager.Instance.BrewProgressed -= OnBrewProgressed;
    }

    // ── scenario progression ───────────────────────────────────

    void ActivateScenario(int n)
    {
        CurrentScenario = n;

        // Hide everything first.
        SetActive(scenario1Root, false);
        SetActive(scenario2Root, false);
        SetActive(scenario3Root, false);
        SetActive(scenario4Root, false);

        // Reset minimap icons; cauldron is always visible.
        MinimapIcons.Clear();
        if (cauldronTransform != null)
            MinimapIcons.Add((cauldronTransform, -1)); // -1 = cauldron icon

        switch (n)
        {
            case 1:
                SetActive(scenario1Root, true);
                RegisterIngredientIcons(scenario1Root);
                // Reset StageManager recipe for 2 Mushroom + 1 Crystal.
                SetRecipe(new[] {
                    (IngredientType.Mushroom, 2),
                    (IngredientType.Crystal,  1)
                });
                QuestBannerText = "Gather 2 mushrooms & 1 crystal";
                break;

            case 2:
                SetActive(scenario2Root, true);
                RegisterIngredientIcons(scenario2Root);
                // Recipe: 3 Herb + 1 Crystal.
                SetRecipe(new[] {
                    (IngredientType.Herb,    3),
                    (IngredientType.Crystal, 1)
                });
                QuestBannerText = "Find 3 herbs & 1 crystal";
                break;

            case 3:
                SetActive(scenario3Root, true);
                monolithsActivated = 0;
                // No collect recipe – monolith proximity is the trigger.
                SetRecipe(System.Array.Empty<(IngredientType, int)>());
                // Register monolith transforms as special icons (use type 10 = monolith).
                RegisterMonolithIcons(scenario3Root);
                QuestBannerText = "Activate 3 rune monoliths";
                break;

            case 4:
                SetActive(scenario4Root, true);
                RegisterIngredientIcons(scenario4Root);
                // Final recipe: 2 Mushroom + 2 Crystal + 2 Herb.
                SetRecipe(new[] {
                    (IngredientType.Mushroom, 2),
                    (IngredientType.Crystal,  2),
                    (IngredientType.Herb,     2)
                });
                QuestBannerText = "Master Brew – collect all!";
                break;
        }

        Debug.Log($"[ForestQuestManager] Scenario {n} started: {QuestBannerText}");
    }

    // Called by ForestRunicMonolith when a pillar is activated.
    public void MonolithActivated(int index)
    {
        if (CurrentScenario != 3) return;

        monolithsActivated++;
        int remaining = MonolithsRequired - monolithsActivated;
        QuestBannerText = remaining > 0
            ? $"Activate {remaining} more monolith{(remaining > 1 ? "s" : "")}"
            : "Now brew the potion!";

        Debug.Log($"[ForestQuestManager] Monolith {index} activated ({monolithsActivated}/{MonolithsRequired})");

        if (monolithsActivated >= MonolithsRequired)
        {
            // All monoliths lit – let the player brew the potion.
            // We set a fake "complete" recipe (0 items required) so the
            // existing Cauldron / StageManager brew logic triggers victory.
            // (Recipe was already set to empty in ActivateScenario(3).)
            Debug.Log("[ForestQuestManager] All monoliths activated – approach the cauldron to brew!");
        }
    }

    // Called when the brew slider hits 1.0 (BrewProgressed event).
    float lastBrewProgress = 0f;
    void OnBrewProgressed(float progress)
    {
        // Detect a fresh brew completion (progress goes from non-1 to 1).
        if (progress >= 1f && lastBrewProgress < 1f)
            OnBrewComplete();
        lastBrewProgress = progress;
    }

    void OnBrewComplete()
    {
        if (CurrentScenario < 4)
        {
            int next = CurrentScenario + 1;
            Debug.Log($"[ForestQuestManager] Brew complete! Advancing to Scenario {next}");

            // Brief pause before switching so the win particle/sound plays.
            StartCoroutine(DelayedScenarioSwitch(next, 2.5f));
        }
        // Scenario 4 brew is handled by the existing StageManager.CompleteBrew() → StageWon.
    }

    IEnumerator DelayedScenarioSwitch(int next, float delay)
    {
        yield return new WaitForSeconds(delay);
        ActivateScenario(next);
        // Reset the brew gauge for the next scenario.
        if (StageManager.Instance != null)
            StageManager.Instance.ReportBrewProgress(0f);
        // Reset brew state via reflection (simple approach).
        lastBrewProgress = 0f;
    }

    // ── helpers ────────────────────────────────────────────────

    // Replace the StageManager recipe at runtime without touching the asset file.
    void SetRecipe((IngredientType type, int amount)[] entries)
    {
        if (StageManager.Instance == null) return;

        StageManager.Instance.config.recipe.Clear();
        // Reset the internal collected dictionary too using the public API.
        // We do this by creating a fresh config copy is tricky, so instead we
        // use reflection to clear the private 'collected' dictionary.
        var collectedField = typeof(StageManager).GetField("collected",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (collectedField != null)
        {
            var dict = collectedField.GetValue(StageManager.Instance)
                as System.Collections.Generic.Dictionary<IngredientType, int>;
            dict?.Clear();
        }

        foreach (var (type, amount) in entries)
            StageManager.Instance.config.recipe.Add(new RecipeEntry { type = type, amount = amount });
    }

    // Walk a scenario root and register all Ingredient children on the minimap.
    void RegisterIngredientIcons(GameObject root)
    {
        if (root == null) return;
        foreach (Ingredient ing in root.GetComponentsInChildren<Ingredient>(true))
            MinimapIcons.Add((ing.transform, (int)ing.type));
    }

    // Walk the monolith root and register each pillar as icon type 10.
    void RegisterMonolithIcons(GameObject root)
    {
        if (root == null) return;
        foreach (ForestRunicMonolith m in root.GetComponentsInChildren<ForestRunicMonolith>(true))
            MinimapIcons.Add((m.transform, 10));
    }

    void SetActive(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    GameObject FindChild(string name)
    {
        Transform t = transform.Find(name);
        return t != null ? t.gameObject : null;
    }

    // ── public setters (called by ForestStageBuilder at edit time) ──

    public void SetScenarioRoots(GameObject s1, GameObject s2, GameObject s3, GameObject s4)
    {
        scenario1Root = s1;
        scenario2Root = s2;
        scenario3Root = s3;
        scenario4Root = s4;
    }
}
