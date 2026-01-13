using UnityEngine;

namespace Pixension.WorldGen.Biomes
{
    /// <summary>
    /// Contains all properties and characteristics of a biome
    /// </summary>
    public class BiomeData
    {
        // Identification
        public BiomeType type;
        public string name;

        // Climate parameters (0-1 range, like Minecraft)
        public float temperature;  // 0 = frozen, 0.5 = temperate, 1 = hot
        public float humidity;     // 0 = dry, 0.5 = normal, 1 = wet

        // Terrain generation modifiers
        public float baseHeight;          // Base terrain height offset
        public float heightVariation;     // How much terrain varies
        public float hillFrequency;       // Frequency of hills
        public float mountainFrequency;   // Frequency of mountains
        public float erosionStrength;     // How eroded/smooth the terrain is

        // Biome scale
        public float biomeScale = 1.0f;   // Scale multiplier for this biome (1.0 = normal, 2.0 = twice as large)

        // Visual properties
        public Color grassColor;
        public Color foliageColor;
        public Color waterColor;
        public Color skyTint;

        // Block types for terrain layers
        public string topBlockID = "grass";
        public string fillerBlockID = "dirt";
        public string stoneBlockID = "stone";
        public string beachBlockID = "sand";

        // Vegetation
        public float treeChance;
        public float tallGrassChance;
        public float flowerChance;

        // Constructor
        public BiomeData(BiomeType type, string name, float temperature, float humidity)
        {
            this.type = type;
            this.name = name;
            this.temperature = temperature;
            this.humidity = humidity;

            // Default values
            this.baseHeight = 0f;
            this.heightVariation = 1f;
            this.hillFrequency = 1f;
            this.mountainFrequency = 0.1f;
            this.erosionStrength = 0.5f;
            this.biomeScale = 1.0f;

            this.grassColor = Color.green;
            this.foliageColor = Color.green;
            this.waterColor = new Color(0.2f, 0.3f, 0.8f);
            this.skyTint = Color.white;

            this.treeChance = 0.01f;
            this.tallGrassChance = 0.1f;
            this.flowerChance = 0.05f;
        }

        /// <summary>
        /// Gets the effective scale for noise calculations
        /// Larger biomes use larger scale values
        /// </summary>
        public float GetNoiseScale(float baseScale)
        {
            return baseScale * biomeScale;
        }
    }
}
