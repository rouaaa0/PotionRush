using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds a menu item that builds the main menu scene (lab backdrop, buttons, stage select)
// and puts the menu and the stages in the Build Settings.
public static class MainMenuBuilder
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";

    static readonly Color Gold = new Color(0.95f, 0.71f, 0.27f, 1f);

    [MenuItem("Potion Rush/Build Main Menu")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        StageCatalog catalog = GetOrCreateCatalog();

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
        scaler.matchWidthOrHeight = 1f; // match the height: wide phones get extra width, nothing is cut off

        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(root.transform, false);

        MainMenu menu = root.AddComponent<MainMenu>();

        Text title = HUDBuilder.MakeText(root.transform, "Title", "Potion Rush", font, 150, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 240f), new Vector2(1000f, 200f));
        title.color = Gold;
        title.raycastTarget = true; // for the hidden "tap 5 times" demo switch
        Button titleButton = title.gameObject.AddComponent<Button>();
        titleButton.transition = Selectable.Transition.None;
        menu.titleButton = titleButton;

        Text subtitle = HUDBuilder.MakeText(root.transform, "Subtitle", "", font, 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 130f), new Vector2(1500f, 70f));
        subtitle.color = new Color(0.9f, 0.86f, 0.96f, 1f);
        menu.subtitleText = subtitle;

        menu.playButton = MakeButton(root.transform, "PlayButton", "Jouer", font, new Vector2(0f, 10f), true);
        menu.instructionsButton = MakeButton(root.transform, "InstructionsButton", "Instructions", font, new Vector2(0f, -110f), false);
        menu.quitButton = MakeButton(root.transform, "QuitButton", "Quitter", font, new Vector2(0f, -230f), false);

        // stage select
        GameObject select = MakeFullScreenPanel(root.transform, "StageSelectPanel", new Color(0.04f, 0.02f, 0.08f, 0.9f));
        Text selectTitle = HUDBuilder.MakeText(select.transform, "Title", "Choisir un niveau", font, 90, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(1500f, 120f));
        selectTitle.color = Gold;
        selectTitle.fontStyle = FontStyle.Bold;

        GameObject cards = new GameObject("Cards", typeof(RectTransform));
        cards.transform.SetParent(select.transform, false);
        StageSelect stageSelect = select.AddComponent<StageSelect>();
        stageSelect.catalog = catalog;
        stageSelect.font = font;
        stageSelect.roundedSprite = HUDBuilder.Rounded();
        stageSelect.cardsRoot = cards.GetComponent<RectTransform>();

        menu.stageSelectBack = MakeButton(select.transform, "BackButton", "Retour", font, new Vector2(0f, -450f), false);
        menu.stageSelectPanel = select;

        // instructions window
        GameObject panel = MakeFullScreenPanel(root.transform, "InstructionsPanel", new Color(0.04f, 0.02f, 0.08f, 0.75f));
        GameObject card = HUDBuilder.MakePanel(panel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 720f), 0.96f);
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
        BuildSettingsUpdater.Apply();

        Debug.Log("Main menu created and saved to " + ScenePath + ". Press Play, then click Jouer.");
    }

    // The list of all stages, in order. Each scene file must be named exactly like its sceneName.
    static StageCatalog GetOrCreateCatalog()
    {
        StageCatalog catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(BuildSettingsUpdater.CatalogPath);
        if (catalog != null) return catalog;

        catalog = ScriptableObject.CreateInstance<StageCatalog>();
        catalog.stages = new List<StageInfo>
        {
            new StageInfo { displayName = "Laboratoire", potionName = "Potion de vitesse", sceneName = "Stage1_Lab" },
            new StageInfo { displayName = "Forêt enchantée", potionName = "Potion de croissance", sceneName = "Stage2_Forest" },
            new StageInfo { displayName = "Grotte gelée", potionName = "Potion anti-feu", sceneName = "Stage3_Cave" },
            new StageInfo { displayName = "Bibliothèque hantée", potionName = "Potion d'invisibilité", sceneName = "Stage4_Library" },
            new StageInfo { displayName = "Tour de l'orage", potionName = "Potion de vol", sceneName = "Stage5_Tower" },
            new StageInfo { displayName = "Antre du dragon", potionName = "Élixir de puissance", sceneName = "Stage6_Dragon" },
        };
        AssetDatabase.CreateAsset(catalog, BuildSettingsUpdater.CatalogPath);
        return catalog;
    }

    static GameObject MakeFullScreenPanel(Transform parent, string name, Color color)
    {
        GameObject panel = HUDBuilder.MakeImage(parent, name, color, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return panel;
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
        HUDBuilder.Round(go.GetComponent<Image>());
        Button button = go.AddComponent<Button>();
        Text text = HUDBuilder.MakeText(go.transform, "Label", label, font, 54, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460f, 100f));
        text.color = primary ? new Color(0.23f, 0.12f, 0f, 1f) : Color.white;
        return button;
    }
}
