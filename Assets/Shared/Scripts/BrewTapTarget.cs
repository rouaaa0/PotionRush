using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

// For stages that brew with touches (StageConfig.tapToBrew, the final stage).
// A glowing circle appears somewhere on the screen while the player is at the cauldron.
// Touching it counts as one "shake" and the circle jumps to a new place; touching anywhere else does nothing.
// GameInput creates it when needed and asks it every frame whether the circle was touched.
public class BrewTapTarget : MonoBehaviour
{
    const float SizeOfScreenHeight = 0.2f; // circle diameter, as a part of the screen height
    const float TouchMargin = 1.2f;        // a finger a little outside the circle still counts
    const float TopBandForHud = 0.24f;     // keep the circle out of the top of the screen (timer, recipe)

    static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

    RectTransform circle;
    Image image;
    float radius;
    float pop; // 1 right after the circle moved, goes down to 0
    bool visible;

    public static BrewTapTarget Create()
    {
        GameObject root = new GameObject("BrewTapTarget");
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // above the HUD

        GameObject go = new GameObject("Circle");
        go.transform.SetParent(root.transform, false);

        BrewTapTarget target = root.AddComponent<BrewTapTarget>();
        target.image = go.AddComponent<Image>();
        target.image.sprite = MakeCircleSprite();
        target.image.raycastTarget = false;
        target.circle = go.GetComponent<RectTransform>();
        go.SetActive(false);
        return target;
    }

    // Called every frame. "active" is true while the player is brewing.
    // Returns true on the frame the circle is touched.
    public bool Poll(bool active)
    {
        active = active && Time.timeScale > 0f;
        if (active != visible)
        {
            visible = active;
            circle.gameObject.SetActive(active);
            if (active) MoveToNewPlace(false);
        }
        if (!active) return false;

        // a small heartbeat so the circle is easy to notice, and a "pop" when it appears
        pop = Mathf.MoveTowards(pop, 0f, Time.deltaTime * 5f);
        float beat = 1f + Mathf.Sin(Time.time * 7f) * 0.06f;
        circle.localScale = Vector3.one * beat * (1f - pop * 0.45f);
        image.color = new Color(Gold.r, Gold.g, Gold.b, 1f - pop * 0.5f);

        if (!TouchedThisFrame()) return false;
        MoveToNewPlace(true);
        return true;
    }

    bool TouchedThisFrame()
    {
        Vector2 centre = circle.position;
        float reach = radius * TouchMargin;

        Touchscreen screen = Touchscreen.current;
        if (screen != null)
        {
            foreach (TouchControl touch in screen.touches)
                if (touch.press.wasPressedThisFrame && Vector2.Distance(touch.position.ReadValue(), centre) <= reach)
                    return true;
        }

        Mouse mouse = Mouse.current; // to test on a computer
        return mouse != null && mouse.leftButton.wasPressedThisFrame &&
               Vector2.Distance(mouse.position.ReadValue(), centre) <= reach;
    }

    void MoveToNewPlace(bool farFromOldPlace)
    {
        float diameter = Screen.height * SizeOfScreenHeight;
        radius = diameter * 0.5f;
        circle.sizeDelta = new Vector2(diameter, diameter);

        // stay inside the safe area (no notch, no rounded corners) and below the HUD
        Rect safe = Screen.safeArea;
        float edge = radius * 1.3f;
        float xMin = safe.xMin + edge, xMax = safe.xMax - edge;
        float yMin = safe.yMin + edge, yMax = safe.yMax - Screen.height * TopBandForHud - radius;

        Vector2 old = circle.position;
        Vector2 place = old;
        for (int i = 0; i < 20; i++)
        {
            place = new Vector2(Random.Range(xMin, xMax), Random.Range(yMin, yMax));
            if (!farFromOldPlace || Vector2.Distance(place, old) > Screen.width * 0.22f) break;
        }

        circle.position = place;
        pop = 1f;
    }

    // A soft disc with a bright ring, drawn in code so no image file is needed.
    static Sprite MakeCircleSprite()
    {
        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        Color[] pixels = new Color[size * size];
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(half, half)) / half; // 0 centre, 1 edge
                float outside = Mathf.Clamp01((1f - d) * 20f);          // smooth outer edge
                float ring = Mathf.Clamp01(1f - Mathf.Abs(d - 0.86f) * 9f); // bright ring near the edge
                float fill = 0.35f;                                      // see-through middle
                float alpha = outside * Mathf.Max(fill, ring);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
