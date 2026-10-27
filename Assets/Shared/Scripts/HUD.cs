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

    void OnWon(int finalScore)
    {
        gaugeRoot.SetActive(false);
        resultPanel.SetActive(true);
        popTime = 0f;

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

    void OnLost()
    {
        gaugeRoot.SetActive(false);
        resultPanel.SetActive(true);
        popTime = 0f;
        resultTitle.text = "Temps écoulé";
        resultDetails.text = "Score   " + stage.Score;
    }

    void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
