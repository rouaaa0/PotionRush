using UnityEngine;

// A tiny ball that flies out, falls and shrinks away. Used for the collect effect.
public class Spark : MonoBehaviour
{
    Vector3 velocity;
    Vector3 baseScale;
    float life;
    float age;

    public void Init(Vector3 startVelocity, float lifetime)
    {
        velocity = startVelocity;
        life = lifetime;
        baseScale = transform.localScale;
    }

    void Update()
    {
        age += Time.deltaTime;
        velocity += Vector3.down * 6f * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;
        transform.localScale = baseScale * Mathf.Max(0f, 1f - age / life);

        if (age >= life) Destroy(gameObject);
    }
}
