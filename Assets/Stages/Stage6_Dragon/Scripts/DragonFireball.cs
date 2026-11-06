using UnityEngine;

// One fireball: it flies from the dragon's mouth to a spot on the floor.
// A ring marks the spot and a disc grows inside it: when the disc fills the ring, it lands.
public class DragonFireball : MonoBehaviour
{
    DragonBoss boss;
    Vector3 start;
    float fallTime;
    float radius;
    float age;
    bool landed;
    Transform ball;
    Transform disc;
    Transform ring;

    public void Launch(DragonBoss owner, Vector3 from, Vector3 target, float seconds, float blastRadius, Mesh ringMesh, Mesh discMesh)
    {
        boss = owner;
        start = from;
        fallTime = seconds;
        radius = blastRadius;
        transform.position = target;

        ring = Flat("Ring", ringMesh, owner.dangerMaterial, 0.05f).transform;
        ring.localScale = new Vector3(radius, 1f, radius);

        disc = Flat("Fill", discMesh, owner.dangerMaterial, 0.06f).transform;
        disc.localScale = Vector3.zero;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(sphere.GetComponent<Collider>());
        sphere.GetComponent<MeshRenderer>().sharedMaterial = owner.fireballMaterial;
        sphere.transform.SetParent(transform, true);
        sphere.transform.position = from;
        sphere.transform.localScale = Vector3.one * 0.7f;
        ball = sphere.transform;
    }

    GameObject Flat(string partName, Mesh mesh, Material material, float height)
    {
        GameObject go = new GameObject(partName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * height;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    void Update()
    {
        StageManager stage = StageManager.Instance;
        if (stage != null && stage.State != StageState.Playing)
        {
            Destroy(gameObject);
            return;
        }

        age += Time.deltaTime;

        if (landed)
        {
            // a quick flash of the whole circle
            float k = (age - fallTime) / 0.2f;
            if (k >= 1f)
            {
                Destroy(gameObject);
                return;
            }
            float size = radius * Mathf.Lerp(1f, 1.2f, k) * (1f - k * k);
            disc.localScale = new Vector3(size, 1f, size);
            return;
        }

        float t = Mathf.Clamp01(age / fallTime);

        float fill = radius * t;
        disc.localScale = new Vector3(fill, 1f, fill);

        // a high arc, faster at the end
        Vector3 position = Vector3.Lerp(start, transform.position, t * t);
        position.y += Mathf.Sin(t * Mathf.PI) * 3.5f;
        ball.position = position;
        ball.Rotate(300f * Time.deltaTime, 500f * Time.deltaTime, 0f);

        if (t >= 1f)
        {
            landed = true;
            Destroy(ball.gameObject);
            ring.gameObject.SetActive(false);
            boss.Explode(transform.position, radius);
        }
    }
}
