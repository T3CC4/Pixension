using UnityEngine;
using System.Collections.Generic;

namespace Pixension.Voxels
{
    public class ChunkMeshGenerator
    {
        private ChunkManager chunkManager;

        private static readonly Vector3Int[] directions = new Vector3Int[6]
        {
            new Vector3Int(0, 0, 1),   // Front
            new Vector3Int(0, 0, -1),  // Back
            new Vector3Int(-1, 0, 0),  // Left
            new Vector3Int(1, 0, 0),   // Right
            new Vector3Int(0, 1, 0),   // Top
            new Vector3Int(0, -1, 0)   // Bottom
        };

        private static readonly Vector3[] faceNormals = new Vector3[6]
        {
            Vector3.forward,  // Front
            Vector3.back,     // Back
            Vector3.left,     // Left
            Vector3.right,    // Right
            Vector3.up,       // Top
            Vector3.down      // Bottom
        };

        public ChunkMeshGenerator(ChunkManager manager)
        {
            chunkManager = manager;
        }

        public Mesh GenerateMesh(Chunk chunk)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> trianglesOpaque = new List<int>();
            List<int> trianglesTransparent = new List<int>();

            // Generate mesh with greedy meshing for each axis
            for (int axis = 0; axis < 3; axis++)
            {
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    GreedyMeshAxis(chunk, axis, direction, vertices, normals, colors, uvs, trianglesOpaque, trianglesTransparent);
                }
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.subMeshCount = 2;

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);

            // Submesh 0: Opaque geometry
            mesh.SetTriangles(trianglesOpaque, 0);
            // Submesh 1: Transparent geometry (water)
            mesh.SetTriangles(trianglesTransparent, 1);

            mesh.RecalculateBounds();

            if (chunk.meshFilter != null)
            {
                chunk.meshFilter.mesh = mesh;
            }

            // Update materials for both submeshes
            if (chunk.meshRenderer != null)
            {
                Material opaqueMat = VoxelMaterialManager.Instance.GetMaterial();
                Material transparentMat = VoxelMaterialManager.Instance.GetTransparentMaterial();
                chunk.meshRenderer.materials = new Material[] { opaqueMat, transparentMat };
            }

            // Update MeshCollider (only opaque geometry)
            if (chunk.meshCollider != null)
            {
                Mesh colliderMesh = new Mesh();
                colliderMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                colliderMesh.SetVertices(vertices);
                colliderMesh.SetTriangles(trianglesOpaque, 0);
                colliderMesh.RecalculateBounds();

                chunk.meshCollider.sharedMesh = null;
                chunk.meshCollider.sharedMesh = colliderMesh;
            }

            return mesh;
        }

        private void GreedyMeshAxis(Chunk chunk, int axis, int direction,
            List<Vector3> vertices, List<Vector3> normals, List<Color> colors, List<Vector2> uvs,
            List<int> trianglesOpaque, List<int> trianglesTransparent)
        {
            int size = Chunk.CHUNK_SIZE;
            int axis1 = (axis + 1) % 3;
            int axis2 = (axis + 2) % 3;

            Vector3Int checkDir = Vector3Int.zero;
            checkDir[axis] = direction;

            bool[,] merged = new bool[size, size];
            VoxelFace[,] faceData = new VoxelFace[size, size];

            // Scan through each slice perpendicular to the axis
            for (int d = -1; d < size; d++)
            {
                // Clear the merged array for this slice
                System.Array.Clear(merged, 0, merged.Length);

                // Build face data for this slice
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        Vector3Int pos = Vector3Int.zero;
                        pos[axis] = d;
                        pos[axis1] = i;
                        pos[axis2] = j;

                        Vector3Int neighborPos = pos + checkDir;

                        VoxelData voxel = GetVoxelSafe(chunk, pos);
                        VoxelData neighbor = GetVoxelSafe(chunk, neighborPos);

                        // Determine if we need to render a face here
                        bool needsFace = false;

                        if (voxel.IsSolid)
                        {
                            // Solid blocks show faces to air or transparent blocks
                            needsFace = !neighbor.IsSolid;
                        }
                        else if (voxel.IsLiquid)
                        {
                            // Water shows faces to air or solid blocks (not to other water)
                            needsFace = neighbor.type == VoxelType.Air || neighbor.IsSolid;
                        }

                        if (needsFace)
                        {
                            faceData[i, j] = new VoxelFace { color = voxel.color, isTransparent = voxel.IsTransparent, exists = true };
                        }
                        else
                        {
                            faceData[i, j] = new VoxelFace { exists = false };
                        }
                    }
                }

                // Greedy meshing on this slice
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        if (merged[i, j] || !faceData[i, j].exists)
                            continue;

                        VoxelFace currentFace = faceData[i, j];

                        // Determine width (along axis1)
                        int width = 1;
                        while (i + width < size &&
                               !merged[i + width, j] &&
                               faceData[i + width, j].exists &&
                               FacesMatch(currentFace, faceData[i + width, j]))
                        {
                            width++;
                        }

                        // Determine height (along axis2)
                        int height = 1;
                        bool canExtendHeight = true;
                        while (j + height < size && canExtendHeight)
                        {
                            for (int w = 0; w < width; w++)
                            {
                                if (merged[i + w, j + height] ||
                                    !faceData[i + w, j + height].exists ||
                                    !FacesMatch(currentFace, faceData[i + w, j + height]))
                                {
                                    canExtendHeight = false;
                                    break;
                                }
                            }
                            if (canExtendHeight)
                                height++;
                        }

                        // Mark all merged faces
                        for (int w = 0; w < width; w++)
                        {
                            for (int h = 0; h < height; h++)
                            {
                                merged[i + w, j + h] = true;
                            }
                        }

                        // Create the merged quad
                        Vector3Int posMin = Vector3Int.zero;
                        posMin[axis] = d + (direction > 0 ? 1 : 0);
                        posMin[axis1] = i;
                        posMin[axis2] = j;

                        AddQuad(posMin, axis, axis1, axis2, direction, width, height,
                                currentFace.color, currentFace.isTransparent,
                                vertices, normals, colors, uvs, trianglesOpaque, trianglesTransparent);
                    }
                }
            }
        }

        private bool FacesMatch(VoxelFace a, VoxelFace b)
        {
            return a.color == b.color && a.isTransparent == b.isTransparent;
        }

        private void AddQuad(Vector3Int pos, int axis, int axis1, int axis2, int direction,
                            int width, int height, Color color, bool isTransparent,
                            List<Vector3> vertices, List<Vector3> normals, List<Color> colors, List<Vector2> uvs,
                            List<int> trianglesOpaque, List<int> trianglesTransparent)
        {
            Vector3 p0 = new Vector3(pos.x, pos.y, pos.z);
            Vector3 offset1 = Vector3.zero;
            Vector3 offset2 = Vector3.zero;
            offset1[axis1] = width;
            offset2[axis2] = height;

            Vector3 normal = Vector3.zero;
            normal[axis] = direction;

            int vertexOffset = vertices.Count;

            // Add 4 vertices for the quad
            if (direction > 0)
            {
                vertices.Add(p0);
                vertices.Add(p0 + offset1);
                vertices.Add(p0 + offset1 + offset2);
                vertices.Add(p0 + offset2);
            }
            else
            {
                vertices.Add(p0);
                vertices.Add(p0 + offset2);
                vertices.Add(p0 + offset1 + offset2);
                vertices.Add(p0 + offset1);
            }

            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
            }

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(width, 0));
            uvs.Add(new Vector2(width, height));
            uvs.Add(new Vector2(0, height));

            // Add triangles to appropriate list
            List<int> triangles = isTransparent ? trianglesTransparent : trianglesOpaque;
            triangles.Add(vertexOffset + 0);
            triangles.Add(vertexOffset + 1);
            triangles.Add(vertexOffset + 2);
            triangles.Add(vertexOffset + 0);
            triangles.Add(vertexOffset + 2);
            triangles.Add(vertexOffset + 3);
        }

        private VoxelData GetVoxelSafe(Chunk chunk, Vector3Int localPos)
        {
            if (localPos.x >= 0 && localPos.x < Chunk.CHUNK_SIZE &&
                localPos.y >= 0 && localPos.y < Chunk.CHUNK_SIZE &&
                localPos.z >= 0 && localPos.z < Chunk.CHUNK_SIZE)
            {
                return chunk.GetVoxel(localPos.x, localPos.y, localPos.z);
            }

            // Check neighbor chunk
            Vector3Int worldPos = new Vector3Int(
                chunk.chunkPosition.x * Chunk.CHUNK_SIZE + localPos.x,
                chunk.chunkPosition.y * Chunk.CHUNK_SIZE + localPos.y,
                chunk.chunkPosition.z * Chunk.CHUNK_SIZE + localPos.z
            );

            Vector3Int neighborChunkPos = new Vector3Int(
                Mathf.FloorToInt((float)worldPos.x / Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)worldPos.y / Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)worldPos.z / Chunk.CHUNK_SIZE)
            );

            Chunk neighborChunk = chunkManager.GetChunk(neighborChunkPos);

            if (neighborChunk != null)
            {
                Vector3Int neighborChunkWorldPos = new Vector3Int(
                    neighborChunkPos.x * Chunk.CHUNK_SIZE,
                    neighborChunkPos.y * Chunk.CHUNK_SIZE,
                    neighborChunkPos.z * Chunk.CHUNK_SIZE
                );

                Vector3Int neighborLocalPos = worldPos - neighborChunkWorldPos;
                return neighborChunk.GetVoxel(neighborLocalPos.x, neighborLocalPos.y, neighborLocalPos.z);
            }

            return VoxelData.Air;
        }

        private struct VoxelFace
        {
            public Color color;
            public bool isTransparent;
            public bool exists;
        }
    }
}
