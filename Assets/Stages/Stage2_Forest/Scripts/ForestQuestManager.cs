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
// ============================================================
public class ForestQuestManager : MonoBehaviour
{
    // ── public state ───────────────────────────────────────────
    public static ForestQuestManager Instance { get; private set; }

    public int CurrentScenario { get; private set; } = 1;
    public string QuestBannerText { get; private set; } = "";

    // Live list of active ingredient transforms tracked by ForestMinimap.
    // Key = Transform, Value = icon type:
    // (int)IngredientType (0=Mush, 1=Cryst, 2=Herb), -1=Cauldron, 10=Monolith
    public List<(Transform t, int iconType)> MinimapIcons { get; private set; }
        = new List<(Transform, int)>();

    [Header("Set by builder")]
    public Transform cauldronTransform;

    GameObject scenario1Root;
    GameObject scenario2Root;
    GameObject scenario3Root;
    GameObject scenario4Root;

    int monolithsActivated = 0;
    const int MonolithsRequired = 3;
    bool isTransitioning = false;

    void Awake()
    {
        Instance = this;
        MinimapIcons = new List<(Transform, int)>();
    }

    void Start()
    {
        // Intercept Cauldron completion so Scenario 1-3 don't trigger the end game screen!
        Cauldron.CustomBrewCompleted = OnCustomBrewCompleted;
        Cauldron.CanBrewCheck = CanCauldronBrew;

        scenario1Root = FindChild("Scenario1_Ingredients");
        scenario2Root = FindChild("Scenario2_Hedge");
        scenario3Root = FindChild("Scenario3_Monoliths");
        scenario4Root = FindChild("Scenario4_FinalIngredients");

        ActivateScenario(1);
    }

    void OnDestroy()
    {
        Cauldron.CustomBrewCompleted = null;
        Cauldron.CanBrewCheck = null;
    }

    bool CanCauldronBrew()
    {
        if (isTransitioning) return false;

        // In Scenario 3, player must awaken all 3 monoliths first!
        if (CurrentScenario == 3)
            return monolithsActivated >= MonolithsRequired;

        return true;
    }

    void OnCustomBrewCompleted(Cauldron cauldron)
    {
        if (isTransitioning) return;

        if (CurrentScenario < 4)
        {
            // Complete current scenario with celebration, then progress to next!
            StartCoroutine(ScenarioTransitionRoutine(cauldron));
        }
        else
        {
            // Scenario 4: The final master brew! Trigger true stage victory!
            if (StageManager.Instance != null)
            {
                StageManager.Instance.CompleteBrew();
            }
        }
    }

    IEnumerator ScenarioTransitionRoutine(Cauldron cauldron)
    {
        isTransitioning = true;
        int completedScenario = CurrentScenario;
        int next = CurrentScenario + 1;

        Debug.Log($"[ForestQuestManager] Scenario {completedScenario} completed! Preparing Scenario {next}...");

        // 1. Mini-celebration burst at cauldron
        if (cauldron != null)
            cauldron.CelebrateMini();

        // 2. Award bonus points
        if (StageManager.Instance != null)
        {
            var scoreProp = typeof(StageManager).GetProperty("Score");
            if (scoreProp != null && scoreProp.CanWrite)
                scoreProp.SetValue(StageManager.Instance, StageManager.Instance.Score + 100);
        }

        QuestBannerText = $"Scenario {completedScenario} Complete! ✨";

        // 3. Wait for celebration particles
        yield return new WaitForSeconds(2.2f);

        // 4. Reset cauldron and brew state
        if (cauldron != null)
            cauldron.ResetBrew();

        if (StageManager.Instance != null)
        {
            StageManager.Instance.ReportBrewProgress(0f);
            StageManager.Instance.SetBrewing(false);
        }

        // 5. Restore player movement
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.CanMove = true;

        // 6. Launch next scenario
        isTransitioning = false;
        ActivateScenario(next);
    }

    void ActivateScenario(int n)
    {
        CurrentScenario = n;

        // Hide all scenario objects first
        SetActive(scenario1Root, false);
        SetActive(scenario2Root, false);
        SetActive(scenario3Root, false);
        SetActive(scenario4Root, false);

        MinimapIcons.Clear();
        if (cauldronTransform != null)
            MinimapIcons.Add((cauldronTransform, -1)); // -1 = cauldron icon

        switch (n)
        {
            case 1:
                SetActive(scenario1Root, true);
                RegisterIngredientIcons(scenario1Root);
                SetRecipe(new[] {
                    (IngredientType.Mushroom, 2),
                    (IngredientType.Crystal,  1)
                });
                QuestBannerText = "Gather 2 mushrooms & 1 crystal";
                break;

            case 2:
                SetActive(scenario2Root, true);
                RegisterIngredientIcons(scenario2Root);
                // Register the monster on the minimap (icon type 99)
                ForestMonster monster = scenario2Root.GetComponentInChildren<ForestMonster>(true);
                if (monster != null)
                    MinimapIcons.Add((monster.transform, 99));

                SetRecipe(new[] {
                    (IngredientType.Herb,    3),
                    (IngredientType.Crystal, 1)
                });
                QuestBannerText = "Fuyez le monstre ! (3 herbes & 1 cristal)";
                break;

            case 3:
                SetActive(scenario3Root, true);
                monolithsActivated = 0;
                // Empty recipe so RecipeComplete evaluates to true, but CanBrewCheck requires 3 monoliths
                SetRecipe(System.Array.Empty<(IngredientType, int)>());
                RegisterMonolithIcons(scenario3Root);
                QuestBannerText = "Activate 3 rune monoliths";
                break;

            case 4:
                SetActive(scenario4Root, true);
                RegisterIngredientIcons(scenario4Root);
                SetRecipe(new[] {
                    (IngredientType.Mushroom, 2),
                    (IngredientType.Crystal,  2),
                    (IngredientType.Herb,     2)
                });
                QuestBannerText = "Master Brew – collect all!";
                break;
        }

        Debug.Log($"[ForestQuestManager] Scenario {n} active: {QuestBannerText}");
    }

    public void MonolithActivated(int index)
    {
        if (CurrentScenario != 3) return;

        monolithsActivated++;
        int remaining = MonolithsRequired - monolithsActivated;
        QuestBannerText = remaining > 0
            ? $"Activate {remaining} more monolith{(remaining > 1 ? "s" : "")}"
            : "All active! Brew at cauldron!";

        Debug.Log($"[ForestQuestManager] Monolith {index} activated ({monolithsActivated}/{MonolithsRequired})");
    }

    void SetRecipe((IngredientType type, int amount)[] entries)
    {
        if (StageManager.Instance == null) return;

        StageManager.Instance.config.recipe.Clear();

        // Clear the internal collected dictionary using reflection
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

        // Refresh the HUD Objectives card immediately!
        HUD hud = FindFirstObjectByType<HUD>();
        if (hud != null) hud.RebuildObjectives();
    }

    void RegisterIngredientIcons(GameObject root)
    {
        if (root == null) return;
        foreach (Ingredient ing in root.GetComponentsInChildren<Ingredient>(true))
            MinimapIcons.Add((ing.transform, (int)ing.type));
    }

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
}
