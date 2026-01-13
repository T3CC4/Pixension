using System.Collections.Generic;
using UnityEngine;
using Pixension.WorldGen.Biomes;

namespace Pixension.WorldGen
{
    /// <summary>
    /// Advanced world generator that uses the biome system for varied terrain
    /// Supports temperature, humidity, and multiple biome types
    /// </summary>
    public class BiomeWorldGenerator : WorldGenerator
    {
        // World height constants
        private const int MIN_WORLD_HEIGHT = 0;
        private const int MAX_WORLD_HEIGHT = 2048;
        private const int BASE_HEIGHT = 1024;
        private const int WATER_LEVEL = 1000;

        private BiomeGenerator biomeGenerator;

        public BiomeWorldGenerator(int seed) : base(seed, "biome_world")
        {
            biomeGenerator = new BiomeGenerator(seed);

            // Optional: Adjust biome scale for bigger/smaller biomes
            // biomeGenerator.SetBiomeScale(1200f, 900f); // Bigger biomes

            RegisterCustomBlocks();
        }

        private void RegisterCustomBlocks()
        {
            // Register any custom blocks needed
            // Most blocks should already be registered by other generators

            // Ensure grassland blocks are available
            if (!blockRegistry.HasBlock("grassland:stone_mountain"))
            {
                blockRegistry.RegisterBlock("grassland:stone_mountain", new Voxels.VoxelData(
                    Voxels.VoxelType.Solid,
                    new Color(0.52f, 0.52f, 0.55f)
                ));
            }

            if (!blockRegistry.HasBlock("grassland:dirt_rich"))
            {
                blockRegistry.RegisterBlock("grassland:dirt_rich", new Voxels.VoxelData(
                    Voxels.VoxelType.Solid,
                    new Color(0.4f, 0.25f, 0.15f)
                ));
            }

            if (!blockRegistry.HasBlock("grassland:sand_wet"))
            {
                blockRegistry.RegisterBlock("grassland:sand_wet", new Voxels.VoxelData(
                    Voxels.VoxelType.Solid,
                    new Color(0.65f, 0.58f, 0.4f)
                ));
            }

            Debug.Log("BiomeWorldGenerator: Custom blocks registered");
        }

        public override void GenerateChunkTerrain(Voxels.Chunk chunk)
        {
            for (int x = 0; x < Voxels.Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Voxels.Chunk.CHUNK_SIZE; z++)
                {
                    int worldX = chunk.chunkPosition.x * Voxels.Chunk.CHUNK_SIZE + x;
                    int worldZ = chunk.chunkPosition.z * Voxels.Chunk.CHUNK_SIZE + z;

                    // Get biome at this position
                    BiomeData biome = biomeGenerator.GetBiome(worldX, worldZ);

                    // Get climate parameters for additional effects
                    float temperature = biomeGenerator.GetTemperature(worldX, worldZ);
                    float humidity = biomeGenerator.GetHumidity(worldX, worldZ);
                    float continental = biomeGenerator.GetContinentalness(worldX, worldZ);

                    // Generate height using biome-modified noise
                    int height = GenerateBiomeHeight(worldX, worldZ, biome, continental);

                    // Generate column
                    for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                    {
                        int worldY = chunk.chunkPosition.y * Voxels.Chunk.CHUNK_SIZE + y;

                        // Skip if outside world height limits
                        if (worldY < MIN_WORLD_HEIGHT || worldY >= MAX_WORLD_HEIGHT)
                        {
                            chunk.voxels[x, y, z] = Voxels.VoxelData.Air;
                            continue;
                        }

                        Voxels.VoxelData voxel = GenerateVoxel(worldX, worldY, worldZ, height, biome, temperature, humidity);
                        chunk.voxels[x, y, z] = voxel;
                    }
                }
            }

            chunk.SetDirty();
        }

        /// <summary>
        /// Generates terrain height for a position using biome modifiers
        /// </summary>
        private int GenerateBiomeHeight(int worldX, int worldZ, BiomeData biome, float continental)
        {
            // Start with base height
            float heightFloat = BASE_HEIGHT;

            // === CONTINENTAL SHAPE ===
            // Controls ocean depth and general land elevation
            float continentalHeight = (continental - 0.3f) * 100f;
            heightFloat += continentalHeight;

            // === BIOME BASE HEIGHT ===
            heightFloat += biome.baseHeight;

            // === LARGE SCALE FEATURES (Mountains, Hills) ===
            if (biome.mountainFrequency > 0f)
            {
                float mountainScale = biome.GetNoiseScale(400f);
                float mountains = noise.Get2DNoise(worldX, worldZ, mountainScale, 5, 0.5f, NoiseType.Ridged);
                mountains = Mathf.Pow(Mathf.Clamp01((mountains + 1f) * 0.5f), 2f);
                heightFloat += mountains * biome.mountainFrequency * 200f * biome.heightVariation;
            }

            if (biome.hillFrequency > 0f)
            {
                float hillScale = biome.GetNoiseScale(200f);
                float hills = noise.Get2DNoise(worldX + 1000, worldZ + 1000, hillScale, 4, 0.55f, NoiseType.Simplex);
                hills = hills * 0.5f + 0.5f;
                heightFloat += hills * biome.hillFrequency * 60f * biome.heightVariation;
            }

            // === MEDIUM SCALE FEATURES (Variation) ===
            float variationScale = biome.GetNoiseScale(100f);
            float variation = noise.Get2DNoise(worldX + 2000, worldZ + 2000, variationScale, 3, 0.6f, NoiseType.Simplex);
            heightFloat += variation * 30f * biome.heightVariation;

            // === DETAIL NOISE ===
            float detailScale = biome.GetNoiseScale(35f);
            float detail = noise.Get2DNoise(worldX, worldZ, detailScale, 2, 0.6f, NoiseType.Billow);
            heightFloat += detail * 8f;

            // === EROSION EFFECT ===
            if (biome.erosionStrength > 0f)
            {
                float erosion = biomeGenerator.GetErosion(worldX, worldZ);
                float erosionEffect = Mathf.Lerp(0f, 1f, biome.erosionStrength);

                // Smooth terrain near water level
                float distanceFromWater = Mathf.Abs(heightFloat - WATER_LEVEL);
                if (distanceFromWater < 30f * erosionEffect)
                {
                    float smoothFactor = 1f - (distanceFromWater / (30f * erosionEffect));
                    float targetHeight = WATER_LEVEL + (heightFloat > WATER_LEVEL ? 2f : -5f);
                    heightFloat = Mathf.Lerp(heightFloat, targetHeight, smoothFactor * erosion * biome.erosionStrength);
                }
            }

            // Clamp to world limits
            int height = Mathf.RoundToInt(Mathf.Clamp(heightFloat, MIN_WORLD_HEIGHT + 1, MAX_WORLD_HEIGHT - 1));

            return height;
        }

        /// <summary>
        /// Generates a voxel at a specific position based on biome and climate
        /// </summary>
        private Voxels.VoxelData GenerateVoxel(int worldX, int worldY, int worldZ, int surfaceHeight,
            BiomeData biome, float temperature, float humidity)
        {
            // Bedrock layer
            if (worldY == MIN_WORLD_HEIGHT)
            {
                return blockRegistry.GetBlock("bedrock");
            }

            // Air above surface
            if (worldY > surfaceHeight)
            {
                // Water fill up to water level
                if (worldY <= WATER_LEVEL)
                {
                    return blockRegistry.GetBlock("water");
                }
                return Voxels.VoxelData.Air;
            }

            // === UNDERGROUND LAYERS ===

            // Deep stone
            if (worldY < surfaceHeight - 12)
            {
                float stoneVariation = noise.Get3DNoise(worldX, worldY, worldZ, 40f, NoiseType.Simplex);

                // Use biome's stone type or variations
                if (blockRegistry.HasBlock(biome.stoneBlockID))
                {
                    if (stoneVariation > 0.4f)
                        return blockRegistry.GetBlock("stone_light");
                    else if (stoneVariation < -0.4f)
                        return blockRegistry.GetBlock("stone_dark");
                    else
                        return blockRegistry.GetBlock(biome.stoneBlockID);
                }
                else
                {
                    if (stoneVariation > 0.3f)
                        return blockRegistry.GetBlock("stone_light");
                    else if (stoneVariation < -0.3f)
                        return blockRegistry.GetBlock("stone_dark");
                    else
                        return blockRegistry.GetBlock("stone");
                }
            }

            // Filler layer (dirt, sand, etc.)
            if (worldY < surfaceHeight - 1)
            {
                string fillerBlock = biome.fillerBlockID;

                // Add variation to filler
                if (fillerBlock == "dirt")
                {
                    float fillerVariation = noise.Get3DNoise(worldX, worldY, worldZ, 25f, NoiseType.Perlin);
                    if (fillerVariation > 0.4f && humidity > 0.6f)
                        return blockRegistry.GetBlock("grassland:dirt_rich");
                    else if (fillerVariation < -0.3f)
                        return blockRegistry.GetBlock("dirt_dark");
                    else
                        return blockRegistry.GetBlock("dirt");
                }

                return blockRegistry.GetBlock(fillerBlock);
            }

            // === SURFACE LAYER ===
            if (worldY == surfaceHeight)
            {
                // Beach/coastal handling
                bool isNearWater = surfaceHeight <= WATER_LEVEL + 5;
                bool isUnderwater = surfaceHeight < WATER_LEVEL;

                if (isNearWater && !isUnderwater)
                {
                    // Use beach block
                    string beachBlock = temperature < 0.3f ? "sand_light" : biome.beachBlockID;

                    if (beachBlock == "sand" || beachBlock == "sand_light")
                    {
                        float sandVariation = noise.Get2DNoise(worldX, worldZ, 20f, 2, 1f);

                        if (Mathf.Abs(surfaceHeight - WATER_LEVEL) <= 2 && blockRegistry.HasBlock("grassland:sand_wet"))
                        {
                            return blockRegistry.GetBlock("grassland:sand_wet");
                        }
                        else if (sandVariation > 0.3f)
                        {
                            return blockRegistry.GetBlock("sand_light");
                        }
                        else if (sandVariation < -0.3f)
                        {
                            return blockRegistry.GetBlock("sand_dark");
                        }
                        else
                        {
                            return blockRegistry.GetBlock("sand");
                        }
                    }

                    return blockRegistry.GetBlock(beachBlock);
                }

                // Underwater surface
                if (isUnderwater)
                {
                    if (temperature < 0.3f)
                        return blockRegistry.GetBlock("sand_light");
                    else
                        return blockRegistry.GetBlock("sand");
                }

                // Regular surface block from biome
                return blockRegistry.GetBlock(biome.topBlockID);
            }

            // Between surface and height (shouldn't happen but fallback)
            return blockRegistry.GetBlock(biome.fillerBlockID);
        }

        public override List<StructurePlacement> GetStructuresForChunk(Vector3Int chunkPos)
        {
            return structureGrid.GetPlacementsForChunk(chunkPos, generatorID, GetTerrainHeight);
        }

        public override Color GetSkyColor()
        {
            // Dynamic sky color based on average biome (could be enhanced)
            return new Color(0.53f, 0.75f, 0.95f);
        }

        public override int GetTerrainHeight(int worldX, int worldZ)
        {
            BiomeData biome = biomeGenerator.GetBiome(worldX, worldZ);
            float continental = biomeGenerator.GetContinentalness(worldX, worldZ);
            return GenerateBiomeHeight(worldX, worldZ, biome, continental);
        }

        /// <summary>
        /// Gets the biome at a specific world position (for debugging/info)
        /// </summary>
        public BiomeData GetBiomeAt(int worldX, int worldZ)
        {
            return biomeGenerator.GetBiome(worldX, worldZ);
        }

        /// <summary>
        /// Gets the biome generator (for debugging/external use)
        /// </summary>
        public BiomeGenerator GetBiomeGenerator()
        {
            return biomeGenerator;
        }
    }
}
