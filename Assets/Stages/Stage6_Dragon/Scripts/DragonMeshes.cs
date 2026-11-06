using UnityEngine;

// Flat shapes for the floor warnings. Unity has no ready-made ring or pie slice, so they are made from triangles.
public static class DragonMeshes
{
    // A pie slice on the floor, centred on +Z. A sweep of 360 makes a full disc.
    public static Mesh Fan(float radius, float sweepDegrees)
    {
        const int steps = 40;
        Vector3[] vertices = new Vector3[steps + 2];
        int[] triangles = new int[steps * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i <= steps; i++)
        {
            float angle = Mathf.Deg2Rad * (-sweepDegrees * 0.5f + sweepDegrees * i / steps);
            vertices[i + 1] = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
        }
        for (int i = 0; i < steps; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        return Finish("DragonFan", vertices, triangles);
    }

    // A hollow circle on the floor. Radius 1, so it can be scaled to any size.
    public static Mesh Ring(float inner, float outer)
    {
        const int steps = 48;
        Vector3[] vertices = new Vector3[(steps + 1) * 2];
        int[] triangles = new int[steps * 6];

        for (int i = 0; i <= steps; i++)
        {
            float angle = Mathf.PI * 2f * i / steps;
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            vertices[i * 2] = direction * inner;
            vertices[i * 2 + 1] = direction * outer;
        }
        for (int i = 0; i < steps; i++)
        {
            int t = i * 6;
            int v = i * 2;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v + 2;
            triangles[t + 4] = v + 1;
            triangles[t + 5] = v + 3;
        }
        return Finish("DragonRing", vertices, triangles);
    }

    static Mesh Finish(string name, Vector3[] vertices, int[] triangles)
    {
        Mesh mesh = new Mesh { name = name };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
