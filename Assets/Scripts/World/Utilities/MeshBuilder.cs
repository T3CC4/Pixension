using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Utilities
{
    public class MeshBuilder
    {
        private class SubmeshData
        {
            public List<Vector3> vertices = new List<Vector3>();
            public List<Vector3> normals = new List<Vector3>();
            public List<Color> colors = new List<Color>();
            public List<Vector2> uvs = new List<Vector2>();
            public List<int> triangles = new List<int>();
        }

        private Dictionary<Color, SubmeshData> submeshes = new Dictionary<Color, SubmeshData>();

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

            SubmeshData submesh = GetOrCreateSubmesh(color);
            int vertexOffset = submesh.vertices.Count;

            for (int i = 0; i < 4; i++)
            {
                submesh.vertices.Add(position + cubeVertices[faceIndices[faceIndex][i]]);
                submesh.normals.Add(normal);
                submesh.colors.Add(color);
                submesh.uvs.Add(faceUVs[i]);
            }

            submesh.triangles.Add(vertexOffset + 0);
            submesh.triangles.Add(vertexOffset + 1);
            submesh.triangles.Add(vertexOffset + 2);
            submesh.triangles.Add(vertexOffset + 0);
            submesh.triangles.Add(vertexOffset + 2);
            submesh.triangles.Add(vertexOffset + 3);
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

            List<Vector3> allVertices = new List<Vector3>();
            List<Vector3> allNormals = new List<Vector3>();
            List<Color> allColors = new List<Color>();
            List<Vector2> allUVs = new List<Vector2>();

            mesh.subMeshCount = submeshes.Count;
            int submeshIndex = 0;

            foreach (var kvp in submeshes)
            {
                SubmeshData submesh = kvp.Value;
                int vertexOffset = allVertices.Count;

                allVertices.AddRange(submesh.vertices);
                allNormals.AddRange(submesh.normals);
                allColors.AddRange(submesh.colors);
                allUVs.AddRange(submesh.uvs);

                List<int> offsetTriangles = new List<int>(submesh.triangles.Count);
                for (int i = 0; i < submesh.triangles.Count; i++)
                {
                    offsetTriangles.Add(submesh.triangles[i] + vertexOffset);
                }

                mesh.SetVertices(allVertices);
                mesh.SetNormals(allNormals);
                mesh.SetColors(allColors);
                mesh.SetUVs(0, allUVs);
                mesh.SetTriangles(offsetTriangles, submeshIndex);

                submeshIndex++;
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        public void Clear()
        {
            submeshes.Clear();
        }

        private SubmeshData GetOrCreateSubmesh(Color color)
        {
            if (!submeshes.ContainsKey(color))
            {
                submeshes[color] = new SubmeshData();
            }
            return submeshes[color];
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
    }
}