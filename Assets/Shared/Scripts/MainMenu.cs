using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The buttons of the main menu.
public class MainMenu : MonoBehaviour
{
    const string Tagline = "Prépare la potion avant la fin du temps";

    public Button playButton;
    public Button instructionsButton;
    public Button quitButton;
    public Button closeButton;
    public GameObject instructionsPanel;

    public GameObject stageSelectPanel;
    public Button stageSelectBack;

    // tapping the title 5 times opens every stage (handy for demos)
    public Button titleButton;
    public Text subtitleText;

    readonly List<GameObject> mainElements = new List<GameObject>();
    int titleTaps;
    float lastTitleTap;

    void Start()
    {
        // match the screen height, so wide phones show extra width instead of cutting off the top and bottom
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler != null) scaler.matchWidthOrHeight = 1f;

        foreach (string childName in new[] { "Title", "Subtitle", "PlayButton", "InstructionsButton", "QuitButton" })
        {
            Transform child = transform.Find(childName);
            if (child != null) mainElements.Add(child.gameObject);
        }

        instructionsPanel.SetActive(false);
        stageSelectPanel.SetActive(false);
        ShowTagline();

        playButton.onClick.AddListener(() => OpenPanel(stageSelectPanel, true));
        stageSelectBack.onClick.AddListener(() => OpenPanel(stageSelectPanel, false));
        instructionsButton.onClick.AddListener(() => OpenPanel(instructionsPanel, true));
        closeButton.onClick.AddListener(() => OpenPanel(instructionsPanel, false));
        quitButton.onClick.AddListener(Quit);
        titleButton.onClick.AddListener(OnTitleTapped);
    }

    // The title and buttons are hidden while a panel is open, so nothing shows through it.
    void OpenPanel(GameObject panel, bool open)
    {
        panel.SetActive(open);
        foreach (GameObject element in mainElements) element.SetActive(!open);
    }

    void OnTitleTapped()
    {
        if (Time.time - lastTitleTap > 2f) titleTaps = 0;
        lastTitleTap = Time.time;
        titleTaps++;

        if (titleTaps >= 5)
        {
            titleTaps = 0;
            GameProgress.UnlockAll = !GameProgress.UnlockAll;
            ShowTagline();
        }
    }

    void ShowTagline()
    {
        subtitleText.text = GameProgress.UnlockAll ? "Mode démo : tous les niveaux sont ouverts" : Tagline;
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
