using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct RecipeEntry
{
    public IngredientType type;
    public int amount;
}

// The settings of one stage. Every stage has its own StageConfig asset
// (Assets/Shared/Data); the game rules read everything from here.
[CreateAssetMenu(menuName = "Potion Rush/Stage Config")]
public class StageConfig : ScriptableObject
{
    public string stageName = "Stage";
    public string potionName = "Potion";
    public float timeLimit = 120f;
    public int shakesRequired = 10;
    public bool showTutorialHints = false; // on for stage 1 only
    public List<RecipeEntry> recipe = new List<RecipeEntry>();

    [Header("Score")]
    public int pointsPerIngredient = 10;
    public int potionBonus = 100;
    public int pointsPerSecondLeft = 1;
}
