using UnityEngine;

namespace Pixension.Voxels
{
    public class VoxelMaterialManager : MonoBehaviour
    {
        private static VoxelMaterialManager instance;
        public static VoxelMaterialManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("VoxelMaterialManager");
                    instance = go.AddComponent<VoxelMaterialManager>();
                    DontDestroyOnLoad(go);
                    instance.Initialize();
                }
                return instance;
            }
        }

        private Material voxelMaterial;
        private Material transparentMaterial;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void Initialize()
        {
            if (voxelMaterial == null)
            {
                Shader shader = Shader.Find("Voxel/VertexColor");
                if (shader == null)
                {
                    Debug.LogError("Shader 'Voxel/VertexColor' not found. Please ensure the shader is in the project.");
                    return;
                }

                voxelMaterial = new Material(shader);
                voxelMaterial.name = "VoxelMaterial";
            }

            if (transparentMaterial == null)
            {
                Shader shader = Shader.Find("Voxel/VertexColor");
                if (shader == null)
                {
                    Debug.LogError("Shader 'Voxel/VertexColor' not found. Please ensure the shader is in the project.");
                    return;
                }

                transparentMaterial = new Material(shader);
                transparentMaterial.name = "VoxelMaterialTransparent";

                // Set transparent rendering mode
                transparentMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                transparentMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                transparentMaterial.SetInt("_ZWrite", 0);
                transparentMaterial.DisableKeyword("_ALPHATEST_ON");
                transparentMaterial.EnableKeyword("_ALPHABLEND_ON");
                transparentMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                transparentMaterial.renderQueue = 3000;
            }
        }

        public Material GetMaterial()
        {
            if (voxelMaterial == null)
            {
                Initialize();
            }
            return voxelMaterial;
        }

        public Material GetTransparentMaterial()
        {
            if (transparentMaterial == null)
            {
                Initialize();
            }
            return transparentMaterial;
        }
    }
}