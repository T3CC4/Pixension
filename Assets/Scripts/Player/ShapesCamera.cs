using UnityEngine;
using Shapes;

namespace Pixension.Player
{
    [RequireComponent(typeof(Camera))]
    public class ShapesCamera : MonoBehaviour 
    {
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            Camera.onPostRender += OnCameraPostRender;
        }

        private void OnDisable()
        {
            Camera.onPostRender -= OnCameraPostRender;
        }

        private void OnCameraPostRender(Camera camera)
        {
            if (camera == cam)
            {
                //Draw.ExecuteAllDrawCommands(camera);
            }
        }
    }
}