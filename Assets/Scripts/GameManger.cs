using System.Collections.Generic;
using UnityEngine;

namespace Pixension
{
    public class GameManager : MonoBehaviour
    {
        private static GameManager instance;
        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("World Settings")]
        public int seed = 0;
        public bool loadExistingSave = false;
        public string saveToLoad = "autosave";

        [Header("Auto-Save")]
        public bool enableAutoSave = true;
        public float autoSaveInterval = 300f; // 5 Minuten
        private float autoSaveTimer = 0f;

        [Header("Spawn Settings")]
        public int spawnPreloadRadius = 3; // Chunks um Spawn
        public Vector3Int spawnWorldPosition = Vector3Int.zero; // x=0, z=0
        public Transform playerPrefab;
        public Transform playerInstance;

        [Header("References")]
        private Dimensions.DimensionManager dimensionManager;
        private SaveSystem.SaveLoadManager saveLoadManager;
        private Voxels.ChunkManager chunkManager;
        private Voxels.FrustumChunkLoader chunkLoader;
        private Voxels.ChunkColliderManager colliderManager;

        private bool isInitialized = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Generiere zufälligen Seed wenn nicht gesetzt
            if (seed == 0)
            {
                seed = Random.Range(int.MinValue, int.MaxValue);
                Debug.Log($"Generated random seed: {seed}");
            }

            InitializeGame();
        }

        private void InitializeGame()
        {
            Debug.Log("=== Game Initialization Started ===");

            // Lade Registries
            LoadRegistries();

            // Hole Manager Instanzen
            dimensionManager = Dimensions.DimensionManager.Instance;
            saveLoadManager = SaveSystem.SaveLoadManager.Instance;
            chunkManager = Voxels.ChunkManager.Instance;

            // Erstelle FrustumChunkLoader
            GameObject loaderGO = new GameObject("FrustumChunkLoader");
            loaderGO.transform.SetParent(transform);
            chunkLoader = loaderGO.AddComponent<Voxels.FrustumChunkLoader>();

            // Erstelle ChunkColliderManager
            GameObject colliderGO = new GameObject("ChunkColliderManager");
            colliderGO.transform.SetParent(transform);
            colliderManager = colliderGO.AddComponent<Voxels.ChunkColliderManager>();

            // Lade bestehenden Save oder erstelle neue Welt
            if (loadExistingSave && !string.IsNullOrEmpty(saveToLoad))
            {
                LoadWorld();
            }
            else
            {
                CreateNewWorld();
            }

            isInitialized = true;
            Debug.Log("=== Game Initialization Complete ===");
        }

        private void LoadRegistries()
        {
            Debug.Log("Loading Registries...");

            // Block Entity Registry
            Entities.BlockEntityRegistry entityRegistry = Resources.Load<Entities.BlockEntityRegistry>("BlockEntityRegistry");
            if (entityRegistry == null)
            {
                Debug.LogWarning("BlockEntityRegistry not found in Resources");
            }
            else
            {
                Debug.Log($"Loaded BlockEntityRegistry: {entityRegistry.GetAllEntities().Count} entities");
            }

            // Mob Registry
            Mobs.MobRegistry mobRegistry = Resources.Load<Mobs.MobRegistry>("MobRegistry");
            if (mobRegistry == null)
            {
                Debug.LogWarning("MobRegistry not found in Resources");
            }
            else
            {
                Debug.Log($"Loaded MobRegistry: {mobRegistry.GetAllMobs().Count} mobs");
            }

            // Structure Loader (lädt automatisch alle Strukturen)
            Structures.StructureLoader structureLoader = Structures.StructureLoader.Instance;
            Debug.Log($"Loaded Structures: {structureLoader.GetAllStructures().Count}");

            Debug.Log("Registries loaded successfully");
        }

        private void LoadWorld()
        {
            Debug.Log($"Loading existing world: {saveToLoad}");

            if (saveLoadManager.SaveExists(saveToLoad))
            {
                bool success = saveLoadManager.LoadWorld(saveToLoad);

                if (success)
                {
                    Debug.Log($"World '{saveToLoad}' loaded successfully");

                    // Seed wird vom Save überschrieben
                    Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
                    if (activeDim != null && activeDim.generator != null)
                    {
                        seed = activeDim.generator.GetSeed();
                        Debug.Log($"World seed: {seed}");
                    }

                    // Preload Spawn Area und spawne Player
                    PreloadSpawnAreaAndSpawnPlayer();
                }
                else
                {
                    Debug.LogError($"Failed to load world '{saveToLoad}', creating new world instead");
                    CreateNewWorld();
                }
            }
            else
            {
                Debug.LogWarning($"Save file '{saveToLoad}' not found, creating new world");
                CreateNewWorld();
            }
        }

        private void CreateNewWorld()
        {
            Debug.Log($"Creating new world with seed: {seed}");

            // Initialisiere DimensionManager mit Seed
            dimensionManager.Initialize(seed);

            // Erstelle Overworld mit Grassland Generator
            Dimensions.Dimension overworld = dimensionManager.CreateDimension("overworld", "grassland");
            if (overworld != null)
            {
                Debug.Log("Created Overworld dimension with Grassland generator");
            }
            else
            {
                Debug.LogError("Failed to create Overworld dimension");
            }

            // Erstelle Nether mit Desert Generator (Beispiel)
            Dimensions.Dimension nether = dimensionManager.CreateDimension("nether", "desert");
            if (nether != null)
            {
                Debug.Log("Created Nether dimension with Desert generator");
            }
            else
            {
                Debug.LogWarning("Failed to create Nether dimension");
            }

            // Wechsle zu Overworld
            dimensionManager.SwitchDimension("overworld");

            // Preload Spawn Area und spawne Player
            PreloadSpawnAreaAndSpawnPlayer();

            Debug.Log("New world created successfully");
        }

        private void PreloadSpawnAreaAndSpawnPlayer()
        {
            Debug.Log($"Preloading spawn area (radius: {spawnPreloadRadius} chunks)...");

            Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
            if (activeDim == null)
            {
                Debug.LogError("No active dimension for spawn preload");
                return;
            }

            // Berechne Chunk-Position des Spawns
            Vector3Int spawnChunkPos = new Vector3Int(
                Mathf.FloorToInt((float)spawnWorldPosition.x / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)spawnWorldPosition.y / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)spawnWorldPosition.z / Voxels.Chunk.CHUNK_SIZE)
            );

            // Preload Chunks um Spawn herum
            int chunksLoaded = 0;
            List<Vector3Int> loadedChunkPositions = new List<Vector3Int>();

            for (int x = -spawnPreloadRadius; x <= spawnPreloadRadius; x++)
            {
                for (int z = -spawnPreloadRadius; z <= spawnPreloadRadius; z++)
                {
                    // Y-Bereich um Spawn
                    for (int y = -1; y <= 2; y++)
                    {
                        Vector3Int chunkPos = spawnChunkPos + new Vector3Int(x, y, z);
                        Voxels.Chunk chunk = activeDim.GetOrCreateChunk(chunkPos);

                        if (chunk != null)
                        {
                            loadedChunkPositions.Add(chunkPos);
                            chunksLoaded++;
                        }
                    }
                }
            }

            Debug.Log($"Preloaded {chunksLoaded} chunks around spawn");

            // Rebuild Meshes für alle geladenen Chunks
            if (chunkManager != null)
            {
                Debug.Log("Building meshes for preloaded chunks...");
                int meshesBuilt = 0;

                foreach (Vector3Int chunkPos in loadedChunkPositions)
                {
                    chunkManager.MarkChunkDirty(chunkPos);
                    meshesBuilt++;
                }

                Debug.Log($"Marked {meshesBuilt} chunks as dirty for mesh rebuild");

                // Force sofortige Mesh-Builds (mehrere Frames)
                StartCoroutine(ForceRebuildChunks(loadedChunkPositions));
            }

            // Finde höchsten Block an Spawn-Position
            int spawnY = FindHighestBlockAt(spawnWorldPosition.x, spawnWorldPosition.z);

            if (spawnY == -1)
            {
                Debug.LogWarning("No solid block found at spawn position, using default height");
                spawnY = 64; // Fallback
            }

            Vector3 playerSpawnPos = new Vector3(
                spawnWorldPosition.x + 0.5f,
                spawnY + 1.5f, // +1 für über Block, +0.5 für Player-Center
                spawnWorldPosition.z + 0.5f
            );

            SpawnPlayer(playerSpawnPos);
        }

        private System.Collections.IEnumerator ForceRebuildChunks(List<Vector3Int> chunkPositions)
        {
            Debug.Log($"Force rebuilding {chunkPositions.Count} chunk meshes...");

            Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
            if (activeDim == null)
                yield break;

            int rebuiltCount = 0;
            int batchSize = 10; // Rebuild 10 chunks pro Frame

            for (int i = 0; i < chunkPositions.Count; i++)
            {
                Voxels.Chunk chunk = activeDim.GetChunk(chunkPositions[i]);

                if (chunk != null && chunk.isDirty)
                {
                    // Force Mesh Rebuild via ChunkManager
                    chunkManager.MarkChunkDirty(chunkPositions[i]);
                    rebuiltCount++;

                    // Alle X Chunks ein Frame warten
                    if (rebuiltCount % batchSize == 0)
                    {
                        yield return null;
                    }
                }
            }

            Debug.Log($"Force rebuilt {rebuiltCount} chunk meshes");
        }

        private int FindHighestBlockAt(int worldX, int worldZ)
        {
            Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
            if (activeDim == null)
                return -1;

            // Suche von oben nach unten
            for (int y = 128; y >= 0; y--)
            {
                Vector3Int worldPos = new Vector3Int(worldX, y, worldZ);

                // Finde Chunk
                Vector3Int chunkPos = new Vector3Int(
                    Mathf.FloorToInt((float)worldX / Voxels.Chunk.CHUNK_SIZE),
                    Mathf.FloorToInt((float)y / Voxels.Chunk.CHUNK_SIZE),
                    Mathf.FloorToInt((float)worldZ / Voxels.Chunk.CHUNK_SIZE)
                );

                Voxels.Chunk chunk = activeDim.GetChunk(chunkPos);
                if (chunk == null)
                    continue;

                // Lokale Position im Chunk
                Vector3Int localPos = chunk.WorldToLocal(worldPos);

                if (chunk.IsInBounds(localPos.x, localPos.y, localPos.z))
                {
                    Voxels.VoxelData voxel = chunk.GetVoxel(localPos.x, localPos.y, localPos.z);

                    if (voxel.IsSolid)
                    {
                        Debug.Log($"Highest block at ({worldX}, {worldZ}) is at Y={y}");
                        return y;
                    }
                }
            }

            return -1;
        }

        private void SpawnPlayer(Vector3 position)
        {
            if (playerInstance != null)
            {
                Debug.Log($"Player already exists, moving to {position}");
                playerInstance.position = position;

                // Update FrustumChunkLoader
                if (chunkLoader != null)
                {
                    chunkLoader.SetPlayer(playerInstance);
                    chunkLoader.ForceUpdate();
                }

                // Update ColliderManager
                if (colliderManager != null)
                {
                    colliderManager.SetPlayer(playerInstance);
                }

                return;
            }

            if (playerPrefab != null)
            {
                playerInstance = Instantiate(playerPrefab, position, Quaternion.identity);
                Debug.Log($"Player spawned at {position}");

                // Setze Player Reference im ChunkManager
                if (chunkManager != null)
                {
                    chunkManager.player = playerInstance;
                }

                // Setze Player im FrustumChunkLoader
                if (chunkLoader != null)
                {
                    chunkLoader.SetPlayer(playerInstance);
                    chunkLoader.ForceUpdate();
                }

                // Setze Player im ColliderManager
                if (colliderManager != null)
                {
                    colliderManager.SetPlayer(playerInstance);
                }
            }
            else
            {
                Debug.LogWarning("No player prefab assigned, searching for existing player...");

                // Suche nach bestehendem Player
                Player.PlayerController player = Object.FindFirstObjectByType<Player.PlayerController>();
                if (player != null)
                {
                    playerInstance = player.transform;
                    playerInstance.position = position;
                    Debug.Log($"Found existing player, moved to {position}");

                    if (chunkManager != null)
                    {
                        chunkManager.player = playerInstance;
                    }

                    if (chunkLoader != null)
                    {
                        chunkLoader.SetPlayer(playerInstance);
                        chunkLoader.ForceUpdate();
                    }

                    if (colliderManager != null)
                    {
                        colliderManager.SetPlayer(playerInstance);
                    }
                }
                else
                {
                    Debug.LogError("No player found in scene and no prefab assigned!");
                }
            }
        }

        private void Update()
        {
            if (!isInitialized)
                return;

            // Auto-Save Timer
            if (enableAutoSave)
            {
                autoSaveTimer += Time.deltaTime;

                if (autoSaveTimer >= autoSaveInterval)
                {
                    AutoSave();
                    autoSaveTimer = 0f;
                }
            }
        }

        private void AutoSave()
        {
            Debug.Log("Auto-saving world...");

            bool success = saveLoadManager.SaveWorld("autosave");

            if (success)
            {
                Debug.Log("Auto-save complete");
            }
            else
            {
                Debug.LogError("Auto-save failed");
            }
        }

        public void SaveWorld(string saveName)
        {
            Debug.Log($"Saving world as '{saveName}'...");

            bool success = saveLoadManager.SaveWorld(saveName);

            if (success)
            {
                Debug.Log($"World saved as '{saveName}'");
            }
            else
            {
                Debug.LogError($"Failed to save world as '{saveName}'");
            }
        }

        public void LoadWorldByName(string saveName)
        {
            Debug.Log($"Loading world '{saveName}'...");

            // Speichere aktuelle Welt vor dem Laden
            if (isInitialized && enableAutoSave)
            {
                saveLoadManager.SaveWorld("autosave_pre_load");
            }

            bool success = saveLoadManager.LoadWorld(saveName);

            if (success)
            {
                Debug.Log($"World '{saveName}' loaded successfully");

                // Update seed
                Dimensions.Dimension activeDim = dimensionManager.GetActiveDimension();
                if (activeDim != null && activeDim.generator != null)
                {
                    seed = activeDim.generator.GetSeed();
                }
            }
            else
            {
                Debug.LogError($"Failed to load world '{saveName}'");
            }
        }

        public void CreateNewWorldWithSeed(int newSeed)
        {
            Debug.Log($"Creating new world with custom seed: {newSeed}");

            // Speichere aktuelle Welt
            if (isInitialized && enableAutoSave)
            {
                saveLoadManager.SaveWorld("autosave_pre_new");
            }

            seed = newSeed;

            // Unload aktuelle Welt (alle Dimensionen)
            UnloadCurrentWorld();

            // Erstelle neue Welt (inkl. Player Spawn)
            CreateNewWorld();
        }

        public void RespawnPlayer()
        {
            Debug.Log("Respawning player at spawn position...");

            // Finde höchsten Block an Spawn
            int spawnY = FindHighestBlockAt(spawnWorldPosition.x, spawnWorldPosition.z);

            if (spawnY == -1)
            {
                spawnY = 64;
            }

            Vector3 playerSpawnPos = new Vector3(
                spawnWorldPosition.x + 0.5f,
                spawnY + 1.5f,
                spawnWorldPosition.z + 0.5f
            );

            SpawnPlayer(playerSpawnPos);
        }

        public Vector3 GetPlayerSpawnPosition()
        {
            int spawnY = FindHighestBlockAt(spawnWorldPosition.x, spawnWorldPosition.z);

            if (spawnY == -1)
            {
                spawnY = 64;
            }

            return new Vector3(
                spawnWorldPosition.x + 0.5f,
                spawnY + 1.5f,
                spawnWorldPosition.z + 0.5f
            );
        }

        private void UnloadCurrentWorld()
        {
            Debug.Log("Unloading current world...");

            // Hole alle Dimensionen
            var allDimensions = dimensionManager.GetAllDimensions();

            foreach (var kvp in allDimensions)
            {
                Dimensions.Dimension dim = kvp.Value;
                dim.UnloadAllChunks();
            }

            Debug.Log("Current world unloaded");
        }

        public void SwitchToDimension(string dimensionID)
        {
            Debug.Log($"Switching to dimension: {dimensionID}");
            dimensionManager.SwitchDimension(dimensionID);
        }

        public int GetCurrentSeed()
        {
            return seed;
        }

        public float GetAutoSaveProgress()
        {
            if (!enableAutoSave)
                return 0f;

            return autoSaveTimer / autoSaveInterval;
        }

        public string GetAutoSaveTimeRemaining()
        {
            if (!enableAutoSave)
                return "Disabled";

            float remaining = autoSaveInterval - autoSaveTimer;
            int minutes = Mathf.FloorToInt(remaining / 60f);
            int seconds = Mathf.FloorToInt(remaining % 60f);

            return $"{minutes:00}:{seconds:00}";
        }

        private void OnApplicationQuit()
        {
            if (isInitialized && enableAutoSave)
            {
                Debug.Log("Application quitting, saving world...");
                saveLoadManager.SaveWorld("autosave");
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && isInitialized && enableAutoSave)
            {
                Debug.Log("Application paused, saving world...");
                saveLoadManager.SaveWorld("autosave");
            }
        }
    }
}