using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Dimensions
{
    public class Dimension
    {
        public string dimensionID;
        public WorldGen.WorldGenerator generator;
        public Dictionary<Vector3Int, Voxels.Chunk> chunks;
        public Transform dimensionRoot;
        public Color skyColor;

        public Dimension(string id, WorldGen.WorldGenerator worldGenerator)
        {
            dimensionID = id;
            generator = worldGenerator;
            chunks = new Dictionary<Vector3Int, Voxels.Chunk>();
            skyColor = generator.GetSkyColor();

            GameObject rootObject = new GameObject($"Dimension_{id}");
            dimensionRoot = rootObject.transform;
            Object.DontDestroyOnLoad(rootObject);
        }

        public Voxels.Chunk GetOrCreateChunk(Vector3Int chunkPos)
        {
            if (chunks.TryGetValue(chunkPos, out Voxels.Chunk existingChunk))
            {
                return existingChunk;
            }

            Voxels.Chunk chunk = new Voxels.Chunk(chunkPos);
            chunk.gameObject.transform.SetParent(dimensionRoot);

            generator.GenerateChunk(chunk);

            chunks[chunkPos] = chunk;

            // Mark neighboring chunks as dirty so they regenerate their meshes with correct face culling
            MarkNeighborsDirty(chunkPos);

            return chunk;
        }

        private void MarkNeighborsDirty(Vector3Int chunkPos)
        {
            // Check all 6 neighboring chunks
            Vector3Int[] neighborOffsets = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),   // Right
                new Vector3Int(-1, 0, 0),  // Left
                new Vector3Int(0, 1, 0),   // Up
                new Vector3Int(0, -1, 0),  // Down
                new Vector3Int(0, 0, 1),   // Forward
                new Vector3Int(0, 0, -1)   // Back
            };

            foreach (Vector3Int offset in neighborOffsets)
            {
                Vector3Int neighborPos = chunkPos + offset;
                if (chunks.TryGetValue(neighborPos, out Voxels.Chunk neighbor))
                {
                    neighbor.SetDirty();
                    // Notify ChunkManager to rebuild this chunk
                    Voxels.ChunkManager.Instance?.MarkChunkDirty(neighborPos);
                }
            }
        }

        public void UnloadChunk(Vector3Int chunkPos)
        {
            if (!chunks.TryGetValue(chunkPos, out Voxels.Chunk chunk))
            {
                return;
            }

            if (chunk.gameObject != null)
            {
                Object.Destroy(chunk.gameObject);
            }

            foreach (GameObject entity in chunk.entities)
            {
                if (entity != null)
                {
                    Object.Destroy(entity);
                }
            }

            chunks.Remove(chunkPos);
        }

        public void UnloadAllChunks()
        {
            List<Vector3Int> chunkPositions = new List<Vector3Int>(chunks.Keys);

            foreach (Vector3Int chunkPos in chunkPositions)
            {
                UnloadChunk(chunkPos);
            }

            chunks.Clear();
        }

        public void SetActive(bool active)
        {
            if (dimensionRoot != null && dimensionRoot.gameObject != null)
            {
                dimensionRoot.gameObject.SetActive(active);
            }
        }

        public Voxels.Chunk GetChunk(Vector3Int chunkPos)
        {
            if (chunks.TryGetValue(chunkPos, out Voxels.Chunk chunk))
            {
                return chunk;
            }
            return null;
        }
    }
}