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

    StageManager stage;

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
        resultTitle.text = "Temps écoulé";
        resultDetails.text = "Score   " + stage.Score;
    }

    void Replay()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
