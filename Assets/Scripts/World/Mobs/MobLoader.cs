using UnityEngine;

namespace Pixension.Mobs
{
    public class MobLoader : MonoBehaviour
    {
        private static MobLoader instance;
        public static MobLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("MobLoader");
                    instance = go.AddComponent<MobLoader>();
                    DontDestroyOnLoad(go);
                    instance.LoadRegistry();
                }
                return instance;
            }
        }

        private MobRegistry registry;

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
                registry = Resources.Load<MobRegistry>("MobRegistry");

                if (registry == null)
                {
                    Debug.LogError("MobRegistry not found in Resources folder. Please create one at Resources/MobRegistry");
                }
                else
                {
                    Debug.Log($"Loaded MobRegistry with {registry.GetAllMobs().Count} mobs");
                }
            }
        }

        public MobDefinition GetMob(string id)
        {
            if (registry == null)
            {
                LoadRegistry();
            }

            return registry?.GetMob(id);
        }

        public GameObject InstantiateMob(string id, Vector3 position, Quaternion rotation)
        {
            MobDefinition definition = GetMob(id);

            if (definition == null || definition.prefab == null)
            {
                Debug.LogError($"Cannot instantiate mob '{id}': Definition or prefab is null");
                return null;
            }

            GameObject instance = Instantiate(definition.prefab, position, rotation);
            instance.name = $"{definition.displayName} ({id})";

            Mob mob = instance.GetComponent<Mob>();
            if (mob == null)
            {
                mob = instance.AddComponent<Mob>();
            }

            mob.Initialize(id, position);

            return instance;
        }

        public MobRegistry GetRegistry()
        {
            if (registry == null)
            {
                LoadRegistry();
            }
            return registry;
        }
    }
}