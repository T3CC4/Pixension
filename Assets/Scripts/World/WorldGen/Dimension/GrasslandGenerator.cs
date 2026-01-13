using System.Collections.Generic;
using UnityEngine;

namespace Pixension.WorldGen
{
    public class GrasslandGenerator : WorldGenerator
    {
        public GrasslandGenerator(int seed) : base(seed, "grassland")
        {
        }

        public override void GenerateChunkTerrain(Voxels.Chunk chunk)
        {
            for (int x = 0; x < Voxels.Chunk.CHUNK_SIZE; x++)
            {
                for (int z = 0; z < Voxels.Chunk.CHUNK_SIZE; z++)
                {
                    int worldX = chunk.chunkPosition.x * Voxels.Chunk.CHUNK_SIZE + x;
                    int worldZ = chunk.chunkPosition.z * Voxels.Chunk.CHUNK_SIZE + z;

                    // Korrigierte Noise-Parameter:
                    // scale: 100 = sanfte Hügel
                    // octaves: 4 = gute Details
                    // persistence: 0.5 = balanced
                    float noiseValue = noise.Get2DNoise(worldX, worldZ, 100f, 4, 0.5f);

                    // noiseValue ist -1 bis 1, konvertiere zu Höhe
                    // Basis-Höhe 50, Variation ±15
                    int height = Mathf.RoundToInt((noiseValue * 15f) + 50f);

                    for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                    {
                        int worldY = chunk.chunkPosition.y * Voxels.Chunk.CHUNK_SIZE + y;

                        Voxels.VoxelData voxel;

                        if (worldY == 0)
                        {
                            // Bedrock
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.2f, 0.2f, 0.2f));
                        }
                        else if (worldY >= 1 && worldY <= height - 4)
                        {
                            // Stein
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.5f, 0.5f, 0.5f));
                        }
                        else if (worldY >= height - 3 && worldY <= height - 1)
                        {
                            // Erde
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.55f, 0.35f, 0.2f));
                        }
                        else if (worldY == height)
                        {
                            // Gras
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.3f, 0.7f, 0.2f));
                        }
                        else
                        {
                            voxel = Voxels.VoxelData.Air;
                        }

                        chunk.voxels[x, y, z] = voxel;
                    }
                }
            }

            // Markiere Chunk als dirty für Mesh-Generierung
            chunk.SetDirty();
        }

        public override List<WorldGen.StructurePlacement> GetStructuresForChunk(Vector3Int chunkPos)
        {
            return structureGrid.GetPlacementsForChunk(chunkPos, generatorID, GetTerrainHeight);
        }

        public override Color GetSkyColor()
        {
            return new Color(0.5f, 0.7f, 1f);
        }
    }
}