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

                    float noiseValue = noise.Get2DNoise(worldX, worldZ, 0.01f, 3, 0.5f);
                    int height = Mathf.RoundToInt(noiseValue * 15f + 35f);

                    for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                    {
                        int worldY = chunk.chunkPosition.y * Voxels.Chunk.CHUNK_SIZE + y;

                        Voxels.VoxelData voxel;

                        if (worldY == 0)
                        {
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, Color.black);
                        }
                        else if (worldY >= 1 && worldY <= height - 4)
                        {
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, Color.gray);
                        }
                        else if (worldY >= height - 3 && worldY <= height - 1)
                        {
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.7f, 0.6f, 0.5f));
                        }
                        else if (worldY == height)
                        {
                            voxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, new Color(0.9f, 0.8f, 0.6f));
                        }
                        else
                        {
                            voxel = Voxels.VoxelData.Air;
                        }

                        chunk.voxels[x, y, z] = voxel;
                    }
                }
            }
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