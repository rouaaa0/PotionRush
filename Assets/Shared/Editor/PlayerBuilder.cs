using UnityEditor;
using UnityEngine;

// Adds a menu item that builds the wizard, the input object and the camera follow,
// and saves the wizard and the input object as prefabs.
public static class PlayerBuilder
{
    const string PlayerPrefabPath = "Assets/Shared/Prefabs/Player.prefab";
    const string InputPrefabPath = "Assets/Shared/Prefabs/GameInput.prefab";

    [MenuItem("Potion Rush/Build Player")]
    static void Build()
    {
        if (GameObject.Find("Player") != null)
        {
            Debug.LogWarning("A Player is already in the scene. Delete it first if you want to rebuild it.");
            return;
        }

        Material robe = LabRoomBuilder.GetMaterial("PlayerRobe", new Color32(0x6B, 0x3F, 0xA0, 255));
        Material skin = LabRoomBuilder.GetMaterial("PlayerSkin", new Color32(0xF1, 0xC9, 0xA5, 255));
        Material hat = LabRoomBuilder.GetMaterial("PlayerHat", new Color32(0x3B, 0x24, 0x66, 255));

        // wizard
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(-4f, 0f, 4f);

        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.9f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.95f, 0f);
        player.AddComponent<PlayerController>();

        GameObject model = new GameObject("Model");
        model.transform.SetParent(player.transform, false);
        Part(model, PrimitiveType.Capsule, "Robe", new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.6f, 0.9f), robe);
        Part(model, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.33f, 0f), Vector3.one * 0.52f, skin);
        Part(model, PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, 1.52f, 0f), new Vector3(0.9f, 0.02f, 0.9f), hat);
        Part(model, PrimitiveType.Cylinder, "HatTop", new Vector3(0f, 1.85f, 0f), new Vector3(0.3f, 0.33f, 0.3f), hat);

        // input
        GameObject inputObj = new GameObject("GameInput");
        inputObj.AddComponent<GameInput>();

        // camera
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.fieldOfView = 38f;
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            cam.transform.position = player.transform.position + new Vector3(0f, 10f, 8.2f);
            cam.transform.LookAt(player.transform.position + Vector3.up * 0.8f);
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(player, PlayerPrefabPath, InteractionMode.UserAction);
        PrefabUtility.SaveAsPrefabAssetAndConnect(inputObj, InputPrefabPath, InteractionMode.UserAction);
        Selection.activeGameObject = player;
        Debug.Log("Player, input and camera created. Press Play, then use W A S D to move.");
    }

    static void Part(GameObject parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // the CharacterController is the collider
    }
}
