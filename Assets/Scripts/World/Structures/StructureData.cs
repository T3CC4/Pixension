using System;
using UnityEngine;

namespace Pixension.Structures
{
    public enum StructureType
    {
        Environmental,
        Architecture
    }

    [Serializable]
    public struct BlockEntityPlacement
    {
        public string entityID;
        public Vector3Int localPosition;
        public Utilities.Direction facing;

        public BlockEntityPlacement(string entityID, Vector3Int localPosition, Utilities.Direction facing)
        {
            this.entityID = entityID;
            this.localPosition = localPosition;
            this.facing = facing;
        }
    }

    [Serializable]
    public struct MobSpawnEntry
    {
        public string mobID;
        public int initialCount;
        public int maxCount;
        public float spawnInterval;

        public MobSpawnEntry(string mobID, int initialCount, int maxCount, float spawnInterval)
        {
            this.mobID = mobID;
            this.initialCount = initialCount;
            this.maxCount = maxCount;
            this.spawnInterval = spawnInterval;
        }
    }

    [Serializable]
    public struct ArchitectureData
    {
        public Vector3Int spawnRangeMin;
        public Vector3Int spawnRangeMax;
        public MobSpawnEntry[] mobs;

        public ArchitectureData(Vector3Int spawnRangeMin, Vector3Int spawnRangeMax, MobSpawnEntry[] mobs)
        {
            this.spawnRangeMin = spawnRangeMin;
            this.spawnRangeMax = spawnRangeMax;
            this.mobs = mobs;
        }
    }

    [Serializable]
    public class StructureData
    {
        public string structureID;
        public string displayName;
        public StructureType type;
        public Vector3Int size;
        public Voxels.VoxelData[] voxels;
        public BlockEntityPlacement[] blockEntities;
        public int spawnWeight;
        public bool[] allowedRotations;
        public ArchitectureData architecture;

        public StructureData()
        {
            structureID = "";
            displayName = "";
            type = StructureType.Environmental;
            size = Vector3Int.zero;
            voxels = new Voxels.VoxelData[0];
            blockEntities = new BlockEntityPlacement[0];
            spawnWeight = 1;
            allowedRotations = new bool[4] { true, true, true, true };
            architecture = new ArchitectureData(Vector3Int.zero, Vector3Int.zero, new MobSpawnEntry[0]);
        }

        public StructureData(string id, string name, StructureType structureType, Vector3Int structureSize)
        {
            structureID = id;
            displayName = name;
            type = structureType;
            size = structureSize;
            voxels = new Voxels.VoxelData[size.x * size.y * size.z];
            blockEntities = new BlockEntityPlacement[0];
            spawnWeight = 1;
            allowedRotations = new bool[4] { true, true, true, true };
            architecture = new ArchitectureData(Vector3Int.zero, Vector3Int.zero, new MobSpawnEntry[0]);

            for (int i = 0; i < voxels.Length; i++)
            {
                voxels[i] = Voxels.VoxelData.Air;
            }
        }

        public Voxels.VoxelData GetVoxel(int x, int y, int z)
        {
            if (x < 0 || x >= size.x || y < 0 || y >= size.y || z < 0 || z >= size.z)
            {
                return Voxels.VoxelData.Air;
            }

            int index = GetIndex(x, y, z);
            return voxels[index];
        }

        public void SetVoxel(int x, int y, int z, Voxels.VoxelData data)
        {
            if (x < 0 || x >= size.x || y < 0 || y >= size.y || z < 0 || z >= size.z)
            {
                return;
            }

            int index = GetIndex(x, y, z);
            voxels[index] = data;
        }

        public int GetIndex(int x, int y, int z)
        {
            return x + y * size.x + z * size.x * size.y;
        }
    }
}