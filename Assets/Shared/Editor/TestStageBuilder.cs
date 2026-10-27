using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Adds a menu item that builds a playable test stage: the rules (StageManager + config),
// 2 mushrooms, 1 crystal and the cauldron. Mushroom, Crystal and Cauldron are saved as prefabs.
public static class TestStageBuilder
{
    const string DataFolder = "Assets/Shared/Data";
    const string PrefabFolder = "Assets/Shared/Prefabs";

    [MenuItem("Potion Rush/Build Test Stage")]
    static void Build()
    {
        if (Object.FindFirstObjectByType<StageManager>() != null)
        {
            Debug.LogWarning("A StageManager is already in the scene. Delete the old test stage objects first.");
            return;
        }

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("There is no Player in the scene. Click Potion Rush > Build Player first.");
            return;
        }

        // stage settings
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/Shared", "Data");
        string configPath = DataFolder + "/SpeedPotion.asset";
        StageConfig config = AssetDatabase.LoadAssetAtPath<StageConfig>(configPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<StageConfig>();
            config.stageName = "Apprentice's Lab";
            config.potionName = "Speed potion";
            config.timeLimit = 120f;
            config.shakesRequired = 10;
            config.recipe = new List<RecipeEntry>
            {
                new RecipeEntry { type = IngredientType.Mushroom, amount = 2 },
                new RecipeEntry { type = IngredientType.Crystal, amount = 1 },
            };
            AssetDatabase.CreateAsset(config, configPath);
        }

        GameObject managerObj = new GameObject("StageManager");
        managerObj.AddComponent<StageManager>().config = config;

        // prefabs
        GameObject mushroomPrefab = SavePrefab(BuildMushroom(), "Mushroom");
        GameObject crystalPrefab = SavePrefab(BuildCrystal(), "Crystal");
        GameObject cauldronPrefab = SavePrefab(BuildCauldron(), "Cauldron");

        Place(mushroomPrefab, new Vector3(-4f, 0f, -2.4f));
        Place(mushroomPrefab, new Vector3(0f, 0f, -4.4f));
        Place(crystalPrefab, new Vector3(4.35f, 0f, 1.0f));
        Place(cauldronPrefab, new Vector3(1.6f, 0f, 3.5f));

        // the player needs to be able to tap ingredients
        if (player.GetComponent<Interactor>() == null)
        {
            player.AddComponent<Interactor>();
            if (PrefabUtility.IsPartOfPrefabInstance(player))
                PrefabUtility.ApplyPrefabInstance(player, InteractionMode.UserAction);
        }

        Debug.Log("Test stage created. Press Play: walk to the 2 mushrooms and the crystal, click them, then walk to the cauldron and press Space 10 times.");
    }

    static GameObject BuildMushroom()
    {
        Material stem = LabRoomBuilder.GetMaterial("MushroomStem", new Color32(0xF3, 0xE9, 0xDC, 255));
        Material cap = LabRoomBuilder.GetMaterial("MushroomCap", new Color32(0xE2, 0x38, 0x4B, 255), 0.4f);

        GameObject root = new GameObject("Mushroom");
        Part(root, PrimitiveType.Cylinder, "Stem", new Vector3(0f, 0.2f, 0f), new Vector3(0.28f, 0.2f, 0.28f), Vector3.zero, stem);
        Part(root, PrimitiveType.Sphere, "Cap", new Vector3(0f, 0.45f, 0f), new Vector3(0.7f, 0.4f, 0.7f), Vector3.zero, cap);
        AddTapTarget(root, IngredientType.Mushroom, 45f);
        return root;
    }

    static GameObject BuildCrystal()
    {
        Material gem = LabRoomBuilder.GetMaterial("CrystalBlue", new Color32(0x6F, 0xD3, 0xFF, 255), 0.9f);

        GameObject root = new GameObject("Crystal");
        Part(root, PrimitiveType.Cube, "Gem", new Vector3(0f, 0.75f, 0f), new Vector3(0.4f, 0.8f, 0.4f), new Vector3(0f, 45f, 0f), gem);
        AddTapTarget(root, IngredientType.Crystal, 90f);
        return root;
    }

    static GameObject BuildCauldron()
    {
        Material iron = LabRoomBuilder.GetMaterial("CauldronIron", new Color32(0x2B, 0x2B, 0x35, 255));
        Material liquid = LabRoomBuilder.GetMaterial("CauldronLiquid", new Color32(0x5F, 0xE3, 0xA1, 255), 0.6f);

        GameObject root = new GameObject("Cauldron");
        Part(root, PrimitiveType.Cylinder, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(1.6f, 0.5f, 1.6f), Vector3.zero, iron, true);
        Part(root, PrimitiveType.Cylinder, "Rim", new Vector3(0f, 1.0f, 0f), new Vector3(1.75f, 0.04f, 1.75f), Vector3.zero, iron);
        Part(root, PrimitiveType.Cylinder, "Liquid", new Vector3(0f, 1.045f, 0f), new Vector3(1.3f, 0.01f, 1.3f), Vector3.zero, liquid);

        GameObject lightObj = new GameObject("Glow");
        lightObj.transform.SetParent(root.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        Light glow = lightObj.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.color = new Color(0.37f, 0.89f, 0.63f);
        glow.range = 6f;
        glow.intensity = 3f;

        root.AddComponent<Cauldron>();
        return root;
    }

    static void AddTapTarget(GameObject root, IngredientType type, float spin)
    {
        SphereCollider tap = root.AddComponent<SphereCollider>();
        tap.isTrigger = true;
        tap.radius = 0.7f;
        tap.center = new Vector3(0f, 0.5f, 0f);

        Ingredient ingredient = root.AddComponent<Ingredient>();
        ingredient.type = type;
        SerializedObject so = new SerializedObject(ingredient);
        so.FindProperty("spinSpeed").floatValue = spin;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

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
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(template, PrefabFolder + "/" + name + ".prefab");
        Object.DestroyImmediate(template);
        return asset;
    }

    static void Place(GameObject prefab, Vector3 position)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
    }
}
