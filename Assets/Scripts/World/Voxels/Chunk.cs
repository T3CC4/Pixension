using System.Collections.Generic;
using UnityEngine;

namespace Pixension.Voxels
{
    public class Chunk
    {
        public const int CHUNK_SIZE = 16;

        public VoxelData[,,] voxels;
        public Vector3Int chunkPosition;
        public List<GameObject> entities;
        public bool isDirty;
        public GameObject gameObject;
        public HashSet<Vector3Int> modifiedVoxelPositions;

        public MeshFilter meshFilter;
        public MeshRenderer meshRenderer;
        public MeshCollider meshCollider;

        public Chunk(Vector3Int position)
        {
            chunkPosition = position;
            voxels = new VoxelData[CHUNK_SIZE, CHUNK_SIZE, CHUNK_SIZE];
            entities = new List<GameObject>();
            modifiedVoxelPositions = new HashSet<Vector3Int>();
            isDirty = true;

            for (int x = 0; x < CHUNK_SIZE; x++)
            {
                for (int y = 0; y < CHUNK_SIZE; y++)
                {
                    for (int z = 0; z < CHUNK_SIZE; z++)
                    {
                        voxels[x, y, z] = VoxelData.Air;
                    }
                }
            }

            gameObject = new GameObject($"Chunk_{position.x}_{position.y}_{position.z}");
            gameObject.transform.position = new Vector3(
                position.x * CHUNK_SIZE,
                position.y * CHUNK_SIZE,
                position.z * CHUNK_SIZE
            );

            meshFilter = gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.material = VoxelMaterialManager.Instance.GetMaterial();

            // MeshCollider hinzufügen
            meshCollider = gameObject.AddComponent<MeshCollider>();
            meshCollider.convex = false; // Non-convex für präzise Kollision
        }

        public VoxelData GetVoxel(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z))
            {
                return VoxelData.Air;
            }
            return voxels[x, y, z];
        }

        public void SetVoxel(int x, int y, int z, VoxelData data)
        {
            if (!IsInBounds(x, y, z))
            {
                return;
            }

            voxels[x, y, z] = data;
            modifiedVoxelPositions.Add(new Vector3Int(x, y, z));
            isDirty = true;
        }

        public void SetDirty()
        {
            isDirty = true;
        }

        public bool IsInBounds(int x, int y, int z)
        {
            return x >= 0 && x < CHUNK_SIZE &&
                   y >= 0 && y < CHUNK_SIZE &&
                   z >= 0 && z < CHUNK_SIZE;
        }

        public void MergeStructureVoxels(VoxelData[,,] structureVoxels, Vector3Int worldOffset)
        {
            int sizeX = structureVoxels.GetLength(0);
            int sizeY = structureVoxels.GetLength(1);
            int sizeZ = structureVoxels.GetLength(2);

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        Vector3Int worldPos = worldOffset + new Vector3Int(x, y, z);
                        Vector3Int localPos = WorldToLocal(worldPos);

                        if (IsInBounds(localPos.x, localPos.y, localPos.z))
                        {
                            VoxelData structureVoxel = structureVoxels[x, y, z];
                            if (structureVoxel.IsSolid)
                            {
                                SetVoxel(localPos.x, localPos.y, localPos.z, structureVoxel);
                            }
                        }
                    }
                }
            }
        }

        public Vector3Int WorldToLocal(Vector3Int worldPos)
        {
            Vector3Int chunkWorldPos = new Vector3Int(
                chunkPosition.x * CHUNK_SIZE,
                chunkPosition.y * CHUNK_SIZE,
                chunkPosition.z * CHUNK_SIZE
            );

            return worldPos - chunkWorldPos;
        }

        public void UpdateMesh(Mesh mesh)
        {
            if (meshFilter != null)
            {
                meshFilter.mesh = mesh;

                // Setze Materials für alle Submeshes
                if (meshRenderer != null && mesh != null)
                {
                    Material voxelMaterial = VoxelMaterialManager.Instance.GetMaterial();

                    // Erstelle Material-Array für alle Submeshes
                    Material[] materials = new Material[mesh.subMeshCount];
                    for (int i = 0; i < mesh.subMeshCount; i++)
                    {
                        materials[i] = voxelMaterial;
                    }

                    meshRenderer.materials = materials;
                }

                // Update MeshCollider
                if (meshCollider != null && mesh != null)
                {
                    meshCollider.sharedMesh = null; // Clear alte Mesh
                    meshCollider.sharedMesh = mesh; // Setze neue Mesh
                }
            }
        }

        public ChunkData Serialize()
        {
            ChunkData data = new ChunkData(chunkPosition);

            // Nur modifizierte Voxels serialisieren
            if (modifiedVoxelPositions.Count > 0)
            {
                data.modifiedVoxels = new VoxelData[modifiedVoxelPositions.Count];
                data.modifiedPositions = new Vector3Int[modifiedVoxelPositions.Count];

                int index = 0;
                foreach (Vector3Int pos in modifiedVoxelPositions)
                {
                    data.modifiedPositions[index] = pos;
                    data.modifiedVoxels[index] = voxels[pos.x, pos.y, pos.z];
                    index++;
                }
            }

            // Serialisiere Entities in diesem Chunk
            List<BlockEntitySaveData> entityList = new List<BlockEntitySaveData>();

            foreach (GameObject entityObject in entities)
            {
                if (entityObject == null)
                    continue;

                Entities.BlockEntity entity = entityObject.GetComponent<Entities.BlockEntity>();
                if (entity != null)
                {
                    entityList.Add(new BlockEntitySaveData(
                        entity.entityID,
                        entity.worldPosition,
                        entity.facing
                    ));
                }
            }

            data.entities = entityList.ToArray();

            return data;
        }

        public void Deserialize(ChunkData data)
        {
            if (data == null)
                return;

            // Setze modifizierte Voxels
            for (int i = 0; i < data.modifiedPositions.Length; i++)
            {
                Vector3Int pos = data.modifiedPositions[i];
                VoxelData voxel = data.modifiedVoxels[i];

                if (IsInBounds(pos.x, pos.y, pos.z))
                {
                    voxels[pos.x, pos.y, pos.z] = voxel;
                    modifiedVoxelPositions.Add(pos);
                }
            }

            // Recreate Entities
            foreach (BlockEntitySaveData entityData in data.entities)
            {
                GameObject entityObject = Entities.BlockEntityLoader.Instance.InstantiateEntity(
                    entityData.entityID,
                    entityData.worldPosition,
                    entityData.GetFacing()
                );

                if (entityObject != null)
                {
                    entityObject.transform.SetParent(gameObject.transform);
                    entities.Add(entityObject);
                }
            }

            // Chunk als dirty markieren für Mesh-Rebuild
            isDirty = true;
        }

        public bool HasModifications()
        {
            return modifiedVoxelPositions.Count > 0 || entities.Count > 0;
        }

        public void ClearModifications()
        {
            modifiedVoxelPositions.Clear();
        }
    }
}