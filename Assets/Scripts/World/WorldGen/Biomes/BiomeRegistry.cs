using System.Collections.Generic;
using UnityEngine;

namespace Pixension.WorldGen.Biomes
{
    /// <summary>
    /// Singleton registry for all biome definitions
    /// </summary>
    public class BiomeRegistry
    {
        private static BiomeRegistry instance;
        public static BiomeRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BiomeRegistry();
                    instance.RegisterDefaultBiomes();
                }
                return instance;
            }
        }

        private Dictionary<BiomeType, BiomeData> biomes = new Dictionary<BiomeType, BiomeData>();

        public void RegisterBiome(BiomeData biome)
        {
            biomes[biome.type] = biome;
        }

        public BiomeData GetBiome(BiomeType type)
        {
            if (biomes.TryGetValue(type, out BiomeData biome))
            {
                return biome;
            }

            Debug.LogWarning($"Biome {type} not registered, returning Plains as default");
            return biomes[BiomeType.Plains];
        }

        /// <summary>
        /// Registers all default biomes with their properties
        /// Inspired by Minecraft's biome system
        /// </summary>
        private void RegisterDefaultBiomes()
        {
            // === OCEANS ===
            RegisterOcean();
            RegisterDeepOcean();
            RegisterFrozenOcean();

            // === COLD BIOMES ===
            RegisterTundra();
            RegisterTaiga();
            RegisterSnowyMountains();
            RegisterIcePlains();

            // === TEMPERATE BIOMES ===
            RegisterPlains();
            RegisterForest();
            RegisterBirchForest();
            RegisterFlowerForest();
            RegisterMountains();
            RegisterHills();
            RegisterRiver();

            // === WARM BIOMES ===
            RegisterSavanna();
            RegisterDesert();
            RegisterMesa();
            RegisterBadlands();

            // === WET BIOMES ===
            RegisterSwamp();
            RegisterJungle();
            RegisterRainforest();

            // === SPECIAL BIOMES ===
            RegisterMushroomIsland();
            RegisterBeach();
            RegisterSnowyBeach();
            RegisterStoneBeach();

            Debug.Log($"BiomeRegistry: Registered {biomes.Count} biomes");
        }

        // === OCEAN BIOMES ===

        private void RegisterOcean()
        {
            var biome = new BiomeData(BiomeType.Ocean, "Ocean", 0.5f, 0.5f)
            {
                baseHeight = -20f,
                heightVariation = 0.2f,
                hillFrequency = 0.1f,
                mountainFrequency = 0f,
                erosionStrength = 0.9f,
                biomeScale = 2.0f,
                waterColor = new Color(0.15f, 0.25f, 0.7f),
                topBlockID = "sand",
                fillerBlockID = "sand"
            };
            RegisterBiome(biome);
        }

        private void RegisterDeepOcean()
        {
            var biome = new BiomeData(BiomeType.DeepOcean, "Deep Ocean", 0.5f, 0.5f)
            {
                baseHeight = -40f,
                heightVariation = 0.3f,
                hillFrequency = 0.05f,
                mountainFrequency = 0f,
                erosionStrength = 0.95f,
                biomeScale = 3.0f,
                waterColor = new Color(0.1f, 0.15f, 0.5f),
                topBlockID = "stone_dark",
                fillerBlockID = "stone"
            };
            RegisterBiome(biome);
        }

        private void RegisterFrozenOcean()
        {
            var biome = new BiomeData(BiomeType.FrozenOcean, "Frozen Ocean", 0.0f, 0.5f)
            {
                baseHeight = -15f,
                heightVariation = 0.2f,
                hillFrequency = 0.1f,
                mountainFrequency = 0f,
                erosionStrength = 0.9f,
                biomeScale = 2.0f,
                waterColor = new Color(0.2f, 0.3f, 0.8f),
                skyTint = new Color(0.9f, 0.95f, 1.0f),
                topBlockID = "sand",
                fillerBlockID = "sand"
            };
            RegisterBiome(biome);
        }

        // === COLD BIOMES ===

        private void RegisterTundra()
        {
            var biome = new BiomeData(BiomeType.Tundra, "Tundra", 0.1f, 0.3f)
            {
                baseHeight = 5f,
                heightVariation = 0.4f,
                hillFrequency = 0.3f,
                mountainFrequency = 0.05f,
                erosionStrength = 0.6f,
                biomeScale = 1.5f,
                grassColor = new Color(0.7f, 0.8f, 0.7f),
                topBlockID = "grass_light",
                treeChance = 0.001f,
                tallGrassChance = 0.02f
            };
            RegisterBiome(biome);
        }

        private void RegisterTaiga()
        {
            var biome = new BiomeData(BiomeType.Taiga, "Taiga", 0.2f, 0.5f)
            {
                baseHeight = 8f,
                heightVariation = 0.6f,
                hillFrequency = 0.5f,
                mountainFrequency = 0.1f,
                erosionStrength = 0.4f,
                biomeScale = 1.2f,
                grassColor = new Color(0.6f, 0.75f, 0.6f),
                foliageColor = new Color(0.4f, 0.6f, 0.4f),
                topBlockID = "grass",
                treeChance = 0.05f,
                tallGrassChance = 0.05f
            };
            RegisterBiome(biome);
        }

        private void RegisterSnowyMountains()
        {
            var biome = new BiomeData(BiomeType.SnowyMountains, "Snowy Mountains", 0.0f, 0.4f)
            {
                baseHeight = 50f,
                heightVariation = 2.5f,
                hillFrequency = 0.8f,
                mountainFrequency = 0.8f,
                erosionStrength = 0.2f,
                biomeScale = 1.8f,
                grassColor = Color.white,
                topBlockID = "grassland:stone_mountain",
                fillerBlockID = "stone",
                stoneBlockID = "stone",
                treeChance = 0f
            };
            RegisterBiome(biome);
        }

        private void RegisterIcePlains()
        {
            var biome = new BiomeData(BiomeType.IcePlains, "Ice Plains", 0.0f, 0.5f)
            {
                baseHeight = 2f,
                heightVariation = 0.3f,
                hillFrequency = 0.2f,
                mountainFrequency = 0f,
                erosionStrength = 0.8f,
                biomeScale = 1.5f,
                grassColor = Color.white,
                waterColor = new Color(0.3f, 0.4f, 0.9f),
                topBlockID = "sand_light",
                treeChance = 0f
            };
            RegisterBiome(biome);
        }

        // === TEMPERATE BIOMES ===

        private void RegisterPlains()
        {
            var biome = new BiomeData(BiomeType.Plains, "Plains", 0.6f, 0.4f)
            {
                baseHeight = 3f,
                heightVariation = 0.4f,
                hillFrequency = 0.2f,
                mountainFrequency = 0f,
                erosionStrength = 0.7f,
                biomeScale = 1.0f,
                grassColor = new Color(0.6f, 0.8f, 0.3f),
                topBlockID = "grass",
                treeChance = 0.001f,
                tallGrassChance = 0.3f,
                flowerChance = 0.1f
            };
            RegisterBiome(biome);
        }

        private void RegisterForest()
        {
            var biome = new BiomeData(BiomeType.Forest, "Forest", 0.6f, 0.6f)
            {
                baseHeight = 5f,
                heightVariation = 0.7f,
                hillFrequency = 0.4f,
                mountainFrequency = 0.05f,
                erosionStrength = 0.5f,
                biomeScale = 1.0f,
                grassColor = new Color(0.5f, 0.75f, 0.3f),
                foliageColor = new Color(0.3f, 0.7f, 0.2f),
                topBlockID = "grass_dark",
                treeChance = 0.08f,
                tallGrassChance = 0.2f
            };
            RegisterBiome(biome);
        }

        private void RegisterBirchForest()
        {
            var biome = new BiomeData(BiomeType.BirchForest, "Birch Forest", 0.6f, 0.5f)
            {
                baseHeight = 4f,
                heightVariation = 0.5f,
                hillFrequency = 0.3f,
                mountainFrequency = 0.02f,
                erosionStrength = 0.6f,
                biomeScale = 0.9f,
                grassColor = new Color(0.6f, 0.8f, 0.4f),
                foliageColor = new Color(0.5f, 0.8f, 0.3f),
                topBlockID = "grass",
                treeChance = 0.06f,
                tallGrassChance = 0.15f
            };
            RegisterBiome(biome);
        }

        private void RegisterFlowerForest()
        {
            var biome = new BiomeData(BiomeType.FlowerForest, "Flower Forest", 0.6f, 0.7f)
            {
                baseHeight = 4f,
                heightVariation = 0.6f,
                hillFrequency = 0.3f,
                mountainFrequency = 0.03f,
                erosionStrength = 0.6f,
                biomeScale = 0.8f,
                grassColor = new Color(0.5f, 0.85f, 0.4f),
                foliageColor = new Color(0.4f, 0.8f, 0.3f),
                topBlockID = "grass",
                treeChance = 0.04f,
                tallGrassChance = 0.25f,
                flowerChance = 0.4f
            };
            RegisterBiome(biome);
        }

        private void RegisterMountains()
        {
            var biome = new BiomeData(BiomeType.Mountains, "Mountains", 0.4f, 0.4f)
            {
                baseHeight = 60f,
                heightVariation = 3.0f,
                hillFrequency = 0.9f,
                mountainFrequency = 0.9f,
                erosionStrength = 0.1f,
                biomeScale = 2.5f,
                grassColor = new Color(0.6f, 0.7f, 0.5f),
                topBlockID = "grassland:stone_mountain",
                fillerBlockID = "stone",
                stoneBlockID = "stone",
                treeChance = 0.01f
            };
            RegisterBiome(biome);
        }

        private void RegisterHills()
        {
            var biome = new BiomeData(BiomeType.Hills, "Hills", 0.5f, 0.5f)
            {
                baseHeight = 15f,
                heightVariation = 1.2f,
                hillFrequency = 0.7f,
                mountainFrequency = 0.2f,
                erosionStrength = 0.3f,
                biomeScale = 1.3f,
                grassColor = new Color(0.6f, 0.75f, 0.4f),
                topBlockID = "grass",
                treeChance = 0.03f,
                tallGrassChance = 0.15f
            };
            RegisterBiome(biome);
        }

        private void RegisterRiver()
        {
            var biome = new BiomeData(BiomeType.River, "River", 0.5f, 0.8f)
            {
                baseHeight = -5f,
                heightVariation = 0.2f,
                hillFrequency = 0.1f,
                mountainFrequency = 0f,
                erosionStrength = 0.95f,
                biomeScale = 0.5f,
                waterColor = new Color(0.2f, 0.4f, 0.8f),
                topBlockID = "sand",
                fillerBlockID = "sand",
                treeChance = 0.02f
            };
            RegisterBiome(biome);
        }

        // === WARM BIOMES ===

        private void RegisterSavanna()
        {
            var biome = new BiomeData(BiomeType.Savanna, "Savanna", 0.8f, 0.2f)
            {
                baseHeight = 5f,
                heightVariation = 0.5f,
                hillFrequency = 0.3f,
                mountainFrequency = 0.05f,
                erosionStrength = 0.6f,
                biomeScale = 1.4f,
                grassColor = new Color(0.7f, 0.7f, 0.3f),
                foliageColor = new Color(0.6f, 0.7f, 0.2f),
                topBlockID = "grass_dry",
                treeChance = 0.01f,
                tallGrassChance = 0.4f
            };
            RegisterBiome(biome);
        }

        private void RegisterDesert()
        {
            var biome = new BiomeData(BiomeType.Desert, "Desert", 1.0f, 0.0f)
            {
                baseHeight = 3f,
                heightVariation = 0.6f,
                hillFrequency = 0.4f,
                mountainFrequency = 0.1f,
                erosionStrength = 0.5f,
                biomeScale = 1.8f,
                grassColor = new Color(0.9f, 0.85f, 0.6f),
                topBlockID = "sand",
                fillerBlockID = "sand",
                stoneBlockID = "sandstone",
                treeChance = 0f,
                tallGrassChance = 0.01f
            };
            RegisterBiome(biome);
        }

        private void RegisterMesa()
        {
            var biome = new BiomeData(BiomeType.Mesa, "Mesa", 0.9f, 0.1f)
            {
                baseHeight = 20f,
                heightVariation = 1.5f,
                hillFrequency = 0.6f,
                mountainFrequency = 0.3f,
                erosionStrength = 0.2f,
                biomeScale = 1.6f,
                grassColor = new Color(0.9f, 0.7f, 0.5f),
                topBlockID = "sand_dark",
                fillerBlockID = "sand_dark",
                stoneBlockID = "stone_dark",
                treeChance = 0f
            };
            RegisterBiome(biome);
        }

        private void RegisterBadlands()
        {
            var biome = new BiomeData(BiomeType.Badlands, "Badlands", 0.95f, 0.05f)
            {
                baseHeight = 15f,
                heightVariation = 1.8f,
                hillFrequency = 0.7f,
                mountainFrequency = 0.4f,
                erosionStrength = 0.1f,
                biomeScale = 1.5f,
                grassColor = new Color(0.85f, 0.6f, 0.4f),
                topBlockID = "sand_dark",
                fillerBlockID = "sand",
                stoneBlockID = "stone_dark",
                treeChance = 0f
            };
            RegisterBiome(biome);
        }

        // === WET BIOMES ===

        private void RegisterSwamp()
        {
            var biome = new BiomeData(BiomeType.Swamp, "Swamp", 0.7f, 0.9f)
            {
                baseHeight = -2f,
                heightVariation = 0.3f,
                hillFrequency = 0.2f,
                mountainFrequency = 0f,
                erosionStrength = 0.9f,
                biomeScale = 1.0f,
                grassColor = new Color(0.5f, 0.7f, 0.4f),
                foliageColor = new Color(0.4f, 0.6f, 0.3f),
                waterColor = new Color(0.3f, 0.4f, 0.3f),
                topBlockID = "grass_dark",
                treeChance = 0.04f,
                tallGrassChance = 0.3f
            };
            RegisterBiome(biome);
        }

        private void RegisterJungle()
        {
            var biome = new BiomeData(BiomeType.Jungle, "Jungle", 0.9f, 0.9f)
            {
                baseHeight = 8f,
                heightVariation = 0.9f,
                hillFrequency = 0.5f,
                mountainFrequency = 0.1f,
                erosionStrength = 0.4f,
                biomeScale = 1.2f,
                grassColor = new Color(0.4f, 0.8f, 0.3f),
                foliageColor = new Color(0.3f, 0.75f, 0.2f),
                topBlockID = "grass_dark",
                treeChance = 0.15f,
                tallGrassChance = 0.5f,
                flowerChance = 0.2f
            };
            RegisterBiome(biome);
        }

        private void RegisterRainforest()
        {
            var biome = new BiomeData(BiomeType.Rainforest, "Rainforest", 0.8f, 1.0f)
            {
                baseHeight = 6f,
                heightVariation = 0.8f,
                hillFrequency = 0.4f,
                mountainFrequency = 0.08f,
                erosionStrength = 0.5f,
                biomeScale = 1.1f,
                grassColor = new Color(0.3f, 0.85f, 0.3f),
                foliageColor = new Color(0.2f, 0.8f, 0.2f),
                topBlockID = "grass_dark",
                treeChance = 0.12f,
                tallGrassChance = 0.6f,
                flowerChance = 0.25f
            };
            RegisterBiome(biome);
        }

        // === SPECIAL BIOMES ===

        private void RegisterMushroomIsland()
        {
            var biome = new BiomeData(BiomeType.MushroomIsland, "Mushroom Island", 0.6f, 0.7f)
            {
                baseHeight = 10f,
                heightVariation = 0.8f,
                hillFrequency = 0.5f,
                mountainFrequency = 0.1f,
                erosionStrength = 0.4f,
                biomeScale = 0.8f,
                grassColor = new Color(0.7f, 0.6f, 0.8f),
                topBlockID = "dirt",
                treeChance = 0f,
                tallGrassChance = 0.05f
            };
            RegisterBiome(biome);
        }

        private void RegisterBeach()
        {
            var biome = new BiomeData(BiomeType.Beach, "Beach", 0.6f, 0.4f)
            {
                baseHeight = 0f,
                heightVariation = 0.2f,
                hillFrequency = 0.1f,
                mountainFrequency = 0f,
                erosionStrength = 0.95f,
                biomeScale = 0.6f,
                grassColor = new Color(0.9f, 0.85f, 0.7f),
                topBlockID = "sand",
                fillerBlockID = "sand",
                treeChance = 0f
            };
            RegisterBiome(biome);
        }

        private void RegisterSnowyBeach()
        {
            var biome = new BiomeData(BiomeType.SnowyBeach, "Snowy Beach", 0.1f, 0.4f)
            {
                baseHeight = 0f,
                heightVariation = 0.2f,
                hillFrequency = 0.1f,
                mountainFrequency = 0f,
                erosionStrength = 0.95f,
                biomeScale = 0.6f,
                grassColor = Color.white,
                topBlockID = "sand_light",
                fillerBlockID = "sand_light",
                treeChance = 0f
            };
            RegisterBiome(biome);
        }

        private void RegisterStoneBeach()
        {
            var biome = new BiomeData(BiomeType.StoneBeach, "Stone Beach", 0.5f, 0.4f)
            {
                baseHeight = 2f,
                heightVariation = 0.4f,
                hillFrequency = 0.2f,
                mountainFrequency = 0f,
                erosionStrength = 0.9f,
                biomeScale = 0.6f,
                grassColor = new Color(0.7f, 0.7f, 0.7f),
                topBlockID = "stone",
                fillerBlockID = "stone",
                treeChance = 0f
            };
            RegisterBiome(biome);
        }
    }
}
