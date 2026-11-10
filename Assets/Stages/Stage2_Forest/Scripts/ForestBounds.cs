using UnityEngine;

// Four invisible walls around the forest ground so the wizard (and the ghost) can never
// walk through the gaps in the tree ring and fall off the edge.
// The ground is a Plane scaled 3.2, so it spans -16..16 on X and Z.
public class ForestBounds : MonoBehaviour
{
    [SerializeField] float minX = -15f;
    [SerializeField] float maxX = 15f;
    [SerializeField] float minZ = -14f;  // north, just behind the north tree line
    [SerializeField] float maxZ = 15.5f; // south, just inside the ground edge
    [SerializeField] float height = 4f;
    [SerializeField] float thickness = 1f;

    void Awake()
    {
        GameObject root = new GameObject("ForestBoundaryWalls");

        float width = maxX - minX;
        float depth = maxZ - minZ;
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;

        Wall(root, "Wall_North", new Vector3(centerX, height * 0.5f, minZ - thickness * 0.5f), new Vector3(width + thickness * 2f, height, thickness));
        Wall(root, "Wall_South", new Vector3(centerX, height * 0.5f, maxZ + thickness * 0.5f), new Vector3(width + thickness * 2f, height, thickness));
        Wall(root, "Wall_West",  new Vector3(minX - thickness * 0.5f, height * 0.5f, centerZ), new Vector3(thickness, height, depth));
        Wall(root, "Wall_East",  new Vector3(maxX + thickness * 0.5f, height * 0.5f, centerZ), new Vector3(thickness, height, depth));
    }

    static void Wall(GameObject parent, string name, Vector3 center, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent.transform, false);
        wall.transform.position = center;
        BoxCollider box = wall.AddComponent<BoxCollider>(); // collider only, nothing is drawn
        box.size = size;
    }
}
