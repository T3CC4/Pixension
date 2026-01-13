using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Mobs
{
    public class MobSpawner : Entities.BlockEntity
    {
        public Vector3Int spawnRangeMin;
        public Vector3Int spawnRangeMax;
        public List<Structures.MobSpawnEntry> mobEntries = new List<Structures.MobSpawnEntry>();

        private Dictionary<string, List<GameObject>> activeMobs = new Dictionary<string, List<GameObject>>();
        private float[] nextSpawnTimes;
        private MobRegistry mobRegistry;
        private bool isSpawnerInitialized = false;

        public void InitializeSpawner(Structures.ArchitectureData archData, Vector3Int structureWorldPos)
        {
            if (archData.mobs == null || archData.mobs.Length == 0)
            {
                Debug.LogWarning("MobSpawner initialized with no mob entries");
                return;
            }

            spawnRangeMin = structureWorldPos + archData.spawnRangeMin;
            spawnRangeMax = structureWorldPos + archData.spawnRangeMax;

            mobEntries = new List<Structures.MobSpawnEntry>(archData.mobs);
            nextSpawnTimes = new float[mobEntries.Count];

            for (int i = 0; i < mobEntries.Count; i++)
            {
                activeMobs[mobEntries[i].mobID] = new List<GameObject>();
                nextSpawnTimes[i] = Time.time + mobEntries[i].spawnInterval;
            }

            mobRegistry = MobLoader.Instance.GetRegistry();
            isSpawnerInitialized = true;

            SpawnInitialMobs();
        }

        private void SpawnInitialMobs()
        {
            if (!isSpawnerInitialized) return;

            for (int i = 0; i < mobEntries.Count; i++)
            {
                Structures.MobSpawnEntry entry = mobEntries[i];

                for (int j = 0; j < entry.initialCount; j++)
                {
                    SpawnMob(entry.mobID);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (!isSpawnerInitialized || !isVisible) return;

            for (int i = 0; i < mobEntries.Count; i++)
            {
                if (Time.time >= nextSpawnTimes[i])
                {
                    Structures.MobSpawnEntry entry = mobEntries[i];

                    CleanupDeadMobs(entry.mobID);

                    if (GetActiveMobCount(entry.mobID) < entry.maxCount)
                    {
                        SpawnMob(entry.mobID);
                    }

                    nextSpawnTimes[i] = Time.time + entry.spawnInterval;
                }
            }
        }

        private void SpawnMob(string mobID)
        {
            if (!activeMobs.ContainsKey(mobID))
            {
                activeMobs[mobID] = new List<GameObject>();
            }

            Vector3 spawnPosition = GetRandomSpawnPosition();

            GameObject mobInstance = MobLoader.Instance.InstantiateMob(
                mobID,
                spawnPosition,
                Quaternion.identity
            );

            if (mobInstance != null)
            {
                mobInstance.transform.SetParent(transform);
                activeMobs[mobID].Add(mobInstance);

                Debug.Log($"Spawned {mobID} at {spawnPosition}");
            }
        }

        private Vector3 GetRandomSpawnPosition()
        {
            float x = Random.Range(spawnRangeMin.x, spawnRangeMax.x + 1);
            float y = Random.Range(spawnRangeMin.y, spawnRangeMax.y + 1);
            float z = Random.Range(spawnRangeMin.z, spawnRangeMax.z + 1);

            return new Vector3(x, y, z);
        }

        private void CleanupDeadMobs(string mobID)
        {
            if (!activeMobs.ContainsKey(mobID)) return;

            activeMobs[mobID].RemoveAll(mob =>
            {
                if (mob == null) return true;

                Mob mobComponent = mob.GetComponent<Mob>();
                return mobComponent != null && !mobComponent.IsAlive();
            });
        }

        private int GetActiveMobCount(string mobID)
        {
            if (!activeMobs.ContainsKey(mobID)) return 0;

            return activeMobs[mobID].Count;
        }

        public override void OnPlace()
        {
            base.OnPlace();
            Debug.Log($"MobSpawner placed at {worldPosition}");
        }

        public override void OnBreak()
        {
            base.OnBreak();

            foreach (var mobList in activeMobs.Values)
            {
                foreach (GameObject mob in mobList)
                {
                    if (mob != null)
                    {
                        Destroy(mob);
                    }
                }
            }

            activeMobs.Clear();
        }

        public Dictionary<string, int> GetMobCounts()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();

            foreach (var kvp in activeMobs)
            {
                CleanupDeadMobs(kvp.Key);
                counts[kvp.Key] = kvp.Value.Count;
            }

            return counts;
        }

        private void OnDrawGizmosSelected()
        {
            if (!isSpawnerInitialized) return;

            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            Vector3 center = new Vector3(
                (spawnRangeMin.x + spawnRangeMax.x) * 0.5f,
                (spawnRangeMin.y + spawnRangeMax.y) * 0.5f,
                (spawnRangeMin.z + spawnRangeMax.z) * 0.5f
            );

            Vector3 size = new Vector3(
                spawnRangeMax.x - spawnRangeMin.x + 1,
                spawnRangeMax.y - spawnRangeMin.y + 1,
                spawnRangeMax.z - spawnRangeMin.z + 1
            );

            Gizmos.DrawCube(center, size);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(center, size);
        }
    }
}