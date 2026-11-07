using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Stage 4 - Haunted Library.
// Adds a menu item that builds the playable stage in the open scene, the same way TestStageBuilder built stage 1:
// the rules (StageManager + InvisibilityPotion settings), 3 scrolls, 1 crystal, the cauldron and the stage twist,
// 5 flying books. The Scroll is saved with the other ingredients in Assets/Shared/Prefabs.
// If the scene has no Player, GameInput or HUD yet, the shared prefabs are placed (they are never rebuilt).
public static class LibraryStageBuilder
{
    const string ScenePath = LibraryRoomBuilder.StageFolder + "/Stage4_Library.unity";
    const string ConfigPath = "Assets/Shared/Data/InvisibilityPotion.asset";
    const string SharedPrefabs = "Assets/Shared/Prefabs";
    const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";

    static readonly Vector3 PlayerStart = new Vector3(0f, 0f, 5f);
    static readonly Vector3 CauldronPosition = new Vector3(0f, 0f, -4.7f);

    [MenuItem("Potion Rush/Stage 4/3. Build Library Stage")]
    static void Build()
    {
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogWarning("Open (or save) the Stage 4 scene first: it must be " + ScenePath + ". Use File > New Scene, then File > Save As.");
            return;
        }
        if (Object.FindAnyObjectByType<StageManager>() != null)
        {
            Debug.LogWarning("A StageManager is already in the scene. Delete the old stage objects first.");
            return;
        }

        LibraryRoomBuilder.EnsureFolders();

        // shared pieces: placed from the team's prefabs if they are not in the scene yet
        GameObject player = GameObject.Find("Player");
        if (player == null) player = Place(LoadShared("Player"), PlayerStart, 180f);
        if (Object.FindAnyObjectByType<GameInput>() == null) Place(LoadShared("GameInput"), Vector3.zero, 0f);
        if (Object.FindAnyObjectByType<HUD>() == null) Place(LoadShared("HUD"), Vector3.zero, 0f);
        if (player == null) return;
        SetupCamera(player.transform);
        SetupMood();

        // stage settings
        StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<StageConfig>();
            config.stageName = "Haunted Library";
            config.potionName = "Invisibility potion";
            config.timeLimit = 100f;
            config.shakesRequired = 12;
            config.recipe = new List<RecipeEntry>
            {
                new RecipeEntry { type = IngredientType.Scroll, amount = 3 },
                new RecipeEntry { type = IngredientType.Crystal, amount = 1 },
            };
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        GameObject managerObj = new GameObject("StageManager");
        managerObj.AddComponent<StageManager>().config = config;
        managerObj.AddComponent<LibraryMessage>(); // "Attention aux livres volants !" and "Aïe !"
        PlayerLives lives = managerObj.AddComponent<PlayerLives>(); // 3 hits from the flying books and the stage is lost
        lives.lives = 3;
        lives.defeatTitle = "Trop de coups !";

        // prefabs
        Material[] covers = LibraryRoomBuilder.BookCovers();
        Material paper = LibraryRoomBuilder.GetMaterial("LibPaper", new Color32(0xEF, 0xE0, 0xB8, 255), 0.25f);
        Material seal = LibraryRoomBuilder.GetMaterial("LibSeal", new Color32(0x9A, 0x1E, 0x2C, 255));
        Material glow = LibraryRoomBuilder.GetMaterial("LibGhostGlow", new Color32(0x9B, 0x5C, 0xFF, 255), 1.6f);
        Material eyes = LibraryRoomBuilder.GetMaterial("LibBookEyes", new Color32(0xFF, 0xE0, 0x6A, 255), 2.5f);

        GameObject scrollPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabs + "/Scroll.prefab");
        if (scrollPrefab == null) scrollPrefab = SavePrefab(BuildScroll(paper, seal), SharedPrefabs + "/Scroll.prefab");
        GameObject bookPrefab = SavePrefab(BuildFlyingBook(covers[0], paper, glow, eyes), LibraryRoomBuilder.PrefabFolder + "/FlyingBook.prefab");
        GameObject crystalPrefab = LoadShared("Crystal");
        GameObject cauldronPrefab = LoadShared("Cauldron");

        // ingredients: 3 scrolls + 1 crystal
        Place(scrollPrefab, new Vector3(-5.3f, 0f, -5.3f), 0f);  // left aisle, back corner
        Place(scrollPrefab, new Vector3(5.3f, 0f, -1.5f), 0f);   // right aisle
        Place(scrollPrefab, new Vector3(0f, 0f, 0.3f), 0f);      // centre lane
        Place(crystalPrefab, new Vector3(5.3f, 0f, 4.5f), 0f);   // front right
        Place(cauldronPrefab, CauldronPosition, 0f);

        // the twist: 5 flying books, each with its own route, speed and wake-up time
        AddBook(bookPrefab, covers[0], 3f, 1.5f, new[] { new Vector3(-2.2f, 0f, 0.3f), new Vector3(2.2f, 0f, 0.3f) });  // across the centre lane
        AddBook(bookPrefab, covers[2], 2.6f, 3f, new[] { new Vector3(5.3f, 0f, -6.2f), new Vector3(5.3f, 0f, 2.6f) });  // right aisle
        AddBook(bookPrefab, covers[1], 2.6f, 5f, new[] { new Vector3(-5.3f, 0f, 2.6f), new Vector3(-5.3f, 0f, -6.2f) }); // left aisle

        List<Vector3> circle = new List<Vector3>(); // guards the cauldron
        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f;
            circle.Add(CauldronPosition + new Vector3(Mathf.Cos(a) * 2.5f, 0f, Mathf.Sin(a) * 1.9f));
        }
        AddBook(bookPrefab, covers[3], 2.2f, 8f, circle.ToArray());

        AddBook(bookPrefab, covers[5], 3.5f, 15f, new[] { new Vector3(-6.3f, 0f, 3.2f), new Vector3(6.3f, 0f, 3.2f) });  // wakes late, sweeps the front

        // bonus: the invisibility bottle (see LibraryPickupBuilder)
        LibraryPickupBuilder.AddToScene();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Library stage created. Save the scene (Ctrl+S), then click Potion Rush > Update Build Settings. " +
                  "Press Play: collect 3 scrolls and 1 crystal, dodge the flying books, then walk to the cauldron and press Space 12 times.");
    }

    // ------------------------------------------------------------------ camera and lighting

    // Same camera as stage 1 (see PlayerBuilder), on a dark background.
    static void SetupCamera(Transform player)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            cam = camObj.AddComponent<Camera>();
            camObj.AddComponent<AudioListener>();
        }

        cam.fieldOfView = 38f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.03f, 0.07f);
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.target = player;
        cam.transform.position = player.position + new Vector3(0f, 10f, 8.2f);
        cam.transform.LookAt(player.position + Vector3.up * 0.8f);
    }

    // Haunted mood: cold dim moonlight, purple ambience, light fog. Bloom (Global Volume) makes candles and books glow.
    static void SetupMood()
    {
        foreach (Light l in Object.FindObjectsByType<Light>())
        {
            if (l.type != LightType.Directional) continue;
            l.color = new Color(0.6f, 0.65f, 1f);
            l.intensity = 0.6f;
            l.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
        }

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.2f, 0.16f, 0.32f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.09f, 0.18f);
        RenderSettings.ambientGroundColor = new Color(0.05f, 0.04f, 0.06f);
        RenderSettings.reflectionIntensity = 0.3f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(0.07f, 0.05f, 0.12f);
        RenderSettings.fogDensity = 0.018f;

        if (Object.FindAnyObjectByType<Volume>() == null)
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null)
            {
                Volume volume = new GameObject("Global Volume").AddComponent<Volume>();
                volume.isGlobal = true;
                volume.sharedProfile = profile;
            }
        }
    }

    // ------------------------------------------------------------------ prefabs

    // A rolled parchment with red caps, collected by walking over it like the mushroom and the crystal.
    static GameObject BuildScroll(Material paper, Material seal)
    {
        GameObject root = new GameObject("Scroll");
        LibraryRoomBuilder.Part(root, PrimitiveType.Cylinder, "Paper", new Vector3(0f, 0.55f, 0f), new Vector3(0.3f, 0.36f, 0.3f), new Vector3(0f, 0f, 90f), paper);
        LibraryRoomBuilder.Part(root, PrimitiveType.Cylinder, "CapL", new Vector3(-0.38f, 0.55f, 0f), new Vector3(0.36f, 0.03f, 0.36f), new Vector3(0f, 0f, 90f), seal);
        LibraryRoomBuilder.Part(root, PrimitiveType.Cylinder, "CapR", new Vector3(0.38f, 0.55f, 0f), new Vector3(0.36f, 0.03f, 0.36f), new Vector3(0f, 0f, 90f), seal);
        LibraryRoomBuilder.Part(root, PrimitiveType.Cylinder, "Ribbon", new Vector3(0f, 0.55f, 0f), new Vector3(0.32f, 0.04f, 0.32f), new Vector3(0f, 0f, 90f), seal);

        // same pickup setup as TestStageBuilder.AddTapTarget
        SphereCollider tap = root.AddComponent<SphereCollider>();
        tap.isTrigger = true;
        tap.radius = 0.7f;
        tap.center = new Vector3(0f, 0.5f, 0f);

        Ingredient ingredient = root.AddComponent<Ingredient>();
        ingredient.type = IngredientType.Scroll;
        SerializedObject so = new SerializedObject(ingredient);
        so.FindProperty("spinSpeed").floatValue = 60f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    // Spine on top with two glowing eyes, the covers hang from it like wings, pages underneath, a purple glow on the floor.
    static GameObject BuildFlyingBook(Material cover, Material paper, Material glow, Material eyes)
    {
        GameObject root = new GameObject("FlyingBook");
        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);

        LibraryRoomBuilder.Part(body, PrimitiveType.Cube, "Spine", Vector3.zero, new Vector3(0.1f, 0.1f, 0.6f), Vector3.zero, cover);
        LibraryRoomBuilder.Part(body, PrimitiveType.Sphere, "EyeL", new Vector3(-0.035f, 0.06f, 0.24f), Vector3.one * 0.06f, Vector3.zero, eyes);
        LibraryRoomBuilder.Part(body, PrimitiveType.Sphere, "EyeR", new Vector3(0.035f, 0.06f, 0.24f), Vector3.one * 0.06f, Vector3.zero, eyes);

        foreach (float side in new[] { -1f, 1f })
        {
            GameObject pivot = new GameObject(side < 0f ? "CoverLeft" : "CoverRight");
            pivot.transform.SetParent(body.transform, false);
            pivot.transform.localPosition = new Vector3(side * 0.04f, 0f, 0f);
            LibraryRoomBuilder.Part(pivot, PrimitiveType.Cube, "Board", new Vector3(side * 0.22f, 0f, 0f), new Vector3(0.44f, 0.035f, 0.6f), Vector3.zero, cover);
            LibraryRoomBuilder.Part(pivot, PrimitiveType.Cube, "Pages", new Vector3(side * 0.2f, -0.035f, 0f), new Vector3(0.38f, 0.04f, 0.54f), Vector3.zero, paper);
        }

        LibraryRoomBuilder.Part(root, PrimitiveType.Cylinder, "Glow", new Vector3(0f, -1f, 0f), new Vector3(0.9f, 0.004f, 0.9f), Vector3.zero, glow);

        root.AddComponent<FlyingBook>();
        return root;
    }

    static void AddBook(GameObject prefab, Material cover, float speed, float wakeDelay, Vector3[] route)
    {
        GameObject book = Place(prefab, route[0] + Vector3.up * 0.35f, 0f);

        FlyingBook fb = book.GetComponent<FlyingBook>();
        fb.route = route;
        fb.speed = speed;
        fb.wakeDelay = wakeDelay;
        PrefabUtility.RecordPrefabInstancePropertyModifications(fb);

        foreach (MeshRenderer r in book.GetComponentsInChildren<MeshRenderer>())
        {
            if (r.name != "Spine" && r.name != "Board") continue;
            r.sharedMaterial = cover;
            PrefabUtility.RecordPrefabInstancePropertyModifications(r);
        }
    }

    // ------------------------------------------------------------------ helpers

    static GameObject LoadShared(string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SharedPrefabs + "/" + name + ".prefab");
        if (prefab == null) Debug.LogError("Missing prefab: " + SharedPrefabs + "/" + name + ".prefab. Pull the latest main branch first.");
        return prefab;
    }

    static GameObject SavePrefab(GameObject template, string path)
    {
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(template, path);
        Object.DestroyImmediate(template);
        return asset;
    }

    static GameObject Place(GameObject prefab, Vector3 position, float yRotation)
    {
        if (prefab == null) return null;
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        return instance;
    }
}
