using UnityEngine;

namespace Pixension.Entities
{
    public class BlockEntityLoader : MonoBehaviour
    {
        private static BlockEntityLoader instance;
        public static BlockEntityLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("BlockEntityLoader");
                    instance = go.AddComponent<BlockEntityLoader>();
                    DontDestroyOnLoad(go);
                    instance.LoadRegistry();
                }
                return instance;
            }
        }

        private BlockEntityRegistry registry;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadRegistry();
        }

        private void LoadRegistry()
        {
            if (registry == null)
            {
                registry = Resources.Load<BlockEntityRegistry>("BlockEntityRegistry");

                if (registry == null)
                {
                    Debug.LogError("BlockEntityRegistry not found in Resources folder. Please create one at Resources/BlockEntityRegistry");
                }
                else
                {
                    Debug.Log($"Loaded BlockEntityRegistry with {registry.GetAllEntities().Count} entities");
                }
            }
        }

        public BlockEntityDefinition GetEntity(string id)
        {
            if (registry == null)
            {
                LoadRegistry();
            }

            return registry?.GetEntity(id);
        }

        public GameObject InstantiateEntity(string id, Vector3Int worldPosition, Utilities.Direction facing)
        {
            BlockEntityDefinition definition = GetEntity(id);

            if (definition == null || definition.prefab == null)
            {
                Debug.LogError($"Cannot instantiate entity '{id}': Definition or prefab is null");
                return null;
            }

            GameObject instance = Instantiate(definition.prefab);

            BlockEntity entity = instance.GetComponent<BlockEntity>();
            if (entity == null)
            {
                entity = instance.AddComponent<BlockEntity>();
            }

            entity.entityType = definition.type;
            entity.Initialize(id, worldPosition, facing);

            return instance;
        }

        public BlockEntityRegistry GetRegistry()
        {
            if (registry == null)
            {
                LoadRegistry();
            }
            return registry;
        }
    }
}