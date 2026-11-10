using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The big ending, only for the final boss (Stage 6). DragonBoss adds this component by itself.
// When the dragon stage is won:
//   1. confetti rains over the screen and firework bursts pop for a few seconds,
//   2. once the Victoire card is up, a popup asks "continue to the next stages?",
//   3. "Oui !" opens a "Prochains niveaux" screen with locked mystery stages (coming soon).
// Everything is built in code on its own canvas, drawn above the HUD.
public class FinalBossVictory : MonoBehaviour
{
    static readonly Color Gold = new Color(0.95f, 0.71f, 0.27f, 1f);
    static readonly Color CardDark = new Color(0.13f, 0.07f, 0.09f, 0.98f);
    static readonly Color Dim = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color Grey = new Color(0.55f, 0.55f, 0.58f, 1f);
    static readonly Color Lavender = new Color(0.85f, 0.8f, 0.95f, 1f);
    static readonly Color[] ConfettiColors =
    {
        new Color(0.95f, 0.71f, 0.27f), new Color(1f, 0.35f, 0.3f), new Color(0.37f, 0.89f, 0.63f),
        new Color(0.44f, 0.83f, 1f), new Color(0.85f, 0.5f, 1f), Color.white
    };

    StageManager stage;
    HUD hud;
    Font font;
    RectTransform overlay;
    RectTransform confettiLayer;
    GameObject askPanel;
    GameObject soonPanel;

    void Start()
    {
        stage = StageManager.Instance;
        hud = FindFirstObjectByType<HUD>();
        if (stage != null) stage.StageWon += OnWon;
    }

    void OnDestroy()
    {
        if (stage != null) stage.StageWon -= OnWon;
    }

    void OnWon(int score)
    {
        StartCoroutine(Celebrate());
    }

    IEnumerator Celebrate()
    {
        BuildOverlay();
        Haptics.Success();

        // confetti from the top of the screen + firework bursts all over
        StartCoroutine(ConfettiRain(8f));
        for (int i = 0; i < 10; i++)
        {
            Burst(new Vector2(Random.Range(-750f, 750f), Random.Range(-100f, 380f)));
            yield return new WaitForSecondsRealtime(Random.Range(0.3f, 0.6f));
        }

        // wait for the HUD's Victoire card, give the player a moment to read the score, then ask
        while (hud != null && hud.resultPanel != null && !hud.resultPanel.activeSelf) yield return null;
        yield return new WaitForSecondsRealtime(1.5f);
        ShowAsk();
    }

    // ── Celebration ─────────────────────────────────────────────

    IEnumerator ConfettiRain(float duration)
    {
        for (float t = 0f; t < duration; t += 0.05f)
        {
            Vector2 start = new Vector2(Random.Range(-1000f, 1000f), 600f);
            Vector2 velocity = new Vector2(Random.Range(-120f, 120f), Random.Range(-250f, -80f));
            Confetti(start, velocity, Random.Range(3f, 5f));
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    void Burst(Vector2 center)
    {
        int count = 28;
        Color color = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
        for (int i = 0; i < count; i++)
        {
            float angle = (i / (float)count) * Mathf.PI * 2f;
            float speed = Random.Range(380f, 620f);
            Vector2 velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed;
            ConfettiPiece piece = Confetti(center, velocity, Random.Range(1.2f, 1.8f));
            piece.GetComponent<Image>().color = Random.value < 0.7f ? color : Color.white;
            piece.drag = 2.2f;
        }
    }

    ConfettiPiece Confetti(Vector2 position, Vector2 velocity, float life)
    {
        GameObject go = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(confettiLayer, false);
        Image image = go.GetComponent<Image>();
        image.color = ConfettiColors[Random.Range(0, ConfettiColors.Length)];
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(Random.Range(14f, 24f), Random.Range(8f, 14f));
        rect.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));

        ConfettiPiece piece = go.AddComponent<ConfettiPiece>();
        piece.velocity = velocity;
        piece.life = life;
        piece.spin = Random.Range(-400f, 400f);
        return piece;
    }

    // ── "Next stages?" popup ───────────────────────────────────

    void ShowAsk()
    {
        if (askPanel == null)
        {
            askPanel = Dimmer("AskPanel");
            RectTransform card = Card(askPanel.transform, new Vector2(1050f, 560f));

            Label(card, "Le Dragon est vaincu !", 76, Gold, new Vector2(0f, 175f), true);
            Label(card, "Vous avez terminé toutes les aventures !\nVoulez-vous continuer vers les prochains niveaux ?",
                  38, Color.white, new Vector2(0f, 30f), false);

            MakeButton(card, "Oui !", Gold, new Color(0.2f, 0.1f, 0.05f), new Vector2(190f, -170f), () =>
            {
                askPanel.SetActive(false);
                ShowComingSoon();
            });
            MakeButton(card, "Plus tard", Grey, Color.white, new Vector2(-190f, -170f), () => askPanel.SetActive(false));
        }

        askPanel.SetActive(true);
        StartCoroutine(Pop(askPanel.transform.GetChild(0)));
    }

    // ── "Coming soon" screen ──────────────────────────────────

    void ShowComingSoon()
    {
        if (soonPanel == null)
        {
            soonPanel = Dimmer("ComingSoonPanel");
            RectTransform card = Card(soonPanel.transform, new Vector2(1500f, 820f));

            Label(card, "Prochains niveaux", 76, Gold, new Vector2(0f, 310f), true);
            Label(card, "De nouvelles aventures arrivent bientôt...", 38, Lavender, new Vector2(0f, 225f), false);

            for (int i = 0; i < 3; i++)
                MysteryStage(card, 7 + i, new Vector2((i - 1) * 440f, 0f));

            MakeButton(card, "Menu", Grey, Color.white, new Vector2(-190f, -300f), () => SceneManager.LoadScene("MainMenu"));
            MakeButton(card, "Retour", Gold, new Color(0.2f, 0.1f, 0.05f), new Vector2(190f, -300f), () => soonPanel.SetActive(false));
        }

        soonPanel.SetActive(true);
        StartCoroutine(Pop(soonPanel.transform.GetChild(0)));
    }

    void MysteryStage(RectTransform parent, int number, Vector2 position)
    {
        RectTransform card = Box(parent, "Stage" + number, new Color(Gold.r, Gold.g, Gold.b, 0.35f), position, new Vector2(400f, 320f));
        RectTransform inner = Box(card, "Inner", new Color(0.08f, 0.05f, 0.12f, 1f), Vector2.zero, new Vector2(392f, 312f));

        Label(inner, number.ToString(), 120, Gold * 0.6f, new Vector2(0f, 60f), true);
        Label(inner, "? ? ?", 44, Color.white * 0.75f, new Vector2(0f, -40f), true);
        Label(inner, "Bientôt", 34, Lavender, new Vector2(0f, -105f), true);
    }

    // ── UI building helpers ────────────────────────────────────

    void BuildOverlay()
    {
        if (overlay != null) return;
        font = hud != null && hud.scoreText != null ? hud.scoreText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject go = new GameObject("FinalBossVictoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above the HUD

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;

        overlay = go.GetComponent<RectTransform>();

        GameObject layer = new GameObject("Confetti", typeof(RectTransform));
        layer.transform.SetParent(overlay, false);
        confettiLayer = layer.GetComponent<RectTransform>();
    }

    // A full-screen dark layer that also blocks taps on the Victoire card behind it.
    GameObject Dimmer(string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(overlay, false);
        go.GetComponent<Image>().color = Dim;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        confettiLayer.SetAsLastSibling(); // confetti keeps falling on top of the popups
        return go;
    }

    // A gold-bordered dark card, like the HUD's end card.
    RectTransform Card(Transform parent, Vector2 size)
    {
        RectTransform border = Box(parent, "Card", Gold, Vector2.zero, size);
        Box(border, "Inner", CardDark, Vector2.zero, size - new Vector2(10f, 10f));
        return border;
    }

    RectTransform Box(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    Text Label(Transform parent, string content, int size, Color color, Vector2 position, bool bold)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.text = content;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        go.GetComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.6f);
        t.rectTransform.anchoredPosition = position;
        t.rectTransform.sizeDelta = new Vector2(1000f, 100f);
        return t;
    }

    void MakeButton(Transform parent, string text, Color color, Color textColor, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = Box(parent, text + "Button", color, position, new Vector2(320f, 110f));
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        Text label = Label(rect, text, 46, textColor, Vector2.zero, true);
        label.GetComponent<Shadow>().enabled = false;
    }

    // Same pop-in as the HUD's end card: grows a little past full size, then settles.
    IEnumerator Pop(Transform target)
    {
        for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
        {
            float x = Mathf.Clamp01(t / 0.35f) - 1f;
            const float c1 = 1.70158f;
            target.localScale = Vector3.one * (1f + (c1 + 1f) * x * x * x + c1 * x * x);
            yield return null;
        }
        target.localScale = Vector3.one;
    }
}

// One piece of confetti: flies, falls, spins and fades out. Uses unscaled time so it also moves if the game is paused.
public class ConfettiPiece : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 3f;
    public float spin;
    public float drag;

    RectTransform rect;
    Image image;
    float age;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        age += dt;

        velocity *= 1f / (1f + drag * dt);
        velocity += Vector2.down * 260f * dt;
        rect.anchoredPosition += velocity * dt;
        rect.localEulerAngles += new Vector3(0f, 0f, spin * dt);

        Color c = image.color;
        c.a = Mathf.Clamp01((life - age) / 0.6f);
        image.color = c;

        if (age >= life) Destroy(gameObject);
    }
}
