using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  ForestMinimap  –  Stage 2 Enchanted Forest
//
//  Creates a circular "fantasy radar" minimap in the top-right
//  corner of the screen entirely in C# (no prefab required).
//
//  Map shows:
//    White  triangle   = Player
//    Orange filled dot = Mushroom
//    Cyan   square     = Crystal
//    Green  filled dot = Herb
//    Gold   diamond    = Cauldron  (icon type -1)
//    Bright green dot  = Runic Monolith (icon type 10)
//
//  Below the circle a yellow banner shows the current quest hint
//  read from ForestQuestManager.QuestBannerText.
// ============================================================
public class ForestMinimap : MonoBehaviour
{
    // ── tuneable constants ─────────────────────────────────────
    const float MapWorldRadius  = 22f;   // how many metres from player edge shows
    const float DisplayRadius   = 72f;   // radius of the circle in pixels
    const float IconSize        = 14f;   // pixel size of each icon sprite
    const float PlayerIconSize  = 16f;
    const float BorderThickness = 5f;

    // ── references – found / created at runtime ────────────────
    Canvas     minimapCanvas;
    RectTransform circleContainer;  // parent that clips icons to the circle
    Image      bgImage;             // dark circle background
    Image      borderImage;         // gold ring
    Image      playerIcon;          // white triangle
    Text       questText;           // banner below the circle
    Text       scenarioLabel;       // "Scenario X/4" small label

    // Icon pool: one Image per tracked transform.
    readonly List<(Transform target, int type, Image icon)> trackedIcons
        = new List<(Transform, int, Image)>();

    // Cached materials/sprites built once.
    Sprite circleSprite;
    Sprite squareSprite;

    // ── player reference ───────────────────────────────────────
    Transform playerTransform;

    // ── Unity lifecycle ────────────────────────────────────────

    void Start()
    {
        // Build sprites from plain Texture2D shapes.
        circleSprite = MakeCircleSprite(64);
        squareSprite = MakeSquareSprite(32);

        BuildCanvas();

        // Find the player.
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    void Update()
    {
        // Keep the tracked list in sync with ForestQuestManager.
        SyncIconList();

        // Update icon positions each frame.
        UpdateIcons();

        // Rotate the player icon to match heading.
        UpdatePlayerArrow();

        // Update quest text.
        UpdateQuestText();
    }

    // ── canvas construction ────────────────────────────────────

    void BuildCanvas()
    {
        // A separate canvas so we don't modify the shared HUD prefab.
        GameObject canvasGO = new GameObject("ForestMinimapCanvas");
        minimapCanvas = canvasGO.AddComponent<Canvas>();
        minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        minimapCanvas.sortingOrder = 10;   // on top of HUD
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode
            = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Outer container anchored top-right ─────────────────
        GameObject outerGO = new GameObject("MinimapAnchor");
        outerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform outer = outerGO.AddComponent<RectTransform>();
        outer.anchorMin  = new Vector2(1f, 1f);
        outer.anchorMax  = new Vector2(1f, 1f);
        outer.pivot      = new Vector2(1f, 1f);
        float totalH     = DisplayRadius * 2f + BorderThickness * 2f + 32f; // circle + banner
        outer.sizeDelta  = new Vector2(DisplayRadius * 2f + BorderThickness * 2f + 10f, totalH);
        outer.anchoredPosition = new Vector2(-10f, -10f);

        // ── Gold border ring ───────────────────────────────────
        float fullD = (DisplayRadius + BorderThickness) * 2f;
        borderImage = MakeImage(outerGO.transform, "MinimapBorder", circleSprite,
            new Color(0.85f, 0.65f, 0.10f, 1f), fullD, fullD);
        RectTransform borderRT = borderImage.GetComponent<RectTransform>();
        borderRT.anchorMin = borderRT.anchorMax = new Vector2(0.5f, 1f);
        borderRT.pivot     = new Vector2(0.5f, 1f);
        borderRT.anchoredPosition = Vector2.zero;

        // ── Dark green background circle ───────────────────────
        bgImage = MakeImage(outerGO.transform, "MinimapBG", circleSprite,
            new Color(0.04f, 0.14f, 0.07f, 0.88f), DisplayRadius * 2f, DisplayRadius * 2f);
        RectTransform bgRT = bgImage.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 1f);
        bgRT.pivot     = new Vector2(0.5f, 1f);
        bgRT.anchoredPosition = new Vector2(0f, -BorderThickness);

        // ── Container that clips icons (uses the circle as a mask) ──
        GameObject containerGO = new GameObject("CircleContainer");
        containerGO.transform.SetParent(outerGO.transform, false);
        circleContainer = containerGO.AddComponent<RectTransform>();
        circleContainer.anchorMin = circleContainer.anchorMax = new Vector2(0.5f, 1f);
        circleContainer.pivot     = new Vector2(0.5f, 1f);
        circleContainer.sizeDelta = new Vector2(DisplayRadius * 2f, DisplayRadius * 2f);
        circleContainer.anchoredPosition = new Vector2(0f, -BorderThickness);

        // Add a Mask so icons outside the circle are clipped.
        Image maskImg = containerGO.AddComponent<Image>();
        maskImg.sprite = circleSprite;
        maskImg.color  = new Color(1f, 1f, 1f, 0f); // invisible but needed for mask
        containerGO.AddComponent<Mask>().showMaskGraphic = false;

        // ── Player icon (white triangle) ───────────────────────
        playerIcon = MakeImage(circleContainer, "PlayerIcon", squareSprite,
            Color.white, PlayerIconSize, PlayerIconSize);
        playerIcon.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // ── Quest banner text below circle ─────────────────────
        scenarioLabel = MakeText(outerGO.transform, "ScenarioLabel",
            "Scenario 1 / 4", 11, new Color(0.9f, 0.75f, 0.3f, 1f));
        RectTransform slRT = scenarioLabel.GetComponent<RectTransform>();
        slRT.anchorMin = slRT.anchorMax = new Vector2(0.5f, 1f);
        slRT.pivot     = new Vector2(0.5f, 1f);
        slRT.sizeDelta = new Vector2(DisplayRadius * 2f + 20f, 20f);
        slRT.anchoredPosition = new Vector2(0f, -(BorderThickness + DisplayRadius * 2f + 4f));

        questText = MakeText(outerGO.transform, "QuestBanner",
            "Gather mushrooms & crystal", 12, new Color(1f, 0.95f, 0.6f, 1f));
        RectTransform qtRT = questText.GetComponent<RectTransform>();
        qtRT.anchorMin = qtRT.anchorMax = new Vector2(0.5f, 1f);
        qtRT.pivot     = new Vector2(0.5f, 1f);
        qtRT.sizeDelta = new Vector2(DisplayRadius * 2f + 20f, 28f);
        qtRT.anchoredPosition = new Vector2(0f, -(BorderThickness + DisplayRadius * 2f + 24f));
    }

    // ── icon sync ──────────────────────────────────────────────

    void SyncIconList()
    {
        if (ForestQuestManager.Instance == null) return;

        var desired = ForestQuestManager.Instance.MinimapIcons;

        // Remove icons for objects that have been destroyed.
        for (int i = trackedIcons.Count - 1; i >= 0; i--)
        {
            if (trackedIcons[i].target == null)
            {
                Destroy(trackedIcons[i].icon.gameObject);
                trackedIcons.RemoveAt(i);
            }
        }

        // Add new icons that aren't tracked yet.
        foreach (var (t, type) in desired)
        {
            if (t == null) continue;
            bool alreadyTracked = false;
            foreach (var entry in trackedIcons)
                if (entry.target == t) { alreadyTracked = true; break; }

            if (!alreadyTracked)
                trackedIcons.Add((t, type, CreateIcon(type)));
        }
    }

    Image CreateIcon(int type)
    {
        Color color;
        Sprite spr = circleSprite;  // default = circle dot

        switch (type)
        {
            case (int)IngredientType.Mushroom:
                color = new Color(1f, 0.55f, 0.05f, 1f); break;  // orange
            case (int)IngredientType.Crystal:
                color = new Color(0.1f, 0.9f, 1.0f, 1f);         // cyan
                spr   = squareSprite; break;                       // diamond (rotated square)
            case (int)IngredientType.Herb:
                color = new Color(0.3f, 1.0f, 0.3f, 1f); break;  // bright green
            case -1: // cauldron
                color = new Color(1.0f, 0.80f, 0.1f, 1f);        // gold
                spr   = squareSprite; break;
            case 10: // runic monolith
                color = new Color(0.4f, 1.0f, 0.5f, 1f); break;  // green glow
            default:
                color = Color.white; break;
        }

        Image img = MakeImage(circleContainer, $"Icon_{type}", spr, color, IconSize, IconSize);

        // Rotate Crystal and Cauldron icons 45° so the square looks like a diamond.
        if (type == (int)IngredientType.Crystal || type == -1)
            img.GetComponent<RectTransform>().localEulerAngles = new Vector3(0f, 0f, 45f);

        return img;
    }

    // ── update icons each frame ────────────────────────────────

    void UpdateIcons()
    {
        if (playerTransform == null)
        {
            // Try to find player again (may have spawned late).
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
            return;
        }

        Vector3 playerPos = playerTransform.position;
        float   playerYaw = playerTransform.eulerAngles.y; // forward rotation

        foreach (var (target, type, icon) in trackedIcons)
        {
            if (target == null) { icon.enabled = false; continue; }

            Vector3 delta = target.position - playerPos;
            // Project onto XZ plane (ignore height).
            float worldX = delta.x;
            float worldZ = delta.z;

            // Rotate the delta by the negative of player yaw to keep map
            // "player-forward = up" orientation.
            float rad = -playerYaw * Mathf.Deg2Rad;
            float rotX =  worldX * Mathf.Cos(rad) - worldZ * Mathf.Sin(rad);
            float rotZ =  worldX * Mathf.Sin(rad) + worldZ * Mathf.Cos(rad);

            // Scale to display pixels.
            float px = rotX / MapWorldRadius * DisplayRadius;
            float py = rotZ / MapWorldRadius * DisplayRadius;

            // Clamp to circle edge so icons don't go outside.
            Vector2 uiPos = new Vector2(px, py);
            if (uiPos.magnitude > DisplayRadius - IconSize * 0.5f)
                uiPos = uiPos.normalized * (DisplayRadius - IconSize * 0.5f);

            icon.rectTransform.anchoredPosition = uiPos;
            icon.enabled = true;
        }
    }

    void UpdatePlayerArrow()
    {
        // The player icon sits at the center; rotate it to show heading.
        if (playerTransform != null && playerIcon != null)
        {
            playerIcon.rectTransform.anchoredPosition = Vector2.zero;
        }
    }

    void UpdateQuestText()
    {
        if (ForestQuestManager.Instance == null) return;

        if (questText != null)
            questText.text = ForestQuestManager.Instance.QuestBannerText;

        if (scenarioLabel != null)
            scenarioLabel.text = $"Scenario {ForestQuestManager.Instance.CurrentScenario} / 4";
    }

    // ── UI helpers ─────────────────────────────────────────────

    static Image MakeImage(Transform parent, string name, Sprite sprite,
                           Color color, float w, float h)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite  = sprite;
        img.color   = color;
        img.type    = Image.Type.Simple;
        img.preserveAspect = false;
        RectTransform rt = img.GetComponent<RectTransform>();
        rt.sizeDelta         = new Vector2(w, h);
        rt.anchoredPosition  = Vector2.zero;
        return img;
    }

    static Text MakeText(Transform parent, string name, string content,
                         int fontSize, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Text txt = go.AddComponent<Text>();
        txt.text      = content;
        txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;
        go.AddComponent<RectTransform>();
        return txt;
    }

    // ── sprite generators ──────────────────────────────────────

    // Creates a smooth circle sprite from a Texture2D.
    static Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size / 2f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((r - dist) / 1.0f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // Creates a solid square sprite.
    static Sprite MakeSquareSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
