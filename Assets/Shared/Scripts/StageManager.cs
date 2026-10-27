using System;
using System.Collections.Generic;
using UnityEngine;

public enum StageState { Playing, Won, Lost }

// The rules of one stage: timer, collected ingredients, score, win and lose.
// Other scripts (HUD, menus) listen to the events instead of changing anything here.
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    public StageConfig config;

    public StageState State { get; private set; } = StageState.Playing;
    public float TimeLeft { get; private set; }
    public int Score { get; private set; }
    public bool Brewing { get; private set; } // true while the player is at the cauldron

    public event Action<IngredientType> IngredientCollected;
    public event Action<float> BrewProgressed; // 0 to 1
    public event Action<int> StageWon;         // final score
    public event Action StageLost;

    readonly Dictionary<IngredientType, int> collected = new Dictionary<IngredientType, int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        TimeLeft = config.timeLimit;
    }

    void Update()
    {
        if (State != StageState.Playing) return;

        TimeLeft -= Time.deltaTime;
        if (TimeLeft <= 0f)
        {
            TimeLeft = 0f;
            State = StageState.Lost;
            StageLost?.Invoke();
        }
    }

    public int Required(IngredientType type)
    {
        foreach (RecipeEntry entry in config.recipe)
            if (entry.type == type) return entry.amount;
        return 0;
    }

    public int Collected(IngredientType type)
    {
        return collected.TryGetValue(type, out int n) ? n : 0;
    }

    public bool RecipeComplete
    {
        get
        {
            foreach (RecipeEntry entry in config.recipe)
                if (Collected(entry.type) < entry.amount) return false;
            return true;
        }
    }

    // Returns false when the ingredient is not needed, so it stays on the ground.
    public bool TryCollect(IngredientType type)
    {
        if (State != StageState.Playing) return false;
        if (Collected(type) >= Required(type)) return false;

        collected[type] = Collected(type) + 1;
        Score += config.pointsPerIngredient;
        IngredientCollected?.Invoke(type);
        return true;
    }

    public void SetBrewing(bool brewing)
    {
        Brewing = brewing;
    }

    public void ReportBrewProgress(float progress)
    {
        BrewProgressed?.Invoke(Mathf.Clamp01(progress));
    }

    public void CompleteBrew()
    {
        if (State != StageState.Playing || !RecipeComplete) return;

        State = StageState.Won;
        Score += config.potionBonus + Mathf.FloorToInt(TimeLeft) * config.pointsPerSecondLeft;
        StageWon?.Invoke(Score);
    }
}
