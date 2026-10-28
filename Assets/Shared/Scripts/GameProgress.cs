using UnityEngine;

// What the player has achieved so far, saved on the device (PlayerPrefs).
// Stage 1 is always open; a stage opens when the one before it has been won.
// Stages whose scene is not made yet are skipped, so the team can build them in any order.
public static class GameProgress
{
    const string UnlockAllKey = "unlock_all";

    // Demo switch: opens every stage. Toggled by tapping the title in the main menu 5 times.
    public static bool UnlockAll
    {
        get { return PlayerPrefs.GetInt(UnlockAllKey, 0) == 1; }
        set
        {
            PlayerPrefs.SetInt(UnlockAllKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static int BestScore(string sceneName)
    {
        return PlayerPrefs.GetInt("best_" + sceneName, 0);
    }

    public static bool HasWon(string sceneName)
    {
        return PlayerPrefs.GetInt("won_" + sceneName, 0) == 1;
    }

    public static void RecordWin(string sceneName, int score)
    {
        if (score > BestScore(sceneName)) PlayerPrefs.SetInt("best_" + sceneName, score);
        PlayerPrefs.SetInt("won_" + sceneName, 1);
        PlayerPrefs.Save();
    }

    public static bool IsUnlocked(StageCatalog catalog, int index)
    {
        if (UnlockAll) return true;

        for (int i = index - 1; i >= 0; i--)
        {
            string previous = catalog.stages[i].sceneName;
            if (Application.CanStreamedLevelBeLoaded(previous)) return HasWon(previous);
        }
        return true; // no playable stage before this one
    }

    // The first stage after this one whose scene exists, or null if there is none yet.
    public static string NextStage(StageCatalog catalog, string sceneName)
    {
        int index = catalog.IndexOf(sceneName);
        if (index < 0) return null;

        for (int i = index + 1; i < catalog.stages.Count; i++)
        {
            string next = catalog.stages[i].sceneName;
            if (Application.CanStreamedLevelBeLoaded(next)) return next;
        }
        return null;
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
