using UnityEngine;

// Something the player can tap to collect. Needs a trigger collider so taps can hit it.
public class Ingredient : MonoBehaviour
{
    public IngredientType type;
    [SerializeField] float spinSpeed = 45f; // degrees per second

    void Update()
    {
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
    }

    public void Collect()
    {
        StageManager stage = StageManager.Instance;
        if (stage == null || !stage.TryCollect(type)) return;

        Destroy(gameObject);
    }
}
