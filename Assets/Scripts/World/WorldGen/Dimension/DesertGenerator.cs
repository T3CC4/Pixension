using System.Collections.Generic;
using UnityEngine;

namespace Pixension.WorldGen
{
    public class DesertGenerator : WorldGenerator
    {
        public DesertGenerator(int seed) : base(seed, "desert")
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

                    // Wüsten-Terrain: größere Wellen, weniger Details
                    // scale: 150 = sanftere Dünen
                    // octaves: 3 = weniger Details als Grassland
                    float noiseValue = noise.Get2DNoise(worldX, worldZ, 150f, 3, 0.5f);

                    // Wüste: flacher, Basis 45, Variation ±10
                    int height = Mathf.RoundToInt((noiseValue * 10f) + 45f);

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
                            // Sandstein
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.7f, 0.6f, 0.5f));
                        }
                        else if (worldY >= height - 3 && worldY <= height - 1)
                        {
                            // Sand (dunkel)
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.8f, 0.7f, 0.5f));
                        }
                        else if (worldY == height)
                        {
                            // Sand (hell)
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.95f, 0.85f, 0.6f));
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

        public override List<StructurePlacement> GetStructuresForChunk(Vector3Int chunkPos)
        {
            return structureGrid.GetPlacementsForChunk(chunkPos, generatorID, GetTerrainHeight);
        }

        public override Color GetSkyColor()
        {
            return new Color(1f, 0.9f, 0.7f);
        }
    }
}