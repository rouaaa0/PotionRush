using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Adds a menu item that builds the HUD (score, timer, objectives, gauge, hints, end screen)
// in the game's style (dark purple panels, gold borders, rounded corners) and saves it as a prefab.
public static class HUDBuilder
{
    const string PrefabPath = "Assets/Shared/Prefabs/HUD.prefab";
    const string ArtFolder = "Assets/Shared/Art";

    static readonly Color Gold = new Color(0.95f, 0.71f, 0.27f, 1f);
    static readonly Color Lavender = new Color(0.85f, 0.8f, 0.95f, 1f);
    static readonly Color Mint = new Color(0.37f, 0.89f, 0.63f, 1f);

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
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // buttons need an event system that understands the new Input System
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystem.transform.SetParent(root.transform, false);

        HUD hud = root.AddComponent<HUD>();
        hud.circleSprite = Circle();

        // score
        GameObject score = MakePanel(root.transform, "ScorePanel", new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(340f, 116f));
        MakeText(score.transform, "Label", "SCORE", font, 26, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(280f, 34f)).color = Lavender;
        hud.scoreText = MakeText(score.transform, "Value", "0", font, 60, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -44f), new Vector2(280f, 70f));
        hud.scoreText.fontStyle = FontStyle.Bold;

        // timer
        GameObject timer = MakePanel(root.transform, "TimerPanel", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(260f, 116f));
        MakeText(timer.transform, "Label", "TEMPS", font, 26, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -16f), new Vector2(200f, 34f)).color = Lavender;
        hud.timerText = MakeText(timer.transform, "Value", "2:00", font, 60, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -44f), new Vector2(200f, 70f));
        hud.timerText.fontStyle = FontStyle.Bold;

        // objectives (the rows are added by the HUD script, one per recipe entry)
        GameObject objectives = MakePanel(root.transform, "Objectives", new Vector2(0f, 1f), new Vector2(30f, -166f), new Vector2(420f, 150f));
        Text objectivesTitle = MakeText(objectives.transform, "Title", "OBJECTIFS", font, 28, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(300f, 36f));
        objectivesTitle.color = Gold;
        objectivesTitle.fontStyle = FontStyle.Bold;
        hud.objectivesCard = objectives.GetComponent<RectTransform>();

        // tutorial hint (only shown when the stage config asks for it)
        GameObject hint = MakePanel(root.transform, "Hint", new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1100f, 84f));
        hud.hintRoot = hint;
        hud.hintText = MakeText(hint.transform, "Text", "", font, 40, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 84f));
        hud.hintText.color = Gold;
        hud.hintText.fontStyle = FontStyle.Bold;

        // brewing gauge
        GameObject gauge = MakePanel(root.transform, "Gauge", new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(900f, 64f));
        GameObject fill = MakeImage(gauge.transform, "Fill", Mint, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        Round(fill.GetComponent<Image>());
        fill.GetComponent<Image>().raycastTarget = false;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(7f, 7f);
        fillRect.offsetMax = new Vector2(-7f, -7f);
        hud.gaugeRoot = gauge;
        hud.gaugeFill = fill.GetComponent<Image>();
        hud.gaugeLabel = MakeText(gauge.transform, "Label", "Préparation  0 %", font, 36, TextAnchor.LowerCenter, new Vector2(0.5f, 1f), new Vector2(0f, 10f), new Vector2(900f, 50f));
        hud.gaugeLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
        hud.gaugeLabel.fontStyle = FontStyle.Bold;

        // end screen
        GameObject panel = MakeImage(root.transform, "ResultPanel", new Color(0.04f, 0.02f, 0.08f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        hud.resultPanel = panel;

        GameObject card = MakePanel(panel.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 640f), 0.96f);
        hud.resultTitle = MakeText(card.transform, "Title", "Victoire", font, 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 110f));
        hud.resultTitle.color = Mint;
        hud.resultTitle.fontStyle = FontStyle.Bold;
        hud.resultDetails = MakeText(card.transform, "Details", "", font, 38, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(800f, 300f));

        GameObject shadow = MakeImage(card.transform, "ButtonShadow", new Color(0.54f, 0.33f, 0.06f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(360f, 92f));
        Round(shadow.GetComponent<Image>());
        shadow.GetComponent<Image>().raycastTarget = false;
        GameObject button = MakeImage(card.transform, "ReplayButton", Gold, new Vector2(0.5f, 0f), new Vector2(0f, 48f), new Vector2(360f, 92f));
        Round(button.GetComponent<Image>());
        hud.replayButton = button.AddComponent<Button>();
        Text label = MakeText(button.transform, "Label", "Rejouer", font, 50, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 92f));
        label.color = new Color(0.23f, 0.12f, 0f, 1f);
        label.fontStyle = FontStyle.Bold;

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = root;
        Debug.Log("HUD created. Press Play to see it.");
    }

    // A dark rounded panel with a gold border. Put the content inside the returned object.
    internal static GameObject MakePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, float fillAlpha = 0.82f)
    {
        GameObject outer = MakeImage(parent, name, new Color(Gold.r, Gold.g, Gold.b, 0.55f), anchor, position, size);
        Round(outer.GetComponent<Image>());
        outer.GetComponent<Image>().raycastTarget = false;

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(outer.transform, false);
        Image image = inner.GetComponent<Image>();
        image.color = new Color(0.09f, 0.055f, 0.14f, fillAlpha);
        image.raycastTarget = false;
        Round(image);

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(4f, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);
        return outer;
    }

    internal static void Round(Image image)
    {
        image.sprite = Rounded();
        image.type = Image.Type.Sliced;
    }

    internal static Sprite Rounded()
    {
        return EnsureSprite("UIRounded", 40f, 44f);
    }

    internal static Sprite Circle()
    {
        return EnsureSprite("UICircle", 64f, 0f);
    }

    // Draws a white rounded square once and saves it in Assets/Shared/Art, so the UI can be tinted any colour.
    static Sprite EnsureSprite(string name, float radius, float border)
    {
        string path = ArtFolder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            const int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float half = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 q = new Vector2(Mathf.Abs(x + 0.5f - half) - (half - radius), Mathf.Abs(y + 0.5f - half) - (half - radius));
                    float distance = Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance)));
                }
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
