using Shapes;
using UnityEngine;
using UnityEngine.Rendering;

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

        private void Start()
        {
            if (Application.isPlaying)
                playerController = Object.FindFirstObjectByType<PlayerController>();
        }

        public override void DrawShapes(Camera cam)
        {
            if (!Application.isPlaying)
                return;

            if (cam != Camera.main)
                return;

            using (Draw.Command(cam))
            {
                //Draw.PushMatrix(); 

                //Draw.Matrix = Camera.main.worldToCameraMatrix;
                Draw.ZTest = CompareFunction.Always;
                Draw.BlendMode = ShapesBlendMode.Transparent;

                DrawCrosshair();
                DrawInfoText();

                if (showHotbar)
                    DrawHotbar();

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

            Draw.Line(center + Vector2.right * offset, center + Vector2.right * (offset + length));
            Draw.Line(center + Vector2.left * offset, center + Vector2.left * (offset + length));
            Draw.Line(center + Vector2.up * offset, center + Vector2.up * (offset + length));
            Draw.Line(center + Vector2.down * offset, center + Vector2.down * (offset + length));
        }

        private void DrawInfoText()
        {
            if (playerController == null)
                return;

            string text = playerController.GetInfoText();
            Vector2 pos = new Vector2(20f, Screen.height - 20f);

            Draw.FontSize = infoTextSize;
            Draw.TextAlign = TextAlign.TopLeft;

            Draw.Color = infoTextShadowColor;
            Draw.Text(pos + new Vector2(2f, -2f), text);

            Draw.Color = infoTextColor;
            Draw.Text(pos, text);
        }

        private void DrawHotbar()
        {
            int slots = 9;
            float width = slots * hotbarSlotSize + (slots - 1) * hotbarSlotSpacing;
            float startX = (Screen.width - width) * 0.5f;
            float y = 40f;

            for (int i = 0; i < slots; i++)
            {
                float x = startX + i * (hotbarSlotSize + hotbarSlotSpacing);
                Vector2 center = new Vector2(
                    x + hotbarSlotSize * 0.5f,
                    y + hotbarSlotSize * 0.5f
                );

                Draw.Color = hotbarBgColor;
                Draw.Rectangle(center, new Vector2(hotbarSlotSize, hotbarSlotSize));

                Draw.Color = hotbarSelectedColor;
                Draw.RectangleBorder(center, new Vector2(hotbarSlotSize, hotbarSlotSize), 2f);

                Draw.FontSize = 12;
                Draw.TextAlign = TextAlign.Center;
                Draw.Color = Color.white;
                Draw.Text(center + Vector2.down * hotbarSlotSize * 0.3f, (i + 1).ToString());
            }
        }
    }
}
