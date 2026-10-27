using UnityEditor;
using UnityEngine;

// Adds a menu item that builds the lab decorations (shelves, table, barrels, rug) as their own prefab.
public static class LabDecorBuilder
{
    const string PrefabPath = "Assets/Shared/Prefabs/LabDecor.prefab";

    static readonly Color32[] BottleColors =
    {
        new Color32(0xFF, 0x6B, 0x9A, 255),
        new Color32(0x6F, 0xD3, 0xFF, 255),
        new Color32(0x9D, 0xFF, 0x6B, 255),
        new Color32(0xFF, 0xC9, 0x4D, 255),
        new Color32(0xC5, 0x8C, 0xFF, 255),
    };

    [MenuItem("Potion Rush/Build Lab Decor")]
    static void Build()
    {
        Material wood = LabRoomBuilder.GetMaterial("DecorWood", new Color32(0x5A, 0x3A, 0x24, 255));
        Material barrel = LabRoomBuilder.GetMaterial("DecorBarrel", new Color32(0x6D, 0x44, 0x26, 255));
        Material iron = LabRoomBuilder.GetMaterial("DecorIron", new Color32(0x2C, 0x2C, 0x30, 255));
        Material book = LabRoomBuilder.GetMaterial("DecorBook", new Color32(0x7A, 0x24, 0x30, 255));
        Material rugRed = LabRoomBuilder.GetMaterial("RugRed", new Color32(0x5A, 0x1F, 0x3A, 255));
        Material rugGold = LabRoomBuilder.GetMaterial("RugGold", new Color32(0xC9, 0x91, 0x3A, 255));
        Material potion = LabRoomBuilder.GetMaterial("DecorPotion", new Color32(0x9D, 0xFF, 0x6B, 255), 0.5f);

        GameObject root = new GameObject("LabDecor");

        // shelves with bottles on the back wall
        BuildShelf(root, "Shelf_Left", -5.8f, -1.6f, wood, 0);
        BuildShelf(root, "Shelf_Right", 1.6f, 5.8f, wood, 1);

        // table with a book and a potion
        GameObject table = new GameObject("Table");
        table.transform.SetParent(root.transform);
        Part(table, PrimitiveType.Cube, "Top", new Vector3(-1.6f, 0.9f, 1.6f), new Vector3(2.2f, 0.12f, 1.2f), wood);
        foreach (Vector2 o in new[] { new Vector2(-0.95f, -0.5f), new Vector2(0.95f, -0.5f), new Vector2(-0.95f, 0.5f), new Vector2(0.95f, 0.5f) })
            Part(table, PrimitiveType.Cube, "Leg", new Vector3(-1.6f + o.x, 0.45f, 1.6f + o.y), new Vector3(0.1f, 0.9f, 0.1f), wood);
        Part(table, PrimitiveType.Cube, "Book", new Vector3(-2.1f, 1.03f, 1.5f), new Vector3(0.6f, 0.14f, 0.45f), book);
        Part(table, PrimitiveType.Sphere, "Potion", new Vector3(-1.1f, 1.14f, 1.7f), Vector3.one * 0.36f, potion);

        // barrels
        foreach (Vector2 p in new[] { new Vector2(5.9f, -5.8f), new Vector2(4.8f, -6.0f), new Vector2(6.0f, 5.6f) })
        {
            GameObject b = new GameObject("Barrel");
            b.transform.SetParent(root.transform);
            Part(b, PrimitiveType.Cylinder, "Body", new Vector3(p.x, 0.525f, p.y), new Vector3(1f, 0.525f, 1f), barrel);
            GameObject ring = Part(b, PrimitiveType.Cylinder, "Ring", new Vector3(p.x, 0.8f, p.y), new Vector3(1.04f, 0.03f, 1.04f), iron);
            Object.DestroyImmediate(ring.GetComponent<Collider>());
        }

        // rug (no collider, it is flat on the floor)
        GameObject rugObj = new GameObject("Rug");
        rugObj.transform.SetParent(root.transform);
        GameObject gold = Part(rugObj, PrimitiveType.Cylinder, "Border", new Vector3(1.6f, 0.008f, 3.5f), new Vector3(3.6f, 0.004f, 3.6f), rugGold);
        GameObject red = Part(rugObj, PrimitiveType.Cylinder, "Center", new Vector3(1.6f, 0.012f, 3.5f), new Vector3(3.1f, 0.004f, 3.1f), rugRed);
        Object.DestroyImmediate(gold.GetComponent<Collider>());
        Object.DestroyImmediate(red.GetComponent<Collider>());

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = root;
        Debug.Log("Lab decor created and saved to " + PrefabPath);
    }

    static void BuildShelf(GameObject parent, string name, float x0, float x1, Material wood, int offset)
    {
        GameObject shelf = new GameObject(name);
        shelf.transform.SetParent(parent.transform);

        float[] levels = { 1.25f, 2.15f };
        for (int l = 0; l < levels.Length; l++)
        {
            float y = levels[l];
            Part(shelf, PrimitiveType.Cube, "Plank", new Vector3((x0 + x1) / 2f, y, -7.0f), new Vector3(x1 - x0, 0.1f, 0.6f), wood);

            for (int i = 0; i < 5; i++)
            {
                int c = (i + l * 2 + offset) % BottleColors.Length;
                float h = 0.3f + ((i * 7) % 3) * 0.08f;
                Material m = LabRoomBuilder.GetMaterial("Bottle_" + c, BottleColors[c], 0.35f);
                float x = x0 + 0.45f + i * ((x1 - x0 - 0.9f) / 4f);
                GameObject bottle = Part(shelf, PrimitiveType.Cylinder, "Bottle", new Vector3(x, y + 0.05f + h / 2f, -6.95f), new Vector3(0.24f, h / 2f, 0.24f), m);
                Object.DestroyImmediate(bottle.GetComponent<Collider>());
            }
        }
    }

    static GameObject Part(GameObject parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }
}
