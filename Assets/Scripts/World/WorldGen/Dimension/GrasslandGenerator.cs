using System.Collections.Generic;
using UnityEngine;

namespace Pixension.WorldGen
{
    public class GrasslandGenerator : WorldGenerator
    {
        private const int WATER_LEVEL = 32;

        public GrasslandGenerator(int seed) : base(seed, "grassland") { }

        public override void GenerateChunkTerrain(Voxels.Chunk chunk)
        {
            for (int x = 0; x < Voxels.Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Voxels.Chunk.CHUNK_SIZE; z++)
                {
                    int worldX = chunk.chunkPosition.x * Voxels.Chunk.CHUNK_SIZE + x;
                    int worldZ = chunk.chunkPosition.z * Voxels.Chunk.CHUNK_SIZE + z;

                    // === 1. CONTINENTAL SHAPE (VERY LARGE SCALE) ===
                    float continental = noise.Get2DNoise(worldX, worldZ, 1200f, 3, 0.45f, NoiseType.Simplex);
                    continental = Mathf.Clamp01((continental + 1f) * 0.5f);
                    continental = Mathf.Pow(continental, 1.5f);

                    // === 2. MOUNTAIN RIDGES ===
                    float ridge = noise.Get2DNoise(worldX + 2000, worldZ + 2000, 200f, 5, 0.5f, NoiseType.Ridged);
                    ridge = Mathf.Pow(ridge, 1.8f);

                    // === 3. ROLLING HILLS ===
                    float hills = noise.Get2DNoise(worldX + 5000, worldZ + 5000, 120f, 4, 0.55f, NoiseType.Simplex);
                    hills = hills * 0.5f + 0.5f; // Normalize to 0-1

                    // === 4. VALLEYS (using cellular noise for interesting patterns) ===
                    float valley = noise.Get2DNoise(worldX - 2000, worldZ - 2000, 400f, 2, 0.5f, NoiseType.Cellular);
                    valley = Mathf.Abs(valley);
                    valley = Mathf.Pow(valley, 1.5f);

                    // === 5. DETAIL NOISE (using billow for puffy terrain) ===
                    float detail = noise.Get2DNoise(worldX, worldZ, 25f, 3, 0.6f, NoiseType.Billow);

                    // === 6. EROSION/FLATNESS NEAR WATER ===
                    float erosion = noise.Get2DNoise(worldX + 7000, worldZ + 7000, 180f, 2, 0.5f);
                    erosion = Mathf.Clamp01((erosion + 1f) * 0.5f);

                    // === HEIGHT COMPOSITION ===
                    float heightFloat =
                        WATER_LEVEL +                 // Base water level
                        continental * 80f +           // Massive height differences
                        ridge * 50f +                 // Sharp mountain ridges
                        hills * 20f +                 // Rolling hills
                        -valley * 25f +               // Valleys
                        detail * 3f;                  // Surface detail

                    // Apply erosion to flatten areas near water level
                    float distanceFromWater = Mathf.Abs(heightFloat - WATER_LEVEL);
                    if (distanceFromWater < 10f)
                    {
                        float erosionFactor = 1f - (distanceFromWater / 10f);
                        heightFloat = Mathf.Lerp(heightFloat, WATER_LEVEL + (heightFloat > WATER_LEVEL ? 2f : 0f), erosionFactor * 0.5f);
                    }

                    int height = Mathf.RoundToInt(heightFloat);

                    // === OPTIONAL: TERRACING for fantasy look (comment out for natural look) ===
                    // int terraceStep = 4;
                    // height = (height / terraceStep) * terraceStep;

                    for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                    {
                        int worldY = chunk.chunkPosition.y * Voxels.Chunk.CHUNK_SIZE + y;
                        Voxels.VoxelData voxel;

                        if (worldY == 0)
                        {
                            // Bedrock
                            voxel = new Voxels.VoxelData(
                                Voxels.VoxelType.Solid,
                                new Color(0.12f, 0.12f, 0.12f)
                            );
                        }
                        else if (worldY < height - 8)
                        {
                            // Deep stone
                            float stoneVariation = noise.Get3DNoise(worldX, worldY, worldZ, 30f) * 0.1f;
                            voxel = new Voxels.VoxelData(
                                Voxels.VoxelType.Solid,
                                new Color(0.42f + stoneVariation, 0.42f + stoneVariation, 0.42f + stoneVariation)
                            );
                        }
                        else if (worldY < height - 1)
                        {
                            // Dirt layer
                            float dirtVariation = noise.Get3DNoise(worldX, worldY, worldZ, 20f) * 0.05f;
                            voxel = new Voxels.VoxelData(
                                Voxels.VoxelType.Solid,
                                new Color(0.5f + dirtVariation, 0.32f + dirtVariation, 0.18f + dirtVariation)
                            );
                        }
                        else if (worldY == height)
                        {
                            // Surface layer - grass or sand near water
                            bool nearWater = height <= WATER_LEVEL + 2;

                            if (nearWater)
                            {
                                // Sand beaches
                                float sandVariation = noise.Get2DNoise(worldX + 9000, worldZ + 9000, 15f, 1, 1f);
                                Color sandColor = Color.Lerp(
                                    new Color(0.76f, 0.7f, 0.5f),
                                    new Color(0.9f, 0.85f, 0.65f),
                                    sandVariation * 0.5f + 0.5f
                                );
                                voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, sandColor);
                            }
                            else
                            {
                                // Grass variation based on height and moisture
                                float grassNoise = noise.Get2DNoise(worldX + 5000, worldZ + 5000, 20f, 1, 1f);
                                float moisture = noise.Get2DNoise(worldX, worldZ, 300f, 2, 0.5f) * 0.5f + 0.5f;

                                // Different grass colors based on height
                                Color grassColorLow = Color.Lerp(
                                    new Color(0.2f, 0.55f, 0.25f),
                                    new Color(0.3f, 0.7f, 0.3f),
                                    moisture
                                );

                                Color grassColorHigh = Color.Lerp(
                                    new Color(0.4f, 0.6f, 0.4f),
                                    new Color(0.5f, 0.75f, 0.5f),
                                    moisture
                                );

                                float heightFactor = Mathf.Clamp01((height - WATER_LEVEL) / 60f);
                                Color grassColor = Color.Lerp(grassColorLow, grassColorHigh, heightFactor);

                                // Add variation
                                grassColor = Color.Lerp(grassColor, grassColor * 1.2f, grassNoise * 0.3f);

                                voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, grassColor);
                            }
                        }
                        else if (worldY <= WATER_LEVEL && worldY > height)
                        {
                            // Water - fill up to water level
                            voxel = Voxels.VoxelData.Water;
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
    }
}
