using UnityEngine;

namespace Pixension.Entities
{
    public enum EntityType
    {
        Static,
        Interactive
    }

    public interface IInteractable
    {
        void OnInteract(GameObject player);
    }

    public class BlockEntity : MonoBehaviour
    {
        public EntityType entityType;
        public string entityID;
        public Vector3Int worldPosition;
        public Utilities.Direction facing;
        public Vector3Int chunkPosition { get; private set; }
        public bool isVisible { get; private set; }

        public virtual void Initialize(string id, Vector3Int worldPos, Utilities.Direction dir)
        {
            entityID = id;
            worldPosition = worldPos;
            facing = dir;
            isVisible = true;

            chunkPosition = new Vector3Int(
                Mathf.FloorToInt((float)worldPos.x / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)worldPos.y / Voxels.Chunk.CHUNK_SIZE),
                Mathf.FloorToInt((float)worldPos.z / Voxels.Chunk.CHUNK_SIZE)
            );

            transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.z);
            transform.rotation = Utilities.RotationHelper.GetRotation(dir);

            BlockEntityManager.Instance.RegisterEntity(this);

            OnPlace();
        }

        public void Show()
        {
            isVisible = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            isVisible = false;
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (isVisible)
            {
                OnUpdate();
            }
        }

        protected virtual void OnUpdate()
        {
        }

        public virtual void OnPlace()
        {
        }

        public virtual void OnBreak()
        {
        }

        private void OnDestroy()
        {
            if (BlockEntityManager.Instance != null)
            {
                BlockEntityManager.Instance.UnregisterEntity(this);
            }
        }
    }
}