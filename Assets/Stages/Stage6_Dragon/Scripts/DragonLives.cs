using UnityEngine;
using UnityEngine.UI;

// The wizard's lives in the boss fight. The hearts are added under the objectives on the HUD.
// When the last one is lost the stage ends, exactly as if the timer had run out,
// and the end card says that the dragon won. The shared HUD and StageManager stay untouched.
public class DragonLives : MonoBehaviour
{
    static readonly Color Lost = new Color(1f, 1f, 1f, 0.2f);
    static readonly Color Full = new Color(1f, 0.25f, 0.25f, 1f);

    public int Left { get; private set; }

    StageManager stage;
    HUD hud;
    Image[] hearts;
    float[] punch;
    bool built;
    bool killed;
    string frozenTimer;

    public void Init(int lives, StageManager stageManager, HUD hudOnScreen)
    {
        Left = lives;
        stage = stageManager;
        hud = hudOnScreen;
    }

    // Takes one life away. Returns true when it was the last one.
    public bool LoseOne()
    {
        if (Left <= 0) return true;

        Left--;
        if (built)
        {
            hearts[Left].color = Lost;
            punch[Left] = 1f;
        }

        if (Left > 0) return false;

        killed = true;
        if (hud != null) frozenTimer = hud.timerText.text;
        EndStage();
        return true;
    }

    // The StageManager ends the stage by itself when its timer reaches zero, but only it may change
    // that timer. So this sets it through reflection instead of changing the shared script.
    void EndStage()
    {
        typeof(StageManager).GetProperty("TimeLeft").GetSetMethod(true).Invoke(stage, new object[] { 0f });
    }

    void LateUpdate()
    {
        if (hud == null) return;

        // every Start has run by now, so the HUD has made its objectives card
        if (!built) BuildHearts();

        for (int i = 0; i < hearts.Length; i++)
        {
            punch[i] = Mathf.MoveTowards(punch[i], 0f, Time.deltaTime * 2.5f);
            hearts[i].transform.localScale = Vector3.one * (1f + punch[i] * 0.8f);
        }

        // runs after the HUD: the time didn't really run out, so keep the real time and the right title
        if (killed)
        {
            hud.timerText.text = frozenTimer;
            hud.timerText.color = Color.white;
            hud.timerText.transform.localScale = Vector3.one;
            hud.resultTitle.text = "Vaincu par le dragon";
        }
    }

    void BuildHearts()
    {
        built = true;

        RectTransform card = hud.objectivesCard;
        float y = -(card.sizeDelta.y - 22f);
        card.sizeDelta = new Vector2(card.sizeDelta.x, card.sizeDelta.y + 52f);

        GameObject labelObject = new GameObject("LivesLabel", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(card, false);
        Text label = labelObject.GetComponent<Text>();
        label.font = hud.scoreText.font;
        label.fontSize = 34;
        label.color = Color.white;
        label.text = "Vies";
        label.alignment = TextAnchor.UpperLeft;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(28f, y), new Vector2(120f, 40f));

        Sprite sprite = MakeHeart();
        int total = Left;
        hearts = new Image[total];
        punch = new float[total];
        for (int i = 0; i < total; i++)
        {
            GameObject heart = new GameObject("Heart", typeof(RectTransform), typeof(Image));
            heart.transform.SetParent(card, false);
            hearts[i] = heart.GetComponent<Image>();
            hearts[i].sprite = sprite;
            hearts[i].color = Full;
            hearts[i].raycastTarget = false;
            // the first heart is on the left, and the hearts are lost from the right
            Place(hearts[i].rectTransform, new Vector2(1f, 1f), new Vector2(-28f - (total - 1 - i) * 46f, y - 2f), new Vector2(40f, 40f));
        }
    }

    static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    // A white heart drawn from its maths formula, so there is no image to import.
    static Sprite MakeHeart()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f - size * 0.5f) / (size * 0.5f) * 1.25f;
                float y = (py + 0.5f - size * 0.5f) / (size * 0.5f) * 1.25f + 0.05f;
                float a = x * x + y * y - 1f;
                bool inside = a * a * a - x * x * y * y * y <= 0f;
                texture.SetPixel(px, py, inside ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }
}
