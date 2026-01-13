using System;
using UnityEngine;

namespace Pixension.SaveSystem
{
    [Serializable]
    public class DimensionSaveData
    {
        public string dimensionID;
        public string generatorID;
        public Voxels.ChunkData[] modifiedChunks;

        public DimensionSaveData()
        {
            modifiedChunks = new Voxels.ChunkData[0];
        }

        public DimensionSaveData(string dimID, string genID)
        {
            dimensionID = dimID;
            generatorID = genID;
            modifiedChunks = new Voxels.ChunkData[0];
        }

        public int GetTotalChunks()
        {
            return modifiedChunks.Length;
        }

        public int GetTotalVoxels()
        {
            int total = 0;
            foreach (var chunk in modifiedChunks)
            {
                total += chunk.modifiedVoxels.Length;
            }
            return total;
        }
    }

    [Serializable]
    public class WorldSaveData
    {
        public int seed;
        public string activeDimensionID;
        public DimensionSaveData[] dimensions;
        public string saveVersion = "1.0";
        public long saveTimestamp;

        public WorldSaveData()
        {
            dimensions = new DimensionSaveData[0];
            saveTimestamp = System.DateTime.Now.Ticks;
        }

        public WorldSaveData(int worldSeed, string activeDimID)
        {
            seed = worldSeed;
            activeDimensionID = activeDimID;
            dimensions = new DimensionSaveData[0];
            saveVersion = "1.0";
            saveTimestamp = System.DateTime.Now.Ticks;
        }

        public DimensionSaveData GetDimension(string dimensionID)
        {
            foreach (var dim in dimensions)
            {
                if (dim.dimensionID == dimensionID)
                    return dim;
            }
            return null;
        }

        public int GetTotalChunks()
        {
            int total = 0;
            foreach (var dim in dimensions)
            {
                total += dim.GetTotalChunks();
            }
            return total;
        }

        public int GetTotalVoxels()
        {
            int total = 0;
            foreach (var dim in dimensions)
            {
                total += dim.GetTotalVoxels();
            }
            return total;
        }

        public System.DateTime GetSaveDateTime()
        {
            return new System.DateTime(saveTimestamp);
        }
    }

    [Serializable]
    public class SaveFileInfo
    {
        public string saveName;
        public string saveVersion;
        public long saveTimestamp;
        public int seed;
        public int totalChunks;
        public int totalVoxels;
        public long fileSizeBytes;

        public System.DateTime GetSaveDateTime()
        {
            return new System.DateTime(saveTimestamp);
        }

        public string GetFileSizeFormatted()
        {
            if (fileSizeBytes < 1024)
                return $"{fileSizeBytes} B";
            else if (fileSizeBytes < 1024 * 1024)
                return $"{fileSizeBytes / 1024f:F2} KB";
            else
                return $"{fileSizeBytes / (1024f * 1024f):F2} MB";
        }
    }
}