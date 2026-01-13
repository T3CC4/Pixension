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

        public float moveSpeed = 5f;
        public float jumpForce = 5f;
        public float gravity = -9.81f;
        public float mouseSensitivity = 2f;
        public float maxReachDistance = 5f;

        private Vector3 velocity;
        private float verticalRotation = 0f;
        private bool cursorLocked = true;

        private Voxels.VoxelData selectedVoxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, Color.red);
        private string selectedEntityID = "";
        private bool isBlockMode = true;

        private Vector3Int currentHitPos;
        private Vector3Int currentHitNormal;
        private bool hasTarget = false;

        private Color[] quickColors = new Color[]
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
            playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            voxelModifier = Voxels.ChunkManager.Instance.GetVoxelModifier();

            LockCursor();

            Voxels.ChunkManager.Instance.player = transform;
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

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.wKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;

            Vector3 move = transform.right * horizontal + transform.forward * vertical;
            controller.Move(move * moveSpeed * Time.deltaTime);

            if (controller.isGrounded)
            {
                if (velocity.y < 0)
                {
                    velocity.y = -2f;
                }

                if (keyboard.spaceKey.wasPressedThisFrame)
                {
                    velocity.y = jumpForce;
                }
            }

            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private void HandleMouseLook()
        {
            if (!cursorLocked) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 mouseDelta = mouse.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseSensitivity * 0.1f;
            float mouseY = mouseDelta.y * mouseSensitivity * 0.1f;

            transform.Rotate(Vector3.up * mouseX);

            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        private void HandleVoxelInteraction()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            hasTarget = voxelModifier.RaycastVoxel(ray, maxReachDistance, out currentHitPos, out currentHitNormal);

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;
            if (mouse == null || keyboard == null) return;

            if (hasTarget)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    RemoveVoxel();
                }

                if (mouse.rightButton.wasPressedThisFrame)
                {
                    PlaceVoxel();
                }

                if (keyboard.eKey.wasPressedThisFrame)
                {
                    InteractWithEntity();
                }
            }
        }

        private void HandleCursorToggle()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && !cursorLocked)
            {
                LockCursor();
            }
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

        private void RemoveVoxel()
        {
            if (isBlockMode)
            {
                voxelModifier.RemoveBlock(currentHitPos);
            }
            else
            {
                voxelModifier.RemoveBlockEntity(currentHitPos);
            }
        }

        private void PlaceVoxel()
        {
            Vector3Int placePos = currentHitPos + currentHitNormal;

            if (isBlockMode)
            {
                voxelModifier.PlaceBlock(placePos, selectedVoxel);
            }
            else
            {
                if (!string.IsNullOrEmpty(selectedEntityID))
                {
                    voxelModifier.PlaceBlockEntity(selectedEntityID, placePos, Utilities.Direction.North);
                }
            }
        }

        private void InteractWithEntity()
        {
            Entities.BlockEntity entity = Entities.BlockEntityManager.Instance.GetEntityAtPosition(currentHitPos);

            if (entity != null && entity is Entities.IInteractable interactable)
            {
                interactable.OnInteract(gameObject);
            }
        }

        private void HandleColorSelection()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.digit1Key.wasPressedThisFrame) SelectColor(0);
            if (keyboard.digit2Key.wasPressedThisFrame) SelectColor(1);
            if (keyboard.digit3Key.wasPressedThisFrame) SelectColor(2);
            if (keyboard.digit4Key.wasPressedThisFrame) SelectColor(3);
            if (keyboard.digit5Key.wasPressedThisFrame) SelectColor(4);
            if (keyboard.digit6Key.wasPressedThisFrame) SelectColor(5);
            if (keyboard.digit7Key.wasPressedThisFrame) SelectColor(6);
            if (keyboard.digit8Key.wasPressedThisFrame) SelectColor(7);
            if (keyboard.digit9Key.wasPressedThisFrame) SelectColor(8);
        }

        private void SelectColor(int index)
        {
            selectedVoxel = new Voxels.VoxelData(Voxels.VoxelType.Solid, quickColors[index]);
            Debug.Log($"Selected color: {quickColors[index]}");
        }

        private void HandleModeSwitch()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.tabKey.wasPressedThisFrame)
            {
                isBlockMode = !isBlockMode;
                Debug.Log($"Mode: {(isBlockMode ? "Block" : "Entity")}");
            }
        }

        public void SetSelectedEntityID(string entityID)
        {
            selectedEntityID = entityID;
            isBlockMode = false;
        }

        public string GetInfoText()
        {
            string modeText = isBlockMode ? "Block Mode" : "Entity Mode";
            string targetText = hasTarget ? $"Target: {currentHitPos}" : "No Target";
            string colorText = isBlockMode ? $"Color: {selectedVoxel.color}" : $"Entity: {selectedEntityID}";

            return $"{modeText}\n{targetText}\n{colorText}";
        }

        public bool HasTarget()
        {
            return hasTarget;
        }
    }
}