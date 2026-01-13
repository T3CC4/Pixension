using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Mobs
{
    public class MobSpawnerManager : MonoBehaviour
    {
        private static MobSpawnerManager instance;
        public static MobSpawnerManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("MobSpawnerManager");
                    instance = go.AddComponent<MobSpawnerManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<Vector3Int, List<MobSpawner>> spawnersByChunk = new Dictionary<Vector3Int, List<MobSpawner>>();
        private List<MobSpawner> allSpawners = new List<MobSpawner>();

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

        public void RegisterSpawner(MobSpawner spawner, Vector3Int chunkPosition)
        {
            if (!allSpawners.Contains(spawner))
            {
                allSpawners.Add(spawner);
            }

            if (!spawnersByChunk.ContainsKey(chunkPosition))
            {
                spawnersByChunk[chunkPosition] = new List<MobSpawner>();
            }

            if (!spawnersByChunk[chunkPosition].Contains(spawner))
            {
                spawnersByChunk[chunkPosition].Add(spawner);
            }

            spawner.transform.SetParent(transform);
        }

        public void UnregisterSpawner(MobSpawner spawner, Vector3Int chunkPosition)
        {
            allSpawners.Remove(spawner);

            if (spawnersByChunk.ContainsKey(chunkPosition))
            {
                spawnersByChunk[chunkPosition].Remove(spawner);

                if (spawnersByChunk[chunkPosition].Count == 0)
                {
                    spawnersByChunk.Remove(chunkPosition);
                }
            }
        }

        public void OnChunkLoad(Vector3Int chunkPos)
        {
            if (spawnersByChunk.TryGetValue(chunkPos, out List<MobSpawner> spawners))
            {
                foreach (MobSpawner spawner in spawners)
                {
                    if (spawner != null)
                    {
                        spawner.Show();
                    }
                }
            }
        }

        public void OnChunkUnload(Vector3Int chunkPos)
        {
            if (spawnersByChunk.TryGetValue(chunkPos, out List<MobSpawner> spawners))
            {
                foreach (MobSpawner spawner in spawners)
                {
                    if (spawner != null)
                    {
                        spawner.Hide();
                    }
                }
            }
        }

        public List<MobSpawner> GetSpawnersInChunk(Vector3Int chunkPos)
        {
            if (spawnersByChunk.TryGetValue(chunkPos, out List<MobSpawner> spawners))
            {
                return new List<MobSpawner>(spawners);
            }
            return new List<MobSpawner>();
        }

        public Dictionary<string, int> GetTotalMobCounts()
        {
            Dictionary<string, int> totalCounts = new Dictionary<string, int>();

            foreach (MobSpawner spawner in allSpawners)
            {
                if (spawner == null) continue;

                Dictionary<string, int> spawnerCounts = spawner.GetMobCounts();

                foreach (var kvp in spawnerCounts)
                {
                    if (!totalCounts.ContainsKey(kvp.Key))
                    {
                        totalCounts[kvp.Key] = 0;
                    }
                    totalCounts[kvp.Key] += kvp.Value;
                }
            }

            return totalCounts;
        }

        public int GetTotalSpawnerCount()
        {
            return allSpawners.Count;
        }

        public void ClearAll()
        {
            foreach (MobSpawner spawner in allSpawners)
            {
                if (spawner != null)
                {
                    Destroy(spawner.gameObject);
                }
            }

            allSpawners.Clear();
            spawnersByChunk.Clear();
        }
    }
}