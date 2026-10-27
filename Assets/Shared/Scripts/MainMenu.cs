using System;
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

        // two small switches in the top-right corner
        StageSelect select = stageSelectPanel.GetComponent<StageSelect>();
        Sprite rounded = select != null ? select.roundedSprite : null;
        mainElements.Add(MakeToggle("Son", () => GameSettings.SoundOn, on => GameSettings.SoundOn = on, -30f, rounded));
        mainElements.Add(MakeToggle("Vibration", () => GameSettings.VibrationOn, on => GameSettings.VibrationOn = on, -126f, rounded));
    }

    // A rounded gold-bordered button that flips a setting between OUI and NON.
    GameObject MakeToggle(string label, Func<bool> get, Action<bool> set, float y, Sprite rounded)
    {
        GameObject outer = new GameObject(label + "Toggle", typeof(RectTransform), typeof(Image), typeof(Button));
        outer.transform.SetParent(transform, false);
        Image border = outer.GetComponent<Image>();
        border.color = new Color(0.95f, 0.71f, 0.27f, 0.55f);
        border.sprite = rounded;
        border.type = Image.Type.Sliced;

        RectTransform rect = border.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-30f, y);
        rect.sizeDelta = new Vector2(360f, 84f);

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(outer.transform, false);
        Image fill = inner.GetComponent<Image>();
        fill.color = new Color(0.09f, 0.055f, 0.14f, 0.9f);
        fill.sprite = rounded;
        fill.type = Image.Type.Sliced;
        fill.raycastTarget = false;
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = new Vector2(4f, 4f);
        fill.rectTransform.offsetMax = new Vector2(-4f, -4f);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(outer.transform, false);
        Text text = textObj.GetComponent<Text>();
        text.font = subtitleText.font;
        text.fontSize = 38;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        Action refresh = () =>
        {
            bool on = get();
            text.text = label + "  " + (on ? "OUI" : "NON");
            text.color = on ? new Color(0.37f, 0.89f, 0.63f, 1f) : new Color(0.6f, 0.57f, 0.68f, 1f);
        };
        refresh();

        outer.GetComponent<Button>().onClick.AddListener(() =>
        {
            set(!get());
            refresh();
        });
        return outer;
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
