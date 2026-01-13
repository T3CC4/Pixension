using UnityEngine;
using System.Collections.Generic;

namespace Pixension.Utilities
{
    /// <summary>
    /// Object pooling system for Mesh objects to reduce GC pressure
    /// </summary>
    public class MeshPool : MonoBehaviour
    {
        private static MeshPool instance;
        public static MeshPool Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("MeshPool");
                    instance = go.AddComponent<MeshPool>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("Pool Settings")]
        public int initialPoolSize = 50;
        public int maxPoolSize = 200;

        private Queue<Mesh> availableMeshes = new Queue<Mesh>();
        private HashSet<Mesh> activeMeshes = new HashSet<Mesh>();
        private int totalCreated = 0;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePool();
        }

        private void InitializePool()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewMesh();
            }

            Debug.Log($"MeshPool initialized with {initialPoolSize} meshes");
        }

        private Mesh CreateNewMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = $"PooledMesh_{totalCreated}";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            availableMeshes.Enqueue(mesh);
            totalCreated++;
            return mesh;
        }

        /// <summary>
        /// Gets a mesh from the pool or creates a new one if pool is empty
        /// </summary>
        public Mesh GetMesh()
        {
            Mesh mesh;

            if (availableMeshes.Count > 0)
            {
                mesh = availableMeshes.Dequeue();
            }
            else
            {
                if (totalCreated < maxPoolSize)
                {
                    mesh = CreateNewMesh();
                    availableMeshes.Dequeue(); // Remove it immediately as we're going to use it
                }
                else
                {
                    // Pool is at max capacity, create a temporary mesh
                    Debug.LogWarning("MeshPool at max capacity, creating temporary mesh");
                    mesh = new Mesh();
                    mesh.name = "TempMesh_PoolFull";
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                }
            }

            mesh.Clear();
            activeMeshes.Add(mesh);
            return mesh;
        }

        /// <summary>
        /// Returns a mesh to the pool for reuse
        /// </summary>
        public void ReturnMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (!activeMeshes.Contains(mesh))
            {
                // This mesh wasn't from our pool, destroy it
                if (mesh.name.StartsWith("TempMesh_"))
                {
                    Destroy(mesh);
                }
                return;
            }

            mesh.Clear();
            activeMeshes.Remove(mesh);

            if (availableMeshes.Count < maxPoolSize)
            {
                availableMeshes.Enqueue(mesh);
            }
            else
            {
                // Pool is full, destroy this mesh
                Destroy(mesh);
                totalCreated--;
            }
        }

        /// <summary>
        /// Gets current pool statistics
        /// </summary>
        public (int available, int active, int total) GetStats()
        {
            return (availableMeshes.Count, activeMeshes.Count, totalCreated);
        }

        /// <summary>
        /// Clears all pooled meshes (use with caution)
        /// </summary>
        public void ClearPool()
        {
            while (availableMeshes.Count > 0)
            {
                Mesh mesh = availableMeshes.Dequeue();
                Destroy(mesh);
            }

            foreach (Mesh mesh in activeMeshes)
            {
                Destroy(mesh);
            }

            activeMeshes.Clear();
            totalCreated = 0;

            Debug.Log("MeshPool cleared");
        }

        private void OnDestroy()
        {
            ClearPool();
        }
    }
}
