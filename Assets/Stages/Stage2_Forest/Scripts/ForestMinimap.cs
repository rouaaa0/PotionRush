using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
//  ForestMinimap  –  Stage 2 Enchanted Forest
//
//  Creates a circular "fantasy radar" minimap in the top-right
//  corner of the screen, placed cleanly below the HUD timer.
//  Scales automatically with screen resolution (1920x1080 canvas).
//
//  Map shows:
//    White  Arrow      = Player (rotates with player heading)
//    Orange dot        = Mushroom
//    Cyan   diamond    = Crystal
//    Green  dot        = Herb
//    Gold   star       = Cauldron  (icon type -1)
//    Mint   glow dot   = Runic Monolith (icon type 10)
//
//  Below the compass, a dark ornate card displays:
//    "SCENARIO X / 4"
//    Current objective text from ForestQuestManager
// ============================================================
public class ForestMinimap : MonoBehaviour
{
    // ── dimensions & tuneables ─────────────────────────────────
    const float MapWorldRadius  = 20f;   // world metres from player to edge
    const float DisplayRadius   = 85f;   // radius of the circle in reference UI pixels
    const float IconSize        = 22f;   // pixel size of objective icons
    const float PlayerIconSize  = 26f;   // pixel size of player arrow
    const float BorderThickness = 5f;    // gold border ring thickness

    // ── UI components ──────────────────────────────────────────
    Canvas     minimapCanvas;
    RectTransform iconContainer;
    Image      bgImage;
    Image      borderImage;
    Image      playerIcon;
    Text       questText;
    Text       scenarioLabel;

    readonly List<(Transform target, int type, Image icon)> trackedIcons
        = new List<(Transform, int, Image)>();

    Sprite circleSprite;
    Sprite diamondSprite;
    Sprite arrowSprite;

    Transform playerTransform;

    void Start()
    {
        circleSprite  = MakeCircleSprite(64);
        diamondSprite = MakeDiamondSprite(32);
        arrowSprite   = MakeArrowSprite(32);

        BuildCanvas();

        FindPlayer();
    }

    void FindPlayer()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform = pc.transform;
            return;
        }

        GameObject go = GameObject.Find("Player");
        if (go != null) playerTransform = go.transform;
    }

    void Update()
    {
        if (playerTransform == null)
            FindPlayer();

        SyncIconList();
        UpdateIcons();
        UpdatePlayerArrow();
        UpdateQuestText();
    }

    // ── canvas construction ────────────────────────────────────

    void BuildCanvas()
    {
        // Standalone Canvas scaled to 1920x1080, matching HUD
        GameObject canvasGO = new GameObject("ForestMinimapCanvas");
        minimapCanvas = canvasGO.AddComponent<Canvas>();
        minimapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        minimapCanvas.sortingOrder = 15; // Renders above standard HUD layers

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Outer container anchored top-right below HUD timer ─
        // In HUD, TEMPS is at (-30, -30), width 260, height 116 (bottom at y = -146).
        // We place the minimap at (-40, -165) so it never overlaps!
        GameObject outerGO = new GameObject("MinimapAnchor");
        outerGO.transform.SetParent(canvasGO.transform, false);
        RectTransform outer = outerGO.AddComponent<RectTransform>();
        outer.anchorMin  = new Vector2(1f, 1f);
        outer.anchorMax  = new Vector2(1f, 1f);
        outer.pivot      = new Vector2(1f, 1f);
        float circleDiameter = (DisplayRadius + BorderThickness) * 2f; // 180px
        float totalH = circleDiameter + 95f; // circle + quest card
        outer.sizeDelta  = new Vector2(circleDiameter + 10f, totalH);
        outer.anchoredPosition = new Vector2(-40f, -165f);

        // ── Gold border ring ───────────────────────────────────
        borderImage = MakeImage(outerGO.transform, "MinimapBorder", circleSprite,
            new Color(0.92f, 0.72f, 0.22f, 1f), circleDiameter, circleDiameter);
        RectTransform borderRT = borderImage.GetComponent<RectTransform>();
        borderRT.anchorMin = borderRT.anchorMax = new Vector2(0.5f, 1f);
        borderRT.pivot     = new Vector2(0.5f, 1f);
        borderRT.anchoredPosition = Vector2.zero;

        // ── Dark forest background circle ──────────────────────
        float innerD = DisplayRadius * 2f;
        bgImage = MakeImage(outerGO.transform, "MinimapBG", circleSprite,
            new Color(0.04f, 0.12f, 0.07f, 0.94f), innerD, innerD);
        RectTransform bgRT = bgImage.GetComponent<RectTransform>();
        bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 1f);
        bgRT.pivot     = new Vector2(0.5f, 1f);
        bgRT.anchoredPosition = new Vector2(0f, -BorderThickness);

        // ── Subtle crosshair / compass lines ───────────────────
        GameObject crossH = new GameObject("Cross_H");
        crossH.transform.SetParent(bgImage.transform, false);
        Image hImg = crossH.AddComponent<Image>();
        hImg.color = new Color(0.2f, 0.5f, 0.3f, 0.35f);
        RectTransform hRT = crossH.GetComponent<RectTransform>();
        hRT.sizeDelta = new Vector2(innerD * 0.85f, 1.5f);
        hRT.anchoredPosition = Vector2.zero;

        GameObject crossV = new GameObject("Cross_V");
        crossV.transform.SetParent(bgImage.transform, false);
        Image vImg = crossV.AddComponent<Image>();
        vImg.color = new Color(0.2f, 0.5f, 0.3f, 0.35f);
        RectTransform vRT = crossV.GetComponent<RectTransform>();
        vRT.sizeDelta = new Vector2(1.5f, innerD * 0.85f);
        vRT.anchoredPosition = Vector2.zero;

        // ── Container for icons (clamped inside the circle) ───
        GameObject containerGO = new GameObject("IconContainer");
        containerGO.transform.SetParent(outerGO.transform, false);
        iconContainer = containerGO.AddComponent<RectTransform>();
        iconContainer.anchorMin = iconContainer.anchorMax = new Vector2(0.5f, 1f);
        iconContainer.pivot     = new Vector2(0.5f, 1f);
        iconContainer.sizeDelta = new Vector2(innerD, innerD);
        iconContainer.anchoredPosition = new Vector2(0f, -BorderThickness);

        // ── Player Arrow (white navigation triangle) ───────────
        playerIcon = MakeImage(iconContainer, "PlayerIcon", arrowSprite,
            Color.white, PlayerIconSize, PlayerIconSize);
        playerIcon.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // ── Quest Card below circle ────────────────────────────
        GameObject cardGO = new GameObject("QuestCard");
        cardGO.transform.SetParent(outerGO.transform, false);
        Image cardBg = cardGO.AddComponent<Image>();
        cardBg.color = new Color(0.05f, 0.10f, 0.06f, 0.90f);
        RectTransform cardRT = cardGO.GetComponent<RectTransform>();
        cardRT.anchorMin = cardRT.anchorMax = new Vector2(0.5f, 1f);
        cardRT.pivot     = new Vector2(0.5f, 1f);
        cardRT.sizeDelta = new Vector2(circleDiameter + 15f, 75f);
        cardRT.anchoredPosition = new Vector2(0f, -(circleDiameter + 6f));

        // Thin gold top bar on the card
        GameObject cardBar = new GameObject("GoldBar");
        cardBar.transform.SetParent(cardGO.transform, false);
        Image barImg = cardBar.AddComponent<Image>();
        barImg.color = new Color(0.92f, 0.72f, 0.22f, 0.8f);
        RectTransform barRT = cardBar.GetComponent<RectTransform>();
        barRT.anchorMin = barRT.anchorMax = new Vector2(0.5f, 1f);
        barRT.pivot     = new Vector2(0.5f, 1f);
        barRT.sizeDelta = new Vector2(circleDiameter + 15f, 2f);
        barRT.anchoredPosition = Vector2.zero;

        // Scenario label ("SCENARIO 1 / 4")
        scenarioLabel = MakeText(cardGO.transform, "ScenarioLabel",
            "SCENARIO 1 / 4", 13, new Color(0.95f, 0.80f, 0.30f, 1f));
        RectTransform slRT = scenarioLabel.GetComponent<RectTransform>();
        slRT.anchorMin = slRT.anchorMax = new Vector2(0.5f, 1f);
        slRT.pivot     = new Vector2(0.5f, 1f);
        slRT.sizeDelta = new Vector2(circleDiameter + 5f, 22f);
        slRT.anchoredPosition = new Vector2(0f, -4f);

        // Quest instruction text
        questText = MakeText(cardGO.transform, "QuestText",
            "Gather 2 mushrooms & 1 crystal", 13, new Color(1f, 0.96f, 0.85f, 1f));
        RectTransform qtRT = questText.GetComponent<RectTransform>();
        qtRT.anchorMin = qtRT.anchorMax = new Vector2(0.5f, 1f);
        qtRT.pivot     = new Vector2(0.5f, 1f);
        qtRT.sizeDelta = new Vector2(circleDiameter + 5f, 44f);
        qtRT.anchoredPosition = new Vector2(0f, -26f);
    }

    // ── icon synchronization ───────────────────────────────────

    void SyncIconList()
    {
        if (ForestQuestManager.Instance == null) return;

        var desired = ForestQuestManager.Instance.MinimapIcons;

        // Clean up icons whose world targets were destroyed (e.g. collected items)
        for (int i = trackedIcons.Count - 1; i >= 0; i--)
        {
            if (trackedIcons[i].target == null)
            {
                if (trackedIcons[i].icon != null)
                    Destroy(trackedIcons[i].icon.gameObject);
                trackedIcons.RemoveAt(i);
            }
        }

        // Add any newly registered targets
        foreach (var (t, type) in desired)
        {
            if (t == null) continue;
            bool exists = false;
            foreach (var entry in trackedIcons)
            {
                if (entry.target == t) { exists = true; break; }
            }

            if (!exists)
            {
                Image img = CreateIcon(type);
                trackedIcons.Add((t, type, img));
            }
        }
    }

    Image CreateIcon(int type)
    {
        Color color;
        Sprite spr = circleSprite;
        float size = IconSize;

        switch (type)
        {
            case (int)IngredientType.Mushroom:
                color = new Color(1.0f, 0.45f, 0.12f, 1f); // Vibrant orange
                break;
            case (int)IngredientType.Crystal:
                color = new Color(0.12f, 0.90f, 1.0f, 1f); // Cyan diamond
                spr   = diamondSprite;
                break;
            case (int)IngredientType.Herb:
                color = new Color(0.25f, 1.0f, 0.35f, 1f); // Emerald green
                break;
            case -1: // Cauldron
                color = new Color(1.0f, 0.82f, 0.18f, 1f); // Radiant gold
                spr   = diamondSprite;
                size  = IconSize + 4f;
                break;
            case 10: // Runic monolith
                color = new Color(0.35f, 1.0f, 0.70f, 1f); // Mint rune glow
                break;
            case 99: // Monster (menacing crimson skull/threat icon)
                color = new Color(1.0f, 0.15f, 0.15f, 1f);
                spr   = diamondSprite;
                size  = IconSize + 6f;
                break;
            default:
                color = Color.white;
                break;
        }

        Image img = MakeImage(iconContainer, $"Icon_{type}", spr, color, size, size);

        // Put cauldron, monster and player on top of ingredient dots
        if (type == -1 || type == 99)
            img.transform.SetAsLastSibling();

        return img;
    }

    // ── per-frame icon update ──────────────────────────────────

    void UpdateIcons()
    {
        if (playerTransform == null) return;

        Vector3 playerPos = playerTransform.position;

        foreach (var (target, type, icon) in trackedIcons)
        {
            if (target == null || icon == null) continue;

            Vector3 delta = target.position - playerPos;

            // North-up radar projection
            float px = (delta.x / MapWorldRadius) * DisplayRadius;
            float py = (delta.z / MapWorldRadius) * DisplayRadius;

            // Clamp inside circular radar boundary
            Vector2 uiPos = new Vector2(px, py);
            float maxR = DisplayRadius - IconSize * 0.5f;
            if (uiPos.magnitude > maxR)
                uiPos = uiPos.normalized * maxR;

            icon.rectTransform.anchoredPosition = uiPos;
            icon.enabled = true;

            // Pulse monster threat icon aggressively on minimap
            if (type == 99)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.22f;
                icon.rectTransform.sizeDelta = new Vector2(IconSize + 6f, IconSize + 6f) * pulse;
            }
        }

        // Keep player arrow at the exact center and on top
        if (playerIcon != null)
        {
            playerIcon.rectTransform.anchoredPosition = Vector2.zero;
            playerIcon.transform.SetAsLastSibling();
        }
    }

    void UpdatePlayerArrow()
    {
        if (playerTransform == null || playerIcon == null) return;

        // Rotate arrow to match player's heading
        float yaw = playerTransform.eulerAngles.y;
        playerIcon.rectTransform.localEulerAngles = new Vector3(0f, 0f, -yaw);
    }

    void UpdateQuestText()
    {
        if (ForestQuestManager.Instance == null) return;

        if (questText != null)
            questText.text = ForestQuestManager.Instance.QuestBannerText;

        if (scenarioLabel != null)
            scenarioLabel.text = $"SCENARIO {ForestQuestManager.Instance.CurrentScenario} / 4";
    }

    // ── UI element creators ────────────────────────────────────

    static Image MakeImage(Transform parent, string name, Sprite sprite,
                           Color color, float w, float h)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.sprite  = sprite;
        img.color   = color;
        img.type    = Image.Type.Simple;
        img.raycastTarget = false;
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
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow   = VerticalWrapMode.Truncate;
        txt.raycastTarget      = false;

        // Subtle drop shadow for crisp readability
        Shadow shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        return txt;
    }

    // ── procedural sprites ─────────────────────────────────────

    static Sprite MakeCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float r = size / 2f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - r + 0.5f;
                float dy = y - r + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((r - dist) / 1.0f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeDiamondSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float mid = size / 2f;
        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float manhattan = Mathf.Abs(x - mid + 0.5f) + Mathf.Abs(y - mid + 0.5f);
                float alpha = Mathf.Clamp01((mid - manhattan) / 1.0f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeArrowSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        float mid = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            if (y < 4 || y > size - 4) continue;
            float progress = (float)(y - 4) / (size - 8); // 0 at bottom, 1 at tip
            float halfWidth = (1f - progress) * (mid - 3f);
            float minX = mid - halfWidth;
            float maxX = mid + halfWidth;

            for (int x = 0; x < size; x++)
            {
                // Notch the bottom center for arrowhead shape
                if (y < 12 && Mathf.Abs(x - mid) < (12 - y) * 0.65f) continue;

                if (x >= minX && x <= maxX)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
