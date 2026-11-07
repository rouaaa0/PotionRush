using UnityEngine;
using UnityEngine.UI;

// Stage 4: short messages in the middle of the screen ("Attention aux livres volants !", "Aïe !").
// It adds its own text to the shared HUD at runtime, so the HUD prefab is never changed.
public class LibraryMessage : MonoBehaviour
{
    public string introText = "Attention aux livres volants !";
    public float introDelay = 0.6f;
    public float introDuration = 2.6f;
    [Tooltip("Shown once when the books get angry (keep the same number as the books' Angry Below Seconds).")]
    public string angryText = "Les livres s'énervent !";
    public float angryAt = 30f;

    static LibraryMessage instance;

    Text text;
    float age;
    float duration;
    bool angryAnnounced;

    void Awake()
    {
        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void Start()
    {
        HUD hud = FindAnyObjectByType<HUD>();
        if (hud == null || hud.scoreText == null)
        {
            enabled = false;
            return;
        }

        GameObject go = new GameObject("LibraryMessage", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(hud.transform, false);

        text = go.GetComponent<Text>();
        text.font = hud.scoreText.font;
        text.fontSize = 80;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        Shadow shadow = go.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.7f);
        shadow.effectDistance = new Vector2(5f, -5f);

        RectTransform rect = text.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 160f);
        rect.sizeDelta = new Vector2(1600f, 140f);

        go.SetActive(false);
        Invoke(nameof(ShowIntro), introDelay);
    }

    void ShowIntro()
    {
        Display(introText, introDuration, new Color(0.85f, 0.75f, 1f));
    }

    public static void Show(string message, float seconds)
    {
        Show(message, seconds, new Color(1f, 0.55f, 0.45f));
    }

    public static void Show(string message, float seconds, Color color)
    {
        if (instance != null) instance.Display(message, seconds, color);
    }

    void Display(string message, float seconds, Color color)
    {
        if (text == null) return;
        text.text = message;
        text.color = color;
        age = 0f;
        duration = seconds;
        text.gameObject.SetActive(true);
    }

    void Update()
    {
        StageManager stage = StageManager.Instance;
        if (!angryAnnounced && stage != null && stage.State == StageState.Playing && stage.TimeLeft <= angryAt)
        {
            angryAnnounced = true;
            Display(angryText, 2.2f, new Color(1f, 0.3f, 0.25f));
        }

        if (text == null || !text.gameObject.activeSelf) return;

        age += Time.deltaTime;

        // pop in, then fade out during the last 0.4 seconds
        float pop = Mathf.Clamp01(age / 0.15f);
        text.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, pop);
        Color c = text.color;
        c.a = Mathf.Clamp01((duration - age) / 0.4f);
        text.color = c;

        if (age >= duration) text.gameObject.SetActive(false);
    }
}
