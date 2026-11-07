using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The show of stage 5, added automatically by TowerStorm. It only listens to the StageManager:
// - every crystal calls lightning onto a pillar and lights it up, every scroll wakes the rune circle
// - when the recipe is complete, a column of light shows the way to the cauldron
// - while brewing, the lit pillars fire lightning into the cauldron on every shake
// - the potion of flight is done: the storm clears (the shared VictoryFlight makes the wizard fly up)
// - stones and pages caught in the storm circle the tower the whole time
public class TowerSpectacle : MonoBehaviour
{
    class Orb
    {
        public Transform transform;
        public Material material;
        public Light light;
        public Vector3 baseScale;
        public bool lit;
    }

    static readonly Color OrbOff = new Color(0.12f, 0.15f, 0.22f);
    static readonly Color OrbOn = new Color(0.6f, 0.9f, 1f);
    static readonly Color Gold = new Color(1f, 0.76f, 0.3f);

    TowerStorm storm;
    StageManager stage;
    Transform player;
    Vector3 cauldronTop;

    readonly List<Orb> orbs = new List<Orb>();
    Material runeMaterial;
    Color runeColor;
    Material goldMaterial;
    GameObject beacon;
    float beaconSparkTimer;
    float arcTimer = 1f;

    Transform vortex;
    Transform[] debris;
    float[] debrisHeight;
    float[] debrisPhase;

    void Start()
    {
        storm = GetComponent<TowerStorm>();
        stage = StageManager.Instance;
        player = FindFirstObjectByType<PlayerController>().transform;
        cauldronTop = FindFirstObjectByType<Cauldron>().transform.position + Vector3.up * 1.1f;

        goldMaterial = new Material(storm.warningMaterial);
        goldMaterial.SetColor("_BaseColor", Gold);
        goldMaterial.SetColor("_EmissionColor", Gold * 1.4f);

        FindOrbs();
        FindRunes();
        BuildVortex();

        stage.IngredientCollected += OnCollected;
        stage.BrewProgressed += OnBrewProgressed;
        stage.StageWon += OnWon;
        stage.StageLost += OnLost;
    }

    void OnDestroy()
    {
        if (stage == null) return;
        stage.IngredientCollected -= OnCollected;
        stage.BrewProgressed -= OnBrewProgressed;
        stage.StageWon -= OnWon;
        stage.StageLost -= OnLost;
    }

    // The pillars start dark and the crystals bring them to life.
    void FindOrbs()
    {
        GameObject tower = GameObject.Find("TowerTop");
        if (tower == null) return;

        foreach (Transform child in tower.transform)
        {
            if (child.name != "PillarOrb") continue;

            Orb orb = new Orb { transform = child, baseScale = child.localScale };
            orb.material = child.GetComponent<Renderer>().material; // a copy, the asset stays untouched
            orb.material.SetColor("_BaseColor", OrbOff);
            orb.material.SetColor("_EmissionColor", Color.black);

            orb.light = child.gameObject.AddComponent<Light>();
            orb.light.type = LightType.Point;
            orb.light.color = OrbOn;
            orb.light.range = 5f;
            orb.light.intensity = 0f;

            orbs.Add(orb);
        }
    }

    void FindRunes()
    {
        GameObject ring = GameObject.Find("RuneRing");
        if (ring == null) return;

        runeMaterial = ring.GetComponent<Renderer>().material;
        runeColor = runeMaterial.GetColor("_BaseColor");
        SetRuneGlow(0.1f);
    }

    void SetRuneGlow(float amount)
    {
        if (runeMaterial != null) runeMaterial.SetColor("_EmissionColor", runeColor * amount);
    }

    // Stones, planks, pages and glowing shards caught in the wind, slowly circling the tower.
    void BuildVortex()
    {
        vortex = new GameObject("Vortex").transform;

        Material stone = new Material(storm.boltMaterial);
        stone.SetColor("_BaseColor", new Color(0.22f, 0.24f, 0.32f));
        stone.SetColor("_EmissionColor", Color.black);
        Material page = new Material(storm.boltMaterial);
        page.SetColor("_BaseColor", new Color(0.9f, 0.8f, 0.55f));
        page.SetColor("_EmissionColor", new Color(0.25f, 0.2f, 0.12f));

        const int count = 24;
        debris = new Transform[count];
        debrisHeight = new float[count];
        debrisPhase = new float[count];

        Random.InitState(9);
        for (int i = 0; i < count; i++)
        {
            int kind = i % 4;
            Material material = kind == 3 ? storm.boltMaterial : kind == 2 ? page : stone;
            GameObject piece = TowerStorm.Visual(PrimitiveType.Cube, material, vortex);

            float angle = i * 15f * Mathf.Deg2Rad + Random.Range(-0.1f, 0.1f);
            float radius = Random.Range(11.5f, 18f);
            debrisHeight[i] = Random.Range(-3f, 3f);
            debrisPhase[i] = Random.Range(0f, Mathf.PI * 2f);
            piece.transform.localPosition = new Vector3(Mathf.Sin(angle) * radius, debrisHeight[i], Mathf.Cos(angle) * radius);

            if (kind == 2) piece.transform.localScale = new Vector3(0.6f, 0.03f, 0.8f);       // a page
            else if (kind == 3) piece.transform.localScale = Vector3.one * Random.Range(0.15f, 0.28f); // a shard
            else piece.transform.localScale = new Vector3(Random.Range(0.4f, 1f), Random.Range(0.3f, 0.7f), Random.Range(0.4f, 1f));

            piece.transform.localRotation = Random.rotation;
            debris[i] = piece.transform;
        }
        Random.InitState(System.Environment.TickCount); // the lightning must not repeat because of the fixed seed above
    }

    void Update()
    {
        AnimateVortex();
        AnimateOrbs();
        AnimateBeacon();
    }

    void AnimateVortex()
    {
        vortex.Rotate(0f, 10f * Time.deltaTime, 0f, Space.World);

        for (int i = 0; i < debris.Length; i++)
        {
            Vector3 p = debris[i].localPosition;
            p.y = debrisHeight[i] + Mathf.Sin(Time.time * 0.8f + debrisPhase[i]) * 0.6f;
            debris[i].localPosition = p;
            debris[i].Rotate(40f * Time.deltaTime, 70f * Time.deltaTime, 0f, Space.Self);
        }
    }

    void AnimateOrbs()
    {
        Orb first = null;
        Orb second = null;

        foreach (Orb orb in orbs)
        {
            if (!orb.lit) continue;

            float pulse = 1f + Mathf.Sin(Time.time * 5f + orb.baseScale.x * 9f) * 0.12f;
            orb.transform.localScale = orb.baseScale * 1.25f * pulse;
            orb.light.intensity = 3f * pulse;

            // pick two different lit pillars, in a different order each time
            if (first == null || Random.value < 0.4f) { second = first; first = orb; }
            else if (second == null || Random.value < 0.5f) second = orb;
        }

        // electricity jumps between the lit pillars
        arcTimer -= Time.deltaTime;
        if (arcTimer <= 0f && first != null && second != null && stage.State == StageState.Playing)
        {
            arcTimer = Random.Range(0.5f, 1.2f);
            storm.SpawnBolt(first.transform.position, second.transform.position, 0.05f, false, false);
        }
    }

    void AnimateBeacon()
    {
        if (beacon == null) return;

        bool show = !stage.Brewing;
        beacon.SetActive(show);
        SetRuneGlow(1.2f + Mathf.Sin(Time.time * 6f) * 0.6f);
        if (!show) return;

        float width = 0.35f + Mathf.Sin(Time.time * 7f) * 0.08f;
        beacon.transform.localScale = new Vector3(width, 10f, width);

        beaconSparkTimer -= Time.deltaTime;
        if (beaconSparkTimer <= 0f)
        {
            beaconSparkTimer = 0.15f;
            TowerStorm.SpawnSparks(cauldronTop, 2, goldMaterial);
        }
    }

    void OnCollected(IngredientType type)
    {
        if (type == IngredientType.Crystal) LightNextOrb();
        else if (type == IngredientType.Scroll) WakeRunes();

        if (stage.RecipeComplete) ShowTheWay();
    }

    // Lightning falls on the pillar closest to the wizard and it stays lit.
    void LightNextOrb()
    {
        Orb closest = null;
        float best = float.MaxValue;
        foreach (Orb orb in orbs)
        {
            float distance = (orb.transform.position - player.position).sqrMagnitude;
            if (!orb.lit && distance < best)
            {
                best = distance;
                closest = orb;
            }
        }
        if (closest == null) return;

        closest.lit = true;
        closest.material.SetColor("_BaseColor", OrbOn);
        closest.material.SetColor("_EmissionColor", OrbOn * 2.5f);

        Vector3 top = closest.transform.position;
        storm.SpawnBolt(top + Vector3.up * 14f, top, 0.14f, true, false);
        storm.SpawnBolt(player.position + Vector3.up * 1f, top, 0.06f, false, false);
        storm.SpawnSparks(top, 12);
        storm.Flash(0.5f, 0.1f);
        storm.PlayThunder(0.45f, 1.3f);
    }

    void WakeRunes()
    {
        int have = stage.Collected(IngredientType.Scroll);
        int need = Mathf.Max(1, stage.Required(IngredientType.Scroll));
        SetRuneGlow(Mathf.Lerp(0.1f, 1.2f, have / (float)need));

        TowerStorm.SpawnSparks(cauldronTop, 14, goldMaterial);
        storm.Flash(0.25f, 0.05f);
    }

    // Everything is collected: a column of golden light rises from the cauldron.
    void ShowTheWay()
    {
        beacon = TowerStorm.Visual(PrimitiveType.Cylinder, goldMaterial, null);
        beacon.name = "Beacon";
        beacon.transform.position = cauldronTop + Vector3.up * 10f;

        storm.SpawnBolt(cauldronTop + Vector3.up * 18f, cauldronTop, 0.25f, true, true);
        storm.Flash(0.8f, 0.2f);
        storm.PlayThunder(0.8f, 1f);
    }

    // The lit pillars charge the potion. The closer it is to ready, the wilder it gets.
    void OnBrewProgressed(float progress)
    {
        foreach (Orb orb in orbs)
            if (orb.lit) storm.SpawnBolt(orb.transform.position, cauldronTop, 0.07f + progress * 0.05f, false, false);

        storm.Flash(0.12f + progress * 0.2f, 0.04f + progress * 0.14f); // kept soft: shakes can come 4 times a second
        storm.PlayThunder(0.2f + progress * 0.3f, 0.9f + progress * 0.5f);
    }

    void OnLost()
    {
        if (beacon != null) Destroy(beacon);
        storm.SpawnBolt(cauldronTop + Vector3.up * 20f, cauldronTop, 0.35f, true, true);
        storm.Flash(1.2f, 0.4f);
        storm.PlayThunder(1f, 0.8f);
    }

    void OnWon(int score)
    {
        if (beacon != null) Destroy(beacon);
        storm.Calm = true;

        storm.SpawnBolt(cauldronTop + Vector3.up * 22f, cauldronTop, 0.45f, true, true);
        storm.Flash(1.5f, 0.5f);
        storm.PlayThunder(1f, 0.8f);

        StartCoroutine(ClearSky());
    }

    // The clouds turn gold, the rain stops and warm light comes back.
    IEnumerator ClearSky()
    {
        GameObject rain = GameObject.Find("Rain");
        if (rain != null) rain.GetComponent<ParticleSystem>().Stop(true, ParticleSystemStopBehavior.StopEmitting);

        Material cloudMaterial = null;
        Color cloudStart = Color.black;
        if (storm.clouds != null && storm.clouds.childCount > 0)
        {
            cloudMaterial = new Material(storm.clouds.GetChild(0).GetComponent<Renderer>().sharedMaterial);
            cloudStart = cloudMaterial.GetColor("_BaseColor");
            foreach (Transform cloud in storm.clouds) cloud.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
        }

        Color skyStart = storm.SkyColor;
        Color skyEnd = new Color(0.98f, 0.62f, 0.4f);
        Color cloudEnd = new Color(1f, 0.82f, 0.68f);
        float lightStart = storm.LightLevel;
        Color sunStart = storm.mainLight != null ? storm.mainLight.color : Color.white;
        Color sunEnd = new Color(1f, 0.85f, 0.65f);
        Color ambientStart = RenderSettings.ambientLight;
        Color ambientEnd = new Color(0.65f, 0.58f, 0.52f);

        const float duration = 3f;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);

            storm.SkyColor = Color.Lerp(skyStart, skyEnd, k);
            storm.LightLevel = Mathf.Lerp(lightStart, 1.8f, k);
            if (storm.mainLight != null) storm.mainLight.color = Color.Lerp(sunStart, sunEnd, k);
            RenderSettings.ambientLight = Color.Lerp(ambientStart, ambientEnd, k);
            if (cloudMaterial != null) cloudMaterial.SetColor("_BaseColor", Color.Lerp(cloudStart, cloudEnd, k));

            yield return null;
        }
    }
}
