using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Systems
{
    public class WaterSimulation : MonoBehaviour, ITickable
    {
        private static WaterSimulation instance;
        public static WaterSimulation Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("WaterSimulation");
                    instance = go.AddComponent<WaterSimulation>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Water Physics Settings")]
        public bool enableWaterFlow = true;
        public int maxWaterUpdatesPerTick = 100;
        public int flowCheckRadius = 1; // How far to check for flow opportunities

        private Queue<Vector3Int> waterUpdateQueue = new Queue<Vector3Int>();
        private HashSet<Vector3Int> queuedPositions = new HashSet<Vector3Int>();
        private Voxels.ChunkManager chunkManager;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            TickManager.Instance.RegisterTickable(this);
        }

        private void Start()
        {
            chunkManager = Voxels.ChunkManager.Instance;
        }

        private void OnDestroy()
        {
            if (TickManager.Instance != null)
            {
                TickManager.Instance.UnregisterTickable(this);
            }
        }

        public void OnTick()
        {
            if (!enableWaterFlow || chunkManager == null)
                return;

            ProcessWaterFlow();
        }

        public void QueueWaterUpdate(Vector3Int worldPos)
        {
            if (!queuedPositions.Contains(worldPos))
            {
                waterUpdateQueue.Enqueue(worldPos);
                queuedPositions.Add(worldPos);
            }
        }

        private void ProcessWaterFlow()
        {
            int updatesThisTick = 0;

            while (waterUpdateQueue.Count > 0 && updatesThisTick < maxWaterUpdatesPerTick)
            {
                Vector3Int pos = waterUpdateQueue.Dequeue();
                queuedPositions.Remove(pos);

                UpdateWaterAt(pos);
                updatesThisTick++;
            }
        }

        private void UpdateWaterAt(Vector3Int worldPos)
        {
            Voxels.VoxelData voxel = GetVoxelAt(worldPos);

            if (!voxel.IsLiquid)
                return;

            // Water physics:
            // 1. Flow down if possible
            // 2. Flow horizontally if can't flow down
            // 3. Try to spread evenly

            Vector3Int below = worldPos + Vector3Int.down;
            Voxels.VoxelData belowVoxel = GetVoxelAt(below);

            if (belowVoxel.type == Voxels.VoxelType.Air)
            {
                // Flow down
                FlowWater(worldPos, below);
                return;
            }

            // Check horizontal flow
            List<Vector3Int> flowableNeighbors = new List<Vector3Int>();

            // Check all 4 horizontal directions
            Vector3Int[] horizontalDirs = new Vector3Int[]
            {
                Vector3Int.forward,
                Vector3Int.back,
                Vector3Int.left,
                Vector3Int.right
            };

            foreach (Vector3Int dir in horizontalDirs)
            {
                Vector3Int neighborPos = worldPos + dir;
                Voxels.VoxelData neighborVoxel = GetVoxelAt(neighborPos);

                // Can flow into air
                if (neighborVoxel.type == Voxels.VoxelType.Air)
                {
                    // Check if there's ground below neighbor (prefer flowing into supported positions)
                    Vector3Int belowNeighbor = neighborPos + Vector3Int.down;
                    Voxels.VoxelData belowNeighborVoxel = GetVoxelAt(belowNeighbor);

                    if (belowNeighborVoxel.IsSolid || belowNeighborVoxel.IsLiquid)
                    {
                        flowableNeighbors.Add(neighborPos);
                    }
                    else
                    {
                        // No support below - still add but with lower priority
                        flowableNeighbors.Add(neighborPos);
                    }
                }
            }

            // Flow to a random flowable neighbor
            if (flowableNeighbors.Count > 0)
            {
                int randomIndex = Random.Range(0, flowableNeighbors.Count);
                Vector3Int flowTarget = flowableNeighbors[randomIndex];

                // Duplicate water (simplified fluid simulation)
                SetVoxelAt(flowTarget, Voxels.VoxelData.Water);

                // Queue neighbors for update
                foreach (Vector3Int dir in horizontalDirs)
                {
                    QueueWaterUpdate(flowTarget + dir);
                }
                QueueWaterUpdate(flowTarget + Vector3Int.down);
            }
        }

        private void FlowWater(Vector3Int from, Vector3Int to)
        {
            // Move water from one position to another
            SetVoxelAt(from, Voxels.VoxelData.Air);
            SetVoxelAt(to, Voxels.VoxelData.Water);

            // Queue surrounding blocks for update
            Vector3Int[] dirs = new Vector3Int[]
            {
                Vector3Int.up,
                Vector3Int.down,
                Vector3Int.left,
                Vector3Int.right,
                Vector3Int.forward,
                Vector3Int.back
            };

            foreach (Vector3Int dir in dirs)
            {
                QueueWaterUpdate(to + dir);
            }
        }

        private Voxels.VoxelData GetVoxelAt(Vector3Int worldPos)
        {
            Vector3Int chunkPos = chunkManager.WorldToChunkPosition(worldPos);
            Voxels.Chunk chunk = chunkManager.GetChunk(chunkPos);

            if (chunk == null)
                return Voxels.VoxelData.Air;

            Vector3Int localPos = chunk.WorldToLocal(worldPos);
            return chunk.GetVoxel(localPos.x, localPos.y, localPos.z);
        }

        private void SetVoxelAt(Vector3Int worldPos, Voxels.VoxelData voxel)
        {
            Vector3Int chunkPos = chunkManager.WorldToChunkPosition(worldPos);
            Voxels.Chunk chunk = chunkManager.GetChunk(chunkPos);

            if (chunk == null)
                return;

            Vector3Int localPos = chunk.WorldToLocal(worldPos);
            chunk.SetVoxel(localPos.x, localPos.y, localPos.z, voxel);
            chunkManager.MarkChunkDirty(chunkPos);

            // Mark neighbor chunks dirty if on boundary
            if (localPos.x == 0)
                chunkManager.MarkChunkDirty(chunkPos + Vector3Int.left);
            else if (localPos.x == Voxels.Chunk.CHUNK_SIZE - 1)
                chunkManager.MarkChunkDirty(chunkPos + Vector3Int.right);

            if (localPos.y == 0)
                chunkManager.MarkChunkDirty(chunkPos + Vector3Int.down);
            else if (localPos.y == Voxels.Chunk.CHUNK_SIZE - 1)
                chunkManager.MarkChunkDirty(chunkPos + Vector3Int.up);

            if (localPos.z == 0)
                chunkManager.MarkChunkDirty(chunkPos + Vector3Int.back);
            else if (localPos.z == Voxels.Chunk.CHUNK_SIZE - 1)
                chunkManager.MarkChunkDirty(chunkPos + Vector3Int.forward);
        }

        public void ScanAndQueueAllWater()
        {
            if (chunkManager == null)
                return;

            Dimensions.Dimension activeDimension = Dimensions.DimensionManager.Instance.GetActiveDimension();
            if (activeDimension == null)
                return;

            foreach (var kvp in activeDimension.chunks)
            {
                Voxels.Chunk chunk = kvp.Value;

                for (int x = 0; x < Voxels.Chunk.CHUNK_SIZE; x++)
                {
                    for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                    {
                        for (int z = 0; z < Voxels.Chunk.CHUNK_SIZE; z++)
                        {
                            Voxels.VoxelData voxel = chunk.GetVoxel(x, y, z);
                            if (voxel.IsLiquid)
                            {
                                Vector3Int worldPos = new Vector3Int(
                                    chunk.chunkPosition.x * Voxels.Chunk.CHUNK_SIZE + x,
                                    chunk.chunkPosition.y * Voxels.Chunk.CHUNK_SIZE + y,
                                    chunk.chunkPosition.z * Voxels.Chunk.CHUNK_SIZE + z
                                );
                                QueueWaterUpdate(worldPos);
                            }
                        }
                    }
                }
            }

            Debug.Log($"Queued {waterUpdateQueue.Count} water blocks for simulation");
        }
    }
}
