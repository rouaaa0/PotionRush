using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The "Choisir un niveau" screen: one card per stage in the StageCatalog.
// The cards are rebuilt every time the screen opens, so scores and locks are always up to date.
public class StageSelect : MonoBehaviour
{
    public StageCatalog catalog;
    public Font font;
    public Sprite roundedSprite;
    public RectTransform cardsRoot;

    static readonly Color Gold = new Color(0.95f, 0.71f, 0.27f, 1f);
    static readonly Color Lavender = new Color(0.85f, 0.8f, 0.95f, 1f);
    static readonly Color Mint = new Color(0.37f, 0.89f, 0.63f, 1f);
    static readonly Color Grey = new Color(0.6f, 0.57f, 0.68f, 1f);

    void OnEnable()
    {
        Rebuild();
    }

    void Rebuild()
    {
        for (int i = cardsRoot.childCount - 1; i >= 0; i--) Destroy(cardsRoot.GetChild(i).gameObject);
        for (int i = 0; i < catalog.stages.Count; i++) MakeCard(i, catalog.stages[i]);
    }

    void MakeCard(int index, StageInfo info)
    {
        bool exists = Application.CanStreamedLevelBeLoaded(info.sceneName);
        bool unlocked = GameProgress.IsUnlocked(catalog, index);
        bool playable = exists && unlocked;

        // 3 cards per row
        Vector2 position = new Vector2((index % 3 - 1) * 500f, 170f - (index / 3) * 340f);

        GameObject outer = NewImage(cardsRoot, "Card" + (index + 1), new Color(Gold.r, Gold.g, Gold.b, playable ? 0.7f : 0.25f), position, new Vector2(460f, 300f));
        outer.GetComponent<Image>().raycastTarget = true;

        GameObject inner = NewImage(outer.transform, "Inner", new Color(0.09f, 0.055f, 0.14f, playable ? 0.95f : 0.8f), Vector2.zero, Vector2.zero);
        inner.GetComponent<Image>().raycastTarget = false;
        RectTransform innerRect = inner.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(4f, 4f);
        innerRect.offsetMax = new Vector2(-4f, -4f);

        float fade = playable ? 1f : 0.5f;
        NewText(outer.transform, "Number", (index + 1).ToString(), 130, TextAnchor.MiddleCenter, Gold * fade, new Vector2(-150f, 0f), new Vector2(150f, 160f)).fontStyle = FontStyle.Bold;
        NewText(outer.transform, "Name", info.displayName, 44, TextAnchor.MiddleLeft, Color.white * fade, new Vector2(80f, 70f), new Vector2(300f, 60f)).fontStyle = FontStyle.Bold;
        NewText(outer.transform, "Potion", info.potionName, 30, TextAnchor.MiddleLeft, Lavender * fade, new Vector2(80f, 18f), new Vector2(300f, 44f));

        string status;
        Color statusColor;
        if (!exists) { status = "Bientôt"; statusColor = Gold * 0.7f; }
        else if (!unlocked) { status = "Verrouillé"; statusColor = Grey; }
        else if (GameProgress.BestScore(info.sceneName) > 0) { status = "Meilleur  " + GameProgress.BestScore(info.sceneName); statusColor = Mint; }
        else { status = "À jouer"; statusColor = Lavender; }
        NewText(outer.transform, "Status", status, 34, TextAnchor.MiddleLeft, statusColor, new Vector2(80f, -62f), new Vector2(300f, 50f)).fontStyle = FontStyle.Bold;

        Button button = outer.AddComponent<Button>();
        button.targetGraphic = outer.GetComponent<Image>();
        button.interactable = playable;
        ColorBlock colors = button.colors;
        colors.disabledColor = Color.white; // the card is already dimmed by hand
        button.colors = colors;
        string scene = info.sceneName;
        button.onClick.AddListener(() => SceneManager.LoadScene(scene));
    }

    GameObject NewImage(Transform parent, string name, Color color, Vector2 position, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.sprite = roundedSprite;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return go;
    }

    Text NewText(Transform parent, string name, string content, int size, TextAnchor align, Color color, Vector2 position, Vector2 box)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = color;
        t.text = content;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;

        RectTransform rect = t.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = box;
        return t;
    }
}
