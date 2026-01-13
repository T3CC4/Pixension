using UnityEngine;

namespace Pixension.Editor
{
    [ExecuteAlways]
    public class StructureEditorGrid : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridSize = 64;
        public float gridSpacing = 1f;
        public Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        public Color centerLineColor = new Color(1f, 0f, 0f, 0.5f);
        public Color yLevelColor = new Color(0f, 1f, 0f, 0.6f);

        [Header("References")]
        public StructureEditorController controller;

        private void OnDrawGizmos()
        {
            if (controller == null)
                return;

            DrawGrid();
            DrawYLevelPlane();
            DrawCenterMarker();
        }

        private void DrawGrid()
        {
            Gizmos.color = gridColor;

            int halfSize = gridSize / 2;

            // X-Z Grid Linien auf allen Y-Ebenen
            for (int y = 0; y <= gridSize; y++)
            {
                float yPos = y * gridSpacing;

                // X Linien
                for (int x = -halfSize; x <= halfSize; x++)
                {
                    Vector3 start = new Vector3(x * gridSpacing, yPos, -halfSize * gridSpacing);
                    Vector3 end = new Vector3(x * gridSpacing, yPos, halfSize * gridSpacing);
                    Gizmos.DrawLine(start, end);
                }

                // Z Linien
                for (int z = -halfSize; z <= halfSize; z++)
                {
                    Vector3 start = new Vector3(-halfSize * gridSpacing, yPos, z * gridSpacing);
                    Vector3 end = new Vector3(halfSize * gridSpacing, yPos, z * gridSpacing);
                    Gizmos.DrawLine(start, end);
                }
            }
        }

        private void DrawYLevelPlane()
        {
            if (controller == null)
                return;

            Gizmos.color = yLevelColor;

            int halfSize = gridSize / 2;
            float yPos = controller.currentYLevel * gridSpacing;

            // Dicke Linien für aktuelle Y-Ebene
            for (int x = -halfSize; x <= halfSize; x++)
            {
                Vector3 start = new Vector3(x * gridSpacing, yPos, -halfSize * gridSpacing);
                Vector3 end = new Vector3(x * gridSpacing, yPos, halfSize * gridSpacing);
                Gizmos.DrawLine(start, end);
            }

            for (int z = -halfSize; z <= halfSize; z++)
            {
                Vector3 start = new Vector3(-halfSize * gridSpacing, yPos, z * gridSpacing);
                Vector3 end = new Vector3(halfSize * gridSpacing, yPos, z * gridSpacing);
                Gizmos.DrawLine(start, end);
            }

            // Plane Fill (halbtransparent)
            Gizmos.color = new Color(yLevelColor.r, yLevelColor.g, yLevelColor.b, 0.1f);
            Vector3 planeCenter = new Vector3(0f, yPos, 0f);
            Vector3 planeSize = new Vector3(gridSize * gridSpacing, 0.01f, gridSize * gridSpacing);
            Gizmos.DrawCube(planeCenter, planeSize);
        }

        private void DrawCenterMarker()
        {
            Gizmos.color = centerLineColor;

            int halfSize = gridSize / 2;

            // Zentrale X-Achse (rot)
            Vector3 xStart = new Vector3(-halfSize * gridSpacing, 0f, 0f);
            Vector3 xEnd = new Vector3(halfSize * gridSpacing, 0f, 0f);
            Gizmos.DrawLine(xStart, xEnd);

            // Zentrale Y-Achse (grün)
            Vector3 yStart = new Vector3(0f, 0f, 0f);
            Vector3 yEnd = new Vector3(0f, gridSize * gridSpacing, 0f);
            Gizmos.color = Color.green;
            Gizmos.DrawLine(yStart, yEnd);

            // Zentrale Z-Achse (blau)
            Vector3 zStart = new Vector3(0f, 0f, -halfSize * gridSpacing);
            Vector3 zEnd = new Vector3(0f, 0f, halfSize * gridSpacing);
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(zStart, zEnd);

            // Zentrum Würfel
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(0.5f, 0.5f, 0.5f), Vector3.one * 0.5f);
        }
    }
}