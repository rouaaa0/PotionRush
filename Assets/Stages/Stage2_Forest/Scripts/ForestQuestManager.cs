using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================
//  ForestQuestManager  –  Stage 2 Enchanted Forest
//  Controls the 3-scenario quest chain with 3-second countdowns
//  and tactical monster guard behaviors.
//
//  Scenario 1  "First Gathering"
//      Collect 2 Mushrooms + 1 Crystal then brew.
//      -> 3s animated countdown before Scenario 2!
//
//  Scenario 2  "The Cauldron Guardian"
//      Hedge labyrinth. Monster guards/patrols around the Cauldron.
//      Wizard must gather 3 Herbs + 1 Crystal and sneak past or
//      lure the monster away to brew!
//      -> 3s animated countdown before Scenario 3!
//
//  Scenario 3  "Master Brew"
//      Collect all 6 ingredients spread through the forest and final brew!
//      (uses the "Scenario4_FinalIngredients" objects in the scene)
//
//  The old monolith scenario was removed: its "Scenario3_Monoliths" objects
//  stay hidden for the whole stage.
// ============================================================
public class ForestQuestManager : MonoBehaviour
{
    public static ForestQuestManager Instance { get; private set; }

    public int CurrentScenario { get; private set; } = 1;
    public string QuestBannerText { get; private set; } = "";

    public List<(Transform t, int iconType)> MinimapIcons { get; private set; }
        = new List<(Transform, int)>();

    [Header("Set by builder")]
    public Transform cauldronTransform;
    public ForestMonster monster;

    // Brewing this scenario wins the whole stage (wings + flight + Victoire card with Suivant).
    // Scenario 2 is the last one now; set this back to 3 to bring the Master Brew back.
    public const int LastScenario = 2;

    // true during the "Scenario réussi" + countdown pause between two scenarios
    public bool IsTransitioning => isTransitioning;

    GameObject scenario1Root;
    GameObject scenario2Root;
    GameObject finalScenarioRoot;
    GameObject unusedMonolithsRoot; // removed scenario, always kept hidden

    bool isTransitioning = false;

    // Checkpoint: "static" values survive the scene reload done by the Rejouer button.
    // When the player loses, Rejouer restarts the scenario they were on instead of Scenario 1.
    static int checkpointScenario = 1;
    static int checkpointScore = 0;
    static bool resumeFromCheckpoint = false;
    static string forestSceneName;

    // Any time another scene opens (menu, stage select, next stage) the checkpoint is forgotten,
    // so coming back to the forest later starts again from Scenario 1.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ListenForSceneChanges()
    {
        checkpointScenario = 1;
        checkpointScore = 0;
        resumeFromCheckpoint = false;
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != forestSceneName) resumeFromCheckpoint = false;
    }

    void OnStageLost()
    {
        resumeFromCheckpoint = true; // checkpointScenario/checkpointScore were saved when the scenario began
    }

    void Awake()
    {
        Instance = this;
        MinimapIcons = new List<(Transform, int)>();

        // invisible walls around the ground so the player can't fall off the map
        if (GetComponent<ForestBounds>() == null) gameObject.AddComponent<ForestBounds>();
    }

    void Start()
    {
        Cauldron.CustomBrewCompleted = OnCustomBrewCompleted;
        Cauldron.CanBrewCheck = CanCauldronBrew;

        scenario1Root = FindChild("Scenario1_Ingredients");
        scenario2Root = FindChild("Scenario2_Hedge");
        unusedMonolithsRoot = FindChild("Scenario3_Monoliths");
        finalScenarioRoot = FindChild("Scenario4_FinalIngredients");

        if (monster == null)
            monster = FindFirstObjectByType<ForestMonster>(FindObjectsInactive.Include);

        forestSceneName = gameObject.scene.name;
        if (StageManager.Instance != null) StageManager.Instance.StageLost += OnStageLost;

        int startScenario = 1;
        if (resumeFromCheckpoint)
        {
            resumeFromCheckpoint = false;
            startScenario = checkpointScenario;
            SetScore(checkpointScore); // keep the points earned in the scenarios already finished
        }

        ActivateScenario(startScenario);
    }

    static void SetScore(int value)
    {
        if (StageManager.Instance == null) return;
        var scoreProp = typeof(StageManager).GetProperty("Score");
        if (scoreProp != null && scoreProp.CanWrite)
            scoreProp.SetValue(StageManager.Instance, value);
    }

    void OnDestroy()
    {
        if (StageManager.Instance != null) StageManager.Instance.StageLost -= OnStageLost;
        Cauldron.CustomBrewCompleted = null;
        Cauldron.CanBrewCheck = null;
    }

    bool CanCauldronBrew()
    {
        return !isTransitioning;
    }

    void OnCustomBrewCompleted(Cauldron cauldron)
    {
        if (isTransitioning) return;

        if (CurrentScenario < LastScenario)
        {
            StartCoroutine(ScenarioTransitionRoutine(cauldron));
        }
        else
        {
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

        Debug.Log($"[ForestQuestManager] Scenario {completedScenario} completed! Starting 3-second countdown for Scenario {next}...");

        // 1. Mini-celebration burst at cauldron
        if (cauldron != null)
            cauldron.CelebrateMini();

        // the ghost leaves while the player is frozen for the countdown (the next scenario brings it back if needed)
        if (monster != null) monster.gameObject.SetActive(false);

        // 2. Award bonus points
        if (StageManager.Instance != null)
        {
            var scoreProp = typeof(StageManager).GetProperty("Score");
            if (scoreProp != null && scoreProp.CanWrite)
                scoreProp.SetValue(StageManager.Instance, StageManager.Instance.Score + 100);
        }

        QuestBannerText = $"Scenario {completedScenario} Réussi ! ✨";

        // 3. Brief moment for celebration particles
        yield return new WaitForSeconds(1.8f);

        // 4. Reset cauldron and brew state
        if (cauldron != null)
            cauldron.ResetBrew();

        if (StageManager.Instance != null)
        {
            StageManager.Instance.ReportBrewProgress(0f);
            StageManager.Instance.SetBrewing(false);
        }

        // 5. Freeze player during the 3-second countdown
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.CanMove = false;

        // 6. 3-second countdown on screen
        if (ForestMinimap.Instance != null)
        {
            string title = "";
            string subtitle = "";

            if (next == 2)
            {
                title = "SCÉNARIO 2 : LE FANTÔME DU CHAUDRON";
                subtitle = "Le petit fantôme hante le chaudron ! Récoltez les herbes et semez-le lentement !";
            }
            else if (next == 3)
            {
                title = "SCÉNARIO 3 : LE BRASSAGE FINAL";
                subtitle = "Rassemblez tous les ingrédients disséminés pour la potion suprême !";
            }

            yield return ForestMinimap.Instance.PlayCountdownRoutine(3, title, subtitle);
        }
        else
        {
            yield return new WaitForSeconds(3.0f);
        }

        // 7. Unfreeze player and launch the scenario
        if (player != null) player.CanMove = true;
        isTransitioning = false;

        ActivateScenario(next);
    }

    void ActivateScenario(int n)
    {
        CurrentScenario = n;

        // save the checkpoint: losing from now on restarts this scenario with this score
        checkpointScenario = n;
        checkpointScore = StageManager.Instance != null ? StageManager.Instance.Score : 0;

        // Hide all scenario specific roots first
        SetActive(scenario1Root, false);
        SetActive(scenario2Root, false);
        SetActive(unusedMonolithsRoot, false);
        SetActive(finalScenarioRoot, false);

        MinimapIcons.Clear();
        if (cauldronTransform != null)
            MinimapIcons.Add((cauldronTransform, -1)); // -1 = cauldron icon

        if (monster == null)
            monster = FindFirstObjectByType<ForestMonster>(FindObjectsInactive.Include);

        switch (n)
        {
            case 1:
                if (monster != null) monster.gameObject.SetActive(false);

                SetActive(scenario1Root, true);
                RegisterIngredientIcons(scenario1Root);
                SetRecipe(new[] {
                    (IngredientType.Mushroom, 2),
                    (IngredientType.Crystal,  1)
                });
                QuestBannerText = "Gather 2 mushrooms & 1 crystal";
                break;

            case 2:
                // Scenario 2: Hedge labyrinth + Monster guards the cauldron
                SetActive(scenario2Root, true);
                RegisterIngredientIcons(scenario2Root);

                if (monster != null)
                {
                    monster.gameObject.SetActive(true);
                    Vector3 cPos = cauldronTransform != null ? cauldronTransform.position : new Vector3(0f, 0f, -2.5f);
                    monster.SetMode(MonsterMode.GuardCauldron, cPos);
                    MinimapIcons.Add((monster.transform, 99)); // 99 = Monster threat
                }

                SetRecipe(new[] {
                    (IngredientType.Herb,    3),
                    (IngredientType.Crystal, 1)
                });
                QuestBannerText = "Évitez le fantôme du chaudron ! (3 herbes & 1 cristal)";
                break;

            case 3:
                // Scenario 3: Master gather (final)
                if (monster != null) monster.gameObject.SetActive(false);

                SetActive(finalScenarioRoot, true);
                RegisterIngredientIcons(finalScenarioRoot);
                SetRecipe(new[] {
                    (IngredientType.Mushroom, 2),
                    (IngredientType.Crystal,  2),
                    (IngredientType.Herb,     2)
                });
                QuestBannerText = "Master Brew – collectez tout !";
                break;
        }

        Debug.Log($"[ForestQuestManager] Scenario {n} active: {QuestBannerText}");
    }

    // The monolith scenario was removed; ForestRunicMonolith still calls this, so it does nothing.
    public void MonolithActivated(int index) { }

    void SetRecipe((IngredientType type, int amount)[] entries)
    {
        if (StageManager.Instance == null) return;

        StageManager.Instance.config.recipe.Clear();

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

        HUD hud = FindFirstObjectByType<HUD>();
        if (hud != null) hud.RebuildObjectives();
    }

    void RegisterIngredientIcons(GameObject root)
    {
        if (root == null) return;
        foreach (Ingredient ing in root.GetComponentsInChildren<Ingredient>(true))
            MinimapIcons.Add((ing.transform, (int)ing.type));
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
