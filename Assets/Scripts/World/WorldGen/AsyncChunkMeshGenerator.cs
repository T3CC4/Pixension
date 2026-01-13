using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using UnityEngine;
using System.Collections.Generic;

namespace Pixension.Voxels
{
    /// <summary>
    /// Async chunk mesh generator using Unity Job System for parallel processing
    /// </summary>
    public class AsyncChunkMeshGenerator
    {
        private ChunkManager chunkManager;
        private Dictionary<Vector3Int, MeshGenerationJob> activeJobs = new Dictionary<Vector3Int, MeshGenerationJob>();
        private List<Vector3Int> completedJobs = new List<Vector3Int>();

        public AsyncChunkMeshGenerator(ChunkManager manager)
        {
            chunkManager = manager;
        }

        /// <summary>
        /// Starts generating a mesh for a chunk asynchronously
        /// </summary>
        public void GenerateMeshAsync(Chunk chunk)
        {
            if (activeJobs.ContainsKey(chunk.chunkPosition))
            {
                // Job already running for this chunk
                return;
            }

            // Copy voxel data to native array for job system
            NativeArray<VoxelDataStruct> voxelData = new NativeArray<VoxelDataStruct>(
                Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE * Chunk.CHUNK_SIZE,
                Allocator.TempJob
            );

            int index = 0;
            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        VoxelData voxel = chunk.voxels[x, y, z];
                        voxelData[index] = new VoxelDataStruct
                        {
                            type = (int)voxel.type,
                            r = voxel.color.r,
                            g = voxel.color.g,
                            b = voxel.color.b,
                            a = voxel.color.a
                        };
                        index++;
                    }
                }
            }

            // Get neighbor chunk data for proper face culling
            NativeArray<VoxelDataStruct> neighborData = GetNeighborData(chunk);

            // Create output arrays
            NativeList<Vector3> vertices = new NativeList<Vector3>(Allocator.TempJob);
            NativeList<Vector3> normals = new NativeList<Vector3>(Allocator.TempJob);
            NativeList<Color> colors = new NativeList<Color>(Allocator.TempJob);
            NativeList<Vector2> uvs = new NativeList<Vector2>(Allocator.TempJob);
            NativeList<int> trianglesOpaque = new NativeList<int>(Allocator.TempJob);
            NativeList<int> trianglesTransparent = new NativeList<int>(Allocator.TempJob);

            // Create and schedule the job
            GreedyMeshJob job = new GreedyMeshJob
            {
                voxelData = voxelData,
                neighborData = neighborData,
                chunkSize = Chunk.CHUNK_SIZE,
                vertices = vertices,
                normals = normals,
                colors = colors,
                uvs = uvs,
                trianglesOpaque = trianglesOpaque,
                trianglesTransparent = trianglesTransparent
            };

            JobHandle handle = job.Schedule();

            // Store job info
            activeJobs[chunk.chunkPosition] = new MeshGenerationJob
            {
                chunk = chunk,
                jobHandle = handle,
                voxelData = voxelData,
                neighborData = neighborData,
                vertices = vertices,
                normals = normals,
                colors = colors,
                uvs = uvs,
                trianglesOpaque = trianglesOpaque,
                trianglesTransparent = trianglesTransparent
            };
        }

        /// <summary>
        /// Updates all active jobs and applies completed meshes
        /// </summary>
        public void Update()
        {
            completedJobs.Clear();

            foreach (var kvp in activeJobs)
            {
                MeshGenerationJob job = kvp.Value;

                if (job.jobHandle.IsCompleted)
                {
                    job.jobHandle.Complete();
                    ApplyGeneratedMesh(job);
                    completedJobs.Add(kvp.Key);
                }
            }

            // Remove completed jobs
            foreach (Vector3Int pos in completedJobs)
            {
                CleanupJob(activeJobs[pos]);
                activeJobs.Remove(pos);
            }
        }

        /// <summary>
        /// Cancels all active jobs (call before shutdown)
        /// </summary>
        public void CancelAllJobs()
        {
            foreach (var job in activeJobs.Values)
            {
                job.jobHandle.Complete();
                CleanupJob(job);
            }
            activeJobs.Clear();
        }

        private void ApplyGeneratedMesh(MeshGenerationJob job)
        {
            Chunk chunk = job.chunk;
            if (chunk == null || chunk.meshFilter == null)
                return;

            // Check if chunk became dirty during mesh generation (e.g., neighbor chunk was created)
            // If so, the generated mesh has stale neighbor data for face culling
            // Skip applying this mesh and queue for regeneration with correct neighbor data
            if (chunk.isDirty)
            {
                chunkManager.MarkChunkDirty(chunk.chunkPosition);
                return;
            }

            // Get mesh from pool
            Mesh mesh = Utilities.MeshPool.Instance.GetMesh();
            mesh.subMeshCount = 2;

            // Set mesh data
            mesh.SetVertices(job.vertices.AsArray().ToArray());
            mesh.SetNormals(job.normals.AsArray().ToArray());
            mesh.SetColors(job.colors.AsArray().ToArray());
            mesh.SetUVs(0, job.uvs.AsArray().ToArray());

            // Set triangles for both submeshes
            mesh.SetTriangles(job.trianglesOpaque.AsArray().ToArray(), 0);
            mesh.SetTriangles(job.trianglesTransparent.AsArray().ToArray(), 1);

            mesh.RecalculateBounds();

            // Return old mesh to pool
            if (chunk.meshFilter.sharedMesh != null)
            {
                Utilities.MeshPool.Instance.ReturnMesh(chunk.meshFilter.sharedMesh);
            }

            chunk.meshFilter.mesh = mesh;

            // Update materials
            if (chunk.meshRenderer != null)
            {
                Material opaqueMat = VoxelMaterialManager.Instance.GetMaterial();
                Material transparentMat = VoxelMaterialManager.Instance.GetTransparentMaterial();
                chunk.meshRenderer.materials = new Material[] { opaqueMat, transparentMat };
            }

            // Update collider (opaque only)
            if (chunk.meshCollider != null)
            {
                Mesh colliderMesh = Utilities.MeshPool.Instance.GetMesh();
                colliderMesh.SetVertices(job.vertices.AsArray().ToArray());
                colliderMesh.SetTriangles(job.trianglesOpaque.AsArray().ToArray(), 0);
                colliderMesh.RecalculateBounds();

                if (chunk.meshCollider.sharedMesh != null)
                {
                    Utilities.MeshPool.Instance.ReturnMesh(chunk.meshCollider.sharedMesh);
                }

                chunk.meshCollider.sharedMesh = colliderMesh;
            }
        }

        private void CleanupJob(MeshGenerationJob job)
        {
            job.voxelData.Dispose();
            job.neighborData.Dispose();
            job.vertices.Dispose();
            job.normals.Dispose();
            job.colors.Dispose();
            job.uvs.Dispose();
            job.trianglesOpaque.Dispose();
            job.trianglesTransparent.Dispose();
        }

        /// <summary>
        /// Gets neighbor voxel data for proper face culling at chunk boundaries
        /// </summary>
        private NativeArray<VoxelDataStruct> GetNeighborData(Chunk chunk)
        {
            // We need to check 6 neighboring chunks (one per face direction)
            // For each chunk boundary face, we need a 16x16 slice of data from the neighbor
            // Format: [face_index][y * 16 + z] for X-facing, [face_index][x * 16 + z] for Y-facing, etc.

            int chunkSize = Chunk.CHUNK_SIZE;
            // 6 faces * 16x16 = 1536 voxels
            NativeArray<VoxelDataStruct> neighborData = new NativeArray<VoxelDataStruct>(6 * chunkSize * chunkSize, Allocator.TempJob);

            // Initialize all to air
            for (int i = 0; i < neighborData.Length; i++)
            {
                neighborData[i] = new VoxelDataStruct { type = 0, r = 0, g = 0, b = 0, a = 0 };
            }

            // Check each of 6 neighbor chunks
            Vector3Int[] neighborOffsets = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),   // Right (+X)
                new Vector3Int(-1, 0, 0),  // Left (-X)
                new Vector3Int(0, 1, 0),   // Top (+Y)
                new Vector3Int(0, -1, 0),  // Bottom (-Y)
                new Vector3Int(0, 0, 1),   // Front (+Z)
                new Vector3Int(0, 0, -1)   // Back (-Z)
            };

            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                Vector3Int neighborChunkPos = chunk.chunkPosition + neighborOffsets[faceIndex];
                Chunk neighborChunk = chunkManager.GetChunk(neighborChunkPos);

                if (neighborChunk == null)
                    continue;

                int baseIndex = faceIndex * chunkSize * chunkSize;

                // Extract the boundary slice from neighbor chunk
                for (int i = 0; i < chunkSize; i++)
                {
                    for (int j = 0; j < chunkSize; j++)
                    {
                        int x, y, z;

                        // Determine which slice to extract based on face direction
                        switch (faceIndex)
                        {
                            case 0: // Right (+X) - get left slice (x=0) of right neighbor
                                x = 0; y = i; z = j;
                                break;
                            case 1: // Left (-X) - get right slice (x=15) of left neighbor
                                x = chunkSize - 1; y = i; z = j;
                                break;
                            case 2: // Top (+Y) - get bottom slice (y=0) of top neighbor
                                x = i; y = 0; z = j;
                                break;
                            case 3: // Bottom (-Y) - get top slice (y=15) of bottom neighbor
                                x = i; y = chunkSize - 1; z = j;
                                break;
                            case 4: // Front (+Z) - get back slice (z=0) of front neighbor
                                x = i; y = j; z = 0;
                                break;
                            case 5: // Back (-Z) - get front slice (z=15) of back neighbor
                                x = i; y = j; z = chunkSize - 1;
                                break;
                            default:
                                x = y = z = 0;
                                break;
                        }

                        VoxelData voxel = neighborChunk.GetVoxel(x, y, z);
                        neighborData[baseIndex + i * chunkSize + j] = new VoxelDataStruct
                        {
                            type = (int)voxel.type,
                            r = voxel.color.r,
                            g = voxel.color.g,
                            b = voxel.color.b,
                            a = voxel.color.a
                        };
                    }
                }
            }

            return neighborData;
        }

        public int GetActiveJobCount()
        {
            return activeJobs.Count;
        }

        private struct MeshGenerationJob
        {
            public Chunk chunk;
            public JobHandle jobHandle;
            public NativeArray<VoxelDataStruct> voxelData;
            public NativeArray<VoxelDataStruct> neighborData;
            public NativeList<Vector3> vertices;
            public NativeList<Vector3> normals;
            public NativeList<Color> colors;
            public NativeList<Vector2> uvs;
            public NativeList<int> trianglesOpaque;
            public NativeList<int> trianglesTransparent;
        }
    }

    /// <summary>
    /// Struct version of VoxelData for use in Job System
    /// </summary>
    public struct VoxelDataStruct
    {
        public int type; // 0 = Air, 1 = Solid, 2 = Liquid
        public float r, g, b, a;

        public bool IsSolid => type == 1;
        public bool IsLiquid => type == 2;
        public bool IsTransparent => type == 2;
    }

    /// <summary>
    /// Job for generating greedy meshed chunk geometry
    /// </summary>
    [BurstCompile]
    public struct GreedyMeshJob : IJob
    {
        [ReadOnly] public NativeArray<VoxelDataStruct> voxelData;
        [ReadOnly] public NativeArray<VoxelDataStruct> neighborData;
        [ReadOnly] public int chunkSize;

        public NativeList<Vector3> vertices;
        public NativeList<Vector3> normals;
        public NativeList<Color> colors;
        public NativeList<Vector2> uvs;
        public NativeList<int> trianglesOpaque;
        public NativeList<int> trianglesTransparent;

        public void Execute()
        {
            // Simplified greedy meshing for burst compilation
            // Full greedy meshing implementation would go here
            // For now, use a simple face-based approach

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        int index = x + y * chunkSize + z * chunkSize * chunkSize;
                        VoxelDataStruct voxel = voxelData[index];

                        if (voxel.type == 0) // Air
                            continue;

                        // Check each face
                        CheckAndAddFace(x, y, z, voxel, 1, 0, 0); // Right
                        CheckAndAddFace(x, y, z, voxel, -1, 0, 0); // Left
                        CheckAndAddFace(x, y, z, voxel, 0, 1, 0); // Top
                        CheckAndAddFace(x, y, z, voxel, 0, -1, 0); // Bottom
                        CheckAndAddFace(x, y, z, voxel, 0, 0, 1); // Front
                        CheckAndAddFace(x, y, z, voxel, 0, 0, -1); // Back
                    }
                }
            }
        }

        private void CheckAndAddFace(int x, int y, int z, VoxelDataStruct voxel, int dx, int dy, int dz)
        {
            int nx = x + dx;
            int ny = y + dy;
            int nz = z + dz;

            VoxelDataStruct neighbor;

            // Check if neighbor is outside chunk bounds
            if (nx < 0 || nx >= chunkSize || ny < 0 || ny >= chunkSize || nz < 0 || nz >= chunkSize)
            {
                // Get neighbor from neighboring chunk data
                neighbor = GetNeighborVoxel(x, y, z, dx, dy, dz);
            }
            else
            {
                // Get neighbor from within this chunk
                int neighborIndex = nx + ny * chunkSize + nz * chunkSize * chunkSize;
                neighbor = voxelData[neighborIndex];
            }

            // Determine if we need to render a face
            bool needsFace = false;
            if (voxel.IsSolid)
            {
                needsFace = !neighbor.IsSolid;
            }
            else if (voxel.IsLiquid)
            {
                needsFace = neighbor.type == 0 || neighbor.IsSolid;
            }

            if (needsFace)
            {
                AddFace(x, y, z, voxel, dx, dy, dz);
            }
        }

        private VoxelDataStruct GetNeighborVoxel(int x, int y, int z, int dx, int dy, int dz)
        {
            // Determine which face we're checking
            int faceIndex = -1;
            int i, j; // Coordinates within the neighbor face slice

            if (dx > 0) // Right face (+X)
            {
                faceIndex = 0;
                i = y;
                j = z;
            }
            else if (dx < 0) // Left face (-X)
            {
                faceIndex = 1;
                i = y;
                j = z;
            }
            else if (dy > 0) // Top face (+Y)
            {
                faceIndex = 2;
                i = x;
                j = z;
            }
            else if (dy < 0) // Bottom face (-Y)
            {
                faceIndex = 3;
                i = x;
                j = z;
            }
            else if (dz > 0) // Front face (+Z)
            {
                faceIndex = 4;
                i = x;
                j = y;
            }
            else if (dz < 0) // Back face (-Z)
            {
                faceIndex = 5;
                i = x;
                j = y;
            }
            else
            {
                // Invalid direction, return air
                return new VoxelDataStruct { type = 0, r = 0, g = 0, b = 0, a = 0 };
            }

            // Check if we have neighbor data
            if (neighborData.Length == 0)
            {
                // No neighbor data available, assume air (show face)
                return new VoxelDataStruct { type = 0, r = 0, g = 0, b = 0, a = 0 };
            }

            // Get voxel from neighbor data
            int neighborIndex = faceIndex * chunkSize * chunkSize + i * chunkSize + j;
            if (neighborIndex >= 0 && neighborIndex < neighborData.Length)
            {
                return neighborData[neighborIndex];
            }

            // Fallback to air
            return new VoxelDataStruct { type = 0, r = 0, g = 0, b = 0, a = 0 };
        }

        private void AddFace(int x, int y, int z, VoxelDataStruct voxel, int dx, int dy, int dz)
        {
            int vertexOffset = vertices.Length;

            Vector3 normal = new Vector3(dx, dy, dz);
            Color color = new Color(voxel.r, voxel.g, voxel.b, voxel.a);

            // Add 4 vertices for the quad
            Vector3 basePos = new Vector3(x, y, z);

            if (dx != 0) // Right/Left faces
            {
                float faceX = x + (dx > 0 ? 1 : 0);
                vertices.Add(new Vector3(faceX, y, z));
                vertices.Add(new Vector3(faceX, y + 1, z));
                vertices.Add(new Vector3(faceX, y + 1, z + 1));
                vertices.Add(new Vector3(faceX, y, z + 1));
            }
            else if (dy != 0) // Top/Bottom faces
            {
                float faceY = y + (dy > 0 ? 1 : 0);
                vertices.Add(new Vector3(x, faceY, z));
                vertices.Add(new Vector3(x, faceY, z + 1));
                vertices.Add(new Vector3(x + 1, faceY, z + 1));
                vertices.Add(new Vector3(x + 1, faceY, z));
            }
            else // Front/Back faces
            {
                float faceZ = z + (dz > 0 ? 1 : 0);
                vertices.Add(new Vector3(x, y, faceZ));
                vertices.Add(new Vector3(x + 1, y, faceZ));
                vertices.Add(new Vector3(x + 1, y + 1, faceZ));
                vertices.Add(new Vector3(x, y + 1, faceZ));
            }

            // Add normals, colors, UVs
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
                uvs.Add(new Vector2(i % 2, i / 2));
            }

            // Add triangles
            NativeList<int> trianglesList = voxel.IsTransparent ? trianglesTransparent : trianglesOpaque;
            trianglesList.Add(vertexOffset + 0);
            trianglesList.Add(vertexOffset + 1);
            trianglesList.Add(vertexOffset + 2);
            trianglesList.Add(vertexOffset + 0);
            trianglesList.Add(vertexOffset + 2);
            trianglesList.Add(vertexOffset + 3);
        }
    }
}
