using System.Collections;
using UnityEngine;

// Something the player can collect by walking over it. Needs a trigger collider.
// It spins and floats while waiting, then pops and bursts into sparks when collected.
public class Ingredient : MonoBehaviour
{
    public IngredientType type;
    [SerializeField] float spinSpeed = 45f; // degrees per second
    [SerializeField] float bobHeight = 0.12f;
    [SerializeField] float bobSpeed = 2.2f;

    Vector3 startPosition;
    float phase;
    bool collected;

    void Start()
    {
        startPosition = transform.position;
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (collected) return;

        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight);
    }

    public void Collect()
    {
        if (collected) return;

        StageManager stage = StageManager.Instance;
        if (stage == null || !stage.TryCollect(type)) return;

        collected = true;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;

        SpawnSparks();
        StartCoroutine(Pop());
    }

    IEnumerator Pop()
    {
        Vector3 baseScale = transform.localScale;
        Vector3 from = transform.position;
        const float duration = 0.45f;

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            float scale = k < 0.3f ? Mathf.Lerp(1f, 1.4f, k / 0.3f) : Mathf.Lerp(1.4f, 0f, (k - 0.3f) / 0.7f);
            transform.localScale = baseScale * scale;
            transform.position = from + Vector3.up * (k * 1.2f);
            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }

        Destroy(gameObject);
    }

    void SpawnSparks()
    {
        Renderer source = GetComponentInChildren<Renderer>();
        if (source == null) return;

        Vector3 origin = transform.position + Vector3.up * 0.5f;
        for (int i = 0; i < 14; i++)
        {
            GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(spark.GetComponent<Collider>());
            spark.GetComponent<MeshRenderer>().sharedMaterial = source.sharedMaterial;
            spark.transform.position = origin;
            spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.11f);

            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(1.5f, 3.2f), Random.Range(-1f, 1f)) * 1.3f;
            spark.AddComponent<Spark>().Init(velocity, Random.Range(0.4f, 0.8f));
        }
    }
}
