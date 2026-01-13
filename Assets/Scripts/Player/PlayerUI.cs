using UnityEngine;
using Shapes;

namespace Pixension.Player
{
    [ExecuteAlways]
    public class PlayerUI : ImmediateModeShapeDrawer
    {
        private PlayerController playerController;

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

        [Header("Hotbar")]
        public bool showHotbar = true;
        public Color hotbarBgColor = new Color(0f, 0f, 0f, 0.5f);
        public Color hotbarSelectedColor = new Color(1f, 1f, 1f, 0.8f);
        public float hotbarSlotSize = 50f;
        public float hotbarSlotSpacing = 5f;

        [Header("Target Indicator")]
        public bool showTargetIndicator = true;
        public Color targetIndicatorColor = new Color(1f, 1f, 1f, 0.3f);
        public float targetIndicatorSize = 1.05f;

        private void Start()
        {
            playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        public override void DrawShapes(Camera cam)
        {
            if (!Application.isPlaying)
                return;

            using (Draw.Command(cam))
            {
                Draw.Matrix = Matrix4x4.identity;
                Draw.BlendMode = ShapesBlendMode.Transparent;

                DrawCrosshair();
                DrawInfoText();

                if (showHotbar)
                    DrawHotbar();
            }
        }

        private void DrawCrosshair()
        {
            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            bool hasTarget = playerController != null && playerController.HasTarget();
            Color color = hasTarget ? crosshairTargetColor : crosshairColor;

            Draw.LineGeometry = LineGeometry.Flat2D;
            Draw.Thickness = crosshairThickness;
            Draw.Color = color;

            float offset = crosshairGap;
            float lineLength = crosshairSize;

            Draw.Line(
                new Vector2(center.x + offset, center.y),
                new Vector2(center.x + offset + lineLength, center.y)
            );

            Draw.Line(
                new Vector2(center.x - offset, center.y),
                new Vector2(center.x - offset - lineLength, center.y)
            );

            Draw.Line(
                new Vector2(center.x, center.y + offset),
                new Vector2(center.x, center.y + offset + lineLength)
            );

            Draw.Line(
                new Vector2(center.x, center.y - offset),
                new Vector2(center.x, center.y - offset - lineLength)
            );
        }

        private void DrawInfoText()
        {
            if (playerController == null)
                return;

            string infoText = playerController.GetInfoText();
            Vector2 position = new Vector2(20f, Screen.height - 20f);

            Draw.FontSize = infoTextSize;
            Draw.TextAlign = TextAlign.TopLeft;

            Draw.Color = infoTextShadowColor;
            Draw.Text(position + new Vector2(2f, -2f), infoText);

            Draw.Color = infoTextColor;
            Draw.Text(position, infoText);
        }

        private void DrawHotbar()
        {
            int slotCount = 9;
            float totalWidth = slotCount * hotbarSlotSize + (slotCount - 1) * hotbarSlotSpacing;
            float startX = (Screen.width - totalWidth) * 0.5f;
            float bottomY = 40f;

            for (int i = 0; i < slotCount; i++)
            {
                float x = startX + i * (hotbarSlotSize + hotbarSlotSpacing);
                Vector2 slotPosition = new Vector2(x + hotbarSlotSize * 0.5f, bottomY + hotbarSlotSize * 0.5f);

                Draw.Color = hotbarBgColor;
                Draw.Rectangle(slotPosition, new Vector2(hotbarSlotSize, hotbarSlotSize));

                Draw.Color = hotbarSelectedColor;
                Draw.RectangleBorder(slotPosition, new Vector2(hotbarSlotSize, hotbarSlotSize), 2f);

                Draw.FontSize = 12;
                Draw.TextAlign = TextAlign.Center;
                Draw.Color = Color.white;
                Draw.Text(new Vector2(slotPosition.x, slotPosition.y - hotbarSlotSize * 0.3f), (i + 1).ToString());
            }
        }
    }
}