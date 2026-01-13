using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Voxels
{
    /// <summary>
    /// Global registry for block definitions. Allows WorldGenerators and other systems
    /// to register and share block types without duplicating definitions.
    /// </summary>
    public class BlockRegistry : MonoBehaviour
    {
        private static BlockRegistry instance;
        public static BlockRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("BlockRegistry");
                    instance = go.AddComponent<BlockRegistry>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<string, VoxelData> registeredBlocks = new Dictionary<string, VoxelData>();
        private bool initialized = false;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterDefaultBlocks();
        }

        /// <summary>
        /// Registers default blocks that are commonly used across all generators
        /// </summary>
        private void RegisterDefaultBlocks()
        {
            if (initialized) return;

            // Air and Water are already defined in VoxelData as static properties
            // but we register them here for consistency
            RegisterBlock("air", VoxelData.Air);
            RegisterBlock("water", VoxelData.Water);

            // Bedrock
            RegisterBlock("bedrock", new VoxelData(
                VoxelType.Solid,
                new Color(0.12f, 0.12f, 0.12f)
            ));

            // Stone variations
            RegisterBlock("stone", new VoxelData(
                VoxelType.Solid,
                new Color(0.42f, 0.42f, 0.42f)
            ));

            RegisterBlock("stone_dark", new VoxelData(
                VoxelType.Solid,
                new Color(0.35f, 0.35f, 0.35f)
            ));

            RegisterBlock("stone_light", new VoxelData(
                VoxelType.Solid,
                new Color(0.52f, 0.52f, 0.52f)
            ));

            // Dirt variations
            RegisterBlock("dirt", new VoxelData(
                VoxelType.Solid,
                new Color(0.5f, 0.32f, 0.18f)
            ));

            RegisterBlock("dirt_dark", new VoxelData(
                VoxelType.Solid,
                new Color(0.45f, 0.28f, 0.15f)
            ));

            // Grass variations
            RegisterBlock("grass", new VoxelData(
                VoxelType.Solid,
                new Color(0.25f, 0.65f, 0.3f)
            ));

            RegisterBlock("grass_dark", new VoxelData(
                VoxelType.Solid,
                new Color(0.2f, 0.55f, 0.25f)
            ));

            RegisterBlock("grass_light", new VoxelData(
                VoxelType.Solid,
                new Color(0.35f, 0.75f, 0.35f)
            ));

            RegisterBlock("grass_dry", new VoxelData(
                VoxelType.Solid,
                new Color(0.4f, 0.6f, 0.35f)
            ));

            // Sand variations
            RegisterBlock("sand", new VoxelData(
                VoxelType.Solid,
                new Color(0.76f, 0.7f, 0.5f)
            ));

            RegisterBlock("sand_light", new VoxelData(
                VoxelType.Solid,
                new Color(0.9f, 0.85f, 0.65f)
            ));

            RegisterBlock("sand_dark", new VoxelData(
                VoxelType.Solid,
                new Color(0.65f, 0.6f, 0.42f)
            ));

            // Snow
            RegisterBlock("snow", new VoxelData(
                VoxelType.Solid,
                new Color(0.95f, 0.95f, 0.98f)
            ));

            // Wood
            RegisterBlock("wood", new VoxelData(
                VoxelType.Solid,
                new Color(0.4f, 0.25f, 0.15f)
            ));

            RegisterBlock("wood_light", new VoxelData(
                VoxelType.Solid,
                new Color(0.55f, 0.35f, 0.2f)
            ));

            // Leaves
            RegisterBlock("leaves", new VoxelData(
                VoxelType.Solid,
                new Color(0.2f, 0.6f, 0.2f)
            ));

            RegisterBlock("leaves_dark", new VoxelData(
                VoxelType.Solid,
                new Color(0.15f, 0.5f, 0.15f)
            ));

            initialized = true;
            Debug.Log($"BlockRegistry initialized with {registeredBlocks.Count} default blocks");
        }

        /// <summary>
        /// Registers a new block type. If the block ID already exists, it will be overwritten.
        /// </summary>
        /// <param name="blockId">Unique identifier for the block (e.g., "stone", "grass", "my_generator:custom_block")</param>
        /// <param name="voxelData">The voxel data for this block</param>
        public void RegisterBlock(string blockId, VoxelData voxelData)
        {
            if (string.IsNullOrEmpty(blockId))
            {
                Debug.LogError("Cannot register block with null or empty ID");
                return;
            }

            if (registeredBlocks.ContainsKey(blockId))
            {
                Debug.LogWarning($"Block '{blockId}' is already registered. Overwriting...");
            }

            registeredBlocks[blockId] = voxelData;
        }

        /// <summary>
        /// Gets a registered block by ID. Returns Air if not found.
        /// </summary>
        /// <param name="blockId">The block identifier</param>
        /// <returns>The VoxelData for the block, or Air if not found</returns>
        public VoxelData GetBlock(string blockId)
        {
            if (string.IsNullOrEmpty(blockId))
            {
                Debug.LogWarning("Attempted to get block with null or empty ID");
                return VoxelData.Air;
            }

            if (registeredBlocks.TryGetValue(blockId, out VoxelData voxelData))
            {
                return voxelData;
            }

            Debug.LogWarning($"Block '{blockId}' not found in registry. Returning Air.");
            return VoxelData.Air;
        }

        /// <summary>
        /// Checks if a block is registered
        /// </summary>
        public bool HasBlock(string blockId)
        {
            return registeredBlocks.ContainsKey(blockId);
        }

        /// <summary>
        /// Gets all registered block IDs
        /// </summary>
        public IEnumerable<string> GetAllBlockIds()
        {
            return registeredBlocks.Keys;
        }

        /// <summary>
        /// Gets the total number of registered blocks
        /// </summary>
        public int GetBlockCount()
        {
            return registeredBlocks.Count;
        }

        /// <summary>
        /// Creates a variation of an existing block with a color multiplier
        /// </summary>
        public VoxelData CreateColorVariation(string baseBlockId, float colorMultiplier)
        {
            VoxelData baseBlock = GetBlock(baseBlockId);
            Color newColor = baseBlock.color * colorMultiplier;
            return new VoxelData(baseBlock.type, newColor);
        }

        /// <summary>
        /// Creates a variation of an existing block by lerping between two colors
        /// </summary>
        public VoxelData CreateColorLerp(string baseBlockId, Color targetColor, float t)
        {
            VoxelData baseBlock = GetBlock(baseBlockId);
            Color newColor = Color.Lerp(baseBlock.color, targetColor, t);
            return new VoxelData(baseBlock.type, newColor);
        }

        /// <summary>
        /// Unregisters a block (use with caution)
        /// </summary>
        public void UnregisterBlock(string blockId)
        {
            if (registeredBlocks.Remove(blockId))
            {
                Debug.Log($"Block '{blockId}' unregistered");
            }
            else
            {
                Debug.LogWarning($"Attempted to unregister non-existent block '{blockId}'");
            }
        }

        /// <summary>
        /// Clears all registered blocks (use with caution - will break generators)
        /// </summary>
        public void ClearRegistry()
        {
            registeredBlocks.Clear();
            initialized = false;
            Debug.LogWarning("Block registry cleared. Call RegisterDefaultBlocks() to reinitialize.");
        }
    }
}
