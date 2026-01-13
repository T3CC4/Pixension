using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Structures
{
    public class StructureLoader : MonoBehaviour
    {
        private static StructureLoader instance;
        public static StructureLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("StructureLoader");
                    instance = go.AddComponent<StructureLoader>();
                    DontDestroyOnLoad(go);
                    instance.LoadAllStructures();
                }
                return instance;
            }
        }

        private Dictionary<string, StructureData> structures = new Dictionary<string, StructureData>();
        private Dictionary<StructureType, List<StructureData>> structuresByType = new Dictionary<StructureType, List<StructureData>>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllStructures();
        }

        public void LoadAllStructures()
        {
            structures.Clear();
            structuresByType.Clear();

            structuresByType[StructureType.Environmental] = new List<StructureData>();
            structuresByType[StructureType.Architecture] = new List<StructureData>();

            TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>("Structures");

            foreach (TextAsset jsonFile in jsonFiles)
            {
                try
                {
                    StructureData structure = JsonUtility.FromJson<StructureData>(jsonFile.text);

                    if (structure != null && !string.IsNullOrEmpty(structure.structureID))
                    {
                        structures[structure.structureID] = structure;
                        structuresByType[structure.type].Add(structure);
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to load structure from {jsonFile.name}: Invalid structure data");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error loading structure from {jsonFile.name}: {e.Message}");
                }
            }

            Debug.Log($"Loaded {structures.Count} structures from Resources/Structures/");
        }

        public StructureData GetStructure(string id)
        {
            if (structures.TryGetValue(id, out StructureData structure))
            {
                return structure;
            }

            Debug.LogWarning($"Structure with ID '{id}' not found");
            return null;
        }

        public List<StructureData> GetStructuresForGenerator(string generatorID)
        {
            List<StructureData> result = new List<StructureData>();

            foreach (var structure in structures.Values)
            {
                if (structure.structureID.StartsWith(generatorID + "_"))
                {
                    result.Add(structure);
                }
            }

            return result;
        }

        public List<StructureData> GetStructuresByType(StructureType type)
        {
            if (structuresByType.TryGetValue(type, out List<StructureData> list))
            {
                return new List<StructureData>(list);
            }
            return new List<StructureData>();
        }

        public Dictionary<string, StructureData> GetAllStructures()
        {
            return new Dictionary<string, StructureData>(structures);
        }
    }
}