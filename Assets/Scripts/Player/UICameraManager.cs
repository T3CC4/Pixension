using UnityEngine;
using UnityEngine.Rendering;

namespace Pixension.Player
{
    /// <summary>
    /// Manages a separate camera for rendering worldspace UI elements
    /// Similar to FPS weapon rendering technique
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UICameraManager : MonoBehaviour
    {
        private static UICameraManager instance;
        public static UICameraManager Instance => instance;

        [Header("Camera Settings")]
        public LayerMask uiLayer;
        public int uiLayerIndex = 8; // Layer 8 for UI elements
        public float uiFieldOfView = 60f;
        public float nearClipPlane = 0.01f;
        public float farClipPlane = 10f;

        [Header("References")]
        public Camera mainCamera;
        public Camera uiCamera;
        public Transform uiRoot; // Parent for all worldspace UI elements

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            SetupUICameraSystem();
        }

        private void SetupUICameraSystem()
        {
            // Find or create main camera
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("UICameraManager: No main camera found!");
                    return;
                }
            }

            // Create UI camera if it doesn't exist
            if (uiCamera == null)
            {
                GameObject uiCamObj = new GameObject("UI Camera");
                uiCamObj.transform.SetParent(transform);
                uiCamObj.transform.localPosition = Vector3.zero;
                uiCamObj.transform.localRotation = Quaternion.identity;

                uiCamera = uiCamObj.AddComponent<Camera>();
            }

            // Configure UI camera
            uiCamera.depth = mainCamera.depth + 1; // Render after main camera
            uiCamera.clearFlags = CameraClearFlags.Depth; // Only clear depth
            uiCamera.cullingMask = 1 << uiLayerIndex; // Only render UI layer
            uiCamera.fieldOfView = uiFieldOfView;
            uiCamera.nearClipPlane = nearClipPlane;
            uiCamera.farClipPlane = farClipPlane;

            // Configure main camera to exclude UI layer
            mainCamera.cullingMask &= ~(1 << uiLayerIndex);

            // Create UI root if it doesn't exist
            if (uiRoot == null)
            {
                GameObject uiRootObj = new GameObject("UI Root");
                uiRootObj.transform.SetParent(uiCamera.transform);
                uiRootObj.transform.localPosition = new Vector3(0, 0, 1f); // 1 unit in front of camera
                uiRootObj.transform.localRotation = Quaternion.identity;
                uiRoot = uiRootObj.transform;

                // Set layer recursively
                SetLayerRecursively(uiRootObj, uiLayerIndex);
            }

            Debug.Log($"UI Camera system initialized on layer {uiLayerIndex}");
        }

        private void LateUpdate()
        {
            // Sync UI camera with main camera transform
            if (mainCamera != null && uiCamera != null)
            {
                uiCamera.transform.position = mainCamera.transform.position;
                uiCamera.transform.rotation = mainCamera.transform.rotation;
            }
        }

        /// <summary>
        /// Creates a worldspace UI element parented to the UI root
        /// </summary>
        public GameObject CreateUIElement(string name, Vector3 localPosition)
        {
            if (uiRoot == null)
            {
                Debug.LogError("UI Root not initialized!");
                return null;
            }

            GameObject uiElement = new GameObject(name);
            uiElement.transform.SetParent(uiRoot);
            uiElement.transform.localPosition = localPosition;
            uiElement.transform.localRotation = Quaternion.identity;
            uiElement.layer = uiLayerIndex;

            return uiElement;
        }

        /// <summary>
        /// Converts screen coordinates to worldspace position on the UI plane
        /// </summary>
        public Vector3 ScreenToUIWorld(Vector2 screenPos, float distance = 1f)
        {
            if (uiCamera == null)
                return Vector3.zero;

            Ray ray = uiCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, distance));
            return ray.GetPoint(distance);
        }

        /// <summary>
        /// Sets layer for GameObject and all children
        /// </summary>
        public void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// Gets the UI camera
        /// </summary>
        public Camera GetUICamera()
        {
            return uiCamera;
        }

        /// <summary>
        /// Gets the UI root transform
        /// </summary>
        public Transform GetUIRoot()
        {
            return uiRoot;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
