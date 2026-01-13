using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Pixension.Mobs
{
    public static class MobPrefabCreator
    {
#if UNITY_EDITOR
        [MenuItem("Pixension/Create Mob Prefabs/Bandit (Cube Placeholder)")]
        public static void CreateBanditPrefab()
        {
            GameObject banditPrefab = new GameObject("Bandit");

            GameObject visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCube.name = "Visual";
            visualCube.transform.SetParent(banditPrefab.transform);
            visualCube.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            visualCube.transform.localScale = new Vector3(0.8f, 1.6f, 0.5f);

            MeshRenderer renderer = visualCube.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.5f, 0.2f, 0.1f);
            renderer.material = mat;

            GameObject headCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            headCube.name = "Head";
            headCube.transform.SetParent(banditPrefab.transform);
            headCube.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            headCube.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);

            MeshRenderer headRenderer = headCube.GetComponent<MeshRenderer>();
            Material headMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            headMat.color = new Color(0.8f, 0.6f, 0.5f);
            headRenderer.material = headMat;

            CapsuleCollider collider = banditPrefab.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.height = 2f;
            collider.radius = 0.4f;

            Rigidbody rb = banditPrefab.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            BanditMob banditScript = banditPrefab.AddComponent<BanditMob>();
            banditScript.health = 50f;
            banditScript.maxHealth = 50f;
            banditScript.moveSpeed = 2f;
            banditScript.detectionRange = 10f;

            string prefabPath = "Assets/Prefabs/Mobs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Mobs");
            }

            string fullPath = $"{prefabPath}/Bandit.prefab";
            PrefabUtility.SaveAsPrefabAsset(banditPrefab, fullPath);
            //DestroyImmediate(banditPrefab);

            Debug.Log($"Bandit prefab created at {fullPath}");

            AssetDatabase.Refresh();
        }

        [MenuItem("Pixension/Create Mob Prefabs/Generic Mob (Cube Placeholder)")]
        public static void CreateGenericMobPrefab()
        {
            GameObject mobPrefab = new GameObject("GenericMob");

            GameObject visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCube.name = "Visual";
            visualCube.transform.SetParent(mobPrefab.transform);
            visualCube.transform.localPosition = new Vector3(0f, 0.5f, 0f);

            MeshRenderer renderer = visualCube.GetComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = Color.gray;
            renderer.material = mat;

            CapsuleCollider collider = mobPrefab.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.height = 1f;
            collider.radius = 0.5f;

            Rigidbody rb = mobPrefab.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            Mob mobScript = mobPrefab.AddComponent<Mob>();
            mobScript.health = 30f;
            mobScript.maxHealth = 30f;
            mobScript.moveSpeed = 3f;
            mobScript.detectionRange = 8f;

            string prefabPath = "Assets/Prefabs/Mobs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "Mobs");
            }

            string fullPath = $"{prefabPath}/GenericMob.prefab";
            PrefabUtility.SaveAsPrefabAsset(mobPrefab, fullPath);
            //DestroyImmediate(mobPrefab);

            Debug.Log($"Generic mob prefab created at {fullPath}");

            AssetDatabase.Refresh();
        }
#endif
    }
}