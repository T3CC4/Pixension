using System;
using UnityEngine;

namespace Pixension.Voxels
{
    [Serializable]
    public class BlockEntitySaveData
    {
        public string entityID;
        public Vector3Int worldPosition;
        public int facingIndex; // Direction als int gespeichert

        public BlockEntitySaveData(string id, Vector3Int pos, Utilities.Direction facing)
        {
            entityID = id;
            worldPosition = pos;
            facingIndex = (int)facing;
        }

        public Utilities.Direction GetFacing()
        {
            return (Utilities.Direction)facingIndex;
        }
    }

    [Serializable]
    public class ChunkData
    {
        public Vector3Int chunkPosition;
        public VoxelData[] modifiedVoxels;
        public Vector3Int[] modifiedPositions;
        public BlockEntitySaveData[] entities;

        public ChunkData()
        {
            modifiedVoxels = new VoxelData[0];
            modifiedPositions = new Vector3Int[0];
            entities = new BlockEntitySaveData[0];
        }

        public ChunkData(Vector3Int position)
        {
            chunkPosition = position;
            modifiedVoxels = new VoxelData[0];
            modifiedPositions = new Vector3Int[0];
            entities = new BlockEntitySaveData[0];
        }

        public bool HasData()
        {
            return modifiedVoxels.Length > 0 || entities.Length > 0;
        }

        public int GetTotalSize()
        {
            return modifiedVoxels.Length + entities.Length;
        }
    }
}