using UnityEngine;

// What the player has achieved so far, saved on the device (PlayerPrefs).
// Stage 1 is always open; a stage opens when the one before it has been won.
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
        if (UnlockAll || index <= 0) return true;
        return HasWon(catalog.stages[index - 1].sceneName);
    }

    public static void ResetAll()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}
