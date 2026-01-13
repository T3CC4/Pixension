using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Utilities
{
    public class MeshBuilder
    {
        private List<Vector3> vertices = new List<Vector3>();
        private List<Vector3> normals = new List<Vector3>();
        private List<Color> colors = new List<Color>();
        private List<Vector2> uvs = new List<Vector2>();
        private List<int> triangles = new List<int>();

        private static readonly Vector3[] cubeVertices = new Vector3[8]
        {
            new Vector3(-0.5f, -0.5f, -0.5f), // 0
            new Vector3( 0.5f, -0.5f, -0.5f), // 1
            new Vector3( 0.5f,  0.5f, -0.5f), // 2
            new Vector3(-0.5f,  0.5f, -0.5f), // 3
            new Vector3(-0.5f, -0.5f,  0.5f), // 4
            new Vector3( 0.5f, -0.5f,  0.5f), // 5
            new Vector3( 0.5f,  0.5f,  0.5f), // 6
            new Vector3(-0.5f,  0.5f,  0.5f)  // 7
        };

        private static readonly int[][] faceIndices = new int[6][]
        {
            new int[] { 4, 5, 6, 7 }, // Front (+Z)
            new int[] { 1, 0, 3, 2 }, // Back (-Z)
            new int[] { 0, 4, 7, 3 }, // Left (-X)
            new int[] { 5, 1, 2, 6 }, // Right (+X)
            new int[] { 7, 6, 2, 3 }, // Top (+Y)
            new int[] { 0, 1, 5, 4 }  // Bottom (-Y)
        };

        private static readonly Vector3[] faceNormals = new Vector3[6]
        {
            Vector3.forward,  // Front (+Z)
            Vector3.back,     // Back (-Z)
            Vector3.left,     // Left (-X)
            Vector3.right,    // Right (+X)
            Vector3.up,       // Top (+Y)
            Vector3.down      // Bottom (-Y)
        };

        private static readonly Vector2[] faceUVs = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };

        public void AddCubeFace(Vector3 position, Vector3 normal, Color color)
        {
            int faceIndex = GetFaceIndexFromNormal(normal);
            if (faceIndex == -1)
            {
                Debug.LogError("Invalid normal vector for cube face");
                return;
            }

            int vertexOffset = vertices.Count;

            for (int i = 0; i < 4; i++)
            {
                vertices.Add(position + cubeVertices[faceIndices[faceIndex][i]]);
                normals.Add(normal);
                colors.Add(color);
                uvs.Add(faceUVs[i]);
            }

            triangles.Add(vertexOffset + 0);
            triangles.Add(vertexOffset + 1);
            triangles.Add(vertexOffset + 2);
            triangles.Add(vertexOffset + 0);
            triangles.Add(vertexOffset + 2);
            triangles.Add(vertexOffset + 3);
        }

        public void AddCube(Vector3 position, Color color, bool[] visibleFaces)
        {
            if (visibleFaces == null || visibleFaces.Length != 6)
            {
                Debug.LogError("visibleFaces array must have exactly 6 elements");
                return;
            }

            for (int i = 0; i < 6; i++)
            {
                if (visibleFaces[i])
                {
                    AddCubeFace(position, faceNormals[i], color);
                }
            }
        }

        public Mesh GenerateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);

            mesh.RecalculateBounds();

            return mesh;
        }

        public void Clear()
        {
            vertices.Clear();
            normals.Clear();
            colors.Clear();
            uvs.Clear();
            triangles.Clear();
        }

        private int GetFaceIndexFromNormal(Vector3 normal)
        {
            for (int i = 0; i < 6; i++)
            {
                if (Vector3.Dot(normal, faceNormals[i]) > 0.99f)
                {
                    return i;
                }
            }
            return -1;
        }

        public int GetVertexCount()
        {
            return vertices.Count;
        }

        public int GetTriangleCount()
        {
            return triangles.Count / 3;
        }
    }
}