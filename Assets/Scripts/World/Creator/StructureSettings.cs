using UnityEngine;

namespace Pixension.Editor
{
    [System.Serializable]
    public class StructureSettings
    {
        public string id;
        public string displayName;
        public Structures.StructureType structureType;
        public string generatorID;
        public int spawnWeight;
        public bool[] allowedRotations;

        // Architecture specific
        public Vector3Int spawnRangeMin;
        public Vector3Int spawnRangeMax;
        public Structures.MobSpawnEntry[] mobEntries;

        public StructureSettings()
        {
            id = "";
            displayName = "";
            structureType = Structures.StructureType.Environmental;
            generatorID = "grassland";
            spawnWeight = 1;
            allowedRotations = new bool[] { true, true, true, true };
            spawnRangeMin = Vector3Int.zero;
            spawnRangeMax = new Vector3Int(10, 5, 10);
            mobEntries = new Structures.MobSpawnEntry[0];
        }

        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrEmpty(id))
            {
                errorMessage = "Structure ID is required";
                return false;
            }

            if (string.IsNullOrEmpty(generatorID))
            {
                errorMessage = "Generator ID is required";
                return false;
            }

            if (spawnWeight < 1)
            {
                errorMessage = "Spawn Weight must be at least 1";
                return false;
            }

            bool hasAnyRotation = false;
            foreach (bool allowed in allowedRotations)
            {
                if (allowed)
                {
                    hasAnyRotation = true;
                    break;
                }
            }

            if (!hasAnyRotation)
            {
                errorMessage = "At least one rotation must be allowed";
                return false;
            }

            errorMessage = "";
            return true;
        }

        public string GetFullID()
        {
            return $"{generatorID}_{id}";
        }
    }
}