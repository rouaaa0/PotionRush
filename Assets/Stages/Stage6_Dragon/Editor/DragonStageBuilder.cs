using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Adds a menu item that builds the whole Dragon's Lair stage in one click: materials, the dragon,
// the room and the scene. It starts from a copy of Stage 1's scene, so the camera, the HUD and the
// wizard are set up the same way as in the other stages. Nothing in Assets/Shared is changed.
public static class DragonStageBuilder
{
    const string Root = "Assets/Stages/Stage6_Dragon";
    const string Art = Root + "/Art";
    const string Prefabs = Root + "/Prefabs";
    const string Data = Root + "/Data";
    const string ScenePath = Root + "/Stage6_Dragon.unity";
    const string SourceScene = "Assets/Stages/Stage1_Lab/Stage1_Lab.unity";

    static readonly Color Hide = new Color32(0x9C, 0x1F, 0x1C, 255);
    static readonly Color HideDark = new Color32(0x5E, 0x12, 0x18, 255);
    static readonly Color Belly = new Color32(0xE0, 0x9A, 0x4A, 255);
    static readonly Color Horn = new Color32(0xE8, 0xDF, 0xC8, 255);
    static readonly Color Lava = new Color32(0xFF, 0x62, 0x12, 255);

    [MenuItem("Potion Rush/Build Dragon Stage")]
    public static void Build()
    {
        if (File.Exists(ScenePath) && !Application.isBatchMode && !EditorUtility.DisplayDialog("Dragon's Lair",
            "The stage already exists. Rebuild it? Changes made by hand in its scene will be lost.", "Rebuild", "Cancel")) return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        CreateFolders();

        StageConfig config = BuildConfig();
        GameObject scalePrefab = BuildScale();
        GameObject lairPrefab = BuildLair();
        GameObject dragonPrefab = BuildDragon();

        AssetDatabase.DeleteAsset(ScenePath);
        AssetDatabase.CopyAsset(SourceScene, ScenePath);
        EditorSceneManager.OpenScene(ScenePath);

        SetUpScene(config, scalePrefab, lairPrefab, dragonPrefab);

        EditorSceneManager.SaveOpenScenes();
        BuildSettingsUpdater.Apply();
        AssetDatabase.SaveAssets();
        Debug.Log("Dragon's Lair built. Press Play to fight the dragon.");
    }

    static void CreateFolders()
    {
        foreach (string folder in new[] { "Art", "Prefabs", "Data" })
            if (!AssetDatabase.IsValidFolder(Root + "/" + folder)) AssetDatabase.CreateFolder(Root, folder);
    }

    // ---------- settings ----------

    static StageConfig BuildConfig()
    {
        string path = Data + "/ElixirConfig.asset";
        StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<StageConfig>();
            AssetDatabase.CreateAsset(config, path);
        }

        config.stageName = "Antre du dragon";
        config.potionName = "Élixir de puissance";
        config.timeLimit = 90f;
        config.shakesRequired = 15;
        config.showTutorialHints = false;
        config.recipe = new List<RecipeEntry>
        {
            new RecipeEntry { type = IngredientType.DragonScale, amount = 3 },
            new RecipeEntry { type = IngredientType.Crystal, amount = 2 },
        };
        config.pointsPerIngredient = 15;
        config.potionBonus = 200;
        config.pointsPerSecondLeft = 2;
        EditorUtility.SetDirty(config);
        return config;
    }

    // ---------- materials ----------

    static Material Lit(string name, Color color, float emission = 0f, float smoothness = 0.2f)
    {
        string path = Art + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            mat.SetColor("_EmissionColor", color * emission);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // See-through and glowing: floor warnings and fire particles.
    static Material Glow(string name, Color color, Texture texture)
    {
        string path = Art + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        if (texture != null) mat.SetTexture("_BaseMap", texture);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 2f); // additive: the colour adds light, so it glows on the dark floor
        mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.SetFloat("_Cull", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // A soft white dot, for the fire particles.
    static Texture2D SoftDot()
    {
        string path = Art + "/SoftDot.png";
        if (!File.Exists(path))
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x - 31.5f, y - 31.5f).magnitude / 31.5f;
                    float a = Mathf.Clamp01(1f - d);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // ---------- the ingredient ----------

    static GameObject BuildScale()
    {
        Material gold = Lit("ScaleGlow", new Color32(0xFF, 0x7A, 0x1E, 255), 1.3f, 0.7f);
        Material dark = Lit("ScaleEdge", HideDark, 0.3f, 0.5f);

        GameObject root = new GameObject("DragonScale");
        Part(root, PrimitiveType.Sphere, "Back", new Vector3(0f, 0.75f, 0f), new Vector3(0.95f, 1.15f, 0.2f), Vector3.zero, dark);
        Part(root, PrimitiveType.Sphere, "Front", new Vector3(0f, 0.75f, 0.06f), new Vector3(0.72f, 0.92f, 0.2f), Vector3.zero, gold);
        Part(root, PrimitiveType.Cube, "Ridge", new Vector3(0f, 0.75f, 0.15f), new Vector3(0.09f, 0.72f, 0.06f), Vector3.zero, dark);

        SphereCollider pickup = root.AddComponent<SphereCollider>();
        pickup.isTrigger = true;
        pickup.radius = 0.8f;
        pickup.center = new Vector3(0f, 0.5f, 0f);

        Ingredient ingredient = root.AddComponent<Ingredient>();
        ingredient.type = IngredientType.DragonScale;
        SerializedObject so = new SerializedObject(ingredient);
        so.FindProperty("spinSpeed").floatValue = 90f;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, "DragonScale");
    }

    // ---------- the room ----------

    static GameObject BuildLair()
    {
        Material floorMat = Lit("LairFloor", new Color32(0x2A, 0x20, 0x24, 255), 0f, 0.1f);
        Material wallMat = Lit("LairWall", new Color32(0x38, 0x28, 0x2E, 255), 0f, 0.1f);
        Material rockMat = Lit("LairRock", new Color32(0x4C, 0x3A, 0x3C, 255), 0f, 0.1f);
        Material lavaMat = Lit("Lava", Lava, 2.2f, 0.6f);
        Material goldMat = Lit("Gold", new Color32(0xF2, 0xB6, 0x3A, 255), 0.35f, 0.8f);
        Material pillarMat = Lit("Pillar", new Color32(0x5C, 0x4B, 0x50, 255), 0f, 0.15f);

        System.Random random = new System.Random(6);
        float R(float a, float b) { return a + (float)random.NextDouble() * (b - a); }

        GameObject root = new GameObject("DragonLair");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform, false);
        floor.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        Wall(root, "Wall_Back", new Vector3(0f, 3.5f, -7.5f), new Vector3(15f, 7f, 0.3f), wallMat);
        Wall(root, "Wall_Left", new Vector3(-7.5f, 1.7f, 0f), new Vector3(0.3f, 3.4f, 15f), wallMat);
        Wall(root, "Wall_Right", new Vector3(7.5f, 1.7f, 0f), new Vector3(0.3f, 3.4f, 15f), wallMat);
        GameObject front = Wall(root, "Wall_FrontLow", new Vector3(0f, 0.2f, 7.5f), new Vector3(15f, 0.4f, 0.3f), wallMat);

        // low so the camera can see in, but the collider is tall so the wizard can't walk out (like the lab room)
        BoxCollider frontBlock = front.GetComponent<BoxCollider>();
        frontBlock.size = new Vector3(1f, 8.5f, 3f);
        frontBlock.center = new Vector3(0f, 3.75f, 1f);

        // glowing cracks in the floor, only decoration
        GameObject cracks = new GameObject("Cracks");
        cracks.transform.SetParent(root.transform, false);
        for (int i = 0; i < 26; i++)
        {
            Vector3 position = new Vector3(R(-6.8f, 6.8f), 0.012f, R(-6.8f, 6.8f));
            Vector3 scale = new Vector3(R(0.06f, 0.14f), 0.02f, R(0.8f, 2.6f));
            Part(cracks, PrimitiveType.Cube, "Crack", position, scale, new Vector3(0f, R(0f, 180f), 0f), lavaMat);
        }

        // lava falling down the back wall into two pools that can't be walked into
        foreach (float x in new[] { -5.2f, 5.2f })
        {
            Part(root, PrimitiveType.Cube, "LavaFall", new Vector3(x, 3.5f, -7.32f), new Vector3(1.2f, 7f, 0.05f), Vector3.zero, lavaMat);
            GameObject pool = Part(root, PrimitiveType.Cylinder, "LavaPool", new Vector3(x, 0.03f, -6.85f), new Vector3(2.6f, 0.03f, 1.1f), Vector3.zero, lavaMat);
            BoxCollider block = pool.AddComponent<BoxCollider>();
            block.size = new Vector3(1f, 100f, 1f);

            GameObject lightObj = new GameObject("LavaLight");
            lightObj.transform.SetParent(root.transform, false);
            lightObj.transform.localPosition = new Vector3(x, 1.8f, -6f);
            Light lavaLight = lightObj.AddComponent<Light>();
            lavaLight.type = LightType.Point;
            lavaLight.color = new Color(1f, 0.42f, 0.12f);
            lavaLight.range = 10f;
            lavaLight.intensity = 6f;
            lightObj.AddComponent<LavaGlow>();
        }

        // the three pillars that block the fire breath
        GameObject pillars = new GameObject("Pillars");
        pillars.transform.SetParent(root.transform, false);
        foreach (Vector3 at in new[] { new Vector3(-3.8f, 0f, -0.6f), new Vector3(3.8f, 0f, -0.6f), new Vector3(0f, 0f, 2.3f) })
        {
            GameObject pillar = new GameObject("Pillar");
            pillar.transform.SetParent(pillars.transform, false);
            pillar.transform.localPosition = at;
            GameObject column = Part(pillar, PrimitiveType.Cylinder, "Column", new Vector3(0f, 1.25f, 0f), new Vector3(1.5f, 1.25f, 1.5f), Vector3.zero, pillarMat, true);
            column.name = "Column";
            Part(pillar, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(1.9f, 0.15f, 1.9f), Vector3.zero, rockMat);
            Part(pillar, PrimitiveType.Cylinder, "Top", new Vector3(0f, 2.55f, 0f), new Vector3(1.8f, 0.12f, 1.8f), new Vector3(4f, 0f, -5f), rockMat);
        }

        // rocks along the walls, only decoration
        GameObject rocks = new GameObject("Rocks");
        rocks.transform.SetParent(root.transform, false);
        for (int i = 0; i < 18; i++)
        {
            bool sideways = i % 2 == 0;
            float along = R(-6.8f, 6.8f);
            float edge = (i % 4 < 2 ? -1f : 1f) * R(7f, 7.3f);
            Vector3 position = sideways ? new Vector3(edge, 0.1f, along) : new Vector3(along, 0.1f, 7.1f);
            Part(rocks, PrimitiveType.Sphere, "Rock", position, new Vector3(R(0.5f, 1.1f), R(0.3f, 0.7f), R(0.5f, 1.1f)), new Vector3(0f, R(0f, 180f), 0f), rockMat);
        }

        // the dragon's treasure, behind it
        GameObject hoard = new GameObject("Hoard");
        hoard.transform.SetParent(root.transform, false);
        for (int i = 0; i < 45; i++)
        {
            float x = R(-4f, -1.8f);
            float z = R(-7.2f, -5.6f);
            float y = Mathf.Max(0.05f, 0.55f - Mathf.Abs(x + 2.9f) * 0.35f - Mathf.Abs(z + 6.4f) * 0.3f) + R(0f, 0.08f);
            Part(hoard, PrimitiveType.Sphere, "Coin", new Vector3(x, y, z), new Vector3(0.3f, 0.09f, 0.3f), new Vector3(R(-25f, 25f), 0f, R(-25f, 25f)), goldMat);
        }

        return SavePrefab(root, "DragonLair");
    }

    static GameObject Wall(GameObject parent, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent.transform, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return wall;
    }

    // ---------- the dragon ----------

    static GameObject BuildDragon()
    {
        Material hide = Lit("DragonHide", Hide, 0f, 0.45f);
        Material dark = Lit("DragonDark", HideDark, 0f, 0.35f);
        Material belly = Lit("DragonBelly", Belly, 0f, 0.3f);
        Material horn = Lit("DragonHorn", Horn, 0f, 0.4f);
        Material eye = Lit("DragonEye", new Color32(0xFF, 0xD8, 0x3A, 255), 2.5f, 0.8f);
        Material pupil = Lit("DragonPupil", new Color32(0x10, 0x08, 0x08, 255), 0f, 0.8f);
        Material inside = Lit("DragonMouth", new Color32(0xFF, 0x58, 0x18, 255), 1.6f, 0.3f);
        Material wingSkin = Lit("DragonWing", new Color32(0x6B, 0x16, 0x20, 255), 0f, 0.15f);
        Material fireball = Lit("Fireball", new Color32(0xFF, 0x8A, 0x1E, 255), 3f, 0.5f);
        Material soot = Lit("Soot", new Color32(0x1C, 0x16, 0x16, 255), 0f, 0.1f);

        Texture2D dot = SoftDot();
        Material flame = Glow("FlameGlow", new Color(1f, 0.65f, 0.25f, 1f), dot);
        Material danger = Glow("DangerGlow", new Color(1f, 0.16f, 0.05f, 0.2f), null);

        GameObject root = new GameObject("Dragon");

        BoxCollider bodyBlock = root.AddComponent<BoxCollider>();
        bodyBlock.center = new Vector3(0f, 1.5f, 0f);
        bodyBlock.size = new Vector3(3.6f, 3f, 4.2f);

        // body and legs
        Part(root, PrimitiveType.Sphere, "Body", new Vector3(0f, 1.45f, -0.1f), new Vector3(3.3f, 2.5f, 4f), Vector3.zero, hide);
        Part(root, PrimitiveType.Sphere, "Belly", new Vector3(0f, 1.2f, 0.75f), new Vector3(2.3f, 2f, 2.4f), Vector3.zero, belly);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(root, PrimitiveType.Sphere, "Haunch", new Vector3(side * 1.55f, 0.85f, -0.9f), new Vector3(1.4f, 1.7f, 1.9f), Vector3.zero, hide);
            Part(root, PrimitiveType.Sphere, "BackFoot", new Vector3(side * 1.65f, 0.2f, -0.2f), new Vector3(0.95f, 0.4f, 1.3f), Vector3.zero, dark);
            Part(root, PrimitiveType.Capsule, "FrontLeg", new Vector3(side * 1.25f, 0.7f, 1.35f), new Vector3(0.75f, 0.75f, 0.75f), Vector3.zero, hide);
            Part(root, PrimitiveType.Sphere, "FrontFoot", new Vector3(side * 1.3f, 0.18f, 1.6f), new Vector3(0.85f, 0.36f, 1.1f), Vector3.zero, dark);
            for (int c = -1; c <= 1; c++)
                Part(root, PrimitiveType.Cube, "Claw", new Vector3(side * 1.3f + c * 0.24f, 0.1f, 2.15f), new Vector3(0.11f, 0.13f, 0.32f), new Vector3(16f, 0f, 0f), horn);
        }

        // spikes along the back
        for (int i = 0; i < 6; i++)
        {
            float k = i / 5f;
            Vector3 position = new Vector3(0f, Mathf.Lerp(2.75f, 2.05f, k) - Mathf.Abs(k - 0.2f) * 0.1f, Mathf.Lerp(0.8f, -1.75f, k));
            float size = Mathf.Lerp(0.6f, 0.34f, k);
            Part(root, PrimitiveType.Cube, "Spike", position, new Vector3(0.1f, size, size), new Vector3(45f, 0f, 0f), dark);
        }

        // the chest is the point the neck grows from; it is an empty marker that the script moves
        GameObject chest = new GameObject("Chest");
        chest.transform.SetParent(root.transform, false);
        chest.transform.localPosition = new Vector3(0f, 1.95f, 1.15f);

        // neck
        List<Transform> neck = new List<Transform>();
        for (int i = 0; i < 5; i++)
        {
            float size = Mathf.Lerp(1.25f, 0.78f, i / 4f);
            neck.Add(Part(root, PrimitiveType.Sphere, "Neck", new Vector3(0f, 2.3f + i * 0.28f, 1.3f + i * 0.28f), Vector3.one * size, Vector3.zero, i % 2 == 0 ? hide : dark).transform);
        }

        // head
        GameObject head = new GameObject("Head");
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0f, 3.65f, 2.85f);
        Part(head, PrimitiveType.Sphere, "Skull", new Vector3(0f, 0f, 0f), new Vector3(1.2f, 0.95f, 1.3f), Vector3.zero, hide);
        Part(head, PrimitiveType.Cube, "Snout", new Vector3(0f, 0.02f, 0.85f), new Vector3(0.82f, 0.42f, 1.05f), Vector3.zero, hide);
        Part(head, PrimitiveType.Cube, "SnoutTop", new Vector3(0f, 0.28f, 0.72f), new Vector3(0.55f, 0.16f, 0.6f), new Vector3(-8f, 0f, 0f), dark);
        Part(head, PrimitiveType.Cube, "MouthInside", new Vector3(0f, -0.2f, 0.72f), new Vector3(0.68f, 0.05f, 0.95f), Vector3.zero, inside);
        foreach (float side in new[] { -1f, 1f })
        {
            Part(head, PrimitiveType.Sphere, "Eye", new Vector3(side * 0.5f, 0.2f, 0.42f), Vector3.one * 0.3f, Vector3.zero, eye);
            Part(head, PrimitiveType.Sphere, "Pupil", new Vector3(side * 0.6f, 0.2f, 0.5f), new Vector3(0.1f, 0.2f, 0.12f), Vector3.zero, pupil);
            Part(head, PrimitiveType.Cube, "Brow", new Vector3(side * 0.42f, 0.42f, 0.42f), new Vector3(0.48f, 0.1f, 0.3f), new Vector3(0f, 0f, side * -18f), dark);
            Part(head, PrimitiveType.Cylinder, "Horn", new Vector3(side * 0.42f, 0.55f, -0.55f), new Vector3(0.17f, 0.55f, 0.17f), new Vector3(-55f, 0f, side * -14f), horn);
            Part(head, PrimitiveType.Sphere, "Nostril", new Vector3(side * 0.22f, 0.2f, 1.33f), Vector3.one * 0.11f, Vector3.zero, pupil);
            for (int t = 0; t < 3; t++)
                Part(head, PrimitiveType.Cube, "Tooth", new Vector3(side * 0.34f, -0.25f, 0.65f + t * 0.3f), new Vector3(0.08f, 0.18f, 0.08f), new Vector3(0f, 45f, 0f), horn);
        }

        GameObject jaw = new GameObject("Jaw");
        jaw.transform.SetParent(head.transform, false);
        jaw.transform.localPosition = new Vector3(0f, -0.26f, 0.3f);
        Part(jaw, PrimitiveType.Cube, "LowerJaw", new Vector3(0f, -0.06f, 0.5f), new Vector3(0.74f, 0.16f, 1f), Vector3.zero, dark);
        Part(jaw, PrimitiveType.Cube, "Tongue", new Vector3(0f, 0.04f, 0.45f), new Vector3(0.45f, 0.04f, 0.8f), Vector3.zero, inside);

        GameObject mouth = new GameObject("Mouth");
        mouth.transform.SetParent(head.transform, false);
        mouth.transform.localPosition = new Vector3(0f, -0.2f, 1.45f);

        GameObject glowObj = new GameObject("MouthGlow");
        glowObj.transform.SetParent(mouth.transform, false);
        Light mouthGlow = glowObj.AddComponent<Light>();
        mouthGlow.type = LightType.Point;
        mouthGlow.color = new Color(1f, 0.5f, 0.15f);
        mouthGlow.range = 9f;
        mouthGlow.intensity = 0f;

        ParticleSystem flames = BuildBreath(mouth.transform, flame);

        // wings
        Mesh wingMesh = BuildWingMesh();
        Transform wingRight = BuildWing(root, "WingRight", 1f, wingMesh, wingSkin, dark);
        Transform wingLeft = BuildWing(root, "WingLeft", -1f, wingMesh, wingSkin, dark);

        // tail, curling along the back wall
        Vector3[] tailPath =
        {
            new Vector3(0.15f, 1f, -2f), new Vector3(0.85f, 0.8f, -2.2f), new Vector3(1.6f, 0.62f, -2.25f), new Vector3(2.35f, 0.48f, -2.15f),
            new Vector3(3.05f, 0.36f, -1.95f), new Vector3(3.7f, 0.27f, -1.65f), new Vector3(4.2f, 0.2f, -1.25f),
        };
        List<Transform> tail = new List<Transform>();
        for (int i = 0; i < tailPath.Length; i++)
        {
            float size = Mathf.Lerp(1.15f, 0.34f, i / (tailPath.Length - 1f));
            tail.Add(Part(root, PrimitiveType.Sphere, "Tail", tailPath[i], Vector3.one * size, Vector3.zero, i % 2 == 0 ? hide : dark).transform);
        }
        GameObject tip = Part(tail[tail.Count - 1].gameObject, PrimitiveType.Cube, "TailTip", new Vector3(0.75f, 0f, 0.75f), new Vector3(1.5f, 0.2f, 1.5f), new Vector3(0f, 45f, 0f), dark);
        tip.transform.localScale = new Vector3(1.6f, 0.3f, 1.6f);

        // the boss script and all its links
        DragonBoss boss = root.AddComponent<DragonBoss>();
        boss.chest = chest.transform;
        boss.head = head.transform;
        boss.jaw = jaw.transform;
        boss.mouth = mouth.transform;
        boss.wingLeft = wingLeft;
        boss.wingRight = wingRight;
        boss.neck = neck.ToArray();
        boss.tail = tail.ToArray();
        boss.breathFlames = flames;
        boss.mouthGlow = mouthGlow;
        boss.fireballMaterial = fireball;
        boss.dangerMaterial = danger;
        boss.sootMaterial = soot;

        return SavePrefab(root, "Dragon");
    }

    static Transform BuildWing(GameObject root, string name, float side, Mesh mesh, Material skin, Material bone)
    {
        GameObject wing = new GameObject(name);
        wing.transform.SetParent(root.transform, false);
        wing.transform.localPosition = new Vector3(side * 1f, 2.45f, -0.25f);

        // the left wing is the right one mirrored
        GameObject membrane = new GameObject("Membrane");
        membrane.transform.SetParent(wing.transform, false);
        membrane.transform.localScale = new Vector3(side, 1f, 1f);
        membrane.AddComponent<MeshFilter>().sharedMesh = mesh;
        membrane.AddComponent<MeshRenderer>().sharedMaterial = skin;

        foreach (Vector3 tip in WingTips)
        {
            Vector3 end = new Vector3(tip.x * side, tip.y, tip.z);
            GameObject finger = Part(wing, PrimitiveType.Cube, "Finger", end * 0.5f, new Vector3(0.09f, 0.09f, end.magnitude), Vector3.zero, bone);
            finger.transform.localRotation = Quaternion.LookRotation(end);
        }
        return wing.transform;
    }

    static readonly Vector3[] WingTips =
    {
        new Vector3(3.3f, 2.1f, -0.3f),
        new Vector3(3.7f, 0.8f, -1f),
        new Vector3(2.9f, -0.1f, -1.6f),
        new Vector3(1.5f, -0.55f, -1.8f),
    };

    // A fan of triangles between the wing bones, with both faces so it can be seen from any side.
    static Mesh BuildWingMesh()
    {
        string path = Art + "/DragonWing.asset";
        Mesh old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old != null) AssetDatabase.DeleteAsset(path);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        for (int i = 0; i < WingTips.Length - 1; i++)
        {
            Vector3 a = WingTips[i];
            Vector3 b = WingTips[i + 1];
            Vector3 dip = (a + b) * 0.5f * 0.82f; // the skin sags in between two bones

            AddFace(vertices, triangles, Vector3.zero, a, dip);
            AddFace(vertices, triangles, Vector3.zero, dip, b);
        }

        Mesh mesh = new Mesh { name = "DragonWing" };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    static void AddFace(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
    {
        foreach (Vector3[] face in new[] { new[] { a, b, c }, new[] { a, c, b } })
        {
            foreach (Vector3 v in face)
            {
                triangles.Add(vertices.Count);
                vertices.Add(v);
            }
        }
    }

    static ParticleSystem BuildBreath(Transform mouth, Material material)
    {
        GameObject go = new GameObject("BreathFlames");
        go.transform.SetParent(mouth, false);

        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8.5f, 11f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 150f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 0.12f;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 3f));

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.92f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.35f), new GradientColorKey(new Color(0.75f, 0.12f, 0.04f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        // the fire stops at the pillars and the floor, so the safe spot can be seen
        ParticleSystem.CollisionModule collision = system.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.Medium;
        collision.lifetimeLoss = 1f;
        collision.bounce = 0f;
        collision.radiusScale = 0.2f;
        collision.sendCollisionMessages = false;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        return system;
    }

    static void BuildEmbers(Transform parent, Material material)
    {
        GameObject go = new GameObject("Embers");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
        main.startColor = new Color(1f, 0.55f, 0.15f, 1f);
        main.maxParticles = 60;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = 12f;

        ParticleSystem.ShapeModule shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(14f, 14f, 0.2f);

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        go.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
    }

    // ---------- the scene ----------

    static void SetUpScene(StageConfig config, GameObject scalePrefab, GameObject lairPrefab, GameObject dragonPrefab)
    {
        // keep the stage's manager, wizard, cauldron, camera, HUD and input; remove the lab
        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go == null || go.transform.parent != null) continue;
            if (go.name == "LabRoom" || go.name == "LabDecor" || go.GetComponent<Ingredient>() != null)
                Object.DestroyImmediate(go);
        }

        Object.FindFirstObjectByType<StageManager>().config = config;

        Cauldron cauldron = Object.FindFirstObjectByType<Cauldron>();
        cauldron.transform.position = new Vector3(-4.6f, 0f, 4.8f);

        PlayerController player = Object.FindFirstObjectByType<PlayerController>();
        player.transform.position = new Vector3(0f, player.transform.position.y, 5.6f);
        player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        Camera camera = Camera.main;
        CameraFollow follow = camera.GetComponent<CameraFollow>();
        if (follow != null) Object.DestroyImmediate(follow);
        LairCamera lairCamera = camera.gameObject.AddComponent<LairCamera>();
        lairCamera.target = player.transform;
        camera.transform.position = lairCamera.position;
        camera.transform.LookAt(lairCamera.lookAt);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color32(0x10, 0x09, 0x0C, 255);
        camera.gameObject.AddComponent<ScreenRumble>();

        // this is the last stage, so the "next stage" button never shows: hide its shadow too
        foreach (Transform t in Object.FindFirstObjectByType<HUD>(FindObjectsInactive.Include).GetComponentsInChildren<Transform>(true))
            if (t.name == "NextButtonShadow") t.gameObject.SetActive(false);

        Light sun = null;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (l.type == LightType.Directional) sun = l;
        if (sun != null)
        {
            sun.color = new Color(1f, 0.66f, 0.5f);
            sun.intensity = 0.8f;
        }

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.44f, 0.33f, 0.35f);
        RenderSettings.fog = false;

        GameObject lair = (GameObject)PrefabUtility.InstantiatePrefab(lairPrefab);
        BuildEmbers(lair.transform, AssetDatabase.LoadAssetAtPath<Material>(Art + "/FlameGlow.mat"));

        GameObject dragon = (GameObject)PrefabUtility.InstantiatePrefab(dragonPrefab);
        dragon.transform.position = new Vector3(0f, 0f, -4.9f);

        List<Transform> cover = new List<Transform>();
        Transform pillars = lair.transform.Find("Pillars");
        foreach (Transform pillar in pillars) cover.Add(pillar);
        DragonBoss boss = dragon.GetComponent<DragonBoss>();
        boss.cover = cover.ToArray();

        // the scales are guarded by the dragon, the crystals are on the way there
        Place(scalePrefab, new Vector3(-5.6f, 0f, -3.9f));
        Place(scalePrefab, new Vector3(5.6f, 0f, -3.9f));
        Place(scalePrefab, new Vector3(0f, 0f, -1.25f));

        GameObject crystalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Shared/Prefabs/Crystal.prefab");
        Place(crystalPrefab, new Vector3(5.6f, 0f, 2.2f));
        Place(crystalPrefab, new Vector3(-5.8f, 0f, 0.4f));

        EditorSceneManager.MarkAllScenesDirty();
    }

    static void Place(GameObject prefab, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
    }

    // ---------- helpers ----------

    static GameObject Part(GameObject parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Vector3 rotation, Material mat, bool keepCollider = false)
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

    static GameObject SavePrefab(GameObject template, string name)
    {
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(template, Prefabs + "/" + name + ".prefab");
        Object.DestroyImmediate(template);
        return asset;
    }
}
