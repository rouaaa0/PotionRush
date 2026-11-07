using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Stage 4 - Haunted Library.
// Adds a menu item that builds the invisibility bottle (a bonus pickup), saves it as a prefab
// and places it in the open stage 4 scene. "3. Build Library Stage" also places it when the stage is rebuilt.
public static class LibraryPickupBuilder
{
    const string PrefabPath = LibraryRoomBuilder.PrefabFolder + "/InvisibilityPotion.prefab";
    const string GhostPath = LibraryRoomBuilder.StageFolder + "/Art/LibGhostWizard.mat";
    static readonly Vector3 Spot = new Vector3(-5.3f, 0f, -1.5f); // left aisle, on the path of a flying book

    [MenuItem("Potion Rush/Stage 4/4. Add Invisibility Potion")]
    static void Run()
    {
        if (Object.FindAnyObjectByType<InvisibilityPickup>() != null)
        {
            Debug.LogWarning("An invisibility potion is already in the scene. Delete it first if you want to rebuild it.");
            return;
        }
        if (Object.FindAnyObjectByType<StageManager>() == null)
        {
            Debug.LogWarning("Open the Stage 4 scene and build it first (Potion Rush > Stage 4 > 3. Build Library Stage).");
            return;
        }

        GameObject bottle = AddToScene();
        Selection.activeGameObject = bottle;
        Debug.Log("Invisibility potion added. Save the scene (Ctrl+S) and press Play: walk over the glowing bottle to become invisible for 5 seconds.");
    }

    internal static GameObject AddToScene()
    {
        LibraryRoomBuilder.EnsureFolders();
        GameObject template = BuildBottle();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(template, PrefabPath);
        Object.DestroyImmediate(template);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = Spot;
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);
        return instance;
    }

    static GameObject BuildBottle()
    {
        Material potion = LibraryRoomBuilder.GetMaterial("LibInvisPotion", new Color32(0x8F, 0xD8, 0xFF, 255), 1.8f);
        Material glass = LibraryRoomBuilder.GetMaterial("LibInvisGlass", new Color32(0xD9, 0xEE, 0xFF, 255), 0.4f);
        Material cork = LibraryRoomBuilder.GetMaterial("LibCork", new Color32(0x8A, 0x5A, 0x33, 255));

        GameObject root = new GameObject("InvisibilityPotion");
        LibraryRoomBuilder.Part(root, PrimitiveType.Sphere, "Potion", new Vector3(0f, 0.55f, 0f), Vector3.one * 0.5f, Vector3.zero, potion);
        LibraryRoomBuilder.Part(root, PrimitiveType.Cylinder, "Neck", new Vector3(0f, 0.88f, 0f), new Vector3(0.16f, 0.12f, 0.16f), Vector3.zero, glass);
        LibraryRoomBuilder.Part(root, PrimitiveType.Cylinder, "Cork", new Vector3(0f, 1.04f, 0f), new Vector3(0.2f, 0.06f, 0.2f), Vector3.zero, cork);

        GameObject lightObj = new GameObject("Glow");
        lightObj.transform.SetParent(root.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.55f, 0.8f, 1f);
        light.range = 3f;
        light.intensity = 2f;
        light.shadows = LightShadows.None;

        InvisibilityPickup pickup = root.AddComponent<InvisibilityPickup>();
        pickup.ghostMaterial = GhostMaterial();
        return root;
    }

    // A pale, see-through version of the URP Lit material, put on the wizard while she is invisible.
    // It is saved as an asset so the transparent shader is included when the game is built.
    static Material GhostMaterial()
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(GhostPath);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1f); // transparent
        mat.SetFloat("_Blend", 0f);   // alpha blending
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Smoothness", 0.6f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)RenderQueue.Transparent;

        Color c = new Color(0.75f, 0.85f, 1f, 0.28f);
        mat.SetColor("_BaseColor", c);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.25f, 0.35f, 0.6f));

        AssetDatabase.CreateAsset(mat, GhostPath);
        return mat;
    }
}
