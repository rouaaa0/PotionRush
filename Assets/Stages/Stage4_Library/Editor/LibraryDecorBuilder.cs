using UnityEditor;
using UnityEngine;

// Stage 4 - Haunted Library.
// Adds a menu item that builds the library decorations (bookshelves, rug, floating candles, fallen books)
// as their own prefab. Same job as LabDecorBuilder for stage 1.
public static class LibraryDecorBuilder
{
    const string PrefabPath = LibraryRoomBuilder.PrefabFolder + "/LibraryDecor.prefab";
    const float ShelfDepth = 0.6f;
    const float ShelfHeight = 2.8f;

    [MenuItem("Potion Rush/Stage 4/2. Build Library Decor")]
    static void Build()
    {
        if (GameObject.Find("LibraryDecor") != null)
        {
            Debug.LogWarning("A LibraryDecor is already in the scene. Delete it first if you want to rebuild it.");
            return;
        }

        LibraryRoomBuilder.EnsureFolders();
        Material wood = LibraryRoomBuilder.GetMaterial("LibWoodDark", new Color32(0x3D, 0x27, 0x1B, 255));
        Material rugRed = LibraryRoomBuilder.GetMaterial("LibRugRed", new Color32(0x6E, 0x1A, 0x2A, 255));
        Material rugGold = LibraryRoomBuilder.GetMaterial("LibRugGold", new Color32(0xC9, 0x95, 0x3A, 255));
        Material wax = LibraryRoomBuilder.GetMaterial("LibWax", new Color32(0xF2, 0xEA, 0xD8, 255));
        Material flame = LibraryRoomBuilder.GetMaterial("LibFlame", new Color32(0xFF, 0xB3, 0x47, 255), 3f);
        Material[] covers = LibraryRoomBuilder.BookCovers();

        GameObject root = new GameObject("LibraryDecor");
        GameObject shelves = new GameObject("Bookshelves");
        shelves.transform.SetParent(root.transform);
        System.Random rng = new System.Random(4); // fixed seed: every rebuild looks the same

        // back wall, around the two windows (facing the player)
        foreach (float x in new[] { -5.6f, 0f, 5.6f })
            BuildShelf(shelves, new Vector3(x, 0f, -7.05f), 0f, 3f, wood, covers, rng);

        // side walls
        foreach (float z in new[] { -4.6f, -1.6f })
        {
            BuildShelf(shelves, new Vector3(-7.05f, 0f, z), 90f, 3f, wood, covers, rng);
            BuildShelf(shelves, new Vector3(7.05f, 0f, z), -90f, 3f, wood, covers, rng);
        }

        // two rows of back-to-back shelves: they split the room into a centre lane and two side aisles
        foreach (float rowX in new[] { -3.3f, 3.3f })
        {
            BuildShelf(shelves, new Vector3(rowX - 0.3f, 0f, -1.2f), -90f, 4.5f, wood, covers, rng);
            BuildShelf(shelves, new Vector3(rowX + 0.3f, 0f, -1.2f), 90f, 4.5f, wood, covers, rng);
        }

        // a long red rug with a gold border down the centre lane
        GameObject rug = new GameObject("Rug");
        rug.transform.SetParent(root.transform);
        LibraryRoomBuilder.Part(rug, PrimitiveType.Cube, "Border", new Vector3(0f, 0.005f, 1.2f), new Vector3(2.4f, 0.01f, 8.4f), Vector3.zero, rugGold);
        LibraryRoomBuilder.Part(rug, PrimitiveType.Cube, "Center", new Vector3(0f, 0.012f, 1.2f), new Vector3(2.1f, 0.012f, 8.1f), Vector3.zero, rugRed);

        // books fallen on the floor
        GameObject fallen = new GameObject("FallenBooks");
        fallen.transform.SetParent(root.transform);
        Vector3[] spots =
        {
            new Vector3(-2.1f, 0f, 3.9f), new Vector3(2.2f, 0f, 2.3f), new Vector3(-6.4f, 0f, 0.6f), new Vector3(6.3f, 0f, 1.2f),
            new Vector3(-1.9f, 0f, -6.3f), new Vector3(2.4f, 0f, -6.2f), new Vector3(4.6f, 0f, 6.3f), new Vector3(-4.9f, 0f, 5.6f),
        };
        for (int i = 0; i < spots.Length; i++)
        {
            Vector3 p = spots[i] + Vector3.up * 0.06f;
            LibraryRoomBuilder.Part(fallen, PrimitiveType.Cube, "Book", p, new Vector3(0.45f, 0.12f, 0.6f), new Vector3(0f, i * 47f, 0f), covers[i % covers.Length]);
            if (i % 3 == 0)
                LibraryRoomBuilder.Part(fallen, PrimitiveType.Cube, "Book", p + Vector3.up * 0.12f, new Vector3(0.4f, 0.1f, 0.55f), new Vector3(0f, i * 47f + 25f, 0f), covers[(i + 2) % covers.Length]);
        }

        LibraryRoomBuilder.MakeStatic(root); // everything above stays still; the candles below float, so they are not static

        // floating candles, each with a small warm light
        GameObject candles = new GameObject("FloatingCandles");
        candles.transform.SetParent(root.transform);
        Vector3[] candleSpots =
        {
            new Vector3(-5.3f, 2.3f, -4.8f), new Vector3(5.3f, 2.3f, -1.4f), new Vector3(-5.3f, 2.3f, 1.4f),
            new Vector3(5.3f, 2.3f, 4.2f), new Vector3(-1.6f, 2.2f, -4.4f), new Vector3(0f, 2.4f, 2.6f), new Vector3(-4.6f, 2.3f, 5.0f),
        };
        foreach (Vector3 p in candleSpots) BuildCandle(candles, p, wax, flame);

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = root;
        Debug.Log("Library decor created and saved to " + PrefabPath);
    }

    // A bookshelf full of books, open side facing its local +Z. One box collider for the whole shelf.
    static void BuildShelf(GameObject parent, Vector3 position, float yRotation, float width, Material wood, Material[] covers, System.Random rng)
    {
        const float D = ShelfDepth, H = ShelfHeight;
        float W = width;

        GameObject shelf = new GameObject("Bookshelf");
        shelf.transform.SetParent(parent.transform);
        shelf.transform.localPosition = position;
        shelf.transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);

        LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "Back", new Vector3(0f, H / 2f, -D / 2f + 0.03f), new Vector3(W, H, 0.06f), Vector3.zero, wood);
        LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "SideL", new Vector3(-W / 2f + 0.05f, H / 2f, 0f), new Vector3(0.1f, H, D), Vector3.zero, wood);
        LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "SideR", new Vector3(W / 2f - 0.05f, H / 2f, 0f), new Vector3(0.1f, H, D), Vector3.zero, wood);
        LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "Top", new Vector3(0f, H - 0.05f, 0f), new Vector3(W + 0.1f, 0.1f, D + 0.06f), Vector3.zero, wood);
        LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "Base", new Vector3(0f, 0.08f, 0f), new Vector3(W, 0.16f, D), Vector3.zero, wood);
        LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "Board", new Vector3(0f, 0.87f, 0f), new Vector3(W - 0.1f, 0.06f, D - 0.04f), Vector3.zero, wood);
        LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "Board", new Vector3(0f, 1.62f, 0f), new Vector3(W - 0.1f, 0.06f, D - 0.04f), Vector3.zero, wood);

        // rows of books with random sizes and colours
        foreach (float level in new[] { 0.16f, 0.9f, 1.65f })
        {
            float x = -W / 2f + 0.14f;
            while (true)
            {
                float bookWidth = Range(rng, 0.16f, 0.3f);
                if (x + bookWidth > W / 2f - 0.14f) break;
                if (rng.NextDouble() < 0.1) { x += 0.2f; continue; } // a gap now and then

                float h = Range(rng, 0.42f, 0.66f);
                float tilt = rng.NextDouble() < 0.08 ? 8f : 0f;
                Material m = covers[rng.Next(covers.Length)];
                LibraryRoomBuilder.Part(shelf, PrimitiveType.Cube, "Book", new Vector3(x + bookWidth / 2f, level + h / 2f, 0.03f), new Vector3(bookWidth, h, D * 0.72f), new Vector3(0f, 0f, tilt), m);
                x += bookWidth + 0.01f;
            }
        }

        BoxCollider col = shelf.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, H / 2f, 0f);
        col.size = new Vector3(W, H, D);
    }

    static void BuildCandle(GameObject parent, Vector3 position, Material wax, Material flame)
    {
        GameObject candle = new GameObject("FloatingCandle");
        candle.transform.SetParent(parent.transform);
        candle.transform.localPosition = position;

        LibraryRoomBuilder.Part(candle, PrimitiveType.Cylinder, "Wax", new Vector3(0f, 0.18f, 0f), new Vector3(0.1f, 0.18f, 0.1f), Vector3.zero, wax);
        LibraryRoomBuilder.Part(candle, PrimitiveType.Sphere, "Flame", new Vector3(0f, 0.44f, 0f), new Vector3(0.07f, 0.13f, 0.07f), Vector3.zero, flame);

        GameObject lightObj = new GameObject("Light");
        lightObj.transform.SetParent(candle.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.68f, 0.35f);
        light.range = 5f;
        light.intensity = 2.8f;
        light.shadows = LightShadows.None;

        candle.AddComponent<CandleFlicker>();
    }

    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
