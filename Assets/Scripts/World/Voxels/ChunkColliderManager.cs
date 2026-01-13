using UnityEngine;

namespace Pixension.Voxels
{
    public class ChunkColliderManager : MonoBehaviour
    {
        [Header("Collider Settings")]
        public bool enableColliders = true;
        public bool onlyCollidersNearPlayer = true;
        public int colliderDistance = 3; // Chunks um Player

        [Header("Performance")]
        public float updateInterval = 1f; // Sekunden zwischen Updates

        private Transform player;
        private ChunkManager chunkManager;
        private Dimensions.DimensionManager dimensionManager;
        private float updateTimer;

        private void Start()
        {
            chunkManager = ChunkManager.Instance;
            dimensionManager = Dimensions.DimensionManager.Instance;

            // Finde Player
            if (player == null)
            {
                Player.PlayerController playerController = FindFirstObjectByType<Player.PlayerController>();
                if (playerController != null)
                {
                    player = playerController.transform;
                }
            }
        }

        private void Update()
        {
            if (!enableColliders || !onlyCollidersNearPlayer || player == null)
                return;

            updateTimer += Time.deltaTime;

            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateColliders();
            }
        }

        private void UpdateColliders()
        {
            Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
            if (activeDim == null)
                return;

            Vector3Int playerChunkPos = chunkManager.WorldToChunkPosition(player.position);

            foreach (var kvp in activeDim.chunks)
            {
                Vector3Int chunkPos = kvp.Key;
                Chunk chunk = kvp.Value;

                if (chunk.meshCollider == null)
                    continue;

                // Berechne Distanz
                float distance = Vector3.Distance(
                    new Vector3(playerChunkPos.x, playerChunkPos.y, playerChunkPos.z),
                    new Vector3(chunkPos.x, chunkPos.y, chunkPos.z)
                );

                // Enable/Disable Collider basierend auf Distanz
                bool shouldBeEnabled = distance <= colliderDistance;

                if (chunk.meshCollider.enabled != shouldBeEnabled)
                {
                    chunk.meshCollider.enabled = shouldBeEnabled;
                }
            }
        }

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
        }

        public void EnableAllColliders(bool enable)
        {
            Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
            if (activeDim == null)
                return;

            foreach (var kvp in activeDim.chunks)
            {
                Chunk chunk = kvp.Value;
                if (chunk.meshCollider != null)
                {
                    chunk.meshCollider.enabled = enable;
                }
            }
        }

        public void RebuildCollider(Vector3Int chunkPos)
        {
            Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
            if (activeDim == null)
                return;

            if (activeDim.chunks.ContainsKey(chunkPos))
            {
                Chunk chunk = activeDim.chunks[chunkPos];

                if (chunk.meshCollider != null && chunk.meshFilter != null)
                {
                    chunk.meshCollider.sharedMesh = null;
                    chunk.meshCollider.sharedMesh = chunk.meshFilter.sharedMesh;
                }
            }
        }
    }
}