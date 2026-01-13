using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Mobs
{
    [Serializable]
    public class MobDefinition
    {
        public string mobID;
        public string displayName;
        public GameObject prefab;
        public Sprite icon;

        public MobDefinition(string id, string name, GameObject prefabObject, Sprite iconSprite)
        {
            mobID = id;
            displayName = name;
            prefab = prefabObject;
            icon = iconSprite;
        }
    }

    [CreateAssetMenu(fileName = "MobRegistry", menuName = "Pixension/Mob Registry", order = 2)]
    public class MobRegistry : ScriptableObject
    {
        public List<MobDefinition> mobs = new List<MobDefinition>();

        private Dictionary<string, MobDefinition> cache;

        private void OnEnable()
        {
            BuildCache();
        }

        private void BuildCache()
        {
            cache = new Dictionary<string, MobDefinition>();

            foreach (MobDefinition mob in mobs)
            {
                if (!string.IsNullOrEmpty(mob.mobID))
                {
                    cache[mob.mobID] = mob;
                }
            }
        }

        public MobDefinition GetMob(string id)
        {
            if (cache == null)
            {
                BuildCache();
            }

            if (cache.TryGetValue(id, out MobDefinition mob))
            {
                return mob;
            }

            Debug.LogWarning($"Mob with ID '{id}' not found in registry");
            return null;
        }

        public List<MobDefinition> GetAllMobs()
        {
            return new List<MobDefinition>(mobs);
        }

        private void OnValidate()
        {
            BuildCache();
        }
    }
}