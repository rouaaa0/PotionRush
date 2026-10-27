using UnityEngine;

// Lives on the player. Walking over an ingredient collects it, no tap needed.
public class Interactor : MonoBehaviour
{
    [SerializeField] float pickupRadius = 0.5f;

    readonly Collider[] hits = new Collider[8];

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
}
