using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Editor
{
    [System.Serializable]
    public class EditorBlockEntity
    {
        public string entityID;
        public Vector3Int position;
        public Utilities.Direction facing;

        public EditorBlockEntity(string id, Vector3Int pos, Utilities.Direction dir)
        {
            entityID = id;
            position = pos;
            facing = dir;
        }
    }

    [ExecuteAlways]
    public class StructureEditorController : MonoBehaviour
    {
        [Header("Editor Data")]
        public Voxels.VoxelData[,,] editorVoxels;
        public List<EditorBlockEntity> editorEntities = new List<EditorBlockEntity>();

        [Header("Editor State")]
        public Vector3Int cursorPosition;
        public int currentYLevel = 0;
        public Color currentColor = Color.red;
        public string currentEntityID = "";
        public bool isEntityMode = false;
        public Utilities.Direction currentRotation = Utilities.Direction.North;

        [Header("Grid Settings")]
        public int gridSize = 64;

        [Header("Rendering")]
        public bool autoRebuildMesh = true;
        private GameObject meshObject;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private bool isDirty = false;

        [Header("Color Palette")]
        public Color[] colorPalette = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.white,
            Color.gray,
            new Color(0.6f, 0.4f, 0.2f) // Brown
        };

        private void OnEnable()
        {
            InitializeEditor();
            CreateMeshObject();
            RebuildMesh();
        }

        private void OnDisable()
        {
            DestroyMeshObject();
        }

        private void InitializeEditor()
        {
            if (editorVoxels == null || editorVoxels.GetLength(0) != gridSize)
            {
                editorVoxels = new Voxels.VoxelData[gridSize, gridSize, gridSize];

                for (int x = 0; x < gridSize; x++)
                {
                    for (int y = 0; y < gridSize; y++)
                    {
                        for (int z = 0; z < gridSize; z++)
                        {
                            editorVoxels[x, y, z] = Voxels.VoxelData.Air;
                        }
                    }
                }
            }
        }

        private void CreateMeshObject()
        {
            if (meshObject == null)
            {
                meshObject = new GameObject("StructureEditorMesh");
                meshObject.transform.SetParent(transform);
                meshObject.transform.localPosition = Vector3.zero;
                meshObject.hideFlags = HideFlags.DontSave;

                meshFilter = meshObject.AddComponent<MeshFilter>();
                meshRenderer = meshObject.AddComponent<MeshRenderer>();

                // Use VoxelMaterialManager for proper vertex color rendering
                meshRenderer.material = Voxels.VoxelMaterialManager.Instance.GetMaterial();
            }
        }

        private void DestroyMeshObject()
        {
            if (meshObject != null)
            {
                DestroyImmediate(meshObject);
                meshObject = null;
                meshFilter = null;
                meshRenderer = null;
            }
        }

        private void Update()
        {
            // Im Editor-Mode Mesh rebuilden wenn dirty
            if (isDirty && autoRebuildMesh)
            {
                RebuildMesh();
                isDirty = false;
            }
        }

        public void RebuildMesh()
        {
            if (meshFilter == null)
            {
                CreateMeshObject();
                return;
            }

            Utilities.MeshBuilder builder = new Utilities.MeshBuilder();

            // Finde Bounds f�r optimierte Mesh-Generierung
            GetStructureBounds(out Vector3Int min, out Vector3Int max);

            // Nur bauen wenn Voxels vorhanden
            bool hasVoxels = min.x < gridSize;
            if (!hasVoxels)
            {
                meshFilter.mesh = null;
                return;
            }

            // Baue Mesh mit Face Culling
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        Voxels.VoxelData voxel = editorVoxels[x, y, z];
                        if (!voxel.IsSolid)
                            continue;

                        bool[] visibleFaces = new bool[6];

                        // Face Culling - nur sichtbare Faces rendern
                        // Front (+Z)
                        visibleFaces[0] = z >= max.z || !editorVoxels[x, y, z + 1].IsSolid;
                        // Back (-Z)
                        visibleFaces[1] = z <= min.z || !editorVoxels[x, y, z - 1].IsSolid;
                        // Left (-X)
                        visibleFaces[2] = x <= min.x || !editorVoxels[x - 1, y, z].IsSolid;
                        // Right (+X)
                        visibleFaces[3] = x >= max.x || !editorVoxels[x + 1, y, z].IsSolid;
                        // Top (+Y)
                        visibleFaces[4] = y >= max.y || !editorVoxels[x, y + 1, z].IsSolid;
                        // Bottom (-Y)
                        visibleFaces[5] = y <= min.y || !editorVoxels[x, y - 1, z].IsSolid;

                        // Konvertiere Grid zu World Position
                        Vector3Int worldPos = GridToWorldPosition(new Vector3Int(x, y, z));
                        Vector3 position = new Vector3(worldPos.x, worldPos.y, worldPos.z);

                        builder.AddCube(position, voxel.color, visibleFaces);
                    }
                }
            }

            Mesh mesh = builder.GenerateMesh();
            meshFilter.mesh = mesh;

            Debug.Log($"Mesh rebuilt: {mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles");
        }

        public void PlaceVoxelAt(Vector3Int position, Voxels.VoxelData voxel)
        {
            Vector3Int gridPos = WorldToGridPosition(position);

            if (IsValidGridPosition(gridPos))
            {
                editorVoxels[gridPos.x, gridPos.y, gridPos.z] = voxel;
                isDirty = true;
                Debug.Log($"Placed voxel at {position} (grid: {gridPos})");
            }
        }

        public void RemoveVoxelAt(Vector3Int position)
        {
            Vector3Int gridPos = WorldToGridPosition(position);

            if (IsValidGridPosition(gridPos))
            {
                editorVoxels[gridPos.x, gridPos.y, gridPos.z] = Voxels.VoxelData.Air;
                isDirty = true;
                Debug.Log($"Removed voxel at {position} (grid: {gridPos})");
            }
        }

        public void PlaceEntityAt(Vector3Int position, string entityID, Utilities.Direction facing)
        {
            if (string.IsNullOrEmpty(entityID))
            {
                Debug.LogWarning("No entity ID selected");
                return;
            }

            // Pr�fe ob bereits Entity an Position
            EditorBlockEntity existing = editorEntities.Find(e => e.position == position);
            if (existing != null)
            {
                editorEntities.Remove(existing);
            }

            editorEntities.Add(new EditorBlockEntity(entityID, position, facing));
            Debug.Log($"Placed entity {entityID} at {position} facing {facing}");
        }

        public void RemoveEntityAt(Vector3Int position)
        {
            EditorBlockEntity existing = editorEntities.Find(e => e.position == position);
            if (existing != null)
            {
                editorEntities.Remove(existing);
                Debug.Log($"Removed entity at {position}");
            }
        }

        private Vector3Int WorldToGridPosition(Vector3Int worldPos)
        {
            int halfSize = gridSize / 2;
            return new Vector3Int(
                worldPos.x + halfSize,
                worldPos.y,
                worldPos.z + halfSize
            );
        }

        private Vector3Int GridToWorldPosition(Vector3Int gridPos)
        {
            int halfSize = gridSize / 2;
            return new Vector3Int(
                gridPos.x - halfSize,
                gridPos.y,
                gridPos.z - halfSize
            );
        }

        private bool IsValidGridPosition(Vector3Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gridSize &&
                   gridPos.y >= 0 && gridPos.y < gridSize &&
                   gridPos.z >= 0 && gridPos.z < gridSize;
        }

        public void SetEntityID(string entityID)
        {
            currentEntityID = entityID;
            isEntityMode = true;
            Debug.Log($"Entity ID set to: {entityID}");
        }

        public void ClearAll()
        {
            InitializeEditor();
            editorEntities.Clear();
            isDirty = true;
            Debug.Log("Editor cleared");
        }

        public Mesh BuildOptimizedMesh(bool saveMeshAsAsset = false, string assetName = "StructureMesh")
        {
            Utilities.MeshBuilder builder = new Utilities.MeshBuilder();

            // Finde Bounds f�r optimierte Mesh-Generierung
            GetStructureBounds(out Vector3Int min, out Vector3Int max);

            // Nur bauen wenn Voxels vorhanden
            bool hasVoxels = min.x < gridSize;
            if (!hasVoxels)
            {
                return null;
            }

            // Baue Mesh mit Face Culling
            for (int x = min.x; x <= max.x; x++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int z = min.z; z <= max.z; z++)
                    {
                        Voxels.VoxelData voxel = editorVoxels[x, y, z];
                        if (!voxel.IsSolid)
                            continue;

                        bool[] visibleFaces = new bool[6];

                        // Face Culling
                        visibleFaces[0] = z >= max.z || !editorVoxels[x, y, z + 1].IsSolid;
                        visibleFaces[1] = z <= min.z || !editorVoxels[x, y, z - 1].IsSolid;
                        visibleFaces[2] = x <= min.x || !editorVoxels[x - 1, y, z].IsSolid;
                        visibleFaces[3] = x >= max.x || !editorVoxels[x + 1, y, z].IsSolid;
                        visibleFaces[4] = y >= max.y || !editorVoxels[x, y + 1, z].IsSolid;
                        visibleFaces[5] = y <= min.y || !editorVoxels[x, y - 1, z].IsSolid;

                        // Position relativ zum Minimum (f�r Export)
                        Vector3 position = new Vector3(x - min.x, y - min.y, z - min.z);
                        builder.AddCube(position, voxel.color, visibleFaces);
                    }
                }
            }

            Mesh mesh = builder.GenerateMesh();
            Debug.Log($"Optimized mesh built: {mesh.vertexCount} vertices, {mesh.triangles.Length / 3} triangles");

#if UNITY_EDITOR
            // Speichere Mesh als Asset wenn gew�nscht
            if (saveMeshAsAsset && mesh != null)
            {
                string meshPath = $"Assets/Meshes/{assetName}.asset";

                // Erstelle Ordner falls nicht vorhanden
                if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Meshes"))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets", "Meshes");
                }

                // Speichere oder update Mesh
                Mesh existingMesh = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existingMesh != null)
                {
                    existingMesh.Clear();
                    existingMesh.vertices = mesh.vertices;
                    existingMesh.triangles = mesh.triangles;
                    existingMesh.normals = mesh.normals;
                    existingMesh.colors = mesh.colors;
                    existingMesh.uv = mesh.uv;
                    existingMesh.RecalculateBounds();
                    UnityEditor.EditorUtility.SetDirty(existingMesh);
                    Debug.Log($"Updated mesh asset: {meshPath}");
                }
                else
                {
                    UnityEditor.AssetDatabase.CreateAsset(mesh, meshPath);
                    Debug.Log($"Created mesh asset: {meshPath}");
                }

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
            }
#endif

            return mesh;
        }

        public void ClearPreview()
        {
            if (meshFilter != null)
            {
                meshFilter.mesh = null;
            }
        }

        public Mesh GetCurrentMesh()
        {
            if (meshFilter != null)
            {
                return meshFilter.sharedMesh;
            }
            return null;
        }

        public Vector3Int GetStructureSize()
        {
            Vector3Int min = new Vector3Int(gridSize, gridSize, gridSize);
            Vector3Int max = new Vector3Int(0, 0, 0);
            bool hasVoxels = false;

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        if (editorVoxels[x, y, z].IsSolid)
                        {
                            hasVoxels = true;
                            min.x = Mathf.Min(min.x, x);
                            min.y = Mathf.Min(min.y, y);
                            min.z = Mathf.Min(min.z, z);
                            max.x = Mathf.Max(max.x, x);
                            max.y = Mathf.Max(max.y, y);
                            max.z = Mathf.Max(max.z, z);
                        }
                    }
                }
            }

            if (!hasVoxels)
                return Vector3Int.one;

            return new Vector3Int(
                max.x - min.x + 1,
                max.y - min.y + 1,
                max.z - min.z + 1
            );
        }

        public void GetStructureBounds(out Vector3Int min, out Vector3Int max)
        {
            min = new Vector3Int(gridSize, gridSize, gridSize);
            max = new Vector3Int(0, 0, 0);

            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    for (int z = 0; z < gridSize; z++)
                    {
                        if (editorVoxels[x, y, z].IsSolid)
                        {
                            min.x = Mathf.Min(min.x, x);
                            min.y = Mathf.Min(min.y, y);
                            min.z = Mathf.Min(min.z, z);
                            max.x = Mathf.Max(max.x, x);
                            max.y = Mathf.Max(max.y, y);
                            max.z = Mathf.Max(max.z, z);
                        }
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (editorVoxels == null)
                return;

            // Zeichne nur Entities als Gizmos (Voxels werden als Mesh gerendert)
            DrawEntities();
            DrawCursor();
        }

        private void DrawEntities()
        {
            Gizmos.color = Color.magenta;

            foreach (EditorBlockEntity entity in editorEntities)
            {
                Vector3 center = new Vector3(entity.position.x + 0.5f, entity.position.y + 0.5f, entity.position.z + 0.5f);
                Gizmos.DrawWireSphere(center, 0.4f);

                // Facing Indikator
                Vector3 forward = Vector3.forward;
                switch (entity.facing)
                {
                    case Utilities.Direction.North: forward = Vector3.forward; break;
                    case Utilities.Direction.East: forward = Vector3.right; break;
                    case Utilities.Direction.South: forward = Vector3.back; break;
                    case Utilities.Direction.West: forward = Vector3.left; break;
                }

                Gizmos.DrawLine(center, center + forward * 0.5f);
            }
        }

        private void DrawCursor()
        {
            Gizmos.color = isEntityMode ? Color.magenta : currentColor;
            Vector3 center = new Vector3(cursorPosition.x + 0.5f, cursorPosition.y + 0.5f, cursorPosition.z + 0.5f);

            if (isEntityMode)
            {
                Gizmos.DrawWireSphere(center, 0.5f);
            }
            else
            {
                Gizmos.DrawWireCube(center, Vector3.one * 1.1f);
            }
        }

        public StructureExportInfo GetExportInfo()
        {
            StructureExportInfo info = new StructureExportInfo();

            GetStructureBounds(out Vector3Int min, out Vector3Int max);
            bool hasVoxels = min.x < gridSize;

            if (hasVoxels)
            {
                info.hasVoxels = true;
                info.size = new Vector3Int(
                    max.x - min.x + 1,
                    max.y - min.y + 1,
                    max.z - min.z + 1
                );

                // Z�hle Solid Voxels
                int solidCount = 0;
                for (int x = min.x; x <= max.x; x++)
                {
                    for (int y = min.y; y <= max.y; y++)
                    {
                        for (int z = min.z; z <= max.z; z++)
                        {
                            if (editorVoxels[x, y, z].IsSolid)
                            {
                                solidCount++;
                            }
                        }
                    }
                }

                info.solidVoxelCount = solidCount;
                info.entityCount = editorEntities.Count;
                info.min = min;
                info.max = max;
            }

            return info;
        }

        public GameObject CreatePreviewObject(Mesh previewMesh)
        {
            GameObject previewObject = new GameObject("StructurePreview");
            previewObject.transform.SetParent(transform);
            previewObject.transform.localPosition = Vector3.zero;

            MeshFilter filter = previewObject.AddComponent<MeshFilter>();
            filter.mesh = previewMesh;

            MeshRenderer renderer = previewObject.AddComponent<MeshRenderer>();
            // Use VoxelMaterialManager for proper vertex color rendering
            renderer.material = Voxels.VoxelMaterialManager.Instance.GetMaterial();

            Debug.Log("Preview object created");
            return previewObject;
        }

        public bool ExportToJSON(StructureSettings settings, out string filePath)
        {
            filePath = null;

#if UNITY_EDITOR
            // Finde Bounds
            GetStructureBounds(out Vector3Int min, out Vector3Int max);
            bool hasVoxels = min.x < gridSize;

            if (!hasVoxels)
            {
                Debug.LogError("No voxels found in structure");
                return false;
            }

            Vector3Int size = new Vector3Int(
                max.x - min.x + 1,
                max.y - min.y + 1,
                max.z - min.z + 1
            );

            // Erstelle StructureData
            Structures.StructureData structure = new Structures.StructureData(
                settings.generatorID + "_" + settings.id,
                settings.displayName,
                settings.structureType,
                size
            );

            structure.spawnWeight = settings.spawnWeight;
            structure.allowedRotations = (bool[])settings.allowedRotations.Clone();

            // Kopiere Voxels (relativ zu min)
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Voxels.VoxelData voxel = editorVoxels[min.x + x, min.y + y, min.z + z];
                        structure.SetVoxel(x, y, z, voxel);
                    }
                }
            }

            // Konvertiere Entities (relativ zu min)
            List<Structures.BlockEntityPlacement> entityPlacements = new List<Structures.BlockEntityPlacement>();
            foreach (EditorBlockEntity entity in editorEntities)
            {
                Vector3Int gridPos = WorldToGridPosition(entity.position);
                Vector3Int localPos = new Vector3Int(
                    gridPos.x - min.x,
                    gridPos.y - min.y,
                    gridPos.z - min.z
                );

                entityPlacements.Add(new Structures.BlockEntityPlacement(
                    entity.entityID,
                    localPos,
                    entity.facing
                ));
            }
            structure.blockEntities = entityPlacements.ToArray();

            // Architecture Data
            if (settings.structureType == Structures.StructureType.Architecture)
            {
                structure.architecture = new Structures.ArchitectureData(
                    settings.spawnRangeMin,
                    settings.spawnRangeMax,
                    settings.mobEntries ?? new Structures.MobSpawnEntry[0]
                );
            }

            // Speichere JSON
            string json = JsonUtility.ToJson(structure, true);
            string directoryPath = "Assets/Resources/Structures";

            if (!System.IO.Directory.Exists(directoryPath))
            {
                System.IO.Directory.CreateDirectory(directoryPath);
            }

            filePath = $"{directoryPath}/{structure.structureID}.json";
            System.IO.File.WriteAllText(filePath, json);

            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"Structure exported: {filePath}");
            return true;
#else
            Debug.LogError("ExportToJSON only works in Editor");
            return false;
#endif
        }
    }

    [System.Serializable]
    public struct StructureExportInfo
    {
        public bool hasVoxels;
        public Vector3Int size;
        public int solidVoxelCount;
        public int entityCount;
        public Vector3Int min;
        public Vector3Int max;

        public override string ToString()
        {
            return $"Size: {size}\nSolid Voxels: {solidVoxelCount}\nEntities: {entityCount}";
        }
    }
}