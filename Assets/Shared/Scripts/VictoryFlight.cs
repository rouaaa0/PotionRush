using System.Collections;
using UnityEngine;

// The victory moment of every stage: once the potion is done, the wizard grows golden wings and
// flies up in a spiral, leaving gold sparks behind. The camera follows the player, so the room
// falls away below. StageManager adds this to every stage, so no scene needs to set it up.
public class VictoryFlight : MonoBehaviour
{
    static readonly Color Gold = new Color(1f, 0.76f, 0.3f);

    StageManager stage;
    Material goldMaterial;

    void OnEnable()
    {
        stage = GetComponent<StageManager>();
        if (stage != null) stage.StageWon += OnWon;
    }

    void OnDisable()
    {
        if (stage != null) stage.StageWon -= OnWon;
    }

    void OnWon(int score)
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null) StartCoroutine(FlyAway(player));
    }

    IEnumerator FlyAway(PlayerController wizard)
    {
        yield return new WaitForSeconds(0.5f);
        if (wizard == null) yield break;

        Transform player = wizard.transform;
        wizard.CanMove = false;

        // the CharacterController must be off, or it overrides a direct position change
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        goldMaterial = MakeGold();
        Transform model = player.Find("Model");
        if (model == null) model = player;
        Transform leftWing = BuildWing(model, -1f);
        Transform rightWing = BuildWing(model, 1f);

        Vector3 start = player.position;
        float trailTimer = 0f;

        for (float t = 0f; player != null; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 2.2f);
            float angle = t * 1.4f;
            float height = k * 7f + Mathf.Sin(t * 2.2f) * 0.15f * k;
            Vector3 circle = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (k * 2.2f);
            player.position = start + Vector3.up * height + circle;
            player.rotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg + 90f, 0f);

            float grow = Mathf.Clamp01(t / 0.3f);
            float flap = Mathf.Sin(t * 14f) * 35f;
            SetWing(leftWing, -flap, grow);
            SetWing(rightWing, flap, grow);

            trailTimer -= Time.deltaTime;
            if (trailTimer <= 0f)
            {
                trailTimer = 0.06f;
                SpawnSparks(player.position + Vector3.up * 0.8f, 2);
            }

            yield return null;
        }
    }

    Transform BuildWing(Transform model, float side)
    {
        Transform pivot = new GameObject("WingPivot").transform;
        pivot.SetParent(model, false);
        pivot.localPosition = new Vector3(side * 0.2f, 1.05f, -0.15f);

        GameObject wing = Visual(goldMaterial, pivot);
        wing.transform.localPosition = new Vector3(side * 0.55f, 0f, 0f);
        wing.transform.localScale = new Vector3(1f, 0.05f, 0.45f);

        pivot.localScale = Vector3.zero;
        return pivot;
    }

    static void SetWing(Transform pivot, float flap, float size)
    {
        pivot.localRotation = Quaternion.Euler(0f, 0f, flap);
        pivot.localScale = Vector3.one * size;
    }

    void SpawnSparks(Vector3 origin, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject spark = Visual(goldMaterial, null);
            spark.transform.position = origin;
            spark.transform.localScale = Vector3.one * Random.Range(0.06f, 0.12f);

            Vector3 velocity = new Vector3(Random.Range(-1f, 1f), Random.Range(1f, 2f), Random.Range(-1f, 1f)).normalized * Random.Range(2.5f, 5f);
            spark.AddComponent<Spark>().Init(velocity, Random.Range(0.3f, 0.6f));
        }
    }

    // A sphere used only for looks: no collider, so it never gets in the way.
    static GameObject Visual(Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = material;
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    static Material MakeGold()
    {
        Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", Gold);
        m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        m.SetColor("_EmissionColor", Gold * 1.4f);
        return m;
    }
}
