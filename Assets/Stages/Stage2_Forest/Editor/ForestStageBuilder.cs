using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Editor builder for Stage 2: Enchanted Forest.
// Builds the environment as a single clean prefab (EnchantedForest.prefab) with standard Unity
// objects and materials (no custom runtime scripts), places it in the scene alongside the shared
// gameplay prefabs, and updates the project build settings.
public static class ForestStageBuilder
{
    const string StageFolder = "Assets/Stages/Stage2_Forest";
    const string ArtFolder = StageFolder + "/Art";
    const string PrefabFolder = StageFolder + "/Prefabs";
    const string ScenePath = StageFolder + "/Stage2_Forest.unity";
    const string ForestPrefabPath = PrefabFolder + "/EnchantedForest.prefab";
    const string SharedPrefabFolder = "Assets/Shared/Prefabs";
    const string PotionConfigPath = "Assets/Shared/Data/GrowthPotion.asset";

    [MenuItem("Potion Rush/Build Stage 2 - Enchanted Forest")]
    public static void BuildStageScene()
    {
        EnsureFolders();

        // 1. Create or open the scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Load Shared Prefabs
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabFolder + "/Player.prefab");
        GameObject inputPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabFolder + "/GameInput.prefab");
        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabFolder + "/HUD.prefab");
        GameObject cauldronPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabFolder + "/Cauldron.prefab");
        GameObject mushroomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabFolder + "/Mushroom.prefab");
        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabFolder + "/Crystal.prefab");

        // 3. Directional Light & Lighting Setup
        GameObject lightObj = new GameObject("Directional Light");
        Light dirLight = lightObj.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.color = new Color(0.96f, 0.93f, 0.84f);
        dirLight.intensity = 1.15f;
        dirLight.shadows = LightShadows.Soft;
        lightObj.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.20f, 0.25f, 0.23f);

        // 4. Main Camera with CameraFollow
        Vector3 spawnPos = new Vector3(0f, 0f, 7.5f);
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Color;
        cam.backgroundColor = new Color(0.11f, 0.16f, 0.15f);
        cam.fieldOfView = 38f;
        camObj.AddComponent<AudioListener>();
        camObj.transform.position = spawnPos + new Vector3(0f, 10f, 8.2f);
        camObj.transform.LookAt(spawnPos + Vector3.up * 0.8f);

        // 5. Global Volume
        GameObject volumeObj = new GameObject("Global Volume");
        Volume vol = volumeObj.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 0;
        vol.weight = 1f;

        // 6. StageManager
        StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(PotionConfigPath);
        GameObject stageManagerObj = new GameObject("StageManager");
        StageManager stageManager = stageManagerObj.AddComponent<StageManager>();
        stageManager.config = config;

        // 7. Instantiate GameInput and HUD
        if (inputPrefab != null) PrefabUtility.InstantiatePrefab(inputPrefab);
        if (hudPrefab != null) PrefabUtility.InstantiatePrefab(hudPrefab);

        // 8. Instantiate Player at spawn point
        GameObject player = null;
        if (playerPrefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = spawnPos;
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

            if (player.GetComponent<Interactor>() == null)
            {
                player.AddComponent<Interactor>();
            }

            CameraFollow follow = camObj.AddComponent<CameraFollow>();
            follow.target = player.transform;
        }

        // 9. Instantiate Cauldron at Ancient Shrine
        Vector3 cauldronPos = new Vector3(0f, 0f, -2.5f);
        if (cauldronPrefab != null)
        {
            GameObject cauldron = (GameObject)PrefabUtility.InstantiatePrefab(cauldronPrefab);
            cauldron.name = "Cauldron";
            cauldron.transform.position = cauldronPos;
        }

        // 10. ForestQuestManager — controls the 4-scenario chain (all scenario roots are
        //     children of this object so the hierarchy stays clean).
        GameObject questManagerObj = new GameObject("ForestQuestManager");
        ForestQuestManager questManager = questManagerObj.AddComponent<ForestQuestManager>();

        // --- Scenario 1 root: original layout (2 Mushrooms + 1 Crystal) ---------------
        GameObject s1 = new GameObject("Scenario1_Ingredients");
        s1.transform.SetParent(questManagerObj.transform, false);

        if (mushroomPrefab != null)
        {
            GameObject m1 = (GameObject)PrefabUtility.InstantiatePrefab(mushroomPrefab);
            m1.name = "Mushroom_East"; m1.transform.SetParent(s1.transform, false);
            m1.transform.position = new Vector3(6.5f, 0f, 2.5f);

            GameObject m2 = (GameObject)PrefabUtility.InstantiatePrefab(mushroomPrefab);
            m2.name = "Mushroom_West"; m2.transform.SetParent(s1.transform, false);
            m2.transform.position = new Vector3(-6.5f, 0f, 1.0f);
        }
        if (crystalPrefab != null)
        {
            GameObject c1 = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab);
            c1.name = "Crystal_S1"; c1.transform.SetParent(s1.transform, false);
            c1.transform.position = new Vector3(-4.5f, 0f, -7.5f);
        }

        // --- Scenario 2 root: hedge labyrinth + Herbs + Crystal -----------------------
        //     Herbs are built as simple primitive spheres (no Herb prefab yet).
        //     Hedge walls are flat cubes with a green material, turned off at start.
        GameObject s2 = new GameObject("Scenario2_Hedge");
        s2.transform.SetParent(questManagerObj.transform, false);
        s2.SetActive(false);  // hidden until Scenario 2

        BuildHedgeLabyrinth(s2);       // creates fence cubes inside s2
        BuildHerbIngredients(s2, crystalPrefab);  // creates Herb objects + 1 Crystal

        // --- Scenario 3 root: 3 runic monoliths ---------------------------------------
        GameObject s3 = new GameObject("Scenario3_Monoliths");
        s3.transform.SetParent(questManagerObj.transform, false);
        s3.SetActive(false);

        BuildMonolithPillars(s3);  // places 3 ForestRunicMonolith pillars

        // --- Scenario 4 root: final gather (all ingredients, spread wide) -------------
        GameObject s4 = new GameObject("Scenario4_FinalIngredients");
        s4.transform.SetParent(questManagerObj.transform, false);
        s4.SetActive(false);

        if (mushroomPrefab != null)
        {
            PlaceIngredient(mushroomPrefab, "Mush_A", new Vector3( 8.0f, 0f,  5.0f), s4);
            PlaceIngredient(mushroomPrefab, "Mush_B", new Vector3(-9.0f, 0f, -4.0f), s4);
        }
        if (crystalPrefab != null)
        {
            PlaceIngredient(crystalPrefab, "Crystal_A", new Vector3( 4.0f, 0f, -9.5f), s4);
            PlaceIngredient(crystalPrefab, "Crystal_B", new Vector3(-3.5f, 0f,  8.5f), s4);
        }
        BuildFinalHerbs(s4);  // 2 extra herbs in new spots

        // Link cauldron to quest manager (find the cauldron placed earlier).
        GameObject cauldronGO = GameObject.Find("Cauldron");
        if (cauldronGO != null) questManager.cauldronTransform = cauldronGO.transform;

        // 11. ForestMinimap — separate Canvas, top-right corner.
        GameObject minimapObj = new GameObject("ForestMinimap");
        minimapObj.AddComponent<ForestMinimap>();

        // 12. Build the complete EnchantedForest environment as a single clean prefab
        GameObject forestPrefab = BuildAndSaveForestPrefab();
        if (forestPrefab != null)
        {
            GameObject forestInstance = (GameObject)PrefabUtility.InstantiatePrefab(forestPrefab);
            forestInstance.name = "EnchantedForest";
            forestInstance.transform.position = Vector3.zero;
        }

        // 13. Save Scene
        EditorSceneManager.SaveScene(scene, ScenePath);

        // 14. Update Build Settings
        BuildSettingsUpdater.Apply();

        Debug.Log("Stage 2 - Enchanted Forest with 4-scenario quest chain built at " + ScenePath);
    }

    // ── Scenario helper methods ───────────────────────────────────────────────────────

    // Scenario 2 – hedge maze: a simple cross-pattern of green box walls.
    static void BuildHedgeLabyrinth(GameObject parent)
    {
        Material hedgeMat = GetLitMaterial("HedgeGreen",
            new Color(0.12f, 0.42f, 0.12f), 0.05f, 0f, 0.1f, new Color(0.05f, 0.22f, 0.05f));

        // Helper: (cx, cz, width, depth, height)
        // We build an H-shaped labyrinth of walls that force a path.
        Vector3[][] hedges = new Vector3[][]
        {
            // North corridor wall left
            new Vector3[]{ new Vector3(-7f, 0f,  0f),  new Vector3(0.8f, 3f, 10f) },
            // North corridor wall right
            new Vector3[]{ new Vector3( 7f, 0f,  0f),  new Vector3(0.8f, 3f, 10f) },
            // East divider
            new Vector3[]{ new Vector3( 2.5f, 0f, -4f), new Vector3(5f, 3f, 0.8f) },
            // West divider
            new Vector3[]{ new Vector3(-2.5f, 0f,  4f), new Vector3(5f, 3f, 0.8f) },
            // South wall with gap
            new Vector3[]{ new Vector3(-3.5f, 0f,-9f),  new Vector3(7f, 3f, 0.8f) },
            new Vector3[]{ new Vector3( 5f,   0f,-9f),  new Vector3(3f, 3f, 0.8f) },
        };

        for (int i = 0; i < hedges.Length; i++)
        {
            Vector3 pos  = hedges[i][0];
            Vector3 size = hedges[i][1];
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Hedge_" + i;
            wall.transform.SetParent(parent.transform, false);
            wall.transform.position = pos + Vector3.up * (size.y * 0.5f);
            wall.transform.localScale = size;
            wall.GetComponent<MeshRenderer>().sharedMaterial = hedgeMat;
            // Keep the box collider so the player can't walk through.
        }
    }

    // Scenario 2 – herb ingredients (green pulsing orbs) + 1 Crystal.
    static void BuildHerbIngredients(GameObject parent, GameObject crystalPrefab)
    {
        // No Herb prefab exists yet, so build simple primitive spheres with an
        // Ingredient component and type = Herb.
        Material herbMat = GetLitMaterial("HerbGlow",
            new Color(0.25f, 0.85f, 0.30f), 0.4f, 0f, 1.5f, new Color(0.1f, 0.65f, 0.15f));

        Vector3[] herbPositions = new Vector3[]
        {
            new Vector3(-5f, 0.4f,  2f),
            new Vector3( 3f, 0.4f, -6f),
            new Vector3( 0f, 0.4f,  7f),
        };

        for (int i = 0; i < herbPositions.Length; i++)
        {
            GameObject herb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            herb.name = "Herb_" + i;
            herb.transform.SetParent(parent.transform, false);
            herb.transform.position = herbPositions[i];
            herb.transform.localScale = Vector3.one * 0.5f;
            herb.GetComponent<MeshRenderer>().sharedMaterial = herbMat;

            // Make the collider a trigger so Interactor can collect it.
            SphereCollider sc = herb.GetComponent<SphereCollider>();
            sc.isTrigger = true;

            // Add Ingredient component and set type to Herb.
            Ingredient ing = herb.AddComponent<Ingredient>();
            ing.type = IngredientType.Herb;

            // Small point light so it glows.
            GameObject lightGO = new GameObject("HerbLight");
            lightGO.transform.SetParent(herb.transform, false);
            Light l = lightGO.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = new Color(0.3f, 1f, 0.4f);
            l.range     = 3.5f;
            l.intensity = 1.5f;
        }

        // 1 Crystal in a new spot for Scenario 2.
        if (crystalPrefab != null)
        {
            GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(crystalPrefab);
            c.name = "Crystal_S2";
            c.transform.SetParent(parent.transform, false);
            c.transform.position = new Vector3(5.5f, 0f, -3.5f);
        }
    }

    // Scenario 3 – 3 runic monolith pillars in a triangle formation.
    static void BuildMonolithPillars(GameObject parent)
    {
        Material runeMat = GetLitMaterial("RuneMonolith",
            new Color(0.18f, 0.22f, 0.24f), 0.3f, 0.1f, 0.4f, new Color(0.05f, 0.4f, 0.35f));

        Vector3[] positions = new Vector3[]
        {
            new Vector3( 0f,  0f, -6f),
            new Vector3(-6f,  0f,  4f),
            new Vector3( 6f,  0f,  4f),
        };

        for (int i = 0; i < positions.Length; i++)
        {
            // Build a tall stone pillar as a cube.
            GameObject pillarRoot = new GameObject("Monolith_" + i);
            pillarRoot.transform.SetParent(parent.transform, false);
            pillarRoot.transform.position = positions[i];

            // Stone body.
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Stone";
            stone.transform.SetParent(pillarRoot.transform, false);
            stone.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            stone.transform.localScale    = new Vector3(0.9f, 3.6f, 0.9f);
            stone.transform.localEulerAngles = new Vector3(0f, 25f + i * 15f, 0f);
            stone.GetComponent<MeshRenderer>().sharedMaterial = runeMat;

            // Dim point light (brightens when activated).
            GameObject lightGO = new GameObject("RuneLight");
            lightGO.transform.SetParent(pillarRoot.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            Light l = lightGO.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = new Color(0.1f, 0.4f, 0.2f);
            l.range     = 3.0f;
            l.intensity = 1.0f;

            // Proximity trigger component.
            ForestRunicMonolith mon = pillarRoot.AddComponent<ForestRunicMonolith>();
            mon.monolithIndex  = i;
            mon.activateRadius = 2.5f;
        }
    }

    // Scenario 4 – extra herbs in final positions.
    static void BuildFinalHerbs(GameObject parent)
    {
        Material herbMat = GetLitMaterial("HerbGlow",
            new Color(0.25f, 0.85f, 0.30f), 0.4f, 0f, 1.5f, new Color(0.1f, 0.65f, 0.15f));

        Vector3[] herbPositions = new Vector3[]
        {
            new Vector3( 7f, 0.4f, -5f),
            new Vector3(-8f, 0.4f,  6f),
        };

        for (int i = 0; i < herbPositions.Length; i++)
        {
            GameObject herb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            herb.name = "FinalHerb_" + i;
            herb.transform.SetParent(parent.transform, false);
            herb.transform.position = herbPositions[i];
            herb.transform.localScale = Vector3.one * 0.5f;
            herb.GetComponent<MeshRenderer>().sharedMaterial = herbMat;

            SphereCollider sc = herb.GetComponent<SphereCollider>();
            sc.isTrigger = true;

            Ingredient ing = herb.AddComponent<Ingredient>();
            ing.type = IngredientType.Herb;
        }
    }

    // Generic helper: instantiate a prefab inside a parent container.
    static void PlaceIngredient(GameObject prefab, string name, Vector3 pos, GameObject parent)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
    }

    // ─────────────────────────────────────────────────────────────────────────────────

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Stages")) AssetDatabase.CreateFolder("Assets", "Stages");
        if (!AssetDatabase.IsValidFolder(StageFolder)) AssetDatabase.CreateFolder("Assets/Stages", "Stage2_Forest");
        if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder(StageFolder, "Art");
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder(StageFolder, "Prefabs");
        if (!AssetDatabase.IsValidFolder(StageFolder + "/Editor"))   AssetDatabase.CreateFolder(StageFolder, "Editor");
        if (!AssetDatabase.IsValidFolder(StageFolder + "/Scripts"))  AssetDatabase.CreateFolder(StageFolder, "Scripts");
    }

    static GameObject BuildAndSaveForestPrefab()
    {
        // Materials
        Material matGrass = GetLitMaterial("ForestGrass", new Color(0.18f, 0.38f, 0.19f), 0.1f);
        Material matPath = GetLitMaterial("ForestPath", new Color(0.46f, 0.36f, 0.25f), 0.1f);
        Material matWater = GetLitMaterial("ForestWater", new Color(0.20f, 0.65f, 0.62f), 0.85f, 0.1f, 0.3f, new Color(0.15f, 0.45f, 0.45f));
        Material matBark = GetLitMaterial("BarkWood", new Color(0.28f, 0.20f, 0.14f), 0.05f);
        Material matFoliageEmerald = GetLitMaterial("FoliageEmerald", new Color(0.14f, 0.44f, 0.24f), 0.1f);
        Material matFoliageLime = GetLitMaterial("FoliageLime", new Color(0.25f, 0.56f, 0.22f), 0.1f);
        Material matFoliageAutumn = GetLitMaterial("FoliageAutumn", new Color(0.72f, 0.52f, 0.16f), 0.1f);
        Material matStone = GetLitMaterial("MossyStone", new Color(0.30f, 0.34f, 0.28f), 0.1f);
        Material matRune = GetLitMaterial("RuneStone", new Color(0.22f, 0.26f, 0.28f), 0.3f, 0.1f, 0.5f, new Color(0.12f, 0.62f, 0.58f));
        Material matMushCapPurple = GetLitMaterial("MushroomGiantPurple", new Color(0.55f, 0.22f, 0.78f), 0.4f, 0.05f, 0.7f, new Color(0.45f, 0.15f, 0.68f));
        Material matMushCapCyan = GetLitMaterial("MushroomGiantCyan", new Color(0.18f, 0.72f, 0.68f), 0.4f, 0.05f, 0.7f, new Color(0.12f, 0.58f, 0.55f));
        Material matMushStem = GetLitMaterial("MushroomGiantStem", new Color(0.92f, 0.89f, 0.84f), 0.1f);
        Material matLanternWood = GetLitMaterial("LanternWood", new Color(0.24f, 0.17f, 0.11f), 0.1f);
        Material matLanternGlow = GetLitMaterial("LanternGlow", new Color(1.0f, 0.72f, 0.24f), 0.5f, 0f, 1.2f, new Color(1.0f, 0.65f, 0.15f));

        // Single root GameObject for the entire forest environment
        GameObject root = new GameObject("EnchantedForest");

        // Subgroup 1: Ground & Pathways
        GameObject groundGroup = new GameObject("GroundAndPaths");
        groundGroup.transform.SetParent(root.transform, false);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(groundGroup.transform, false);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3.2f, 1f, 3.2f);
        ground.GetComponent<MeshRenderer>().sharedMaterial = matGrass;

        CreatePathSegment(groundGroup, "Path_SpawnToCenter", new Vector3(0f, 0.01f, 5.0f), new Vector3(3.0f, 0.02f, 6.0f), matPath);
        CreatePathSegment(groundGroup, "Path_Center", new Vector3(0f, 0.01f, 0.0f), new Vector3(2.8f, 0.02f, 5.0f), matPath);
        CreatePathSegment(groundGroup, "Path_ToMushroomEast", new Vector3(3.2f, 0.01f, 2.5f), new Vector3(5.0f, 0.02f, 2.4f), matPath);
        CreatePathSegment(groundGroup, "Path_ToMushroomWest", new Vector3(-3.2f, 0.01f, 1.5f), new Vector3(5.0f, 0.02f, 2.4f), matPath);
        CreatePathSegment(groundGroup, "Path_ToCrystalNorth", new Vector3(-2.5f, 0.01f, -5.0f), new Vector3(2.4f, 0.02f, 6.0f), matPath);

        // Subgroup 2: Cauldron Shrine
        GameObject shrineGroup = new GameObject("CauldronShrine");
        shrineGroup.transform.SetParent(root.transform, false);
        shrineGroup.transform.position = new Vector3(0f, 0f, -2.5f);

        Part(shrineGroup, PrimitiveType.Cylinder, "AltarBase", new Vector3(0f, 0.03f, 0f), new Vector3(5.2f, 0.06f, 5.2f), Vector3.zero, matRune, false);
        CreateDolmenArch(shrineGroup, "DolmenArch_Left", new Vector3(-2.8f, 0f, 0f), new Vector3(0.6f, 2.4f, 1.2f), 15f, matStone, matRune);
        CreateDolmenArch(shrineGroup, "DolmenArch_Right", new Vector3(2.8f, 0f, 0f), new Vector3(0.6f, 2.4f, 1.2f), -15f, matStone, matRune);
        CreateDolmenArch(shrineGroup, "DolmenArch_Back", new Vector3(0f, 0f, -2.8f), new Vector3(1.2f, 2.6f, 0.6f), 0f, matStone, matRune);
        CreateFairyLantern(shrineGroup, "ShrineLantern_L", new Vector3(-1.8f, 0f, 2.0f), matLanternWood, matLanternGlow);
        CreateFairyLantern(shrineGroup, "ShrineLantern_R", new Vector3(1.8f, 0f, 2.0f), matLanternWood, matLanternGlow);

        // Subgroup 3: Enchanted Trees
        GameObject treesGroup = new GameObject("EnchantedTrees");
        treesGroup.transform.SetParent(root.transform, false);
        CreateEnchantedTree(treesGroup, "Tree_1", new Vector3(4.5f, 0f, 7.0f), 1.2f, matBark, matFoliageEmerald, matFoliageLime);
        CreateEnchantedTree(treesGroup, "Tree_2", new Vector3(-4.5f, 0f, 7.2f), 1.1f, matBark, matFoliageLime, matFoliageAutumn);
        CreateEnchantedTree(treesGroup, "Tree_3", new Vector3(5.5f, 0f, -4.5f), 1.3f, matBark, matFoliageAutumn, matFoliageEmerald);
        CreateEnchantedTree(treesGroup, "Tree_4", new Vector3(-7.5f, 0f, -4.0f), 1.2f, matBark, matFoliageEmerald, matFoliageLime);
        CreateEnchantedTree(treesGroup, "Tree_5", new Vector3(2.5f, 0f, -7.8f), 1.1f, matBark, matFoliageLime, matFoliageEmerald);

        // Subgroup 4: Giant Mushrooms
        GameObject shroomsGroup = new GameObject("GiantMushrooms");
        shroomsGroup.transform.SetParent(root.transform, false);
        CreateGiantMushroom(shroomsGroup, "Mushroom_Purple_1", new Vector3(7.8f, 0f, 3.5f), 1.3f, matMushStem, matMushCapPurple, new Color(0.6f, 0.25f, 0.85f));
        CreateGiantMushroom(shroomsGroup, "Mushroom_Cyan_1", new Vector3(5.2f, 0f, 4.2f), 0.9f, matMushStem, matMushCapCyan, new Color(0.2f, 0.75f, 0.7f));
        CreateGiantMushroom(shroomsGroup, "Mushroom_Cyan_2", new Vector3(8.2f, 0f, 1.2f), 1.0f, matMushStem, matMushCapCyan, new Color(0.2f, 0.75f, 0.7f));
        CreateGiantMushroom(shroomsGroup, "Mushroom_Purple_2", new Vector3(-5.8f, 0f, -7.0f), 1.1f, matMushStem, matMushCapPurple, new Color(0.6f, 0.25f, 0.85f));

        // Subgroup 5: Enchanted Pond & Rocks
        GameObject pondGroup = new GameObject("EnchantedPond");
        pondGroup.transform.SetParent(root.transform, false);
        GameObject pond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pond.name = "PondWater";
        pond.transform.SetParent(pondGroup.transform, false);
        pond.transform.position = new Vector3(-7.5f, 0.02f, 1.2f);
        pond.transform.localScale = new Vector3(4.5f, 0.04f, 4.0f);
        pond.GetComponent<MeshRenderer>().sharedMaterial = matWater;
        Object.DestroyImmediate(pond.GetComponent<Collider>());

        CreateRock(pondGroup, "PondRock_1", new Vector3(-9.2f, 0f, 2.2f), new Vector3(1.4f, 0.8f, 1.2f), matStone);
        CreateRock(pondGroup, "PondRock_2", new Vector3(-6.0f, 0f, 3.0f), new Vector3(1.0f, 0.6f, 1.0f), matStone);
        CreateRock(pondGroup, "PondRock_3", new Vector3(-8.8f, 0f, -0.6f), new Vector3(1.2f, 0.7f, 1.1f), matStone);

        // Subgroup 6: Monolith Grove (North-West)
        GameObject groveGroup = new GameObject("MonolithGrove");
        groveGroup.transform.SetParent(root.transform, false);
        CreateRunicPillar(groveGroup, "Monolith_1", new Vector3(-3.5f, 0f, -8.5f), 2.8f, matRune);
        CreateRunicPillar(groveGroup, "Monolith_2", new Vector3(-5.8f, 0f, -8.2f), 2.4f, matRune);
        CreateRunicPillar(groveGroup, "Monolith_3", new Vector3(-5.5f, 0f, -6.2f), 2.0f, matRune);
        CreateFairyLantern(groveGroup, "GroveLantern", new Vector3(-3.2f, 0f, -6.8f), matLanternWood, matLanternGlow);

        // Subgroup 7: Forest Lanterns & Fallen Logs
        GameObject propsGroup = new GameObject("ForestLanterns");
        propsGroup.transform.SetParent(root.transform, false);
        CreateFairyLantern(propsGroup, "Lantern_Spawn_L", new Vector3(-2.2f, 0f, 8.2f), matLanternWood, matLanternGlow);
        CreateFairyLantern(propsGroup, "Lantern_Spawn_R", new Vector3(2.2f, 0f, 8.2f), matLanternWood, matLanternGlow);
        CreateFallenLog(propsGroup, "FallenLog_1", new Vector3(3.0f, 0.3f, 4.8f), new Vector3(0.5f, 2.8f, 0.5f), new Vector3(90f, 35f, 0f), matBark);
        CreateFallenLog(propsGroup, "FallenLog_2", new Vector3(-3.5f, 0.3f, 3.8f), new Vector3(0.5f, 2.5f, 0.5f), new Vector3(90f, -40f, 0f), matBark);

        // Subgroup 8: Forest Boundaries (Natural impassable tree/rock ring)
        GameObject boundsGroup = new GameObject("ForestBoundaries");
        boundsGroup.transform.SetParent(root.transform, false);

        for (float x = -13f; x <= 13f; x += 3.5f)
        {
            CreateEnchantedTree(boundsGroup, "Bound_S_Tree_" + x, new Vector3(x, 0f, 12.5f), 1.4f, matBark, matFoliageEmerald, matFoliageLime);
            CreateRock(boundsGroup, "Bound_S_Rock_" + x, new Vector3(x + 1.2f, 0f, 11.5f), new Vector3(2.2f, 1.8f, 1.8f), matStone);
            CreateEnchantedTree(boundsGroup, "Bound_N_Tree_" + x, new Vector3(x, 0f, -12.5f), 1.4f, matBark, matFoliageEmerald, matFoliageAutumn);
            CreateRock(boundsGroup, "Bound_N_Rock_" + x, new Vector3(x + 1.5f, 0f, -11.5f), new Vector3(2.2f, 1.8f, 1.8f), matStone);
        }

        for (float z = -12f; z <= 12f; z += 3.5f)
        {
            CreateEnchantedTree(boundsGroup, "Bound_E_Tree_" + z, new Vector3(13.0f, 0f, z), 1.4f, matBark, matFoliageLime, matFoliageEmerald);
            CreateRock(boundsGroup, "Bound_E_Rock_" + z, new Vector3(11.8f, 0f, z + 1.0f), new Vector3(1.8f, 1.8f, 2.2f), matStone);
            CreateEnchantedTree(boundsGroup, "Bound_W_Tree_" + z, new Vector3(-13.0f, 0f, z), 1.4f, matBark, matFoliageAutumn, matFoliageEmerald);
            CreateRock(boundsGroup, "Bound_W_Rock_" + z, new Vector3(-11.8f, 0f, z + 1.0f), new Vector3(1.8f, 1.8f, 2.2f), matStone);
        }

        // Save as standard Unity Prefab
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, ForestPrefabPath);
        Object.DestroyImmediate(root);
        return savedPrefab;
    }

    static void CreatePathSegment(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent.transform, false);
        segment.transform.position = pos;
        segment.transform.localScale = size;
        segment.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(segment.GetComponent<Collider>());
    }

    static void CreateEnchantedTree(GameObject parent, string name, Vector3 pos, float scale, Material trunkMat, Material crownMatA, Material crownMatB)
    {
        GameObject tree = new GameObject(name);
        tree.transform.SetParent(parent.transform, false);
        tree.transform.position = pos;

        float trunkHeight = 3.6f * scale;
        float trunkRadius = 0.55f * scale;
        Part(tree, PrimitiveType.Cylinder, "Trunk", new Vector3(0f, trunkHeight * 0.5f, 0f), new Vector3(trunkRadius * 2f, trunkHeight * 0.5f, trunkRadius * 2f), Vector3.zero, trunkMat, true);

        Part(tree, PrimitiveType.Cube, "RootA", new Vector3(trunkRadius * 0.7f, 0.2f, 0f), new Vector3(trunkRadius * 1.6f, 0.4f, trunkRadius * 0.8f), new Vector3(0f, 25f, -15f), trunkMat, false);
        Part(tree, PrimitiveType.Cube, "RootB", new Vector3(-trunkRadius * 0.7f, 0.2f, trunkRadius * 0.5f), new Vector3(trunkRadius * 1.5f, 0.4f, trunkRadius * 0.8f), new Vector3(0f, -40f, 15f), trunkMat, false);

        float crownY = trunkHeight * 0.85f;
        Part(tree, PrimitiveType.Sphere, "FoliageA", new Vector3(0f, crownY, 0f), Vector3.one * (3.4f * scale), Vector3.zero, crownMatA, false);
        Part(tree, PrimitiveType.Sphere, "FoliageB", new Vector3(0.5f * scale, crownY + 1.0f * scale, 0.3f * scale), Vector3.one * (2.6f * scale), Vector3.zero, crownMatB, false);
        Part(tree, PrimitiveType.Sphere, "FoliageC", new Vector3(-0.6f * scale, crownY + 0.6f * scale, -0.4f * scale), Vector3.one * (2.4f * scale), Vector3.zero, crownMatA, false);
    }

    static void CreateGiantMushroom(GameObject parent, string name, Vector3 pos, float scale, Material stemMat, Material capMat, Color lightColor)
    {
        GameObject shroom = new GameObject(name);
        shroom.transform.SetParent(parent.transform, false);
        shroom.transform.position = pos;

        float stemH = 2.4f * scale;
        Part(shroom, PrimitiveType.Cylinder, "Stem", new Vector3(0f, stemH * 0.5f, 0f), new Vector3(0.4f * scale, stemH * 0.5f, 0.4f * scale), new Vector3(0f, 0f, 3f), stemMat, true);
        Part(shroom, PrimitiveType.Sphere, "Cap", new Vector3(0f, stemH, 0f), new Vector3(2.2f * scale, 0.9f * scale, 2.2f * scale), Vector3.zero, capMat, false);

        GameObject lightObj = new GameObject("GlowLight");
        lightObj.transform.SetParent(shroom.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, stemH * 0.9f, 0f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightColor;
        light.range = 5.0f * scale;
        light.intensity = 2.0f;
    }

    static void CreateDolmenArch(GameObject parent, string name, Vector3 pos, Vector3 size, float yRot, Material stoneMat, Material runeMat)
    {
        GameObject arch = new GameObject(name);
        arch.transform.SetParent(parent.transform, false);
        arch.transform.localPosition = pos;
        arch.transform.localEulerAngles = new Vector3(0f, yRot, 0f);

        Part(arch, PrimitiveType.Cube, "Pillar", new Vector3(0f, size.y * 0.5f, 0f), size, Vector3.zero, runeMat, true);
    }

    static void CreateRunicPillar(GameObject parent, string name, Vector3 pos, float height, Material mat)
    {
        GameObject pillar = new GameObject(name);
        pillar.transform.SetParent(parent.transform, false);
        pillar.transform.position = pos;

        Part(pillar, PrimitiveType.Cube, "Stone", new Vector3(0f, height * 0.5f, 0f), new Vector3(0.8f, height, 0.8f), new Vector3(0f, 35f, 0f), mat, true);
    }

    static void CreateFairyLantern(GameObject parent, string name, Vector3 pos, Material woodMat, Material glowMat)
    {
        GameObject lantern = new GameObject(name);
        lantern.transform.SetParent(parent.transform, false);
        lantern.transform.localPosition = pos;

        Part(lantern, PrimitiveType.Cylinder, "Post", new Vector3(0f, 0.8f, 0f), new Vector3(0.18f, 0.8f, 0.18f), Vector3.zero, woodMat, false);
        Part(lantern, PrimitiveType.Cube, "Cap", new Vector3(0f, 1.75f, 0f), new Vector3(0.4f, 0.08f, 0.4f), Vector3.zero, woodMat, false);
        Part(lantern, PrimitiveType.Sphere, "Orb", new Vector3(0f, 1.6f, 0f), Vector3.one * 0.28f, Vector3.zero, glowMat, false);

        GameObject lObj = new GameObject("Light");
        lObj.transform.SetParent(lantern.transform, false);
        lObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        Light l = lObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1.0f, 0.72f, 0.25f);
        l.range = 4.5f;
        l.intensity = 2.0f;
    }

    static void CreateRock(GameObject parent, string name, Vector3 pos, Vector3 size, Material mat)
    {
        Part(parent, PrimitiveType.Sphere, name, pos, size, new Vector3(0f, 45f, 0f), mat, true);
    }

    static void CreateFallenLog(GameObject parent, string name, Vector3 pos, Vector3 size, Vector3 rot, Material mat)
    {
        Part(parent, PrimitiveType.Cylinder, name, pos, size, rot, mat, true);
    }

    static GameObject Part(GameObject parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Vector3 rotation, Material mat, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.transform.localEulerAngles = rotation;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    static Material GetLitMaterial(string name, Color color, float smoothness = 0.2f, float metallic = 0f, float emission = 0f, Color? emissionColor = null)
    {
        string path = ArtFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        if (emission > 0f || emissionColor.HasValue)
        {
            mat.EnableKeyword("_EMISSION");
            Color eColor = emissionColor ?? (color * emission);
            mat.SetColor("_EmissionColor", eColor);
        }

        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
