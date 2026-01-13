using System;
using UnityEngine;

namespace Pixension.Voxels
{
    public enum VoxelType
    {
        Air,
        Solid,
        Water
    }

    [Serializable]
    public struct VoxelData
    {
        public VoxelType type;
        public Color color;

        public VoxelData(VoxelType type, Color color)
        {
            this.type = type;
            this.color = color;
        }

        public static VoxelData Air => new VoxelData(VoxelType.Air, Color.clear);
        public static VoxelData Water => new VoxelData(VoxelType.Water, new Color(0.2f, 0.4f, 0.8f, 0.6f));

        public bool IsSolid => type == VoxelType.Solid;
        public bool IsTransparent => type == VoxelType.Water || type == VoxelType.Air;
        public bool IsLiquid => type == VoxelType.Water;
    }
}