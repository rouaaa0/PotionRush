using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shows the score, timer, objectives, brewing gauge, hints and the end screen.
// It only reads from the StageManager and never changes the rules.
public class HUD : MonoBehaviour
{
    public Text scoreText;
    public Text timerText;
    public RectTransform objectivesCard;
    public Sprite circleSprite;
    public GameObject gaugeRoot;
    public Image gaugeFill;
    public Text gaugeLabel;
    public GameObject resultPanel;
    public Text resultTitle;
    public Text resultDetails;
    public Button replayButton;
    public Button menuButton;
    public Button nextButton;
    public StageCatalog catalog;
    public GameObject hintRoot;
    public Text hintText;

    class ObjectiveRow
    {
        public IngredientType type;
        public int amount;
        public Image dot;
        public Text label;
        public Text count;
    }

    static readonly Color Done = new Color(0.37f, 0.89f, 0.63f, 1f);
    static readonly Color Danger = new Color(1f, 0.35f, 0.3f, 1f);

    readonly List<ObjectiveRow> rows = new List<ObjectiveRow>();

    StageManager stage;
    bool hasMoved;
    Transform card;
    int lastScore;
    float scorePunch;
    float popTime;
    float loseTimer = -1f;
    float winTimer = -1f;
    int winScore;
    Text banner;
    float bannerTime;

    void Start()
    {
        stage = StageManager.Instance;
        if (stage == null)
        {
            Debug.LogWarning("HUD: there is no StageManager in the scene.");
            enabled = false;
            return;
        }

        // match the screen height, so wide phones show extra width instead of cutting off the top and bottom
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null) scaler.matchWidthOrHeight = 1f;

        gaugeRoot.SetActive(false);
        resultPanel.SetActive(false);
        card = resultPanel.transform.Find("Card");
        BuildObjectives();

        stage.BrewProgressed += OnBrewProgressed;
        stage.StageWon += OnWon;
        stage.StageLost += OnLost;
        replayButton.onClick.AddListener(Replay);
        if (menuButton != null) menuButton.onClick.AddListener(() => SceneManager.LoadScene("MainMenu"));
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() =>
            {
                string next = NextStageScene();
                if (next != null) SceneManager.LoadScene(next);
            });
        }
    }

    // The scene of the next stage that exists (stages not made yet are skipped), or null if there is none.
    string NextStageScene()
    {
        if (catalog == null) return null;
        return GameProgress.NextStage(catalog, SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        if (stage == null) return;
        stage.BrewProgressed -= OnBrewProgressed;
        stage.StageWon -= OnWon;
        stage.StageLost -= OnLost;
    }

    // One row per recipe entry: a coloured dot, the name and a counter.
    void BuildObjectives()
    {
        float y = -72f;
        foreach (RecipeEntry entry in stage.config.recipe)
        {
            ObjectiveRow row = new ObjectiveRow { type = entry.type, amount = entry.amount };

            GameObject dotObj = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(objectivesCard, false);
            row.dot = dotObj.GetComponent<Image>();
            row.dot.sprite = circleSprite;
            row.dot.raycastTarget = false;
            Place(row.dot.rectTransform, new Vector2(0f, 1f), new Vector2(28f, y - 4f), new Vector2(28f, 28f));

            row.label = NewText("Name", TextAnchor.UpperLeft, 34);
            Place(row.label.rectTransform, new Vector2(0f, 1f), new Vector2(72f, y), new Vector2(260f, 40f));

            row.count = NewText("Count", TextAnchor.UpperRight, 34);
            row.count.fontStyle = FontStyle.Bold;
            Place(row.count.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, y), new Vector2(110f, 40f));

            rows.Add(row);
            y -= 52f;
        }

        objectivesCard.sizeDelta = new Vector2(objectivesCard.sizeDelta.x, -y + 22f);
    }

    Text NewText(string name, TextAnchor align, int size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(objectivesCard, false);
        Text t = go.GetComponent<Text>();
        t.font = scoreText.font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
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

    void Update()
    {
        scoreText.text = stage.Score.ToString();

        int seconds = Mathf.CeilToInt(stage.TimeLeft);
        timerText.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");

        // the timer turns red and pulses in the last 10 seconds
        bool low = stage.State == StageState.Playing && seconds <= 10;
        timerText.color = low ? Danger : Color.white;
        timerText.transform.localScale = Vector3.one * (low ? 1f + Mathf.Abs(Mathf.Sin(Time.time * 4f)) * 0.1f : 1f);

        foreach (ObjectiveRow row in rows)
        {
            int have = stage.Collected(row.type);
            bool complete = have >= row.amount;
            Color text = complete ? Done : Color.white;

            row.dot.color = complete ? Done : IngredientColor(row.type);
            row.label.color = text;
            row.count.color = text;
            row.label.text = Label(row.type, row.amount);
            row.count.text = have + "/" + row.amount;
        }

        if (loseTimer > 0f)
        {
            loseTimer -= Time.deltaTime;
            if (loseTimer <= 0f) ShowLoseCard();
        }

        if (winTimer > 0f)
        {
            winTimer -= Time.deltaTime;
            if (winTimer <= 0f) ShowWinCard();
        }

        UpdateHint();
        Animate();
    }

    static Color IngredientColor(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.Mushroom: return new Color(0.89f, 0.22f, 0.29f, 1f);
            case IngredientType.Crystal: return new Color(0.44f, 0.83f, 1f, 1f);
            case IngredientType.Herb: return new Color(0.4f, 0.8f, 0.3f, 1f);
            case IngredientType.IceFlower: return new Color(0.75f, 0.93f, 1f, 1f);
            case IngredientType.Scroll: return new Color(0.9f, 0.8f, 0.55f, 1f);
            case IngredientType.DragonScale: return new Color(1f, 0.5f, 0.15f, 1f);
            default: return Color.white;
        }
    }

    static string Label(IngredientType type, int amount)
    {
        bool many = amount > 1;
        switch (type)
        {
            case IngredientType.Mushroom: return many ? "Champignons" : "Champignon";
            case IngredientType.Crystal: return many ? "Cristaux" : "Cristal";
            case IngredientType.Herb: return many ? "Herbes" : "Herbe";
            case IngredientType.IceFlower: return many ? "Fleurs de glace" : "Fleur de glace";
            case IngredientType.Scroll: return many ? "Parchemins" : "Parchemin";
            case IngredientType.DragonScale: return many ? "Écailles" : "Écaille";
            default: return type.ToString();
        }
    }

    // The score bumps when it changes, the banner bounces and the end card pops in.
    void Animate()
    {
        if (stage.Score != lastScore)
        {
            lastScore = stage.Score;
            scorePunch = 1f;
        }
        scorePunch = Mathf.MoveTowards(scorePunch, 0f, Time.deltaTime * 4f);
        scoreText.transform.localScale = Vector3.one * (1f + scorePunch * 0.25f);

        if (banner != null && banner.gameObject.activeSelf)
        {
            bannerTime += Time.deltaTime;
            float x = Mathf.Clamp01(bannerTime / 0.5f) - 1f;
            const float c1 = 1.70158f;
            float pop = 1f + (c1 + 1f) * x * x * x + c1 * x * x;
            banner.transform.localScale = Vector3.one * pop * (1f + Mathf.Sin(bannerTime * 6f) * 0.03f);
        }

        if (resultPanel.activeSelf && card != null)
        {
            popTime += Time.unscaledDeltaTime;
            float x = Mathf.Clamp01(popTime / 0.35f) - 1f;
            const float c1 = 1.70158f;
            card.localScale = Vector3.one * (1f + (c1 + 1f) * x * x * x + c1 * x * x); // overshoots a little, then settles
        }
    }

    // Short instructions for the first stage, driven by what the player has done so far.
    void UpdateHint()
    {
        string hint = "";

        if (stage.config.showTutorialHints && stage.State == StageState.Playing)
        {
            if (GameInput.Instance != null && GameInput.Instance.Move.sqrMagnitude > 0.01f) hasMoved = true;

            int total = 0;
            foreach (RecipeEntry entry in stage.config.recipe) total += stage.Collected(entry.type);

            if (stage.Brewing) hint = "Secouez votre téléphone !";
            else if (stage.RecipeComplete) hint = "Allez jusqu'au chaudron";
            else if (!hasMoved) hint = "Inclinez votre téléphone pour vous déplacer";
            else if (total == 0) hint = "Marchez sur un ingrédient pour le ramasser";
            else hint = "Ramassez tous les ingrédients";
        }

        hintRoot.SetActive(hint.Length > 0);
        hintText.text = hint;
    }

    void OnBrewProgressed(float progress)
    {
        gaugeRoot.SetActive(true);
        gaugeFill.rectTransform.anchorMax = new Vector2(progress, 1f);
        gaugeLabel.text = "Préparation  " + Mathf.RoundToInt(progress * 100f) + " %";
    }

    // A big "Potion réussie !" shows first, then the score card follows once the celebration has played.
    void OnWon(int finalScore)
    {
        gaugeRoot.SetActive(false);
        winScore = finalScore;
        winTimer = 2.8f;
        CreateBanner();
    }

    void CreateBanner()
    {
        GameObject go = new GameObject("WinBanner", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(transform, false);

        banner = go.GetComponent<Text>();
        banner.font = scoreText.font;
        banner.fontSize = 130;
        banner.fontStyle = FontStyle.Bold;
        banner.alignment = TextAnchor.MiddleCenter;
        banner.color = new Color(0.95f, 0.71f, 0.27f, 1f);
        banner.text = "Potion réussie !";
        banner.horizontalOverflow = HorizontalWrapMode.Overflow;
        banner.verticalOverflow = VerticalWrapMode.Overflow;
        banner.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(6f, -6f);

        RectTransform rect = banner.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 220f);
        rect.sizeDelta = new Vector2(1600f, 200f);

        bannerTime = 0f;
    }

    void ShowWinCard()
    {
        if (banner != null) banner.gameObject.SetActive(false);
        resultPanel.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(NextStageScene() != null);
        popTime = 0f;
        int finalScore = winScore;

        StageConfig c = stage.config;
        int ingredients = 0;
        foreach (RecipeEntry entry in c.recipe) ingredients += entry.amount;
        int timeLeft = Mathf.FloorToInt(stage.TimeLeft);

        resultTitle.text = "Victoire";
        resultDetails.text =
            "Ingrédients (" + ingredients + " × " + c.pointsPerIngredient + ")   " + (ingredients * c.pointsPerIngredient) + "\n" +
            "Potion terminée   " + c.potionBonus + "\n" +
            "Temps restant (" + timeLeft + " s × " + c.pointsPerSecondLeft + ")   " + (timeLeft * c.pointsPerSecondLeft) + "\n\n" +
            "Score final   " + finalScore;
    }

    // The end card waits a moment so the player can see the explosion first.
    void OnLost()
    {
        gaugeRoot.SetActive(false);
        loseTimer = 1.8f;
    }

    void ShowLoseCard()
    {
        resultPanel.SetActive(true);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        popTime = 0f;
        resultTitle.text = "Temps écoulé";
        resultTitle.color = new Color(1f, 0.3f, 0.25f, 1f);
        resultDetails.text = "Score   " + stage.Score;
    }

    void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
