using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Pixension.Editor
{
    public class StructureEditorWindow : EditorWindow
    {
        private enum EditorTab
        {
            Build,
            Settings,
            Save
        }

        private EditorTab currentTab = EditorTab.Build;
        private StructureEditorController controller;
        private Vector2 scrollPosition;

        // Build Tab
        private Color selectedColor = Color.red;
        private int selectedEntityIndex = 0;
        private string[] entityNames = new string[] { "None" };
        private string[] entityIDs = new string[] { "" };

        // Settings Tab
        private string structureID = "";
        private Structures.StructureType structureType = Structures.StructureType.Environmental;
        private string generatorID = "grassland";
        private int spawnWeight = 1;
        private bool[] allowedRotations = new bool[] { true, true, true, true };
        private bool showRotationFoldout = false;

        // Architecture Settings
        private Vector3Int spawnRangeMin = Vector3Int.zero;
        private Vector3Int spawnRangeMax = new Vector3Int(10, 5, 10);
        private List<MobEntryEditor> mobEntries = new List<MobEntryEditor>();
        private bool showMobsFoldout = false;

        // Save Tab
        private Mesh previewMesh;
        private Material previewMaterial;

        [System.Serializable]
        private class MobEntryEditor
        {
            public string mobID = "";
            public int initialCount = 1;
            public int maxCount = 3;
            public float spawnInterval = 5f;
        }

        [MenuItem("Window/Voxel/Structure Editor")]
        public static void ShowWindow()
        {
            StructureEditorWindow window = GetWindow<StructureEditorWindow>("Structure Editor");
            window.minSize = new Vector2(400, 600);
            window.Show();
        }

        private void OnEnable()
        {
            FindController();
            LoadEntityRegistry();
        }

        private void FindController()
        {
            controller = FindFirstObjectByType<StructureEditorController>();

            if (controller == null)
            {
                Debug.LogWarning("No StructureEditorController found in scene. Please create one.");
            }
        }

        private void LoadEntityRegistry()
        {
            Entities.BlockEntityRegistry registry = Resources.Load<Entities.BlockEntityRegistry>("BlockEntityRegistry");

            if (registry != null)
            {
                List<Entities.BlockEntityDefinition> entities = registry.GetAllEntities();
                entityNames = new string[entities.Count + 1];
                entityIDs = new string[entities.Count + 1];

                entityNames[0] = "None";
                entityIDs[0] = "";

                for (int i = 0; i < entities.Count; i++)
                {
                    entityNames[i + 1] = entities[i].displayName;
                    entityIDs[i + 1] = entities[i].entityID;
                }
            }
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                EditorGUILayout.HelpBox("No StructureEditorController found in scene!", MessageType.Warning);
                if (GUILayout.Button("Find Controller"))
                {
                    FindController();
                }
                return;
            }

            DrawTabs();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case EditorTab.Build:
                    DrawBuildTab();
                    break;
                case EditorTab.Settings:
                    DrawSettingsTab();
                    break;
                case EditorTab.Save:
                    DrawSaveTab();
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(currentTab == EditorTab.Build, "Build", EditorStyles.toolbarButton))
                currentTab = EditorTab.Build;

            if (GUILayout.Toggle(currentTab == EditorTab.Settings, "Settings", EditorStyles.toolbarButton))
                currentTab = EditorTab.Settings;

            if (GUILayout.Toggle(currentTab == EditorTab.Save, "Save", EditorStyles.toolbarButton))
                currentTab = EditorTab.Save;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
        }

        private void DrawBuildTab()
        {
            EditorGUILayout.LabelField("Build Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Cursor Position Control
            EditorGUILayout.LabelField("Cursor Position", EditorStyles.miniBoldLabel);
            controller.cursorPosition = EditorGUILayout.Vector3IntField("Position", controller.cursorPosition);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Y-Level", GUILayout.Width(80));
            controller.currentYLevel = EditorGUILayout.IntSlider(controller.currentYLevel, 0, controller.gridSize - 1);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Set Cursor to Y-Level"))
            {
                controller.cursorPosition.y = controller.currentYLevel;
            }

            EditorGUILayout.Space();

            // Color Picker
            EditorGUILayout.LabelField("Voxel Color", EditorStyles.miniBoldLabel);
            selectedColor = EditorGUILayout.ColorField("Color", selectedColor);

            if (GUILayout.Button("Apply Color"))
            {
                controller.currentColor = selectedColor;
                Debug.Log($"Color set to: {selectedColor}");
            }

            EditorGUILayout.Space();

            // Quick Place/Remove
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Place Voxel at Cursor", GUILayout.Height(30)))
            {
                controller.PlaceVoxelAt(controller.cursorPosition, new Voxels.VoxelData(Voxels.VoxelType.Solid, controller.currentColor));
            }
            if (GUILayout.Button("Remove Voxel at Cursor", GUILayout.Height(30)))
            {
                controller.RemoveVoxelAt(controller.cursorPosition);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Entity Selector
            EditorGUILayout.LabelField("Block Entity", EditorStyles.miniBoldLabel);
            selectedEntityIndex = EditorGUILayout.Popup("Entity", selectedEntityIndex, entityNames);

            controller.currentRotation = (Utilities.Direction)EditorGUILayout.EnumPopup("Rotation", controller.currentRotation);

            if (GUILayout.Button("Select Entity"))
            {
                controller.SetEntityID(entityIDs[selectedEntityIndex]);
                Debug.Log($"Entity set to: {entityNames[selectedEntityIndex]}");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Place Entity at Cursor", GUILayout.Height(30)))
            {
                controller.PlaceEntityAt(controller.cursorPosition, controller.currentEntityID, controller.currentRotation);
            }
            if (GUILayout.Button("Remove Entity at Cursor", GUILayout.Height(30)))
            {
                controller.RemoveEntityAt(controller.cursorPosition);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Mode Toggle
            EditorGUILayout.LabelField("Mode", EditorStyles.miniBoldLabel);
            bool isEntityMode = EditorGUILayout.Toggle("Entity Mode", controller.isEntityMode);
            if (isEntityMode != controller.isEntityMode)
            {
                controller.isEntityMode = isEntityMode;
            }

            EditorGUILayout.Space();

            // Current State
            EditorGUILayout.LabelField("Current State", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField($"Mode: {(controller.isEntityMode ? "Entity" : "Voxel")}");
            EditorGUILayout.LabelField($"Y-Level: {controller.currentYLevel}");
            EditorGUILayout.LabelField($"Cursor: {controller.cursorPosition}");
            EditorGUILayout.LabelField($"Rotation: {controller.currentRotation}");
            EditorGUILayout.LabelField($"Current Color: RGB({controller.currentColor.r:F2}, {controller.currentColor.g:F2}, {controller.currentColor.b:F2})");

            EditorGUILayout.Space();

            // Mesh Control
            EditorGUILayout.LabelField("Mesh", EditorStyles.miniBoldLabel);
            controller.autoRebuildMesh = EditorGUILayout.Toggle("Auto Rebuild", controller.autoRebuildMesh);

            if (GUILayout.Button("Rebuild Mesh Now", GUILayout.Height(25)))
            {
                controller.RebuildMesh();
            }

            EditorGUILayout.Space();

            // Clear All
            EditorGUILayout.LabelField("Actions", EditorStyles.miniBoldLabel);
            if (GUILayout.Button("Clear All", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear All",
                    "Are you sure you want to clear all voxels and entities?",
                    "Yes", "No"))
                {
                    controller.ClearAll();
                    Debug.Log("Editor cleared");
                }
            }
        }

        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Structure Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Basic Settings
            EditorGUILayout.LabelField("Basic", EditorStyles.miniBoldLabel);
            structureID = EditorGUILayout.TextField("Structure ID", structureID);
            structureType = (Structures.StructureType)EditorGUILayout.EnumPopup("Structure Type", structureType);
            generatorID = EditorGUILayout.TextField("Generator ID", generatorID);
            spawnWeight = EditorGUILayout.IntField("Spawn Weight", spawnWeight);

            EditorGUILayout.Space();

            // Allowed Rotations
            showRotationFoldout = EditorGUILayout.Foldout(showRotationFoldout, "Allowed Rotations", true);
            if (showRotationFoldout)
            {
                EditorGUI.indentLevel++;
                allowedRotations[0] = EditorGUILayout.Toggle("North", allowedRotations[0]);
                allowedRotations[1] = EditorGUILayout.Toggle("East", allowedRotations[1]);
                allowedRotations[2] = EditorGUILayout.Toggle("South", allowedRotations[2]);
                allowedRotations[3] = EditorGUILayout.Toggle("West", allowedRotations[3]);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            // Architecture Settings
            if (structureType == Structures.StructureType.Architecture)
            {
                EditorGUILayout.LabelField("Architecture", EditorStyles.miniBoldLabel);

                spawnRangeMin = EditorGUILayout.Vector3IntField("Spawn Range Min", spawnRangeMin);
                spawnRangeMax = EditorGUILayout.Vector3IntField("Spawn Range Max", spawnRangeMax);

                EditorGUILayout.Space();

                // Mob Entries
                showMobsFoldout = EditorGUILayout.Foldout(showMobsFoldout, "Mob Entries", true);
                if (showMobsFoldout)
                {
                    EditorGUI.indentLevel++;

                    for (int i = 0; i < mobEntries.Count; i++)
                    {
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField($"Mob {i + 1}", EditorStyles.boldLabel);

                        mobEntries[i].mobID = EditorGUILayout.TextField("Mob ID", mobEntries[i].mobID);
                        mobEntries[i].initialCount = EditorGUILayout.IntField("Initial Count", mobEntries[i].initialCount);
                        mobEntries[i].maxCount = EditorGUILayout.IntField("Max Count", mobEntries[i].maxCount);
                        mobEntries[i].spawnInterval = EditorGUILayout.FloatField("Spawn Interval", mobEntries[i].spawnInterval);

                        if (GUILayout.Button("Remove"))
                        {
                            mobEntries.RemoveAt(i);
                            i--;
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(5);
                    }

                    if (GUILayout.Button("Add Mob Entry"))
                    {
                        mobEntries.Add(new MobEntryEditor());
                    }

                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();

            // Validation
            EditorGUILayout.LabelField("Validation", EditorStyles.miniBoldLabel);
            bool isValid = ValidateSettings(out string errorMessage);

            if (isValid)
            {
                EditorGUILayout.HelpBox("Settings are valid", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
        }

        private void DrawSaveTab()
        {
            EditorGUILayout.LabelField("Export Structure", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Info
            StructureExportInfo info = controller.GetExportInfo();

            if (info.hasVoxels)
            {
                EditorGUILayout.LabelField($"Structure Size: {info.size.x} x {info.size.y} x {info.size.z}");
                EditorGUILayout.LabelField($"Solid Voxels: {info.solidVoxelCount}");
                EditorGUILayout.LabelField($"Entities: {info.entityCount}");
            }
            else
            {
                EditorGUILayout.HelpBox("No voxels found in structure", MessageType.Warning);
            }

            EditorGUILayout.Space();

            // Build Mesh Preview
            EditorGUILayout.LabelField("Preview", EditorStyles.miniBoldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build Optimized Mesh", GUILayout.Height(30)))
            {
                previewMesh = controller.BuildOptimizedMesh();
                controller.ClearPreview();
                if (previewMesh != null)
                {
                    controller.CreatePreviewObject(previewMesh);
                }
            }

            if (GUILayout.Button("Clear Preview", GUILayout.Height(30)))
            {
                controller.ClearPreview();
                previewMesh = null;
            }
            EditorGUILayout.EndHorizontal();

            if (previewMesh != null)
            {
                EditorGUILayout.HelpBox(
                    $"Vertices: {previewMesh.vertexCount}\n" +
                    $"Triangles: {previewMesh.triangles.Length / 3}\n" +
                    $"Submeshes: {previewMesh.subMeshCount}",
                    MessageType.Info);
            }

            EditorGUILayout.Space();

            // Save Mesh as Asset
            EditorGUILayout.LabelField("Save Mesh", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Save Mesh as Asset"))
            {
                if (!string.IsNullOrEmpty(structureID))
                {
                    string meshName = $"{generatorID}_{structureID}_mesh";
                    controller.BuildOptimizedMesh(saveMeshAsAsset: true, assetName: meshName);
                    EditorUtility.DisplayDialog("Mesh Saved",
                        $"Mesh saved as: Assets/Meshes/{meshName}.asset",
                        "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error",
                        "Please set Structure ID in Settings tab first",
                        "OK");
                }
            }

            EditorGUILayout.Space();

            // Export
            EditorGUILayout.LabelField("Export JSON", EditorStyles.miniBoldLabel);

            bool canExport = ValidateSettings(out string errorMessage) && info.hasVoxels;

            if (!canExport)
            {
                if (!info.hasVoxels)
                {
                    EditorGUILayout.HelpBox("No voxels to export", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox(errorMessage, MessageType.Warning);
                }
            }

            EditorGUI.BeginDisabledGroup(!canExport);

            if (GUILayout.Button("Export to JSON", GUILayout.Height(40)))
            {
                ExportToJSON();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // Export Path Info
            if (!string.IsNullOrEmpty(structureID) && !string.IsNullOrEmpty(generatorID))
            {
                string exportPath = $"Assets/Resources/Structures/{generatorID}_{structureID}.json";
                EditorGUILayout.HelpBox($"Export Path:\n{exportPath}", MessageType.Info);
            }
        }

        private bool ValidateSettings(out string errorMessage)
        {
            if (string.IsNullOrEmpty(structureID))
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

        private void ExportToJSON()
        {
            // Create settings from current values
            StructureSettings settings = new StructureSettings
            {
                id = structureID,
                displayName = structureID,
                structureType = structureType,
                generatorID = generatorID,
                spawnWeight = spawnWeight,
                allowedRotations = (bool[])allowedRotations.Clone(),
                spawnRangeMin = spawnRangeMin,
                spawnRangeMax = spawnRangeMax
            };

            // Convert mob entries
            if (structureType == Structures.StructureType.Architecture)
            {
                Structures.MobSpawnEntry[] mobSpawnEntries = new Structures.MobSpawnEntry[mobEntries.Count];
                for (int i = 0; i < mobEntries.Count; i++)
                {
                    mobSpawnEntries[i] = new Structures.MobSpawnEntry(
                        mobEntries[i].mobID,
                        mobEntries[i].initialCount,
                        mobEntries[i].maxCount,
                        mobEntries[i].spawnInterval
                    );
                }
                settings.mobEntries = mobSpawnEntries;
            }

            // Export using controller
            if (controller.ExportToJSON(settings, out string filePath))
            {
                StructureExportInfo info = controller.GetExportInfo();

                EditorUtility.DisplayDialog("Export Successful",
                    $"Structure saved to:\n{filePath}\n\n{info}",
                    "OK");

                AssetDatabase.Refresh();
            }
            else
            {
                EditorUtility.DisplayDialog("Export Failed",
                    "Failed to export structure. Check console for details.",
                    "OK");
            }
        }
    }
}