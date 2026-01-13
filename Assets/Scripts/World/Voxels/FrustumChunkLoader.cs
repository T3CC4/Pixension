using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Voxels
{
    public class FrustumChunkLoader : MonoBehaviour
    {
        [Header("Loading Settings")]
        public int horizontalLoadDistance = 8; // Chunks horizontal
        public int verticalLoadDistance = 4;   // Chunks vertikal
        public float updateInterval = 0.5f;    // Sekunden zwischen Updates

        [Header("Frustum Culling")]
        public bool useFrustumCulling = true;
        public float frustumPadding = 1.5f;    // Extra Chunks auﬂerhalb Frustum

        [Header("Performance")]
        public int maxChunksPerFrame = 5;      // Max Chunks zum Laden pro Frame
        public int maxUnloadsPerFrame = 10;    // Max Chunks zum Entladen pro Frame

        private Transform player;
        private Camera playerCamera;
        private ChunkManager chunkManager;
        private Dimensions.DimensionManager dimensionManager;

        private Vector3Int lastPlayerChunkPos;
        private float updateTimer;
        private Plane[] frustumPlanes;

        private Queue<Vector3Int> chunksToLoad = new Queue<Vector3Int>();
        private List<Vector3Int> chunksToUnload = new List<Vector3Int>();

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

            // Finde Camera
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (player == null || playerCamera == null)
            {
                Debug.LogError("FrustumChunkLoader: Player or Camera not found!");
                enabled = false;
                return;
            }

            lastPlayerChunkPos = GetPlayerChunkPosition();
            frustumPlanes = new Plane[6];
        }

        private void Update()
        {
            if (player == null || playerCamera == null)
                return;

            updateTimer += Time.deltaTime;

            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateChunkLoading();
            }

            // Verarbeite Lade-Queue
            ProcessLoadQueue();
        }

        private void UpdateChunkLoading()
        {
            Vector3Int currentPlayerChunkPos = GetPlayerChunkPosition();

            // Nur updaten wenn Player sich bewegt hat
            if (currentPlayerChunkPos == lastPlayerChunkPos && chunksToLoad.Count == 0)
                return;

            lastPlayerChunkPos = currentPlayerChunkPos;

            // Berechne Frustum
            if (useFrustumCulling)
            {
                GeometryUtility.CalculateFrustumPlanes(playerCamera, frustumPlanes);
            }

            // Finde chunks die geladen werden sollen
            HashSet<Vector3Int> desiredChunks = GetDesiredChunks(currentPlayerChunkPos);

            // Finde chunks die entladen werden sollen
            Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
            if (activeDim != null)
            {
                FindChunksToUnload(activeDim, desiredChunks);
            }

            // Queue chunks zum Laden
            foreach (Vector3Int chunkPos in desiredChunks)
            {
                if (activeDim != null && !activeDim.chunks.ContainsKey(chunkPos))
                {
                    if (!chunksToLoad.Contains(chunkPos))
                    {
                        chunksToLoad.Enqueue(chunkPos);
                    }
                }
            }
        }

        private HashSet<Vector3Int> GetDesiredChunks(Vector3Int centerChunk)
        {
            HashSet<Vector3Int> chunks = new HashSet<Vector3Int>();

            for (int x = -horizontalLoadDistance; x <= horizontalLoadDistance; x++)
            {
                for (int z = -horizontalLoadDistance; z <= horizontalLoadDistance; z++)
                {
                    for (int y = -verticalLoadDistance; y <= verticalLoadDistance; y++)
                    {
                        Vector3Int chunkPos = centerChunk + new Vector3Int(x, y, z);

                        // Distance Check
                        float distance = Vector3.Distance(
                            new Vector3(centerChunk.x, centerChunk.y, centerChunk.z),
                            new Vector3(chunkPos.x, chunkPos.y, chunkPos.z)
                        );

                        if (distance > horizontalLoadDistance)
                            continue;

                        // Frustum Check
                        if (useFrustumCulling && !IsChunkInFrustum(chunkPos))
                            continue;

                        chunks.Add(chunkPos);
                    }
                }
            }

            return chunks;
        }

        private bool IsChunkInFrustum(Vector3Int chunkPos)
        {
            // Berechne Chunk Bounds mit Padding
            Vector3 chunkWorldPos = new Vector3(
                chunkPos.x * Chunk.CHUNK_SIZE,
                chunkPos.y * Chunk.CHUNK_SIZE,
                chunkPos.z * Chunk.CHUNK_SIZE
            );

            float chunkSize = Chunk.CHUNK_SIZE;
            Vector3 chunkCenter = chunkWorldPos + new Vector3(chunkSize * 0.5f, chunkSize * 0.5f, chunkSize * 0.5f);
            Vector3 chunkExtents = new Vector3(chunkSize, chunkSize, chunkSize) * 0.5f * frustumPadding;

            Bounds chunkBounds = new Bounds(chunkCenter, chunkExtents * 2f);

            // Teste gegen Frustum
            return GeometryUtility.TestPlanesAABB(frustumPlanes, chunkBounds);
        }

        private void FindChunksToUnload(Dimensions.Dimension dimension, HashSet<Vector3Int> desiredChunks)
        {
            chunksToUnload.Clear();

            foreach (var kvp in dimension.chunks)
            {
                Vector3Int chunkPos = kvp.Key;

                if (!desiredChunks.Contains(chunkPos))
                {
                    chunksToUnload.Add(chunkPos);
                }
            }

            // Sortiere nach Distanz (weiteste zuerst)
            Vector3Int playerChunk = GetPlayerChunkPosition();
            chunksToUnload.Sort((a, b) =>
            {
                float distA = Vector3.Distance(
                    new Vector3(playerChunk.x, playerChunk.y, playerChunk.z),
                    new Vector3(a.x, a.y, a.z)
                );
                float distB = Vector3.Distance(
                    new Vector3(playerChunk.x, playerChunk.y, playerChunk.z),
                    new Vector3(b.x, b.y, b.z)
                );
                return distB.CompareTo(distA);
            });

            // Entlade Chunks
            int unloaded = 0;
            foreach (Vector3Int chunkPos in chunksToUnload)
            {
                if (unloaded >= maxUnloadsPerFrame)
                    break;

                chunkManager.UnloadChunk(chunkPos);
                unloaded++;
            }
        }

        private void ProcessLoadQueue()
        {
            int loaded = 0;

            while (chunksToLoad.Count > 0 && loaded < maxChunksPerFrame)
            {
                Vector3Int chunkPos = chunksToLoad.Dequeue();

                // Nochmal pr¸fen ob Chunk noch gebraucht wird
                Vector3Int playerChunk = GetPlayerChunkPosition();
                float distance = Vector3.Distance(
                    new Vector3(playerChunk.x, playerChunk.y, playerChunk.z),
                    new Vector3(chunkPos.x, chunkPos.y, chunkPos.z)
                );

                if (distance > horizontalLoadDistance + 2)
                    continue;

                chunkManager.LoadChunk(chunkPos);
                loaded++;
            }
        }

        private Vector3Int GetPlayerChunkPosition()
        {
            if (player == null)
                return Vector3Int.zero;

            return chunkManager.WorldToChunkPosition(player.position);
        }

        public void SetPlayer(Transform newPlayer)
        {
            player = newPlayer;
            if (player != null)
            {
                lastPlayerChunkPos = GetPlayerChunkPosition();
            }
        }

        public void ForceUpdate()
        {
            updateTimer = updateInterval;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || player == null)
                return;

            // Zeichne Load Distance
            Vector3Int playerChunk = GetPlayerChunkPosition();
            Vector3 playerChunkWorld = new Vector3(
                playerChunk.x * Chunk.CHUNK_SIZE,
                playerChunk.y * Chunk.CHUNK_SIZE,
                playerChunk.z * Chunk.CHUNK_SIZE
            );

            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawWireCube(
                playerChunkWorld + new Vector3(Chunk.CHUNK_SIZE * 0.5f, Chunk.CHUNK_SIZE * 0.5f, Chunk.CHUNK_SIZE * 0.5f),
                new Vector3(
                    horizontalLoadDistance * 2 * Chunk.CHUNK_SIZE,
                    verticalLoadDistance * 2 * Chunk.CHUNK_SIZE,
                    horizontalLoadDistance * 2 * Chunk.CHUNK_SIZE
                )
            );

            // Zeichne Frustum (wenn culling aktiv)
            if (useFrustumCulling && playerCamera != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.matrix = Matrix4x4.TRS(playerCamera.transform.position, playerCamera.transform.rotation, Vector3.one);
                Gizmos.DrawFrustum(Vector3.zero, playerCamera.fieldOfView, playerCamera.farClipPlane, playerCamera.nearClipPlane, playerCamera.aspect);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}