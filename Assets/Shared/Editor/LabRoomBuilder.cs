using UnityEditor;
using UnityEngine;

// Adds a menu item that builds the shared lab room (floor + walls) and saves it as a prefab.
public static class LabRoomBuilder
{
    const string ArtFolder = "Assets/Shared/Art";
    const string PrefabPath = "Assets/Shared/Prefabs/LabRoom.prefab";

    [MenuItem("Potion Rush/Build Lab Room")]
    static void Build()
    {
        Material floorMat = GetMaterial("FloorWood", new Color32(0x6B, 0x47, 0x2E, 255));
        Material wallMat = GetMaterial("WallStone", new Color32(0x4B, 0x3F, 0x60, 255));

        GameObject root = new GameObject("LabRoom");

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(root.transform);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = new Vector3(1.5f, 1f, 1.5f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

        CreateWall(root, "Wall_Back", new Vector3(0f, 1.7f, -7.5f), new Vector3(15f, 3.4f, 0.3f), wallMat);
        CreateWall(root, "Wall_Left", new Vector3(-7.5f, 1.7f, 0f), new Vector3(0.3f, 3.4f, 15f), wallMat);
        CreateWall(root, "Wall_Right", new Vector3(7.5f, 1.7f, 0f), new Vector3(0.3f, 3.4f, 15f), wallMat);
        CreateWall(root, "Wall_FrontLow", new Vector3(0f, 0.2f, 7.5f), new Vector3(15f, 0.4f, 0.3f), wallMat);

        PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = root;
        Debug.Log("Lab room created and saved to " + PrefabPath);
    }

    static void CreateWall(GameObject parent, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent.transform);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
    }

    static Material GetMaterial(string name, Color color)
    {
        string path = ArtFolder + "/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
