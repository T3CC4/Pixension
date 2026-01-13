using Shapes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.InputSystem;

namespace Pixension.Player
{
    [ExecuteAlways]
    public class PlayerUI : ImmediateModeShapeDrawer
    {
        private PlayerController playerController;
        private UICameraManager uiCameraManager;

        [Header("Crosshair")]
        public Color crosshairColor = Color.white;
        public Color crosshairTargetColor = Color.green;
        public float crosshairSize = 12f;
        public float crosshairThickness = 2f;
        public float crosshairGap = 4f;

        [Header("Info Text")]
        public Color infoTextColor = Color.white;
        public Color infoTextShadowColor = new Color(0f, 0f, 0f, 0.7f);
        public int infoTextSize = 16;
        public bool showDebugInfo = true;

        [Header("Hotbar")]
        public bool showHotbar = true;
        public Color hotbarBgColor = new Color(0f, 0f, 0f, 0.5f);
        public Color hotbarSelectedColor = new Color(1f, 1f, 1f, 0.8f);
        public Color hotbarHighlightColor = new Color(1f, 1f, 0f, 0.9f);
        public float hotbarSlotSize = 50f;
        public float hotbarSlotSpacing = 5f;

        [Header("Input Settings")]
        public bool handleOwnInput = true;
        private int selectedHotbarSlot = 0;

        [Header("Performance Stats")]
        public bool showPerformanceStats = true;
        private float frameTime;
        private float updateInterval = 0.5f;
        private float lastUpdateTime;

        private void Start()
        {
            if (Application.isPlaying)
            {
                playerController = Object.FindFirstObjectByType<PlayerController>();
                uiCameraManager = Object.FindFirstObjectByType<UICameraManager>();

                lastUpdateTime = Time.time;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || !handleOwnInput)
                return;

            HandleHotbarInput();
            UpdatePerformanceStats();
        }

        private void HandleHotbarInput()
        {
            var k = Keyboard.current;
            if (k == null) return;

            // Handle number keys for hotbar selection
            if (k.digit1Key.wasPressedThisFrame) SelectHotbarSlot(0);
            if (k.digit2Key.wasPressedThisFrame) SelectHotbarSlot(1);
            if (k.digit3Key.wasPressedThisFrame) SelectHotbarSlot(2);
            if (k.digit4Key.wasPressedThisFrame) SelectHotbarSlot(3);
            if (k.digit5Key.wasPressedThisFrame) SelectHotbarSlot(4);
            if (k.digit6Key.wasPressedThisFrame) SelectHotbarSlot(5);
            if (k.digit7Key.wasPressedThisFrame) SelectHotbarSlot(6);
            if (k.digit8Key.wasPressedThisFrame) SelectHotbarSlot(7);
            if (k.digit9Key.wasPressedThisFrame) SelectHotbarSlot(8);

            // Mouse wheel for hotbar selection
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0)
                {
                    selectedHotbarSlot = (selectedHotbarSlot - 1 + 9) % 9;
                }
                else if (scroll < 0)
                {
                    selectedHotbarSlot = (selectedHotbarSlot + 1) % 9;
                }
            }

            // Toggle debug info
            if (k.f3Key.wasPressedThisFrame)
            {
                showDebugInfo = !showDebugInfo;
            }

            // Toggle performance stats
            if (k.f4Key.wasPressedThisFrame)
            {
                showPerformanceStats = !showPerformanceStats;
            }
        }

        private void SelectHotbarSlot(int slot)
        {
            selectedHotbarSlot = slot;
            // Notify PlayerController of selection if needed
            // Could implement event system here
        }

        private void UpdatePerformanceStats()
        {
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                frameTime = Time.deltaTime * 1000f;
                lastUpdateTime = Time.time;
            }
        }

        public override void DrawShapes(Camera cam)
        {
            if (!Application.isPlaying)
                return;

            // Check if this is the UI camera or main camera
            bool isUICamera = uiCameraManager != null && cam == uiCameraManager.GetUICamera();
            bool isMainCamera = cam == Camera.main;

            if (!isUICamera && !isMainCamera)
                return;

            using (Draw.Command(cam))
            {
                Draw.ZTest = CompareFunction.Always;
                Draw.BlendMode = ShapesBlendMode.Transparent;

                DrawCrosshair();

                if (showDebugInfo)
                {
                    DrawInfoText();
                }

                if (showPerformanceStats)
                {
                    DrawPerformanceStats();
                }

                if (showHotbar)
                {
                    DrawHotbar();
                }

                Draw.PopMatrix();
            }
        }

        private void DrawCrosshair()
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            bool hasTarget = playerController != null && playerController.HasTarget();
            Draw.Color = hasTarget ? crosshairTargetColor : crosshairColor;

            Draw.LineGeometry = LineGeometry.Flat2D;
            Draw.Thickness = crosshairThickness;

            float offset = crosshairGap;
            float length = crosshairSize;

            // Draw crosshair lines
            Draw.Line(center + Vector2.right * offset, center + Vector2.right * (offset + length));
            Draw.Line(center + Vector2.left * offset, center + Vector2.left * (offset + length));
            Draw.Line(center + Vector2.up * offset, center + Vector2.up * (offset + length));
            Draw.Line(center + Vector2.down * offset, center + Vector2.down * (offset + length));

            // Draw center dot
            Draw.Color = crosshairColor;
            Draw.Disc(center, 1.5f);
        }

        private void DrawInfoText()
        {
            if (playerController == null)
                return;

            string text = playerController.GetInfoText();
            Vector2 pos = new Vector2(20f, Screen.height - 20f);

            Draw.FontSize = infoTextSize;
            Draw.TextAlign = TextAlign.TopLeft;

            // Shadow
            Draw.Color = infoTextShadowColor;
            Draw.Text(pos + new Vector2(2f, -2f), text);

            // Main text
            Draw.Color = infoTextColor;
            Draw.Text(pos, text);
        }

        private void DrawPerformanceStats()
        {
            Vector2 pos = new Vector2(20f, 20f);
            Draw.FontSize = 14;
            Draw.TextAlign = TextAlign.TopLeft;

            // Get chunk manager stats
            var chunkManager = Voxels.ChunkManager.Instance;
            int loadedChunks = 0;
            if (chunkManager != null)
            {
                var activeDimension = Dimensions.DimensionManager.Instance?.GetActiveDimension();
                if (activeDimension != null)
                {
                    loadedChunks = activeDimension.chunks.Count;
                }
            }

            // Get mesh pool stats
            var meshPool = Utilities.MeshPool.Instance;
            var poolStats = meshPool != null ? meshPool.GetStats() : (0, 0, 0);

            // Build stats text
            float fps = 1000f / frameTime;
            string statsText = $"FPS: {fps:F0} ({frameTime:F1}ms)\n" +
                             $"Chunks: {loadedChunks}\n" +
                             $"Mesh Pool: {poolStats.active}/{poolStats.total} (Free: {poolStats.available})";

            // Shadow
            Draw.Color = infoTextShadowColor;
            Draw.Text(pos + new Vector2(1f, -1f), statsText);

            // Main text
            Draw.Color = new Color(0.5f, 1f, 0.5f);
            Draw.Text(pos, statsText);
        }

        private void DrawHotbar()
        {
            int slots = 9;
            float totalWidth = slots * hotbarSlotSize + (slots - 1) * hotbarSlotSpacing;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float y = 40f;

            for (int i = 0; i < slots; i++)
            {
                float x = startX + i * (hotbarSlotSize + hotbarSlotSpacing);
                Vector2 center = new Vector2(
                    x + hotbarSlotSize * 0.5f,
                    y + hotbarSlotSize * 0.5f
                );

                bool isSelected = (i == selectedHotbarSlot);

                // Background
                Draw.Color = hotbarBgColor;
                Draw.Rectangle(center, new Vector2(hotbarSlotSize, hotbarSlotSize));

                // Border
                Draw.Color = isSelected ? hotbarHighlightColor : hotbarSelectedColor;
                Draw.RectangleBorder(center, new Vector2(hotbarSlotSize, hotbarSlotSize), isSelected ? 3f : 2f);

                // Slot number
                Draw.FontSize = 12;
                Draw.TextAlign = TextAlign.Center;
                Draw.Color = isSelected ? hotbarHighlightColor : Color.white;
                Draw.Text(center + Vector2.down * hotbarSlotSize * 0.3f, (i + 1).ToString());

                // Selected indicator
                if (isSelected)
                {
                    Draw.Color = hotbarHighlightColor;
                    Draw.Rectangle(
                        new Vector2(center.x, y - 5f),
                        new Vector2(hotbarSlotSize * 0.8f, 3f)
                    );
                }
            }
        }

        public int GetSelectedHotbarSlot()
        {
            return selectedHotbarSlot;
        }

        public void SetSelectedHotbarSlot(int slot)
        {
            selectedHotbarSlot = Mathf.Clamp(slot, 0, 8);
        }
    }
}
