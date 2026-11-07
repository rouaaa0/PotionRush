using UnityEditor;
using UnityEngine;

// Stage 4 - Haunted Library.
// Adds a menu item that builds the library room (tiled floor, stone walls, moonlit windows) and saves it as a prefab.
// Same job as LabRoomBuilder for stage 1, with the same 15 x 15 m size.
// Running it again updates the LibraryRoom prefab, and the room already in the scene changes with it.
public static class LibraryRoomBuilder
{
    internal const string StageFolder = "Assets/Stages/Stage4_Library";
    internal const string PrefabFolder = StageFolder + "/Prefabs";
    const string ArtFolder = StageFolder + "/Art";
    const string PrefabPath = PrefabFolder + "/LibraryRoom.prefab";

    [MenuItem("Potion Rush/Stage 4/1. Build Library Room")]
    static void Build()
    {
        GameObject existing = GameObject.Find("LibraryRoom"); // the room already in the scene, if any
        EnsureFolders();
        Material floorA = GetMaterial("LibFloorA", new Color32(0x5A, 0x3B, 0x29, 255));
        Material floorB = GetMaterial("LibFloorB", new Color32(0x4E, 0x33, 0x24, 255));
        Material wallMat = GetMaterial("LibWall", new Color32(0x36, 0x2C, 0x4E, 255));
        Material wood = GetMaterial("LibWoodDark", new Color32(0x3D, 0x27, 0x1B, 255));
        Material glass = GetMaterial("LibWindow", new Color32(0x4A, 0x6C, 0xD8, 255), 1.4f);

        GameObject root = new GameObject("LibraryRoom");

        // The floor is 5 x 5 tiles of 3 m (instead of one big plane): on phones each object is lit
        // by only a few lights, so small tiles let every candle light up the floor around it.
        GameObject floor = new GameObject("Floor");
        floor.transform.SetParent(root.transform);
        for (int x = 0; x < 5; x++)
            for (int z = 0; z < 5; z++)
                Part(floor, PrimitiveType.Cube, "Tile", new Vector3(-6f + x * 3f, -0.05f, -6f + z * 3f), new Vector3(3f, 0.1f, 3f), Vector3.zero, (x + z) % 2 == 0 ? floorA : floorB);
        BoxCollider floorCollider = floor.AddComponent<BoxCollider>();
        floorCollider.center = new Vector3(0f, -0.1f, 0f);
        floorCollider.size = new Vector3(15f, 0.2f, 15f);

        // walls, same places as the lab
        Part(root, PrimitiveType.Cube, "Wall_Back", new Vector3(0f, 1.7f, -7.5f), new Vector3(15f, 3.4f, 0.3f), Vector3.zero, wallMat, true);
        Part(root, PrimitiveType.Cube, "Wall_Left", new Vector3(-7.5f, 1.7f, 0f), new Vector3(0.3f, 3.4f, 15f), Vector3.zero, wallMat, true);
        Part(root, PrimitiveType.Cube, "Wall_Right", new Vector3(7.5f, 1.7f, 0f), new Vector3(0.3f, 3.4f, 15f), Vector3.zero, wallMat, true);
        Part(root, PrimitiveType.Cube, "Wall_FrontLow", new Vector3(0f, 0.2f, 7.5f), new Vector3(15f, 0.4f, 0.3f), Vector3.zero, wallMat, true);

        // The front wall is kept low so the camera can see over it, but the wizard could climb a 0.4 m step
        // and fall off the floor. This invisible wall is as high as the others and stops her.
        GameObject border = new GameObject("Border_Front");
        border.transform.SetParent(root.transform);
        border.transform.localPosition = new Vector3(0f, 3f, 7.85f); // its inside face lines up with the low wall
        BoxCollider borderCollider = border.AddComponent<BoxCollider>();
        borderCollider.size = new Vector3(15.6f, 6f, 1f);

        // two tall moonlit windows on the back wall, between the bookshelves
        foreach (float x in new[] { -2.8f, 2.8f })
        {
            GameObject window = new GameObject("Window");
            window.transform.SetParent(root.transform);
            Part(window, PrimitiveType.Cube, "Glass", new Vector3(x, 1.9f, -7.33f), new Vector3(1.2f, 2f, 0.04f), Vector3.zero, glass);
            Part(window, PrimitiveType.Cube, "FrameV", new Vector3(x, 1.9f, -7.3f), new Vector3(0.08f, 2.1f, 0.06f), Vector3.zero, wood);
            Part(window, PrimitiveType.Cube, "FrameH", new Vector3(x, 2.1f, -7.3f), new Vector3(1.3f, 0.08f, 0.06f), Vector3.zero, wood);
            Part(window, PrimitiveType.Cube, "Sill", new Vector3(x, 0.85f, -7.25f), new Vector3(1.4f, 0.1f, 0.2f), Vector3.zero, wood);
        }

        MakeStatic(root);

        // safety net: puts the wizard back on the floor if she ever falls below it
        root.AddComponent<FallGuard>();

        // save the prefab; the room already in the scene (if any) is updated, otherwise one is placed
        if (existing != null)
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            Selection.activeGameObject = existing;
            Debug.Log("Library room updated in " + PrefabPath + " (the room in the scene has changed too).");
            return;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = root;
        Debug.Log("Library room created and saved to " + PrefabPath);
    }

    // ------------------------------------------------------------------ helpers shared by the stage 4 builders

    internal static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Stages")) AssetDatabase.CreateFolder("Assets", "Stages");
        if (!AssetDatabase.IsValidFolder(StageFolder)) AssetDatabase.CreateFolder("Assets/Stages", "Stage4_Library");
        if (!AssetDatabase.IsValidFolder(ArtFolder)) AssetDatabase.CreateFolder(StageFolder, "Art");
        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder(StageFolder, "Prefabs");
    }

    // Like LabRoomBuilder.GetMaterial, but the stage 4 materials live in the stage 4 folder.
    internal static Material GetMaterial(string name, Color color, float emission = 0f)
    {
        string path = ArtFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 0.2f);
        if (emission > 0f)
        {
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.SetColor("_EmissionColor", color * emission);
        }
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // The book cover colours, used by the shelves and the flying books.
    internal static Material[] BookCovers()
    {
        return new[]
        {
            GetMaterial("LibBookRed", new Color32(0x8E, 0x24, 0x2E, 255)),
            GetMaterial("LibBookGreen", new Color32(0x2E, 0x6B, 0x45, 255)),
            GetMaterial("LibBookBlue", new Color32(0x2F, 0x4B, 0x8C, 255)),
            GetMaterial("LibBookPurple", new Color32(0x5E, 0x34, 0x86, 255)),
            GetMaterial("LibBookBrown", new Color32(0x7A, 0x4E, 0x2A, 255)),
            GetMaterial("LibBookTeal", new Color32(0x1F, 0x6E, 0x73, 255)),
        };
    }

    // Positions are in the parent's space. Small parts lose their collider unless keepCollider is true.
    internal static GameObject Part(GameObject parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Vector3 rotation, Material mat, bool keepCollider = false)
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

    // Things that never move are marked static so Unity can draw them faster.
    internal static void MakeStatic(GameObject root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
    }
}
