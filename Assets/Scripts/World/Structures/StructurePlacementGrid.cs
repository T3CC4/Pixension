using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Structures
{
    public class StructurePlacementGrid
    {
        private int seed;
        private StructureLoader structureLoader;
        private WorldGen.NoiseGenerator noise;

        private const int ENVIRONMENTAL_GRID_SIZE = 8;
        private const int ARCHITECTURE_GRID_SIZE = 128;

        public StructurePlacementGrid(int seed, StructureLoader loader)
        {
            this.seed = seed;
            this.structureLoader = loader;
            this.noise = new WorldGen.NoiseGenerator(seed);
        }

        public List<WorldGen.StructurePlacement> GetPlacementsForChunk(Vector3Int chunkPos, string generatorID, Func<int, int, int> getTerrainHeight)
        {
            List<WorldGen.StructurePlacement> placements = new List<WorldGen.StructurePlacement>();

            int chunkWorldX = chunkPos.x * Voxels.Chunk.CHUNK_SIZE;
            int chunkWorldZ = chunkPos.z * Voxels.Chunk.CHUNK_SIZE;

            placements.AddRange(GetPlacementsForGrid(chunkWorldX, chunkWorldZ, generatorID, getTerrainHeight, ENVIRONMENTAL_GRID_SIZE, StructureType.Environmental));
            placements.AddRange(GetPlacementsForGrid(chunkWorldX, chunkWorldZ, generatorID, getTerrainHeight, ARCHITECTURE_GRID_SIZE, StructureType.Architecture));

            return placements;
        }

        private List<WorldGen.StructurePlacement> GetPlacementsForGrid(int chunkWorldX, int chunkWorldZ, string generatorID, Func<int, int, int> getTerrainHeight, int gridSize, StructureType structureType)
        {
            List<WorldGen.StructurePlacement> placements = new List<WorldGen.StructurePlacement>();

            int minGridX = Mathf.FloorToInt((float)chunkWorldX / gridSize);
            int maxGridX = Mathf.FloorToInt((float)(chunkWorldX + Voxels.Chunk.CHUNK_SIZE - 1) / gridSize);
            int minGridZ = Mathf.FloorToInt((float)chunkWorldZ / gridSize);
            int maxGridZ = Mathf.FloorToInt((float)(chunkWorldZ + Voxels.Chunk.CHUNK_SIZE - 1) / gridSize);

            for (int gridX = minGridX; gridX <= maxGridX; gridX++)
            {
                for (int gridZ = minGridZ; gridZ <= maxGridZ; gridZ++)
                {
                    int gridHash = HashGridCell(gridX, gridZ, structureType, generatorID);
                    System.Random random = new System.Random(gridHash);

                    List<StructureData> availableStructures = GetFilteredStructures(generatorID, structureType);

                    if (availableStructures.Count == 0)
                        continue;

                    StructureData selectedStructure = SelectStructureByWeight(availableStructures, random);

                    if (selectedStructure == null)
                        continue;

                    int offsetX = random.Next(0, gridSize);
                    int offsetZ = random.Next(0, gridSize);

                    int worldX = gridX * gridSize + offsetX;
                    int worldZ = gridZ * gridSize + offsetZ;

                    int worldY = getTerrainHeight(worldX, worldZ) + 1;

                    Utilities.Direction rotation = SelectAllowedRotation(selectedStructure, random);

                    Vector3Int worldPosition = new Vector3Int(worldX, worldY, worldZ);
                    placements.Add(new WorldGen.StructurePlacement(selectedStructure, worldPosition, rotation));
                }
            }

            return placements;
        }

        private int HashGridCell(int gridX, int gridZ, StructureType structureType, string generatorID)
        {
            int typeHash = structureType == StructureType.Environmental ? 1 : 2;
            int genHash = generatorID.GetHashCode();
            return (gridX * 73856093) ^ (gridZ * 19349663) ^ (seed * 83492791) ^ (typeHash * 50331653) ^ genHash;
        }

        private List<StructureData> GetFilteredStructures(string generatorID, StructureType structureType)
        {
            List<StructureData> filtered = new List<StructureData>();
            List<StructureData> generatorStructures = structureLoader.GetStructuresForGenerator(generatorID);

            foreach (StructureData structure in generatorStructures)
            {
                if (structure.type == structureType)
                {
                    filtered.Add(structure);
                }
            }

            return filtered;
        }

        private StructureData SelectStructureByWeight(List<StructureData> structures, System.Random random)
        {
            int totalWeight = 0;
            foreach (StructureData structure in structures)
            {
                totalWeight += structure.spawnWeight;
            }

            if (totalWeight == 0)
                return null;

            int randomWeight = random.Next(0, totalWeight);
            int currentWeight = 0;

            foreach (StructureData structure in structures)
            {
                currentWeight += structure.spawnWeight;
                if (randomWeight < currentWeight)
                {
                    return structure;
                }
            }

            return structures[structures.Count - 1];
        }

        private Utilities.Direction SelectAllowedRotation(StructureData structure, System.Random random)
        {
            List<Utilities.Direction> allowedDirections = new List<Utilities.Direction>();

            for (int i = 0; i < 4; i++)
            {
                if (structure.allowedRotations[i])
                {
                    allowedDirections.Add((Utilities.Direction)i);
                }
            }

            if (allowedDirections.Count == 0)
            {
                return Utilities.Direction.North;
            }

            int randomIndex = random.Next(0, allowedDirections.Count);
            return allowedDirections[randomIndex];
        }
    }
}