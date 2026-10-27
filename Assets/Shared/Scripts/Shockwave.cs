using UnityEngine;

// A flat ring that grows outwards along the floor and then disappears.
public class Shockwave : MonoBehaviour
{
    [SerializeField] float maxSize = 14f;
    [SerializeField] float duration = 0.8f;

    float age;

    void Update()
    {
        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / duration);
        float size = Mathf.Lerp(1f, maxSize, 1f - (1f - k) * (1f - k)); // fast at first, then slows down
        float thickness = Mathf.Lerp(0.08f, 0f, k);
        transform.localScale = new Vector3(size, thickness, size);

        if (age >= duration) Destroy(gameObject);
    }
}
