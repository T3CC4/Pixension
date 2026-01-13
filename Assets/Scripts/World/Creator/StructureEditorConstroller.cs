using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pixension.Editor
{
    [ExecuteInEditMode]
    public class StructureEditorController : MonoBehaviour
    {
        [Header("Structure Settings")]
        public Vector3Int structureSize = new Vector3Int(16, 16, 16);
        public string structureName = "NewStructure";
        public Structures.StructureType structureType = Structures.StructureType.Environmental;

        [Header("Editor State")]
        public int currentYLevel = 0;
        public Color currentColor = Color.green;
        public string currentEntityID = "";
        public bool isEntityMode = false;
        public Utilities.Direction currentRotation = Utilities.Direction.North;

        private Voxels.VoxelData[,,] editorVoxels;
        private List<Structures.BlockEntityPlacement> editorEntities = new List<Structures.BlockEntityPlacement>();
        private Vector3Int cursorPosition = Vector3Int.zero;
        private bool hasCursor = false;

        private Color[] quickColors = new Color[]
        {
            Color.green,    // 1
            Color.red,      // 2
            Color.blue,     // 3
            Color.yellow,   // 4
            Color.cyan,     // 5
            Color.magenta,  // 6
            Color.white,    // 7
            Color.gray,     // 8
            new Color(0.6f, 0.4f, 0.2f) // 9 - Brown
        };

        private void OnEnable()
        {
            InitializeEditorVoxels();
        }

        private void InitializeEditorVoxels()
        {
            editorVoxels = new Voxels.VoxelData[structureSize.x, structureSize.y, structureSize.z];

            for (int x = 0; x < structureSize.x; x++)
            {
                for (int y = 0; y < structureSize.y; y++)
                {
                    for (int z = 0; z < structureSize.z; z++)
                    {
                        editorVoxels[x, y, z] = Voxels.VoxelData.Air;
                    }
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                HandleEditorInput();
            }
        }

        private void HandleEditorInput()
        {
            UpdateCursor();
            HandlePlacement();
            HandleYLevelChange();
            HandleColorSelection();
            HandleRotation();
            HandleModeSwitch();
        }

        private void UpdateCursor()
        {
#if UNITY_EDITOR
            Camera sceneCamera = Camera.current;
            if (sceneCamera == null) sceneCamera = SceneView.lastActiveSceneView?.camera;
            if (sceneCamera == null) return;

            if (Event.current == null) return;

            Vector2 mousePos = Event.current.mousePosition;
            mousePos.y = sceneCamera.pixelHeight - mousePos.y;
            Ray ray = sceneCamera.ScreenPointToRay(mousePos);

            float yPlaneHeight = currentYLevel;
            float t = (yPlaneHeight - ray.origin.y) / ray.direction.y;

            if (t > 0)
            {
                Vector3 hitPoint = ray.origin + ray.direction * t;

                cursorPosition = new Vector3Int(
                    Mathf.FloorToInt(hitPoint.x - transform.position.x + structureSize.x * 0.5f),
                    currentYLevel,
                    Mathf.FloorToInt(hitPoint.z - transform.position.z + structureSize.z * 0.5f)
                );

                hasCursor = IsInBounds(cursorPosition);
            }
            else
            {
                hasCursor = false;
            }
#endif
        }

        private void HandlePlacement()
        {
            var mouse = Mouse.current;
            if (mouse == null || !hasCursor) return;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                if (isEntityMode)
                {
                    PlaceEntity();
                }
                else
                {
                    PlaceVoxel();
                }
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
            {
                if (isEntityMode)
                {
                    RemoveEntity();
                }
                else
                {
                    RemoveVoxel();
                }
                Event.current.Use();
            }
        }

        private void HandleYLevelChange()
        {
            if (Event.current.type == EventType.ScrollWheel)
            {
                int delta = Event.current.delta.y > 0 ? -1 : 1;
                currentYLevel = Mathf.Clamp(currentYLevel + delta, 0, structureSize.y - 1);
                Event.current.Use();
            }

            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame)
                {
                    currentYLevel = Mathf.Clamp(currentYLevel + 1, 0, structureSize.y - 1);
                }
                if (keyboard.downArrowKey.wasPressedThisFrame)
                {
                    currentYLevel = Mathf.Clamp(currentYLevel - 1, 0, structureSize.y - 1);
                }
            }
        }

        private void HandleColorSelection()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode >= KeyCode.Alpha1 && Event.current.keyCode <= KeyCode.Alpha9)
                {
                    int index = Event.current.keyCode - KeyCode.Alpha1;
                    if (index < quickColors.Length)
                    {
                        currentColor = quickColors[index];
                        Debug.Log($"Selected color: {currentColor}");
                        Event.current.Use();
                    }
                }
            }
        }

        private void HandleRotation()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.qKey.wasPressedThisFrame)
            {
                currentRotation = Utilities.RotationHelper.RotateDirection(currentRotation, -1);
                Debug.Log($"Rotation: {currentRotation}");
            }
            if (keyboard.eKey.wasPressedThisFrame)
            {
                currentRotation = Utilities.RotationHelper.RotateDirection(currentRotation, 1);
                Debug.Log($"Rotation: {currentRotation}");
            }
        }

        private void HandleModeSwitch()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                isEntityMode = !isEntityMode;
                Debug.Log($"Mode: {(isEntityMode ? "Entity" : "Voxel")}");
            }
        }

        private void PlaceVoxel()
        {
            if (!IsInBounds(cursorPosition)) return;

            editorVoxels[cursorPosition.x, cursorPosition.y, cursorPosition.z] =
                new Voxels.VoxelData(Voxels.VoxelType.Solid, currentColor);
        }

        private void RemoveVoxel()
        {
            if (!IsInBounds(cursorPosition)) return;

            editorVoxels[cursorPosition.x, cursorPosition.y, cursorPosition.z] = Voxels.VoxelData.Air;
        }

        private void PlaceEntity()
        {
            if (!IsInBounds(cursorPosition) || string.IsNullOrEmpty(currentEntityID)) return;

            RemoveEntity();

            Structures.BlockEntityPlacement placement = new Structures.BlockEntityPlacement(
                currentEntityID,
                cursorPosition,
                currentRotation
            );

            editorEntities.Add(placement);
        }

        private void RemoveEntity()
        {
            editorEntities.RemoveAll(e => e.localPosition == cursorPosition);
        }

        private bool IsInBounds(Vector3Int pos)
        {
            return pos.x >= 0 && pos.x < structureSize.x &&
                   pos.y >= 0 && pos.y < structureSize.y &&
                   pos.z >= 0 && pos.z < structureSize.z;
        }

        public void ClearAll()
        {
            InitializeEditorVoxels();
            editorEntities.Clear();
        }

        public Structures.StructureData ExportStructure()
        {
            Structures.StructureData structure = new Structures.StructureData(
                structureName.ToLower().Replace(" ", "_"),
                structureName,
                structureType,
                structureSize
            );

            for (int x = 0; x < structureSize.x; x++)
            {
                for (int y = 0; y < structureSize.y; y++)
                {
                    for (int z = 0; z < structureSize.z; z++)
                    {
                        structure.SetVoxel(x, y, z, editorVoxels[x, y, z]);
                    }
                }
            }

            structure.blockEntities = editorEntities.ToArray();

            return structure;
        }

        private void OnDrawGizmos()
        {
            if (editorVoxels == null) return;

            DrawGrid();
            DrawVoxels();
            DrawEntities();
            DrawCenter();
            DrawCursor();
        }

        private void DrawGrid()
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);

            for (int x = 0; x <= structureSize.x; x++)
            {
                Vector3 start = transform.position + new Vector3(x - structureSize.x * 0.5f, currentYLevel, -structureSize.z * 0.5f);
                Vector3 end = transform.position + new Vector3(x - structureSize.x * 0.5f, currentYLevel, structureSize.z * 0.5f);
                Gizmos.DrawLine(start, end);
            }

            for (int z = 0; z <= structureSize.z; z++)
            {
                Vector3 start = transform.position + new Vector3(-structureSize.x * 0.5f, currentYLevel, z - structureSize.z * 0.5f);
                Vector3 end = transform.position + new Vector3(structureSize.x * 0.5f, currentYLevel, z - structureSize.z * 0.5f);
                Gizmos.DrawLine(start, end);
            }
        }

        private void DrawVoxels()
        {
            for (int x = 0; x < structureSize.x; x++)
            {
                for (int y = 0; y < structureSize.y; y++)
                {
                    for (int z = 0; z < structureSize.z; z++)
                    {
                        Voxels.VoxelData voxel = editorVoxels[x, y, z];
                        if (!voxel.IsSolid) continue;

                        Vector3 worldPos = transform.position + new Vector3(
                            x - structureSize.x * 0.5f + 0.5f,
                            y + 0.5f,
                            z - structureSize.z * 0.5f + 0.5f
                        );

                        Gizmos.color = voxel.color;
                        Gizmos.DrawWireCube(worldPos, Vector3.one * 0.95f);
                    }
                }
            }
        }

        private void DrawEntities()
        {
            Gizmos.color = Color.yellow;
            foreach (var entity in editorEntities)
            {
                Vector3 worldPos = transform.position + new Vector3(
                    entity.localPosition.x - structureSize.x * 0.5f + 0.5f,
                    entity.localPosition.y + 0.5f,
                    entity.localPosition.z - structureSize.z * 0.5f + 0.5f
                );

                Gizmos.DrawWireCube(worldPos, Vector3.one * 0.9f);
                Gizmos.DrawLine(worldPos, worldPos + Vector3.up * 1.5f);
            }
        }

        private void DrawCenter()
        {
            Gizmos.color = Color.red;
            Vector3 center = transform.position;
            Gizmos.DrawWireSphere(center, 0.5f);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(center, center + Vector3.up * 2f);
        }

        private void DrawCursor()
        {
            if (!hasCursor) return;

            Vector3 worldPos = transform.position + new Vector3(
                cursorPosition.x - structureSize.x * 0.5f + 0.5f,
                cursorPosition.y + 0.5f,
                cursorPosition.z - structureSize.z * 0.5f + 0.5f
            );

            Gizmos.color = isEntityMode ? new Color(1f, 1f, 0f, 0.5f) : new Color(currentColor.r, currentColor.g, currentColor.b, 0.5f);
            Gizmos.DrawCube(worldPos, Vector3.one * 0.9f);

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(worldPos, Vector3.one);
        }
    }
}