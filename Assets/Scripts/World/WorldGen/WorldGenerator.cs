using System.Collections.Generic;
using UnityEngine;

namespace Pixension.WorldGen
{
    public class StructurePlacement
    {
        public Structures.StructureData structure;
        public Vector3Int worldPosition;
        public Utilities.Direction rotation;

        public StructurePlacement(Structures.StructureData structure, Vector3Int worldPosition, Utilities.Direction rotation)
        {
            this.structure = structure;
            this.worldPosition = worldPosition;
            this.rotation = rotation;
        }
    }

    public abstract class WorldGenerator
    {
        protected int seed;
        protected NoiseGenerator noise;
        protected string generatorID;
        protected Structures.StructurePlacementGrid structureGrid;
        protected Structures.StructurePlacer structurePlacer;
        protected List<Structures.BlockEntityPlacement> pendingEntities;
        protected Voxels.BlockRegistry blockRegistry;

        public WorldGenerator(int seed, string id)
        {
            this.seed = seed;
            this.noise = new NoiseGenerator(seed);
            this.generatorID = id;
            this.structureGrid = new Structures.StructurePlacementGrid(seed, Structures.StructureLoader.Instance);
            this.structurePlacer = new Structures.StructurePlacer(Voxels.ChunkManager.Instance);
            this.pendingEntities = new List<Structures.BlockEntityPlacement>();
            this.blockRegistry = Voxels.BlockRegistry.Instance;
        }

        public abstract void GenerateChunkTerrain(Voxels.Chunk chunk);

        public abstract List<StructurePlacement> GetStructuresForChunk(Vector3Int chunkPos);

        public abstract Color GetSkyColor();

        public void GenerateChunk(Voxels.Chunk chunk)
        {
            GenerateChunkTerrain(chunk);

            List<StructurePlacement> placements = GetStructuresForChunk(chunk.chunkPosition);

            foreach (StructurePlacement placement in placements)
            {
                List<Structures.BlockEntityPlacement> entityPlacements = structurePlacer.PlaceStructure(placement);
                pendingEntities.AddRange(entityPlacements);

                if (placement.structure.type == Structures.StructureType.Architecture &&
                    placement.structure.architecture.mobs != null &&
                    placement.structure.architecture.mobs.Length > 0)
                {
                    CreateMobSpawner(placement.structure, placement.worldPosition);
                }
            }

            SpawnPendingEntities();

            OnChunkGenerated(chunk);
        }

        private void CreateMobSpawner(Structures.StructureData structure, Vector3Int worldPosition)
        {
            GameObject spawnerObject = new GameObject($"Spawner_{structure.structureID}");

            Mobs.MobSpawner spawner = spawnerObject.AddComponent<Mobs.MobSpawner>();
            spawner.entityType = Entities.EntityType.Static;
            spawner.Initialize(structure.structureID, worldPosition, Utilities.Direction.North);
            spawner.InitializeSpawner(structure.architecture, worldPosition);

            Debug.Log($"Created MobSpawner for {structure.structureID} at {worldPosition} with {structure.architecture.mobs.Length} mob types");
        }

        private void SpawnPendingEntities()
        {
            foreach (Structures.BlockEntityPlacement entityPlacement in pendingEntities)
            {
                Entities.BlockEntityLoader.Instance.InstantiateEntity(
                    entityPlacement.entityID,
                    entityPlacement.localPosition,
                    entityPlacement.facing
                );
            }

            pendingEntities.Clear();
        }

        public virtual int GetTerrainHeight(int worldX, int worldZ)
        {
            // Default implementation - should be overridden by specific generators
            float noiseValue = noise.Get2DNoise(worldX, worldZ, 200f, 4, 0.5f);
            int baseHeight = 1024;
            int variation = Mathf.RoundToInt(noiseValue * 200f);
            int height = baseHeight + variation;
            return Mathf.Clamp(height, 0, 2048);
        }

        public virtual void OnChunkGenerated(Voxels.Chunk chunk)
        {
        }

        public string GetGeneratorID()
        {
            return generatorID;
        }

        public int GetSeed()
        {
            return seed;
        }
    }
}