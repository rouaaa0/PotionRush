using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds a menu item that builds the main menu scene (lab backdrop + buttons)
// and puts the menu and Stage 1 in the Build Settings.
public static class MainMenuBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string Stage1Path = "Assets/Stages/Stage1_Lab/Stage1_Lab.unity";

    static readonly Color Gold = new Color(0.95f, 0.71f, 0.27f, 1f);
    static readonly Color Dark = new Color(0.09f, 0.055f, 0.14f, 0.95f);

    [MenuItem("Potion Rush/Build Main Menu")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // backdrop: the lab, seen from a slowly circling camera
        Camera cam = Camera.main;
        cam.fieldOfView = 38f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.05f, 0.11f);
        cam.gameObject.AddComponent<MenuCameraOrbit>();

        PlacePrefab("Assets/Shared/Prefabs/LabRoom.prefab", Vector3.zero);
        PlacePrefab("Assets/Shared/Prefabs/LabDecor.prefab", Vector3.zero);
        PlacePrefab("Assets/Shared/Prefabs/Cauldron.prefab", new Vector3(1.6f, 0f, 3.5f));

        // interface
        GameObject root = new GameObject("MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(root.transform, false);

        MainMenu menu = root.AddComponent<MainMenu>();

        Text title = HUDBuilder.MakeText(root.transform, "Title", "Potion Rush", font, 150, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(1500f, 200f));
        title.color = Gold;
        Text subtitle = HUDBuilder.MakeText(root.transform, "Subtitle", "Prépare la potion avant la fin du temps", font, 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1500f, 70f));
        subtitle.color = new Color(0.9f, 0.86f, 0.96f, 1f);

        menu.playButton = MakeButton(root.transform, "PlayButton", "Jouer", font, new Vector2(0f, 10f), true);
        menu.instructionsButton = MakeButton(root.transform, "InstructionsButton", "Instructions", font, new Vector2(0f, -110f), false);
        menu.quitButton = MakeButton(root.transform, "QuitButton", "Quitter", font, new Vector2(0f, -230f), false);

        // instructions window
        GameObject panel = HUDBuilder.MakeImage(root.transform, "InstructionsPanel", new Color(0.04f, 0.02f, 0.08f, 0.75f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject card = HUDBuilder.MakeImage(panel.transform, "Card", Dark, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 720f));
        Text cardTitle = HUDBuilder.MakeText(card.transform, "Title", "Comment jouer", font, 72, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 100f));
        cardTitle.color = Gold;
        HUDBuilder.MakeText(card.transform, "Text",
            "Inclinez votre téléphone pour déplacer le sorcier.\n\n" +
            "Marchez sur les ingrédients pour les ramasser.\n\n" +
            "Allez jusqu'au chaudron et secouez\nvotre téléphone pour préparer la potion.\n\n" +
            "Attention : si le temps s'écoule, le chaudron explose !",
            font, 40, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(1000f, 420f));
        menu.closeButton = MakeButton(card.transform, "CloseButton", "Fermer", font, Vector2.zero, true);
        RectTransform closeRect = menu.closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 40f);
        menu.instructionsPanel = panel;

        EditorSceneManager.SaveScene(scene, ScenePath);

        // the game needs to know both scenes so one can load the other
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(Stage1Path, true),
        };

        Debug.Log("Main menu created and saved to " + ScenePath + ". Press Play, then click Jouer.");
    }

    static void PlacePrefab(string path, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            Debug.LogWarning("Missing prefab: " + path);
            return;
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
    }

    static Button MakeButton(Transform parent, string name, string label, Font font, Vector2 position, bool primary)
    {
        GameObject go = HUDBuilder.MakeImage(parent, name, primary ? Gold : new Color(1f, 1f, 1f, 0.14f),
            new Vector2(0.5f, 0.5f), position, new Vector2(460f, 100f));
        Button button = go.AddComponent<Button>();
        Text text = HUDBuilder.MakeText(go.transform, "Label", label, font, 54, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 100f));
        text.color = primary ? new Color(0.23f, 0.12f, 0f, 1f) : Color.white;
        return button;
    }
}
