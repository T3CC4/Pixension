using System;
using UnityEngine;

namespace Pixension.Voxels
{
    public enum VoxelType
    {
        Air,
        Solid
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

        public bool IsSolid => type == VoxelType.Solid;
    }
}