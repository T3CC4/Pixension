using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Pixension.SaveSystem
{
    public class SaveLoadManager : MonoBehaviour
    {
        private static SaveLoadManager instance;
        public static SaveLoadManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SaveLoadManager");
                    instance = go.AddComponent<SaveLoadManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        public string savePath;
        public bool useCompression = true;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Application.persistentDataPath + "/Saves/";
            EnsureSaveDirectoryExists();
        }

        private void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                Debug.Log($"Created save directory: {savePath}");
            }
        }

        public bool SaveWorld(string saveName)
        {
            try
            {
                EnsureSaveDirectoryExists();

                // Sammle Daten
                Dimensions.DimensionManager dimManager = Dimensions.DimensionManager.Instance;
                if (dimManager == null)
                {
                    Debug.LogError("DimensionManager not found");
                    return false;
                }

                Dimensions.Dimension activeDim = dimManager.GetActiveDimension();
                if (activeDim == null)
                {
                    Debug.LogError("No active dimension");
                    return false;
                }

                // Erstelle WorldSaveData
                WorldSaveData worldData = new WorldSaveData(
                    GetWorldSeed(),
                    activeDim.dimensionID
                );

                // Sammle Dimensionen
                Dictionary<string, Dimensions.Dimension> allDimensions = dimManager.GetAllDimensions();
                List<DimensionSaveData> dimensionDataList = new List<DimensionSaveData>();

                foreach (var kvp in allDimensions)
                {
                    Dimensions.Dimension dimension = kvp.Value;
                    DimensionSaveData dimData = SerializeDimension(dimension);

                    // Nur speichern wenn modifizierte Chunks vorhanden
                    if (dimData.modifiedChunks.Length > 0)
                    {
                        dimensionDataList.Add(dimData);
                    }
                }

                worldData.dimensions = dimensionDataList.ToArray();

                // Serialisiere zu JSON
                string json = JsonUtility.ToJson(worldData, true);

                // Speichere
                string filePath = GetSaveFilePath(saveName);

                if (useCompression)
                {
                    SaveCompressed(filePath, json);
                }
                else
                {
                    File.WriteAllText(filePath, json);
                }

                Debug.Log($"World saved: {filePath}");
                Debug.Log($"Dimensions: {worldData.dimensions.Length}, Total Chunks: {worldData.GetTotalChunks()}, Total Voxels: {worldData.GetTotalVoxels()}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save world: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        public bool LoadWorld(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);

                if (!File.Exists(filePath))
                {
                    Debug.LogError($"Save file not found: {filePath}");
                    return false;
                }

                // Lade JSON
                string json;
                if (useCompression)
                {
                    json = LoadCompressed(filePath);
                }
                else
                {
                    json = File.ReadAllText(filePath);
                }

                // Deserialisiere
                WorldSaveData worldData = JsonUtility.FromJson<WorldSaveData>(json);

                if (worldData == null)
                {
                    Debug.LogError("Failed to deserialize world data");
                    return false;
                }

                // Initialisiere DimensionManager mit Seed
                Dimensions.DimensionManager dimManager = Dimensions.DimensionManager.Instance;
                dimManager.Initialize(worldData.seed);

                // Lade Dimensionen
                foreach (DimensionSaveData dimData in worldData.dimensions)
                {
                    LoadDimension(dimData);
                }

                // Wechsle zu aktiver Dimension
                if (!string.IsNullOrEmpty(worldData.activeDimensionID))
                {
                    dimManager.SwitchDimension(worldData.activeDimensionID);
                }

                Debug.Log($"World loaded: {saveName}");
                Debug.Log($"Seed: {worldData.seed}, Dimensions: {worldData.dimensions.Length}, Total Chunks: {worldData.GetTotalChunks()}");

                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load world: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        private DimensionSaveData SerializeDimension(Dimensions.Dimension dimension)
        {
            DimensionSaveData dimData = new DimensionSaveData(
                dimension.dimensionID,
                dimension.generator.GetGeneratorID()
            );

            // Sammle modifizierte Chunks
            List<Voxels.ChunkData> chunkDataList = new List<Voxels.ChunkData>();

            foreach (var kvp in dimension.chunks)
            {
                Voxels.Chunk chunk = kvp.Value;

                // Nur Chunks mit Modifikationen speichern
                if (chunk.HasModifications())
                {
                    Voxels.ChunkData chunkData = chunk.Serialize();
                    if (chunkData.HasData())
                    {
                        chunkDataList.Add(chunkData);
                    }
                }
            }

            dimData.modifiedChunks = chunkDataList.ToArray();

            Debug.Log($"Serialized dimension {dimension.dimensionID}: {dimData.modifiedChunks.Length} modified chunks");

            return dimData;
        }

        private void LoadDimension(DimensionSaveData dimData)
        {
            Dimensions.DimensionManager dimManager = Dimensions.DimensionManager.Instance;

            // Erstelle oder hole Dimension
            Dimensions.Dimension dimension = dimManager.GetDimension(dimData.dimensionID);
            if (dimension == null)
            {
                dimension = dimManager.CreateDimension(dimData.dimensionID, dimData.generatorID);
            }

            if (dimension == null)
            {
                Debug.LogError($"Failed to create/load dimension: {dimData.dimensionID}");
                return;
            }

            // Lade modifizierte Chunks
            foreach (Voxels.ChunkData chunkData in dimData.modifiedChunks)
            {
                // Hole oder erstelle Chunk
                Voxels.Chunk chunk = dimension.GetOrCreateChunk(chunkData.chunkPosition);

                if (chunk != null)
                {
                    chunk.Deserialize(chunkData);
                }
            }

            Debug.Log($"Loaded dimension {dimData.dimensionID}: {dimData.modifiedChunks.Length} chunks");
        }

        private void SaveCompressed(string filePath, string json)
        {
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
            {
                gzipStream.Write(jsonBytes, 0, jsonBytes.Length);
            }
        }

        private string LoadCompressed(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
            using (GZipStream gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            using (MemoryStream memoryStream = new MemoryStream())
            {
                gzipStream.CopyTo(memoryStream);
                byte[] jsonBytes = memoryStream.ToArray();
                return System.Text.Encoding.UTF8.GetString(jsonBytes);
            }
        }

        public List<SaveFileInfo> GetSaveFiles()
        {
            EnsureSaveDirectoryExists();

            List<SaveFileInfo> saveFiles = new List<SaveFileInfo>();
            string[] files = Directory.GetFiles(savePath, "*.sav");

            foreach (string file in files)
            {
                try
                {
                    string saveName = Path.GetFileNameWithoutExtension(file);
                    SaveFileInfo info = GetSaveFileInfo(saveName);

                    if (info != null)
                    {
                        saveFiles.Add(info);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to read save file info: {file}\n{e.Message}");
                }
            }

            // Sortiere nach Datum (neueste zuerst)
            saveFiles.Sort((a, b) => b.saveTimestamp.CompareTo(a.saveTimestamp));

            return saveFiles;
        }

        public SaveFileInfo GetSaveFileInfo(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);

                if (!File.Exists(filePath))
                    return null;

                FileInfo fileInfo = new FileInfo(filePath);

                // Lade nur Header-Daten
                string json;
                if (useCompression)
                {
                    json = LoadCompressed(filePath);
                }
                else
                {
                    json = File.ReadAllText(filePath);
                }

                WorldSaveData worldData = JsonUtility.FromJson<WorldSaveData>(json);

                SaveFileInfo info = new SaveFileInfo
                {
                    saveName = saveName,
                    saveVersion = worldData.saveVersion,
                    saveTimestamp = worldData.saveTimestamp,
                    seed = worldData.seed,
                    totalChunks = worldData.GetTotalChunks(),
                    totalVoxels = worldData.GetTotalVoxels(),
                    fileSizeBytes = fileInfo.Length
                };

                return info;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to get save file info: {e.Message}");
                return null;
            }
        }

        public bool DeleteSave(string saveName)
        {
            try
            {
                string filePath = GetSaveFilePath(saveName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"Deleted save: {saveName}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Save file not found: {saveName}");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to delete save: {e.Message}");
                return false;
            }
        }

        public bool SaveExists(string saveName)
        {
            string filePath = GetSaveFilePath(saveName);
            return File.Exists(filePath);
        }

        private string GetSaveFilePath(string saveName)
        {
            return Path.Combine(savePath, saveName + ".sav");
        }

        private int GetWorldSeed()
        {
            // Hole Seed vom DimensionManager
            Dimensions.DimensionManager dimManager = Dimensions.DimensionManager.Instance;
            Dimensions.Dimension activeDim = dimManager.GetActiveDimension();

            if (activeDim != null && activeDim.generator != null)
            {
                return activeDim.generator.GetSeed();
            }

            return 0;
        }

        public string GetSavePath()
        {
            return savePath;
        }
    }
}