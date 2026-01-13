using UnityEngine;

namespace Pixension.Voxels
{
    public class ChunkMeshGenerator
    {
        private Utilities.MeshBuilder builder;
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

        public ChunkMeshGenerator(ChunkManager manager)
        {
            builder = new Utilities.MeshBuilder();
            chunkManager = manager;
        }

        public Mesh GenerateMesh(Chunk chunk)
        {
            builder.Clear();

            for (int x = 0; x < Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Chunk.CHUNK_SIZE; z++)
                    {
                        VoxelData voxel = chunk.GetVoxel(x, y, z);
                        if (!voxel.IsSolid)
                            continue;

                        bool[] visibleFaces = new bool[6];

                        for (int i = 0; i < 6; i++)
                        {
                            Vector3Int neighborPos = new Vector3Int(x, y, z) + directions[i];
                            visibleFaces[i] = IsFaceVisible(chunk, neighborPos.x, neighborPos.y, neighborPos.z);
                        }

                        Vector3 position = new Vector3(x, y, z);
                        builder.AddCube(position, voxel.color, visibleFaces);
                    }
                }
            }

            Mesh mesh = builder.GenerateMesh();

            if (chunk.meshFilter != null)
            {
                chunk.meshFilter.mesh = mesh;
            }

            if (chunk.meshRenderer != null && chunk.meshRenderer.material == null)
            {
                chunk.meshRenderer.material = VoxelMaterialManager.Instance.GetMaterial();
            }

            return mesh;
        }

        private bool IsFaceVisible(Chunk chunk, int x, int y, int z)
        {
            if (chunk.IsInBounds(x, y, z))
            {
                return !chunk.GetVoxel(x, y, z).IsSolid;
            }

            Vector3Int worldPos = new Vector3Int(
                chunk.chunkPosition.x * Chunk.CHUNK_SIZE + x,
                chunk.chunkPosition.y * Chunk.CHUNK_SIZE + y,
                chunk.chunkPosition.z * Chunk.CHUNK_SIZE + z
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

                Vector3Int localPos = worldPos - neighborChunkWorldPos;
                return !neighborChunk.GetVoxel(localPos.x, localPos.y, localPos.z).IsSolid;
            }

            return true;
        }
    }
}