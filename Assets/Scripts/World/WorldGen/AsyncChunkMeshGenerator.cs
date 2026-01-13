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

        private NativeArray<VoxelDataStruct> GetNeighborData(Chunk chunk)
        {
            // For now, return empty array - implement neighbor checking if needed
            // This would require checking adjacent chunks for proper face culling
            return new NativeArray<VoxelDataStruct>(0, Allocator.TempJob);
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

            // Check bounds
            if (nx < 0 || nx >= chunkSize || ny < 0 || ny >= chunkSize || nz < 0 || nz >= chunkSize)
            {
                AddFace(x, y, z, voxel, dx, dy, dz);
                return;
            }

            // Check neighbor
            int neighborIndex = nx + ny * chunkSize + nz * chunkSize * chunkSize;
            VoxelDataStruct neighbor = voxelData[neighborIndex];

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
