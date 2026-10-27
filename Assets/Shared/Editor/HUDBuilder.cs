using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Adds a menu item that builds the HUD (score, timer, recipe, gauge, end screen) and saves it as a prefab.
public static class HUDBuilder
{
    const string PrefabPath = "Assets/Shared/Prefabs/HUD.prefab";

    static readonly Color Panel = new Color(0.08f, 0.05f, 0.13f, 0.72f);
    static readonly Color Gold = new Color(0.95f, 0.71f, 0.27f, 1f);

    [MenuItem("Potion Rush/Build HUD")]
    static void Build()
    {
        if (Object.FindFirstObjectByType<HUD>() != null)
        {
            Debug.LogWarning("A HUD is already in the scene. Delete it first if you want to rebuild it.");
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject root = new GameObject("HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // buttons need an event system that understands the new Input System
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(root.transform, false);

        HUD hud = root.AddComponent<HUD>();

        hud.scoreText = MakeText(root.transform, "Score", "Score  0", font, 46, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(600f, 70f));
        hud.timerText = MakeText(root.transform, "Timer", "2:00", font, 46, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(400f, 70f));
        hud.recipeText = MakeText(root.transform, "Recipe", "OBJECTIFS", font, 34, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(30f, -110f), new Vector2(600f, 240f));

        // tutorial hint (only shown when the stage config asks for it)
        GameObject hint = MakeImage(root.transform, "Hint", new Color(0.08f, 0.05f, 0.13f, 0.72f), new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(1300f, 80f));
        hud.hintRoot = hint;
        hud.hintText = MakeText(hint.transform, "Text", "", font, 40, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300f, 80f));
        hud.hintText.color = Gold;

        // brewing gauge
        GameObject gauge = MakeImage(root.transform, "Gauge", Panel, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(900f, 60f));
        GameObject fill = MakeImage(gauge.transform, "Fill", new Color(0.37f, 0.89f, 0.63f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        hud.gaugeRoot = gauge;
        hud.gaugeFill = fill.GetComponent<Image>();
        hud.gaugeLabel = MakeText(gauge.transform, "Label", "Préparation  0 %", font, 34, TextAnchor.LowerCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, 6f), new Vector2(900f, 50f));
        hud.gaugeLabel.rectTransform.pivot = new Vector2(0.5f, 0f);

        // end screen
        GameObject panel = MakeImage(root.transform, "ResultPanel", new Color(0.04f, 0.02f, 0.08f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        hud.resultPanel = panel;

        GameObject card = MakeImage(panel.transform, "Card", new Color(0.09f, 0.055f, 0.14f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 640f));
        hud.resultTitle = MakeText(card.transform, "Title", "Victoire", font, 72, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 100f));
        hud.resultTitle.color = new Color(0.37f, 0.89f, 0.63f, 1f);
        hud.resultDetails = MakeText(card.transform, "Details", "", font, 38, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(800f, 300f));

        GameObject button = MakeImage(card.transform, "ReplayButton", Gold, new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 90f));
        hud.replayButton = button.AddComponent<Button>();
        Text label = MakeText(button.transform, "Label", "Rejouer", font, 48, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 90f));
        label.color = new Color(0.23f, 0.12f, 0f, 1f);

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = root;
        Debug.Log("HUD created. Press Play to see it.");
    }

    internal static GameObject MakeImage(Transform parent, string name, Color color, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        Place(go.GetComponent<RectTransform>(), anchor, position, size);
        return go;
    }

    internal static Text MakeText(Transform parent, string name, string content, Font font, int size, TextAnchor align,
        Vector2 anchor, Vector2 position, Vector2 boxSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.text = content;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        Place(t.rectTransform, anchor, position, boxSize);
        return t;
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
