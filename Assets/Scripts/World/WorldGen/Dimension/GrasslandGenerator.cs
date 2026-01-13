using System.Collections.Generic;
using UnityEngine;

namespace Pixension.WorldGen
{
    public class GrasslandGenerator : WorldGenerator
    {
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
                    float continental = noise.Get2DNoise(worldX, worldZ, 900f, 2, 0.4f);
                    continental = Mathf.Clamp01((continental + 1f) * 0.5f);
                    continental = Mathf.Pow(continental, 1.8f); // dramatic height bias

                    // === 2. RIDGE / MOUNTAIN NOISE ===
                    float ridge = noise.Get2DNoise(worldX + 2000, worldZ + 2000, 160f, 4, 0.5f);
                    ridge = 1f - Mathf.Abs(ridge); // sharp ridges
                    ridge = Mathf.Pow(ridge, 2.5f);

                    // === 3. VALLEY NOISE ===
                    float valley = noise.Get2DNoise(worldX - 2000, worldZ - 2000, 300f, 2, 0.5f);
                    valley = Mathf.Abs(valley);
                    valley = Mathf.Pow(valley, 2f);

                    // === 4. DETAIL NOISE ===
                    float detail = noise.Get2DNoise(worldX, worldZ, 35f, 2, 0.6f);

                    // === HEIGHT COMPOSITION ===
                    float heightFloat =
                        35f +                         // base world height
                        continental * 70f +           // massive height differences
                        ridge * 40f -                 // mountains
                        valley * 30f +                // valleys
                        detail * 4f;                  // surface detail

                    int height = Mathf.RoundToInt(heightFloat);

                    // === TERRACING (FANTASY LOOK) ===
                    int terraceStep = 4;
                    height = (height / terraceStep) * terraceStep;

                    for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                    {
                        int worldY = chunk.chunkPosition.y * Voxels.Chunk.CHUNK_SIZE + y;
                        Voxels.VoxelData voxel;

                        if (worldY == 0)
                        {
                            voxel = new Voxels.VoxelData(
                                Voxels.VoxelType.Solid,
                                new Color(0.12f, 0.12f, 0.12f)
                            );
                        }
                        else if (worldY < height - 6)
                        {
                            // Deep stone
                            voxel = new Voxels.VoxelData(
                                Voxels.VoxelType.Solid,
                                new Color(0.42f, 0.42f, 0.42f)
                            );
                        }
                        else if (worldY < height - 1)
                        {
                            // Dirt
                            voxel = new Voxels.VoxelData(
                                Voxels.VoxelType.Solid,
                                new Color(0.5f, 0.32f, 0.18f)
                            );
                        }
                        else if (worldY == height)
                        {
                            // Grass variation
                            float grassNoise = noise.Get2DNoise(worldX + 5000, worldZ + 5000, 20f, 1, 1f);
                            Color grassColor = Color.Lerp(
                                new Color(0.22f, 0.6f, 0.25f),
                                new Color(0.35f, 0.8f, 0.35f),
                                grassNoise * 0.5f + 0.5f
                            );

                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, grassColor);
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
            // Slightly more epic fantasy sky
            return new Color(0.45f, 0.65f, 0.95f);
        }
    }
}
