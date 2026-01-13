using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Voxels
{
    public class ChunkManager : MonoBehaviour
    {
        private static ChunkManager instance;
        public static ChunkManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("ChunkManager");
                    instance = go.AddComponent<ChunkManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Queue<Vector3Int> rebuildQueue = new Queue<Vector3Int>();
        private ChunkMeshGenerator meshGenerator;
        private VoxelModifier voxelModifier;

        public int renderDistance = 4;
        public Transform player;
        public int maxRebuildsPerFrame = 1;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            meshGenerator = new ChunkMeshGenerator(this);
        }

        private void Update()
        {
            if (player != null)
            {
                UpdateChunksAroundPlayer();
            }
            RebuildDirtyChunks();
        }

        public Chunk GetChunk(Vector3Int chunkPos)
        {
            Dimensions.Dimension activeDimension = Dimensions.DimensionManager.Instance.GetActiveDimension();
            if (activeDimension == null)
            {
                return null;
            }

            return activeDimension.GetChunk(chunkPos);
        }

        public Chunk CreateChunk(Vector3Int chunkPos)
        {
            Dimensions.Dimension activeDimension = Dimensions.DimensionManager.Instance.GetActiveDimension();
            if (activeDimension == null)
            {
                Debug.LogError("No active dimension set in ChunkManager");
                return null;
            }

            return activeDimension.GetOrCreateChunk(chunkPos);
        }

        public void LoadChunk(Vector3Int chunkPos)
        {
            Dimensions.Dimension activeDimension = Dimensions.DimensionManager.Instance.GetActiveDimension();
            if (activeDimension == null)
            {
                return;
            }

            if (activeDimension.chunks.ContainsKey(chunkPos))
            {
                return;
            }

            CreateChunk(chunkPos);
            Entities.BlockEntityManager.Instance.OnChunkLoad(chunkPos);
        }

        public void UnloadChunk(Vector3Int chunkPos)
        {
            Dimensions.Dimension activeDimension = Dimensions.DimensionManager.Instance.GetActiveDimension();
            if (activeDimension == null)
            {
                return;
            }

            Entities.BlockEntityManager.Instance.OnChunkUnload(chunkPos);
            activeDimension.UnloadChunk(chunkPos);
        }

        public void MarkChunkDirty(Vector3Int chunkPos)
        {
            Dimensions.Dimension activeDimension = Dimensions.DimensionManager.Instance.GetActiveDimension();
            if (activeDimension != null && activeDimension.chunks.ContainsKey(chunkPos) && !rebuildQueue.Contains(chunkPos))
            {
                rebuildQueue.Enqueue(chunkPos);
            }
        }

        public void SetActiveDimension(Dimensions.Dimension dimension)
        {
        }

        public VoxelModifier GetVoxelModifier()
        {
            if (voxelModifier == null)
            {
                Entities.BlockEntityRegistry registry = Entities.BlockEntityLoader.Instance.GetRegistry();
                voxelModifier = new VoxelModifier(this, registry);
            }
            return voxelModifier;
        }

        private void UpdateChunksAroundPlayer()
        {
            Dimensions.Dimension activeDimension = Dimensions.DimensionManager.Instance.GetActiveDimension();
            if (activeDimension == null)
            {
                return;
            }

            Vector3Int playerChunkPos = WorldToChunkPosition(player.position);

            HashSet<Vector3Int> chunksToKeep = new HashSet<Vector3Int>();

            for (int x = -renderDistance; x <= renderDistance; x++)
            {
                for (int y = -renderDistance; y <= renderDistance; y++)
                {
                    for (int z = -renderDistance; z <= renderDistance; z++)
                    {
                        Vector3Int chunkPos = playerChunkPos + new Vector3Int(x, y, z);
                        chunksToKeep.Add(chunkPos);
                        LoadChunk(chunkPos);
                    }
                }
            }

            List<Vector3Int> chunksToUnload = new List<Vector3Int>();
            foreach (var chunkPos in activeDimension.chunks.Keys)
            {
                if (!chunksToKeep.Contains(chunkPos))
                {
                    chunksToUnload.Add(chunkPos);
                }
            }

            foreach (var chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }
        }

        private void RebuildDirtyChunks()
        {
            int rebuildsThisFrame = 0;

            while (rebuildQueue.Count > 0 && rebuildsThisFrame < maxRebuildsPerFrame)
            {
                Vector3Int chunkPos = rebuildQueue.Dequeue();
                Chunk chunk = GetChunk(chunkPos);

                if (chunk != null && chunk.isDirty)
                {
                    RebuildChunkMesh(chunk);
                    chunk.isDirty = false;
                    rebuildsThisFrame++;
                }
            }
        }

        private void RebuildChunkMesh(Chunk chunk)
        {
            meshGenerator.GenerateMesh(chunk);
        }

        public Vector3Int WorldToChunkPosition(Vector3 worldPos)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / Chunk.CHUNK_SIZE),
                Mathf.FloorToInt(worldPos.y / Chunk.CHUNK_SIZE),
                Mathf.FloorToInt(worldPos.z / Chunk.CHUNK_SIZE)
            );
        }

        public Vector3Int WorldToLocalVoxel(Vector3 worldPos, out Vector3Int chunkPos)
        {
            chunkPos = WorldToChunkPosition(worldPos);

            Vector3Int chunkWorldPos = new Vector3Int(
                chunkPos.x * Chunk.CHUNK_SIZE,
                chunkPos.y * Chunk.CHUNK_SIZE,
                chunkPos.z * Chunk.CHUNK_SIZE
            );

            Vector3Int localPos = new Vector3Int(
                Mathf.FloorToInt(worldPos.x) - chunkWorldPos.x,
                Mathf.FloorToInt(worldPos.y) - chunkWorldPos.y,
                Mathf.FloorToInt(worldPos.z) - chunkWorldPos.z
            );

            return localPos;
        }
    }
}