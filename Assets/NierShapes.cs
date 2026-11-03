using System.Collections.Generic;
using UnityEngine;

// Procedural meshes for the sharp shapes that cubes and spheres can't make (arrowheads, diamonds,
// spikes, octahedra). Every mesh is double-sided, so face winding never hides a face, and the unlit
// material makes each one read as a flat NieR-style silhouette.
public static class NierShapes
{
    // A flat polygon extruded along z (front face toward the camera at -z).
    // The outline must be star-shaped around (0, 0): every point visible from the origin.
    public static Mesh Prism(Vector2[] outline, float depth)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        float front = -depth / 2;
        float back = depth / 2;
        int n = outline.Length;

        // Front and back faces: triangle fans from the origin
        int frontCenter = AddVertex(vertices, new Vector3(0, 0, front));
        int backCenter = AddVertex(vertices, new Vector3(0, 0, back));
        int frontStart = vertices.Count;
        foreach (Vector2 p in outline)
        {
            vertices.Add(new Vector3(p.x, p.y, front));
        }
        int backStart = vertices.Count;
        foreach (Vector2 p in outline)
        {
            vertices.Add(new Vector3(p.x, p.y, back));
        }

        for (int i = 0; i < n; i++)
        {
            int next = (i + 1) % n;
            AddDoubleSided(triangles, frontCenter, frontStart + next, frontStart + i);
            AddDoubleSided(triangles, backCenter, backStart + i, backStart + next);

            // Side quad
            AddDoubleSided(triangles, frontStart + i, frontStart + next, backStart + next);
            AddDoubleSided(triangles, frontStart + i, backStart + next, backStart + i);
        }
        return Build(vertices, triangles);
    }

    // Six points on the x, y and z axes joined into eight triangles
    public static Mesh Octahedron(float width, float height, float depth)
    {
        List<Vector3> vertices = new List<Vector3>
        {
            new Vector3(width / 2, 0, 0), new Vector3(-width / 2, 0, 0),
            new Vector3(0, height / 2, 0), new Vector3(0, -height / 2, 0),
            new Vector3(0, 0, depth / 2), new Vector3(0, 0, -depth / 2),
        };
        int[] ring = { 0, 4, 1, 5 }; // around the middle
        List<int> triangles = new List<int>();
        for (int i = 0; i < ring.Length; i++)
        {
            int a = ring[i];
            int b = ring[(i + 1) % ring.Length];
            AddDoubleSided(triangles, 2, a, b); // top half
            AddDoubleSided(triangles, 3, b, a); // bottom half
        }
        return Build(vertices, triangles);
    }

    // Corner points of a regular polygon, counter-clockwise
    public static Vector2[] RegularPolygon(int sides, float radius, float rotationDegrees = 0)
    {
        Vector2[] points = new Vector2[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = (rotationDegrees + 360f * i / sides) * Mathf.Deg2Rad;
            points[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return points;
    }

    public static Vector2[] Scale(Vector2[] outline, float factor)
    {
        Vector2[] scaled = new Vector2[outline.Length];
        for (int i = 0; i < outline.Length; i++)
        {
            scaled[i] = outline[i] * factor;
        }
        return scaled;
    }

    static int AddVertex(List<Vector3> vertices, Vector3 vertex)
    {
        vertices.Add(vertex);
        return vertices.Count - 1;
    }

    static void AddDoubleSided(List<int> triangles, int a, int b, int c)
    {
        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);
        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(b);
    }

    static Mesh Build(List<Vector3> vertices, List<int> triangles)
    {
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
