using UnityEngine;

// Lives on the player. A tap on an ingredient collects it when the player is close enough.
public class Interactor : MonoBehaviour
{
    [SerializeField] float range = 3f;

    void Start()
    {
        if (GameInput.Instance != null) GameInput.Instance.Tapped += OnTap;
    }

    void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.Tapped -= OnTap;
    }

    void OnTap(Vector2 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        Ingredient ingredient = hit.collider.GetComponentInParent<Ingredient>();
        if (ingredient == null) return;

        if (Vector3.Distance(transform.position, ingredient.transform.position) <= range)
            ingredient.Collect();
    }
}
