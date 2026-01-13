using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Dimensions
{
    public class DimensionManager : MonoBehaviour
    {
        private static DimensionManager instance;
        public static DimensionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("DimensionManager");
                    instance = go.AddComponent<DimensionManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private Dictionary<string, Dimension> dimensions = new Dictionary<string, Dimension>();
        private Dictionary<string, Type> registeredGenerators = new Dictionary<string, Type>();
        private Dimension activeDimension;
        private int worldSeed;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Initialize(int seed)
        {
            worldSeed = seed;

            RegisterGenerator("grassland", typeof(WorldGen.GrasslandGenerator));
            RegisterGenerator("desert", typeof(WorldGen.DesertGenerator));

            CreateDimension("overworld", "grassland");

            SwitchDimension("overworld");
        }

        public void RegisterGenerator(string id, Type generatorType)
        {
            if (!typeof(WorldGen.WorldGenerator).IsAssignableFrom(generatorType))
            {
                Debug.LogError($"Type {generatorType.Name} is not a WorldGenerator");
                return;
            }

            registeredGenerators[id] = generatorType;
            Debug.Log($"Registered generator: {id} -> {generatorType.Name}");
        }

        public Dimension CreateDimension(string dimensionID, string generatorID)
        {
            if (dimensions.ContainsKey(dimensionID))
            {
                Debug.LogWarning($"Dimension {dimensionID} already exists");
                return dimensions[dimensionID];
            }

            if (!registeredGenerators.TryGetValue(generatorID, out Type generatorType))
            {
                Debug.LogError($"Generator {generatorID} not registered");
                return null;
            }

            WorldGen.WorldGenerator generator = (WorldGen.WorldGenerator)Activator.CreateInstance(generatorType, worldSeed);

            Dimension dimension = new Dimension(dimensionID, generator);
            dimensions[dimensionID] = dimension;

            Debug.Log($"Created dimension: {dimensionID} with generator {generatorID}");

            return dimension;
        }

        public void SwitchDimension(string dimensionID)
        {
            if (!dimensions.TryGetValue(dimensionID, out Dimension dimension))
            {
                Debug.LogError($"Dimension {dimensionID} does not exist");
                return;
            }

            if (activeDimension != null)
            {
                activeDimension.SetActive(false);
                activeDimension.UnloadAllChunks();
            }

            activeDimension = dimension;
            activeDimension.SetActive(true);

            RenderSettings.ambientLight = activeDimension.skyColor;
            Camera.main.backgroundColor = activeDimension.skyColor;

            Voxels.ChunkManager.Instance.SetActiveDimension(activeDimension);

            Debug.Log($"Switched to dimension: {dimensionID}");
        }

        public Dimension GetActiveDimension()
        {
            return activeDimension;
        }

        public Dimension GetDimension(string dimensionID)
        {
            if (dimensions.TryGetValue(dimensionID, out Dimension dimension))
            {
                return dimension;
            }
            return null;
        }

        public Dictionary<string, Dimension> GetAllDimensions()
        {
            return new Dictionary<string, Dimension>(dimensions);
        }
    }
}