using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct StageInfo
{
    public string displayName;
    public string potionName;
    public string sceneName; // must match the name of the stage's scene file
}

// The list of all stages, in play order. The stage select screen and the "next stage"
// button both read it. One asset: Assets/Shared/Data/StageCatalog.asset
[CreateAssetMenu(menuName = "Potion Rush/Stage Catalog")]
public class StageCatalog : ScriptableObject
{
    public List<StageInfo> stages = new List<StageInfo>();

    public int IndexOf(string sceneName)
    {
        for (int i = 0; i < stages.Count; i++)
            if (stages[i].sceneName == sceneName) return i;
        return -1;
    }
}
