using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Entities
{
    [Serializable]
    public class BlockEntityDefinition
    {
        public string entityID;
        public string displayName;
        public GameObject prefab;
        public EntityType type;
        public Sprite icon;

        public BlockEntityDefinition(string id, string name, GameObject prefabObject, EntityType entityType, Sprite iconSprite)
        {
            entityID = id;
            displayName = name;
            prefab = prefabObject;
            type = entityType;
            icon = iconSprite;
        }
    }

    [CreateAssetMenu(fileName = "BlockEntityRegistry", menuName = "Pixension/BlockEntity Registry", order = 1)]
    public class BlockEntityRegistry : ScriptableObject
    {
        public List<BlockEntityDefinition> entities = new List<BlockEntityDefinition>();

        private Dictionary<string, BlockEntityDefinition> cache;

        private void OnEnable()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            cache = new Dictionary<string, BlockEntityDefinition>();

            foreach (BlockEntityDefinition entity in entities)
            {
                if (!string.IsNullOrEmpty(entity.entityID))
                {
                    cache[entity.entityID] = entity;
                }
            }
        }

        public BlockEntityDefinition GetEntity(string id)
        {
            if (cache == null)
            {
                BuildCache();
            }

            if (cache.TryGetValue(id, out BlockEntityDefinition entity))
            {
                return entity;
            }

            Debug.LogWarning($"BlockEntity with ID '{id}' not found in registry");
            return null;
        }

        public List<BlockEntityDefinition> GetAllEntities()
        {
            return new List<BlockEntityDefinition>(entities);
        }

        public List<BlockEntityDefinition> GetEntitiesByType(EntityType type)
        {
            List<BlockEntityDefinition> result = new List<BlockEntityDefinition>();

            foreach (BlockEntityDefinition entity in entities)
            {
                if (entity.type == type)
                {
                    result.Add(entity);
                }
            }

            return result;
        }

        private void OnValidate()
        {
            BuildCache();
        }
    }
}