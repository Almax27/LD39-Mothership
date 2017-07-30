using System.Collections.Generic;
using UnityEngine;

public class CircleMeshGenerator : MonoBehaviour
{
    public float innerRadius = 0;
    public float outerRadius = 1;
    [Range(6, 9999)]
    public int segmentCount = 6;

    public MeshRenderer meshRenderer = null;
    public MeshFilter meshFilter = null;

    [ContextMenu("Generate Mesh")]
    public void Generate()
    {
        outerRadius = Mathf.Max(0, outerRadius);
        innerRadius = Mathf.Clamp(innerRadius, 0, outerRadius);
        segmentCount = Mathf.Max(6, segmentCount);

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangleList = new List<int>();

        float stepAngle = 360.0f / segmentCount;
        float angle = 0;
        vertices.Add(Vector3.up * innerRadius);
        vertices.Add(Vector3.up * outerRadius);
        for (int i = 0; i < segmentCount; i++)
        {
            angle += stepAngle;
            vertices.Add(Quaternion.Euler(0, 0, angle) * Vector3.up * innerRadius);
            vertices.Add(Quaternion.Euler(0, 0, angle) * Vector3.up * outerRadius);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int startIndex = i * 2;
            triangleList.Add(startIndex);
            triangleList.Add(startIndex + 1);
            triangleList.Add(startIndex + 2);

            triangleList.Add(startIndex + 1);
            triangleList.Add(startIndex + 3);
            triangleList.Add(startIndex + 2);
        }

        // Create the mesh
        Mesh msh = new Mesh();
        msh.vertices = vertices.ToArray();
        msh.triangles = triangleList.ToArray();
        msh.RecalculateNormals();
        msh.RecalculateBounds();

        // Set up game object with mesh;
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        meshFilter.mesh = msh;
    }
    void Awake()
    {
        Generate();
    }
}
