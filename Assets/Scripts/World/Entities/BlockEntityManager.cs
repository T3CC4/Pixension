using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Entities
{
    public class BlockEntityManager : MonoBehaviour
    {
        private static BlockEntityManager instance;
        public static BlockEntityManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("BlockEntityManager");
                    instance = go.AddComponent<BlockEntityManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<Vector3Int, List<BlockEntity>> entitiesByChunk = new Dictionary<Vector3Int, List<BlockEntity>>();
        private List<BlockEntity> allEntities = new List<BlockEntity>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RegisterEntity(BlockEntity entity)
        {
            if (!allEntities.Contains(entity))
            {
                allEntities.Add(entity);
            }

            if (!entitiesByChunk.ContainsKey(entity.chunkPosition))
            {
                entitiesByChunk[entity.chunkPosition] = new List<BlockEntity>();
            }

            if (!entitiesByChunk[entity.chunkPosition].Contains(entity))
            {
                entitiesByChunk[entity.chunkPosition].Add(entity);
            }
        }

        public void UnregisterEntity(BlockEntity entity)
        {
            allEntities.Remove(entity);

            if (entitiesByChunk.ContainsKey(entity.chunkPosition))
            {
                entitiesByChunk[entity.chunkPosition].Remove(entity);

                if (entitiesByChunk[entity.chunkPosition].Count == 0)
                {
                    entitiesByChunk.Remove(entity.chunkPosition);
                }
            }
        }

        public void OnChunkLoad(Vector3Int chunkPos)
        {
            List<BlockEntity> entities = GetEntitiesInChunk(chunkPos);
            foreach (BlockEntity entity in entities)
            {
                if (entity != null)
                {
                    entity.Show();
                }
            }
        }

        public void OnChunkUnload(Vector3Int chunkPos)
        {
            List<BlockEntity> entities = GetEntitiesInChunk(chunkPos);
            foreach (BlockEntity entity in entities)
            {
                if (entity != null)
                {
                    entity.Hide();
                }
            }
        }

        public List<BlockEntity> GetEntitiesInChunk(Vector3Int chunkPos)
        {
            if (entitiesByChunk.TryGetValue(chunkPos, out List<BlockEntity> entities))
            {
                return new List<BlockEntity>(entities);
            }
            return new List<BlockEntity>();
        }

        public BlockEntity GetEntityAtPosition(Vector3Int worldPos)
        {
            foreach (BlockEntity entity in allEntities)
            {
                if (entity.worldPosition == worldPos)
                {
                    return entity;
                }
            }
            return null;
        }

        public List<BlockEntity> GetAllEntities()
        {
            return new List<BlockEntity>(allEntities);
        }

        public void RemoveEntity(BlockEntity entity)
        {
            if (entity != null)
            {
                entity.OnBreak();
                UnregisterEntity(entity);
                Destroy(entity.gameObject);
            }
        }
    }
}