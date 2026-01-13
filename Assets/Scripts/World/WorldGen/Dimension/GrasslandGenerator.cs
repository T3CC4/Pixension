using System.Collections.Generic;
using UnityEngine;

namespace Pixension.WorldGen
{
    public class GrasslandGenerator : WorldGenerator
    {
        // World height constants
        private const int MIN_WORLD_HEIGHT = 0;
        private const int MAX_WORLD_HEIGHT = 2048;
        private const int BASE_HEIGHT = 1024;
        private const int WATER_LEVEL = 1000;

        public GrasslandGenerator(int seed) : base(seed, "grassland")
        {
            RegisterCustomBlocks();
        }

        /// <summary>
        /// Registers custom blocks specific to this generator
        /// </summary>
        private void RegisterCustomBlocks()
        {
            // Register grassland-specific block variations
            // These can be used by other generators or systems

            // Mountain stone (lighter for high altitude)
            blockRegistry.RegisterBlock("grassland:stone_mountain", new Voxels.VoxelData(
                Voxels.VoxelType.Solid,
                new Color(0.52f, 0.52f, 0.55f)
            ));

            // Rich soil (for fertile areas)
            blockRegistry.RegisterBlock("grassland:dirt_rich", new Voxels.VoxelData(
                Voxels.VoxelType.Solid,
                new Color(0.4f, 0.25f, 0.15f)
            ));

            // Wet sand (near water)
            blockRegistry.RegisterBlock("grassland:sand_wet", new Voxels.VoxelData(
                Voxels.VoxelType.Solid,
                new Color(0.65f, 0.58f, 0.4f)
            ));

            Debug.Log("GrasslandGenerator: Registered custom blocks");
        }

        public override void GenerateChunkTerrain(Voxels.Chunk chunk)
        {
            for (int x = 0; x < Voxels.Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Voxels.Chunk.CHUNK_SIZE; z++)
                {
                    int worldX = chunk.chunkPosition.x * Voxels.Chunk.CHUNK_SIZE + x;
                    int worldZ = chunk.chunkPosition.z * Voxels.Chunk.CHUNK_SIZE + z;

                    // === 1. CONTINENTAL SHAPE (VERY LARGE SCALE) ===
                    float continental = noise.Get2DNoise(worldX, worldZ, 1500f, 3, 0.45f, NoiseType.Simplex);
                    continental = Mathf.Clamp01((continental + 1f) * 0.5f);
                    continental = Mathf.Pow(continental, 1.5f);

                    // === 2. MOUNTAIN RIDGES ===
                    float ridge = noise.Get2DNoise(worldX + 2000, worldZ + 2000, 300f, 5, 0.5f, NoiseType.Ridged);
                    ridge = Mathf.Pow(ridge, 1.8f);

                    // === 3. ROLLING HILLS ===
                    float hills = noise.Get2DNoise(worldX + 5000, worldZ + 5000, 150f, 4, 0.55f, NoiseType.Simplex);
                    hills = hills * 0.5f + 0.5f; // Normalize to 0-1

                    // === 4. VALLEYS (using cellular noise for interesting patterns) ===
                    float valley = noise.Get2DNoise(worldX - 2000, worldZ - 2000, 500f, 2, 0.5f, NoiseType.Cellular);
                    valley = Mathf.Abs(valley);
                    valley = Mathf.Pow(valley, 1.5f);

                    // === 5. DETAIL NOISE (using billow for puffy terrain) ===
                    float detail = noise.Get2DNoise(worldX, worldZ, 35f, 3, 0.6f, NoiseType.Billow);

                    // === 6. EROSION/FLATNESS NEAR WATER ===
                    float erosion = noise.Get2DNoise(worldX + 7000, worldZ + 7000, 250f, 2, 0.5f);
                    erosion = Mathf.Clamp01((erosion + 1f) * 0.5f);

                    // === HEIGHT COMPOSITION ===
                    float heightFloat =
                        BASE_HEIGHT +                 // Base world height (1024)
                        continental * 300f +          // Massive height differences (-300 to +300)
                        ridge * 200f +                // Sharp mountain ridges
                        hills * 80f +                 // Rolling hills
                        -valley * 100f +              // Valleys
                        detail * 8f;                  // Surface detail

                    // Apply erosion to flatten areas near water level
                    float distanceFromWater = Mathf.Abs(heightFloat - WATER_LEVEL);
                    if (distanceFromWater < 20f)
                    {
                        float erosionFactor = 1f - (distanceFromWater / 20f);
                        float targetHeight = WATER_LEVEL + (heightFloat > WATER_LEVEL ? 3f : 0f);
                        heightFloat = Mathf.Lerp(heightFloat, targetHeight, erosionFactor * 0.6f);
                    }

                    // Clamp height to world limits
                    int height = Mathf.RoundToInt(Mathf.Clamp(heightFloat, MIN_WORLD_HEIGHT + 1, MAX_WORLD_HEIGHT - 1));

                    // === OPTIONAL: TERRACING for fantasy look (comment out for natural look) ===
                    // int terraceStep = 8;
                    // height = (height / terraceStep) * terraceStep;

                    for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                    {
                        int worldY = chunk.chunkPosition.y * Voxels.Chunk.CHUNK_SIZE + y;

                        // Skip if outside world height limits
                        if (worldY < MIN_WORLD_HEIGHT || worldY >= MAX_WORLD_HEIGHT)
                        {
                            chunk.voxels[x, y, z] = Voxels.VoxelData.Air;
                            continue;
                        }

                        // Check for cave generation
                        bool isCave = GenerateCave(worldX, worldY, worldZ, height);

                        Voxels.VoxelData voxel;

                        if (isCave && worldY < height && worldY > MIN_WORLD_HEIGHT + 1)
                        {
                            // Cave - hollow out unless underwater
                            if (worldY <= WATER_LEVEL && worldY > height - 10)
                            {
                                voxel = blockRegistry.GetBlock("water");
                            }
                            else
                            {
                                voxel = Voxels.VoxelData.Air;
                            }
                        }
                        else if (worldY == MIN_WORLD_HEIGHT)
                        {
                            // Bedrock layer at absolute bottom
                            voxel = blockRegistry.GetBlock("bedrock");
                        }
                        else if (worldY < height - 12)
                        {
                            // Deep stone with variation
                            float stoneVariation = noise.Get3DNoise(worldX, worldY, worldZ, 40f, NoiseType.Simplex);

                            if (stoneVariation > 0.3f)
                            {
                                voxel = blockRegistry.GetBlock("stone_light");
                            }
                            else if (stoneVariation < -0.3f)
                            {
                                voxel = blockRegistry.GetBlock("stone_dark");
                            }
                            else
                            {
                                voxel = blockRegistry.GetBlock("stone");
                            }
                        }
                        else if (worldY < height - 1)
                        {
                            // Dirt layer with variation
                            float dirtVariation = noise.Get3DNoise(worldX, worldY, worldZ, 25f, NoiseType.Perlin);

                            if (dirtVariation > 0.4f && height > WATER_LEVEL + 20)
                            {
                                // Rich soil in high, vegetated areas
                                voxel = blockRegistry.GetBlock("grassland:dirt_rich");
                            }
                            else if (dirtVariation < -0.3f)
                            {
                                voxel = blockRegistry.GetBlock("dirt_dark");
                            }
                            else
                            {
                                voxel = blockRegistry.GetBlock("dirt");
                            }
                        }
                        else if (worldY == height)
                        {
                            // Surface layer - grass, sand, or mountain stone
                            bool nearWater = height <= WATER_LEVEL + 5;
                            bool isHigh = height > BASE_HEIGHT + 200;

                            if (nearWater)
                            {
                                // Sand beaches with variation
                                float sandVariation = noise.Get2DNoise(worldX + 9000, worldZ + 9000, 20f, 2, 1f);

                                if (Mathf.Abs(height - WATER_LEVEL) <= 2)
                                {
                                    // Wet sand right at water line
                                    voxel = blockRegistry.GetBlock("grassland:sand_wet");
                                }
                                else if (sandVariation > 0.3f)
                                {
                                    voxel = blockRegistry.GetBlock("sand_light");
                                }
                                else if (sandVariation < -0.3f)
                                {
                                    voxel = blockRegistry.GetBlock("sand_dark");
                                }
                                else
                                {
                                    voxel = blockRegistry.GetBlock("sand");
                                }
                            }
                            else if (isHigh)
                            {
                                // Mountain stone at high elevations
                                voxel = blockRegistry.GetBlock("grassland:stone_mountain");
                            }
                            else
                            {
                                // Grass variation based on height and moisture
                                float grassNoise = noise.Get2DNoise(worldX + 5000, worldZ + 5000, 25f, 2, 1f);
                                float moisture = noise.Get2DNoise(worldX, worldZ, 400f, 2, 0.5f) * 0.5f + 0.5f;

                                // Height-based grass color selection
                                float heightFactor = Mathf.Clamp01((height - WATER_LEVEL) / 200f);

                                if (moisture < 0.3f)
                                {
                                    // Dry grass
                                    voxel = blockRegistry.GetBlock("grass_dry");
                                }
                                else if (heightFactor < 0.3f)
                                {
                                    // Low elevation - dark grass
                                    voxel = grassNoise > 0.2f ?
                                        blockRegistry.GetBlock("grass") :
                                        blockRegistry.GetBlock("grass_dark");
                                }
                                else
                                {
                                    // High elevation - lighter grass
                                    voxel = grassNoise > 0.2f ?
                                        blockRegistry.GetBlock("grass_light") :
                                        blockRegistry.GetBlock("grass");
                                }
                            }
                        }
                        else if (worldY <= WATER_LEVEL && worldY > height)
                        {
                            // Water - fill up to water level
                            voxel = blockRegistry.GetBlock("water");
                        }
                        else
                        {
                            voxel = Voxels.VoxelData.Air;
                        }

                        chunk.voxels[x, y, z] = voxel;
                    }
                }
            }

            chunk.SetDirty();
        }

        public override List<StructurePlacement> GetStructuresForChunk(Vector3Int chunkPos)
        {
            return structureGrid.GetPlacementsForChunk(chunkPos, generatorID, GetTerrainHeight);
        }

        public override Color GetSkyColor()
        {
            // Beautiful sky for grassland biome
            return new Color(0.53f, 0.75f, 0.95f);
        }

        /// <summary>
        /// Gets the height limits for this generator
        /// </summary>
        public static (int min, int max) GetHeightLimits()
        {
            return (MIN_WORLD_HEIGHT, MAX_WORLD_HEIGHT);
        }

        /// <summary>
        /// Gets the water level for this generator
        /// </summary>
        public static int GetWaterLevel()
        {
            return WATER_LEVEL;
        }

        /// <summary>
        /// Gets the base/average terrain height
        /// </summary>
        public static int GetBaseHeight()
        {
            return BASE_HEIGHT;
        }

        /// <summary>
        /// Generates caves using 3D noise
        /// Returns true if the position should be carved out as a cave
        /// </summary>
        private bool GenerateCave(int worldX, int worldY, int worldZ, int surfaceHeight)
        {
            // Don't generate caves too close to surface or bedrock
            if (worldY > surfaceHeight - 5 || worldY < MIN_WORLD_HEIGHT + 3)
            {
                return false;
            }

            // Use 3D noise for cave generation
            // Layer 1: Large cave systems
            float caveNoise1 = noise.Get3DNoise(worldX, worldY, worldZ, 80f, NoiseType.Simplex);

            // Layer 2: Smaller tunnels
            float caveNoise2 = noise.Get3DNoise(worldX + 1000, worldY + 1000, worldZ + 1000, 40f, NoiseType.Simplex);

            // Layer 3: Vertical variation
            float caveNoise3 = noise.Get3DNoise(worldX - 1000, worldY - 1000, worldZ - 1000, 60f, NoiseType.Ridged);

            // Combine noise layers
            float caveDensity = (caveNoise1 + caveNoise2 * 0.5f + caveNoise3 * 0.3f) / 1.8f;

            // Add depth-based variation (more caves at certain depths)
            float depthFactor = Mathf.Sin((worldY - BASE_HEIGHT) / 200f) * 0.1f;
            caveDensity += depthFactor;

            // Threshold for cave formation (adjust for more/fewer caves)
            float caveThreshold = 0.35f;

            // More caves at mid-depths
            if (worldY > BASE_HEIGHT - 100 && worldY < BASE_HEIGHT + 100)
            {
                caveThreshold -= 0.05f; // Easier to form caves at mid-depths
            }

            return caveDensity > caveThreshold;
        }
    }
}
