using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Shows the score, timer, recipe, brewing gauge and the end screen.
// It only reads from the StageManager and never changes the rules.
public class HUD : MonoBehaviour
{
    public Text scoreText;
    public Text timerText;
    public Text recipeText;
    public GameObject gaugeRoot;
    public Image gaugeFill;
    public Text gaugeLabel;
    public GameObject resultPanel;
    public Text resultTitle;
    public Text resultDetails;
    public Button replayButton;
    public GameObject hintRoot;
    public Text hintText;

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

        gaugeRoot.SetActive(false);
        resultPanel.SetActive(false);
        card = resultPanel.transform.Find("Card");

        stage.BrewProgressed += OnBrewProgressed;
        stage.StageWon += OnWon;
        stage.StageLost += OnLost;
        replayButton.onClick.AddListener(Replay);
    }

    void OnDestroy()
    {
        if (stage == null) return;
        stage.BrewProgressed -= OnBrewProgressed;
        stage.StageWon -= OnWon;
        stage.StageLost -= OnLost;
    }

    void Update()
    {
        scoreText.text = "Score  " + stage.Score;

        int seconds = Mathf.CeilToInt(stage.TimeLeft);
        timerText.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");

        StringBuilder sb = new StringBuilder("OBJECTIFS");
        foreach (RecipeEntry entry in stage.config.recipe)
            sb.Append("\n").Append(entry.type).Append("  ").Append(stage.Collected(entry.type)).Append("/").Append(entry.amount);
        recipeText.text = sb.ToString();

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

    // The score bumps when it changes and the end card pops in.
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
