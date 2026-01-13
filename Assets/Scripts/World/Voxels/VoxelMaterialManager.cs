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
        }

        public Material GetMaterial()
        {
            if (voxelMaterial == null)
            {
                Initialize();
            }
            return voxelMaterial;
        }
    }
}