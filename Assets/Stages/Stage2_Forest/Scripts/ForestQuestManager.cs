using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================
//  ForestQuestManager  –  Stage 2 Enchanted Forest
//  Controls the 4-scenario quest chain with 5-second countdowns
//  and tactical monster guard behaviors.
//
//  Scenario 1  "First Gathering"
//      Collect 2 Mushrooms + 1 Crystal then brew.
//      -> 5s animated countdown before Scenario 2!
//
//  Scenario 2  "The Cauldron Guardian"
//      Hedge labyrinth. Monster guards/patrols around the Cauldron.
//      Wizard must gather 3 Herbs + 1 Crystal and sneak past or
//      lure the monster away to brew!
//      -> 5s animated countdown before Scenario 3!
//
//  Scenario 3  "The Guarded Relics"
//      Monster actively patrols between the 3 rune monoliths.
//      Wizard must activate all 3 monoliths without touching the monster!
//      Once active, brew at the cauldron to finish!
//
//  Scenario 4  "Master Brew"
//      Collect all 6 ingredients spread through the forest and final brew!
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
        Cauldron.CustomBrewCompleted = OnCustomBrewCompleted;
        Cauldron.CanBrewCheck = CanCauldronBrew;

        scenario1Root = FindChild("Scenario1_Ingredients");
        scenario2Root = FindChild("Scenario2_Hedge");
        scenario3Root = FindChild("Scenario3_Monoliths");
        scenario4Root = FindChild("Scenario4_FinalIngredients");

        if (monster == null)
            monster = FindFirstObjectByType<ForestMonster>(FindObjectsInactive.Include);

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

        Debug.Log($"[ForestQuestManager] Scenario {completedScenario} completed! Starting 5-second countdown for Scenario {next}...");

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

        // 5. Freeze player during the 5-second countdown
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null) player.CanMove = false;

        // 6. 5-second countdown on screen
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
                title = "SCÉNARIO 3 : LES MONOLITHES HANTÉS";
                subtitle = "Le fantôme hante les 3 monolithes ! Activez-les sans vous faire toucher !";
            }
            else if (next == 4)
            {
                title = "SCÉNARIO 4 : LE BRASSAGE FINAL";
                subtitle = "Rassemblez tous les ingrédients disséminés pour la potion suprême !";
            }

            yield return ForestMinimap.Instance.PlayCountdownRoutine(5, title, subtitle);
        }
        else
        {
            yield return new WaitForSeconds(5.0f);
        }

        // 7. Unfreeze player and launch the scenario
        if (player != null) player.CanMove = true;
        isTransitioning = false;

        ActivateScenario(next);
    }

    void ActivateScenario(int n)
    {
        CurrentScenario = n;

        // Hide all scenario specific roots first
        SetActive(scenario1Root, false);
        SetActive(scenario2Root, false);
        SetActive(scenario3Root, false);
        SetActive(scenario4Root, false);

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
                // Scenario 3: Monoliths + Monster patrols between them
                SetActive(scenario3Root, true);
                monolithsActivated = 0;
                SetRecipe(System.Array.Empty<(IngredientType, int)>());
                RegisterMonolithIcons(scenario3Root);

                if (monster != null)
                {
                    monster.gameObject.SetActive(true);
                    monster.SetMode(MonsterMode.GuardObjectives, Vector3.zero, new Vector3[] {
                        new Vector3( 0f, 0f, -6f),
                        new Vector3(-6f, 0f,  4f),
                        new Vector3( 6f, 0f,  4f)
                    });
                    MinimapIcons.Add((monster.transform, 99));
                }

                QuestBannerText = "Activez 3 monolithes sans toucher le fantôme !";
                break;

            case 4:
                // Scenario 4: Master gather
                if (monster != null) monster.gameObject.SetActive(false);

                SetActive(scenario4Root, true);
                RegisterIngredientIcons(scenario4Root);
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

    public void MonolithActivated(int index)
    {
        if (CurrentScenario != 3) return;

        monolithsActivated++;
        int remaining = MonolithsRequired - monolithsActivated;
        QuestBannerText = remaining > 0
            ? $"Activez encore {remaining} monolithe{(remaining > 1 ? "s" : "")} !"
            : "Tous allumés ! Foncez au chaudron brasser !";

        Debug.Log($"[ForestQuestManager] Monolith {index} activated ({monolithsActivated}/{MonolithsRequired})");
    }

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
