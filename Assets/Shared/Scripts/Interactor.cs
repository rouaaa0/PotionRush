using UnityEngine;
using UnityEngine.EventSystems;

// Lives on the player. Walking over an ingredient or tapping it on mobile collects it.
public class Interactor : MonoBehaviour
{
    [SerializeField] float pickupRadius = 0.5f;
    [SerializeField] float tapMaxReach = 5.0f;

    readonly Collider[] hits = new Collider[8];

    void Start()
    {
        if (GameInput.Instance != null) GameInput.Instance.Tapped += OnTapped;
    }

    void OnDestroy()
    {
        if (GameInput.Instance != null) GameInput.Instance.Tapped -= OnTapped;
    }

    void Update()
    {
        Vector3 center = transform.position + Vector3.up * 0.9f;
        int count = Physics.OverlapSphereNonAlloc(center, pickupRadius, hits, ~0, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Ingredient ingredient = hits[i].GetComponentInParent<Ingredient>();
            if (ingredient != null) ingredient.Collect();
        }
    }

    void OnTapped(Vector2 screenPos)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Collide))
        {
            Ingredient ingredient = hit.collider.GetComponentInParent<Ingredient>();
            if (ingredient != null)
            {
                if (Vector3.Distance(transform.position, ingredient.transform.position) <= tapMaxReach)
                {
                    ingredient.Collect();
                }
            }
        }
    }
}
