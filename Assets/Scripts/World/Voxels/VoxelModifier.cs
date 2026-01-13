using UnityEngine;

namespace Pixension.Voxels
{
    public class VoxelModifier
    {
        private ChunkManager chunkManager;
        private Entities.BlockEntityManager entityManager;
        private Entities.BlockEntityRegistry entityRegistry;

        public VoxelModifier(ChunkManager manager, Entities.BlockEntityRegistry registry)
        {
            chunkManager = manager;
            entityManager = Entities.BlockEntityManager.Instance;
            entityRegistry = registry;
        }

        public bool PlaceBlock(Vector3 worldPos, VoxelData voxelData)
        {
            Vector3Int blockPos = new Vector3Int(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.y),
                Mathf.FloorToInt(worldPos.z)
            );

            Vector3Int chunkPos = chunkManager.WorldToChunkPosition(blockPos);
            Chunk chunk = chunkManager.GetChunk(chunkPos);

            if (chunk == null)
            {
                return false;
            }

            Vector3Int localPos = chunk.WorldToLocal(blockPos);
            chunk.SetVoxel(localPos.x, localPos.y, localPos.z, voxelData);
            chunk.SetDirty();
            chunkManager.MarkChunkDirty(chunkPos);

            MarkNeighborChunksIfOnBoundary(chunk, localPos);

            return true;
        }

        public bool RemoveBlock(Vector3 worldPos)
        {
            return PlaceBlock(worldPos, VoxelData.Air);
        }

        public GameObject PlaceBlockEntity(string entityID, Vector3 worldPos, Utilities.Direction facing)
        {
            Vector3Int blockPos = new Vector3Int(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.y),
                Mathf.FloorToInt(worldPos.z)
            );

            GameObject entityObject = Entities.BlockEntityLoader.Instance.InstantiateEntity(
                entityID,
                blockPos,
                facing
            );

            return entityObject;
        }

        public bool RemoveBlockEntity(Vector3 worldPos)
        {
            Vector3Int blockPos = new Vector3Int(
                Mathf.FloorToInt(worldPos.x),
                Mathf.FloorToInt(worldPos.y),
                Mathf.FloorToInt(worldPos.z)
            );

            Entities.BlockEntity entity = entityManager.GetEntityAtPosition(blockPos);

            if (entity != null)
            {
                entityManager.RemoveEntity(entity);
                return true;
            }

            return false;
        }

        public bool RaycastVoxel(Ray ray, float maxDistance, out Vector3Int hitPos, out Vector3Int hitNormal)
        {
            hitPos = Vector3Int.zero;
            hitNormal = Vector3Int.zero;

            Vector3 currentPos = ray.origin;
            Vector3 direction = ray.direction.normalized;

            int stepX = direction.x > 0 ? 1 : -1;
            int stepY = direction.y > 0 ? 1 : -1;
            int stepZ = direction.z > 0 ? 1 : -1;

            float tMaxX = GetInitialTMax(currentPos.x, direction.x, stepX);
            float tMaxY = GetInitialTMax(currentPos.y, direction.y, stepY);
            float tMaxZ = GetInitialTMax(currentPos.z, direction.z, stepZ);

            float tDeltaX = Mathf.Abs(1f / direction.x);
            float tDeltaY = Mathf.Abs(1f / direction.y);
            float tDeltaZ = Mathf.Abs(1f / direction.z);

            int maxSteps = Mathf.CeilToInt(maxDistance * 2f);
            Vector3Int lastNormal = Vector3Int.zero;

            for (int i = 0; i < maxSteps; i++)
            {
                Vector3Int voxelPos = new Vector3Int(
                    Mathf.FloorToInt(currentPos.x),
                    Mathf.FloorToInt(currentPos.y),
                    Mathf.FloorToInt(currentPos.z)
                );

                Vector3Int chunkPos = chunkManager.WorldToChunkPosition(voxelPos);
                Chunk chunk = chunkManager.GetChunk(chunkPos);

                if (chunk != null)
                {
                    Vector3Int localPos = chunk.WorldToLocal(voxelPos);
                    VoxelData voxel = chunk.GetVoxel(localPos.x, localPos.y, localPos.z);

                    if (voxel.IsSolid)
                    {
                        hitPos = voxelPos;
                        hitNormal = lastNormal;
                        return true;
                    }
                }

                if (tMaxX < tMaxY)
                {
                    if (tMaxX < tMaxZ)
                    {
                        if (tMaxX > maxDistance) break;
                        currentPos.x += stepX;
                        tMaxX += tDeltaX;
                        lastNormal = new Vector3Int(-stepX, 0, 0);
                    }
                    else
                    {
                        if (tMaxZ > maxDistance) break;
                        currentPos.z += stepZ;
                        tMaxZ += tDeltaZ;
                        lastNormal = new Vector3Int(0, 0, -stepZ);
                    }
                }
                else
                {
                    if (tMaxY < tMaxZ)
                    {
                        if (tMaxY > maxDistance) break;
                        currentPos.y += stepY;
                        tMaxY += tDeltaY;
                        lastNormal = new Vector3Int(0, -stepY, 0);
                    }
                    else
                    {
                        if (tMaxZ > maxDistance) break;
                        currentPos.z += stepZ;
                        tMaxZ += tDeltaZ;
                        lastNormal = new Vector3Int(0, 0, -stepZ);
                    }
                }
            }

            return false;
        }

        private float GetInitialTMax(float position, float direction, int step)
        {
            if (Mathf.Approximately(direction, 0f))
            {
                return float.MaxValue;
            }

            float boundary = step > 0 ? Mathf.Ceil(position) : Mathf.Floor(position);
            return (boundary - position) / direction;
        }

        private void MarkNeighborChunksIfOnBoundary(Chunk chunk, Vector3Int localPos)
        {
            if (localPos.x == 0)
            {
                Vector3Int neighborChunk = chunk.chunkPosition + new Vector3Int(-1, 0, 0);
                chunkManager.MarkChunkDirty(neighborChunk);
            }
            else if (localPos.x == Chunk.CHUNK_SIZE - 1)
            {
                Vector3Int neighborChunk = chunk.chunkPosition + new Vector3Int(1, 0, 0);
                chunkManager.MarkChunkDirty(neighborChunk);
            }

            if (localPos.y == 0)
            {
                Vector3Int neighborChunk = chunk.chunkPosition + new Vector3Int(0, -1, 0);
                chunkManager.MarkChunkDirty(neighborChunk);
            }
            else if (localPos.y == Chunk.CHUNK_SIZE - 1)
            {
                Vector3Int neighborChunk = chunk.chunkPosition + new Vector3Int(0, 1, 0);
                chunkManager.MarkChunkDirty(neighborChunk);
            }

            if (localPos.z == 0)
            {
                Vector3Int neighborChunk = chunk.chunkPosition + new Vector3Int(0, 0, -1);
                chunkManager.MarkChunkDirty(neighborChunk);
            }
            else if (localPos.z == Chunk.CHUNK_SIZE - 1)
            {
                Vector3Int neighborChunk = chunk.chunkPosition + new Vector3Int(0, 0, 1);
                chunkManager.MarkChunkDirty(neighborChunk);
            }
        }
    }
}