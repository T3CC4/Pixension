using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Structures
{
    public class StructurePlacer
    {
        private Voxels.ChunkManager chunkManager;

        public StructurePlacer(Voxels.ChunkManager manager)
        {
            this.chunkManager = manager;
        }

        public List<BlockEntityPlacement> PlaceStructure(WorldGen.StructurePlacement placement)
        {
            Voxels.VoxelData[,,] rotatedVoxels = RotateStructure(placement.structure, placement.rotation);
            List<BlockEntityPlacement> rotatedEntities = RotateBlockEntities(placement.structure, placement.rotation);

            Vector3Int rotatedSize = GetRotatedSize(placement.structure.size, placement.rotation);
            List<Vector3Int> affectedChunks = GetAffectedChunks(placement.worldPosition, rotatedSize);

            foreach (Vector3Int chunkPos in affectedChunks)
            {
                Voxels.Chunk chunk = chunkManager.GetChunk(chunkPos);
                if (chunk == null)
                    continue;

                Voxels.VoxelData[,,] chunkSection = ExtractChunkSection(rotatedVoxels, placement.worldPosition, rotatedSize, chunkPos);

                Vector3Int chunkWorldPos = new Vector3Int(
                    chunkPos.x * Voxels.Chunk.CHUNK_SIZE,
                    chunkPos.y * Voxels.Chunk.CHUNK_SIZE,
                    chunkPos.z * Voxels.Chunk.CHUNK_SIZE
                );

                chunk.MergeStructureVoxels(chunkSection, placement.worldPosition - chunkWorldPos);
                chunk.SetDirty();
                chunkManager.MarkChunkDirty(chunkPos);
            }

            List<BlockEntityPlacement> worldEntities = new List<BlockEntityPlacement>();
            foreach (BlockEntityPlacement entity in rotatedEntities)
            {
                BlockEntityPlacement worldEntity = new BlockEntityPlacement(
                    entity.entityID,
                    placement.worldPosition + entity.localPosition,
                    entity.facing
                );
                worldEntities.Add(worldEntity);
            }

            return worldEntities;
        }

        private Voxels.VoxelData[,,] RotateStructure(StructureData structure, Utilities.Direction rotation)
        {
            Vector3Int rotatedSize = GetRotatedSize(structure.size, rotation);
            Voxels.VoxelData[,,] rotated = new Voxels.VoxelData[rotatedSize.x, rotatedSize.y, rotatedSize.z];

            for (int x = 0; x < structure.size.x; x++)
            {
                for (int y = 0; y < structure.size.y; y++)
                {
                    for (int z = 0; z < structure.size.z; z++)
                    {
                        Vector3Int originalPos = new Vector3Int(x, y, z);
                        Vector3Int rotatedPos = Utilities.RotationHelper.RotatePosition(originalPos, rotation, Vector3Int.zero);

                        if (rotation == Utilities.Direction.East || rotation == Utilities.Direction.West)
                        {
                            rotatedPos.x += rotatedSize.x - 1;
                            rotatedPos.x = Mathf.Abs(rotatedPos.x);
                        }
                        if (rotation == Utilities.Direction.South)
                        {
                            rotatedPos.x += rotatedSize.x - 1;
                            rotatedPos.z += rotatedSize.z - 1;
                        }
                        if (rotation == Utilities.Direction.West)
                        {
                            rotatedPos.z += rotatedSize.z - 1;
                        }

                        rotatedPos.x = Mathf.Clamp(rotatedPos.x, 0, rotatedSize.x - 1);
                        rotatedPos.y = Mathf.Clamp(rotatedPos.y, 0, rotatedSize.y - 1);
                        rotatedPos.z = Mathf.Clamp(rotatedPos.z, 0, rotatedSize.z - 1);

                        rotated[rotatedPos.x, rotatedPos.y, rotatedPos.z] = structure.GetVoxel(x, y, z);
                    }
                }
            }

            return rotated;
        }

        private List<BlockEntityPlacement> RotateBlockEntities(StructureData structure, Utilities.Direction rotation)
        {
            List<BlockEntityPlacement> rotated = new List<BlockEntityPlacement>();

            foreach (BlockEntityPlacement entity in structure.blockEntities)
            {
                Vector3Int rotatedPos = Utilities.RotationHelper.RotatePosition(entity.localPosition, rotation, Vector3Int.zero);
                Utilities.Direction rotatedFacing = Utilities.RotationHelper.RotateDirection(entity.facing, (int)rotation);

                rotated.Add(new BlockEntityPlacement(entity.entityID, rotatedPos, rotatedFacing));
            }

            return rotated;
        }

        private Vector3Int GetRotatedSize(Vector3Int originalSize, Utilities.Direction rotation)
        {
            if (rotation == Utilities.Direction.East || rotation == Utilities.Direction.West)
            {
                return new Vector3Int(originalSize.z, originalSize.y, originalSize.x);
            }
            return originalSize;
        }

        private Voxels.VoxelData[,,] ExtractChunkSection(Voxels.VoxelData[,,] structureVoxels, Vector3Int structureWorldPos, Vector3Int structureSize, Vector3Int chunkPos)
        {
            Voxels.VoxelData[,,] section = new Voxels.VoxelData[Voxels.Chunk.CHUNK_SIZE, Voxels.Chunk.CHUNK_SIZE, Voxels.Chunk.CHUNK_SIZE];

            Vector3Int chunkWorldPos = new Vector3Int(
                chunkPos.x * Voxels.Chunk.CHUNK_SIZE,
                chunkPos.y * Voxels.Chunk.CHUNK_SIZE,
                chunkPos.z * Voxels.Chunk.CHUNK_SIZE
            );

            for (int x = 0; x < Voxels.Chunk.CHUNK_SIZE; x++)
            {
                for (int y = 0; y < Voxels.Chunk.CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < Voxels.Chunk.CHUNK_SIZE; z++)
                    {
                        Vector3Int worldPos = chunkWorldPos + new Vector3Int(x, y, z);
                        Vector3Int structureLocalPos = worldPos - structureWorldPos;

                        if (structureLocalPos.x >= 0 && structureLocalPos.x < structureSize.x &&
                            structureLocalPos.y >= 0 && structureLocalPos.y < structureSize.y &&
                            structureLocalPos.z >= 0 && structureLocalPos.z < structureSize.z)
                        {
                            section[x, y, z] = structureVoxels[structureLocalPos.x, structureLocalPos.y, structureLocalPos.z];
                        }
                        else
                        {
                            section[x, y, z] = Voxels.VoxelData.Air;
                        }
                    }
                }
            }

            return section;
        }

        public List<Vector3Int> GetAffectedChunks(Vector3Int pos, Vector3Int size)
        {
            List<Vector3Int> chunks = new List<Vector3Int>();

            Vector3Int minChunk = new Vector3Int(
                Mathf.FloorToInt((float)pos.x / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)pos.y / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)pos.z / Voxels.Chunk.CHUNK_SIZE)
            );

            Vector3Int maxPos = pos + size - Vector3Int.one;
            Vector3Int maxChunk = new Vector3Int(
                Mathf.FloorToInt((float)maxPos.x / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)maxPos.y / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)maxPos.z / Voxels.Chunk.CHUNK_SIZE)
            );

            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    for (int z = minChunk.z; z <= maxChunk.z; z++)
                    {
                        chunks.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            return chunks;
        }
    }
}