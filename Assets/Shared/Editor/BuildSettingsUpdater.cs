using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Unity can only load a scene by name if it is listed in the Build Settings.
// This puts the main menu and every stage from the StageCatalog there, in order.
// Teammates run it once after adding their stage scene.
public static class BuildSettingsUpdater
{
    const string MenuScene = "Assets/Scenes/MainMenu.unity";
    internal const string CatalogPath = "Assets/Shared/Data/StageCatalog.asset";

    [MenuItem("Potion Rush/Update Build Settings")]
    static void Run()
    {
        Apply();
    }

    internal static void Apply()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        List<string> names = new List<string>();

        if (File.Exists(MenuScene))
        {
            scenes.Add(new EditorBuildSettingsScene(MenuScene, true));
            names.Add("MainMenu");
        }

        StageCatalog catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
        if (catalog != null)
        {
            foreach (StageInfo info in catalog.stages)
            {
                string path = FindScene(info.sceneName);
                if (path == null) continue;
                scenes.Add(new EditorBuildSettingsScene(path, true));
                names.Add(info.sceneName);
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("Build Settings updated: " + string.Join(", ", names));
    }

    static string FindScene(string sceneName)
    {
        foreach (string guid in AssetDatabase.FindAssets(sceneName + " t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) return path;
        }
        return null;
    }
}
