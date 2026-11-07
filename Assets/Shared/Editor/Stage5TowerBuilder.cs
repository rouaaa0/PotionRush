using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Adds a menu item that creates the scene of stage 5, "Tour de l'orage".
// It starts from a copy of stage 1, so the player, HUD, camera and input work exactly the same,
// then replaces the lab with the top of a stormy tower and adds the recipe of the flying potion.
public static class Stage5TowerBuilder
{
    const string StageFolder = "Assets/Stages/Stage5_Tower";
    const string ScenePath = StageFolder + "/Stage5_Tower.unity";
    const string MaterialFolder = "Assets/Shared/Art";
    const string SourceScene = "Assets/Stages/Stage1_Lab/Stage1_Lab.unity";
    const string ConfigPath = "Assets/Shared/Data/FlyingPotion.asset";
    const string CrystalPrefab = "Assets/Shared/Prefabs/Crystal.prefab";
    const string ScrollPrefab = "Assets/Shared/Prefabs/TowerScroll.prefab";

    const float TowerRadius = 9f;

    [MenuItem("Potion Rush/Build Stage 5 (Tower)")]
    static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("Stop Play mode first, then build stage 5.");
            return;
        }
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning(ScenePath + " already exists. Delete it first if you really want to rebuild it.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.CopyAsset(SourceScene, ScenePath))
        {
            Debug.LogError("Could not copy " + SourceScene);
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        RemoveLab(scene);
        FindInScene<StageManager>(scene).config = GetConfig();
        Light sun = SetUpLighting(scene);

        GameObject tower = BuildTower();
        Transform clouds = BuildClouds();
        BuildRain();
        PlaceGameplay(scene);
        BuildStorm(sun, clouds);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        BuildSettingsUpdater.Apply();

        Selection.activeGameObject = tower;
        Debug.Log("Stage 5 created: " + ScenePath + ". Press Play to test it.");
    }

    // Everything from the copied scene that belongs to the lab.
    static void RemoveLab(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "LabRoom" || root.name == "LabDecor" || root.GetComponent<Ingredient>() != null)
                Object.DestroyImmediate(root);
        }
    }

    static StageConfig GetConfig()
    {
        StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(ConfigPath);
        if (config != null) return config;

        config = ScriptableObject.CreateInstance<StageConfig>();
        config.stageName = "Tour de l'orage";
        config.potionName = "Potion de vol";
        config.timeLimit = 90f;
        config.shakesRequired = 15;
        config.recipe = new List<RecipeEntry>
        {
            new RecipeEntry { type = IngredientType.Crystal, amount = 4 },
            new RecipeEntry { type = IngredientType.Scroll, amount = 2 },
        };
        AssetDatabase.CreateAsset(config, ConfigPath);
        return config;
    }

    // A dark night sky and a cold, dim light. Returns the main light so the lightning can flash it.
    static Light SetUpLighting(Scene scene)
    {
        Light sun = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
                if (light.type == LightType.Directional) sun = light;
        }
        if (sun != null)
        {
            sun.color = new Color(0.6f, 0.68f, 0.95f);
            sun.intensity = 0.9f;
        }

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.26f, 0.36f);

        Camera cam = FindInScene<Camera>(scene);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.1f);
        return sun;
    }

    static GameObject BuildTower()
    {
        Material stone = GetMaterial("TowerStone", new Color32(0x3A, 0x3F, 0x52, 255));
        Material floor = GetMaterial("TowerFloor", new Color32(0x4E, 0x52, 0x68, 255));
        Material floorDark = GetMaterial("TowerFloorDark", new Color32(0x40, 0x43, 0x57, 255));
        Material rune = GetMaterial("TowerRune", new Color32(0x6F, 0xD3, 0xFF, 255), 1f);
        Material orb = GetMaterial("TowerOrb", new Color32(0x9A, 0xE4, 0xFF, 255), 1.6f);

        GameObject root = new GameObject("TowerTop");

        // the round floor and the tower going down into the clouds; a cylinder's own collider is a capsule,
        // so an invisible flat box is what the wizard actually walks on
        Part(root, PrimitiveType.Cylinder, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(TowerRadius * 2f, 0.25f, TowerRadius * 2f), Quaternion.identity, floor);
        Part(root, PrimitiveType.Cylinder, "FloorCenter", new Vector3(0f, 0.005f, 0f), new Vector3(10f, 0.004f, 10f), Quaternion.identity, floorDark);
        Part(root, PrimitiveType.Cylinder, "TowerBody", new Vector3(0f, -20.5f, 0f), new Vector3(TowerRadius * 2.1f, 20f, TowerRadius * 2.1f), Quaternion.identity, stone);
        BoxCollider ground = new GameObject("FloorCollider").AddComponent<BoxCollider>();
        ground.transform.SetParent(root.transform, false);
        ground.size = new Vector3(30f, 1f, 30f);
        ground.center = new Vector3(0f, -0.5f, 0f);

        // a glowing circle under the cauldron
        Part(root, PrimitiveType.Cylinder, "RuneRing", new Vector3(0f, 0.01f, -5.5f), new Vector3(3.6f, 0.004f, 3.6f), Quaternion.identity, rune);
        Part(root, PrimitiveType.Cylinder, "RuneCenter", new Vector3(0f, 0.015f, -5.5f), new Vector3(3.2f, 0.004f, 3.2f), Quaternion.identity, floorDark);

        BuildWalls(root, stone);

        foreach (Vector2 p in new[] { new Vector2(-3.5f, -1.5f), new Vector2(3.5f, -1.5f), new Vector2(-3f, 3f), new Vector2(3f, 3f) })
        {
            Part(root, PrimitiveType.Cube, "Pillar", new Vector3(p.x, 1.2f, p.y), new Vector3(1f, 2.4f, 1f), Quaternion.identity, stone, true);
            Part(root, PrimitiveType.Sphere, "PillarOrb", new Vector3(p.x, 2.7f, p.y), Vector3.one * 0.5f, Quaternion.identity, orb);
        }

        return root;
    }

    // A ring of wall pieces. The side near the camera stays low so the wall never hides the wizard,
    // and invisible colliders of the same height all around keep the wizard on the tower.
    static void BuildWalls(GameObject root, Material stone)
    {
        const int pieces = 24;
        GameObject walls = new GameObject("Walls");
        walls.transform.SetParent(root.transform, false);

        for (int i = 0; i < pieces; i++)
        {
            float angle = i * 360f / pieces;
            Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
            Vector3 outward = rotation * Vector3.forward;
            Vector3 position = outward * TowerRadius;

            bool nearCamera = outward.z > 0.2f;
            float height = nearCamera ? 0.5f : (i % 2 == 0 ? 2.6f : 1.8f); // battlements on the far side

            Part(walls, PrimitiveType.Cube, "Wall", position + Vector3.up * (height / 2f), new Vector3(2.5f, height, 0.6f), rotation, stone);

            GameObject block = new GameObject("WallCollider");
            block.transform.SetParent(walls.transform, false);
            block.transform.localPosition = position;
            block.transform.localRotation = rotation;
            BoxCollider box = block.AddComponent<BoxCollider>();
            box.size = new Vector3(2.6f, 6f, 1.2f);
            box.center = new Vector3(0f, 3f, 0f);
        }
    }

    static Transform BuildClouds()
    {
        Material cloud = GetMaterial("TowerCloud", new Color32(0x2A, 0x2D, 0x3A, 255));
        GameObject root = new GameObject("Clouds");

        Random.InitState(5);
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f * Mathf.Deg2Rad;
            float distance = Random.Range(14f, 22f);
            Vector3 position = new Vector3(Mathf.Sin(angle) * distance, Random.Range(-6f, -2.5f), Mathf.Cos(angle) * distance);
            Vector3 scale = new Vector3(Random.Range(8f, 13f), Random.Range(1.5f, 3f), Random.Range(6f, 10f));
            Part(root, PrimitiveType.Sphere, "Cloud", position, scale, Quaternion.identity, cloud);
        }
        return root.transform;
    }

    static void BuildRain()
    {
        Material mat = GetAsset("TowerRain", () =>
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetColor("_BaseColor", new Color(0.6f, 0.7f, 0.95f));
            return m;
        });

        GameObject rain = new GameObject("Rain");
        rain.transform.position = new Vector3(0f, 12f, 0f);
        rain.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // particles leave along the object's forward: straight down

        ParticleSystem system = rain.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.prewarm = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 16f;
        main.startSize = 0.04f;
        main.maxParticles = 400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 250f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(26f, 26f, 1f);

        ParticleSystemRenderer renderer = rain.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;
        renderer.velocityScale = 0.05f;
    }

    // The wizard, the cauldron and the ingredients. The player starts near the camera
    // and the cauldron is on the far side, so the whole tower has to be crossed.
    static void PlaceGameplay(Scene scene)
    {
        PlayerController player = FindInScene<PlayerController>(scene);
        player.transform.position = new Vector3(0f, 0f, 6.5f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Camera cam = FindInScene<Camera>(scene);
        cam.transform.position = player.transform.position + new Vector3(0f, 10f, 8.2f);
        cam.transform.LookAt(player.transform.position + Vector3.up * 0.8f);

        FindInScene<Cauldron>(scene).transform.position = new Vector3(0f, 0f, -5.5f);

        GameObject crystal = AssetDatabase.LoadAssetAtPath<GameObject>(CrystalPrefab);
        Place(crystal, scene, new Vector3(-6.5f, 0f, -2f));
        Place(crystal, scene, new Vector3(6.5f, 0f, -2f));
        Place(crystal, scene, new Vector3(-4.5f, 0f, 5.5f));
        Place(crystal, scene, new Vector3(4.5f, 0f, -6f));

        GameObject scroll = BuildScrollPrefab();
        Place(scroll, scene, new Vector3(6f, 0f, 4f));
        Place(scroll, scene, new Vector3(-3.5f, 0f, -6.5f));
    }

    static GameObject BuildScrollPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(ScrollPrefab);
        if (existing != null) return existing;

        Material paper = GetMaterial("ScrollPaper", new Color32(0xE6, 0xCC, 0x8C, 255), 0.25f);
        Material ribbon = GetMaterial("ScrollRibbon", new Color32(0xC0, 0x39, 0x2B, 255), 0.3f);
        Quaternion lying = Quaternion.Euler(0f, 0f, 90f);

        GameObject root = new GameObject("TowerScroll");
        Part(root, PrimitiveType.Cylinder, "Paper", new Vector3(0f, 0.65f, 0f), new Vector3(0.32f, 0.42f, 0.32f), lying, paper);
        Part(root, PrimitiveType.Cylinder, "Ribbon", new Vector3(0f, 0.65f, 0f), new Vector3(0.36f, 0.05f, 0.36f), lying, ribbon);

        SphereCollider pickup = root.AddComponent<SphereCollider>();
        pickup.isTrigger = true;
        pickup.radius = 0.7f;
        pickup.center = new Vector3(0f, 0.5f, 0f);
        root.AddComponent<Ingredient>().type = IngredientType.Scroll;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ScrollPrefab);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static void BuildStorm(Light sun, Transform clouds)
    {
        TowerStorm storm = new GameObject("Storm").AddComponent<TowerStorm>();
        storm.mainLight = sun;
        storm.clouds = clouds;
        storm.boltMaterial = GetMaterial("TowerBolt", new Color32(0xD8, 0xEC, 0xFF, 255), 3f);
        storm.warningMaterial = GetMaterial("TowerWarning", new Color32(0xFF, 0xB0, 0x30, 255), 1.2f);
    }

    static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>(true);
            if (found != null) return found;
        }
        throw new FileNotFoundException("Stage 1 has no " + typeof(T).Name + " to copy.");
    }

    static void Place(GameObject prefab, Scene scene, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.transform.position = position;
    }

    static GameObject Part(GameObject parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material mat, bool keepCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Material GetMaterial(string name, Color color, float emission = 0f)
    {
        return GetAsset(name, () =>
        {
            Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            if (emission > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * emission);
            }
            return m;
        });
    }

    static Material GetAsset(string name, System.Func<Material> create)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = create();
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
