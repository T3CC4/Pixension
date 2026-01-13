using UnityEngine;
using UnityEngine.InputSystem;

namespace Pixension.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        private CharacterController controller;
        private Camera playerCamera;
        private Voxels.VoxelModifier voxelModifier;

        [Header("Movement")]
        public float moveSpeed = 5f;
        public float jumpHeight = 1.6f;
        public float gravity = -25f;

        [Header("Mouse")]
        public float mouseSensitivity = 2f;
        public float maxReachDistance = 5f;

        private Vector3 velocity;
        private float verticalRotation;
        private bool cursorLocked = true;
        private bool isGrounded;

        private Voxels.VoxelData selectedVoxel =
            new Voxels.VoxelData(Voxels.VoxelType.Solid, Color.red);

        private string selectedEntityID = "";
        private bool isBlockMode = true;

        private Vector3Int currentHitPos;
        private Vector3Int currentHitNormal;
        private bool hasTarget;

        private readonly Color[] quickColors =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            Color.white,
            Color.gray,
            new Color(0.6f, 0.4f, 0.2f)
        };

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;

            voxelModifier = Voxels.ChunkManager.Instance.GetVoxelModifier();
            Voxels.ChunkManager.Instance.player = transform;

            LockCursor();
        }

        private void Update()
        {
            HandleMovement();
            HandleMouseLook();
            HandleVoxelInteraction();
            HandleColorSelection();
            HandleModeSwitch();
            HandleCursorToggle();
        }

        private void HandleMovement()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0f)
                velocity.y = -2f;

            float x = 0f;
            float z = 0f;

            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;

            Vector3 move =
                (transform.right * x + transform.forward * z).normalized;

            controller.Move(move * moveSpeed * Time.deltaTime);

            if (keyboard.spaceKey.wasPressedThisFrame && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            if (!cursorLocked) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 delta = mouse.delta.ReadValue() * mouseSensitivity * 0.1f;

            transform.Rotate(Vector3.up * delta.x);

            verticalRotation -= delta.y;
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
            playerCamera.transform.localRotation =
                Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        private void HandleVoxelInteraction()
        {
            Ray ray = playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f));

            hasTarget = voxelModifier.RaycastVoxel(
                ray,
                maxReachDistance,
                out currentHitPos,
                out currentHitNormal
            );

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) return;

            if (!hasTarget) return;

            if (mouse.leftButton.wasPressedThisFrame)
                RemoveVoxel();

            if (mouse.rightButton.wasPressedThisFrame)
                PlaceVoxel();

            if (keyboard.eKey.wasPressedThisFrame)
                InteractWithEntity();
        }

        private void RemoveVoxel()
        {
            if (isBlockMode)
                voxelModifier.RemoveBlock(currentHitPos);
            else
                voxelModifier.RemoveBlockEntity(currentHitPos);
        }

        private void PlaceVoxel()
        {
            Vector3Int pos = currentHitPos + currentHitNormal;

            if (isBlockMode)
                voxelModifier.PlaceBlock(pos, selectedVoxel);
            else if (!string.IsNullOrEmpty(selectedEntityID))
                voxelModifier.PlaceBlockEntity(
                    selectedEntityID,
                    pos,
                    Utilities.Direction.North
                );
        }

        private void InteractWithEntity()
        {
            var entity =
                Entities.BlockEntityManager.Instance
                    .GetEntityAtPosition(currentHitPos);

            if (entity is Entities.IInteractable interactable)
                interactable.OnInteract(gameObject);
        }

        private void HandleColorSelection()
        {
            var k = Keyboard.current;
            if (k == null) return;

            if (k.digit1Key.wasPressedThisFrame) SelectColor(0);
            if (k.digit2Key.wasPressedThisFrame) SelectColor(1);
            if (k.digit3Key.wasPressedThisFrame) SelectColor(2);
            if (k.digit4Key.wasPressedThisFrame) SelectColor(3);
            if (k.digit5Key.wasPressedThisFrame) SelectColor(4);
            if (k.digit6Key.wasPressedThisFrame) SelectColor(5);
            if (k.digit7Key.wasPressedThisFrame) SelectColor(6);
            if (k.digit8Key.wasPressedThisFrame) SelectColor(7);
            if (k.digit9Key.wasPressedThisFrame) SelectColor(8);
        }

        private void SelectColor(int index)
        {
            selectedVoxel =
                new Voxels.VoxelData(
                    Voxels.VoxelType.Solid,
                    quickColors[index]
                );
        }

        private void HandleModeSwitch()
        {
            var k = Keyboard.current;
            if (k == null) return;

            if (k.tabKey.wasPressedThisFrame)
                isBlockMode = !isBlockMode;
        }

        private void HandleCursorToggle()
        {
            var k = Keyboard.current;
            var m = Mouse.current;

            if (k != null && k.escapeKey.wasPressedThisFrame)
                UnlockCursor();

            if (m != null && m.leftButton.wasPressedThisFrame && !cursorLocked)
                LockCursor();
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorLocked = false;
        }

        public string GetInfoText()
        {
            return
                $"{(isBlockMode ? "Block" : "Entity")} Mode\n" +
                $"{(hasTarget ? currentHitPos.ToString() : "No Target")}";
        }

        public bool HasTarget()
        {
            return hasTarget;
        }
    }
}
