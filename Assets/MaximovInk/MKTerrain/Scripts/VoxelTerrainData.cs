using System;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public struct ChunkLODParameter
    {
        public int LOD;
        public float DistanceInChunks;

        [SerializeField, HideInInspector]
        public float DistanceDoubled;
    }

    [System.Serializable]
    public class VoxelTerrainLODSettings
    {
        public ChunkLODParameter[] LODs;

        public float Delay;

        [HideInInspector]
        public float _timer;

        public int LODFreeChunk;
    }

    [System.Serializable]
    public class VoxelTerrainLoaderSettings
    {
        public int3 ChunkAroundUpdate;

        public float Delay;

        [HideInInspector]
        public float _timer;

        [HideInInspector]
        public int3 _loaderMax;
        [HideInInspector]
        public int3 _loaderMin;
    }


    [CreateAssetMenu(menuName = "MaximovInk/VoxelTerrainData", fileName = "VoxelTerrainData")]
    public class VoxelTerrainData : ScriptableObject
    {
        [Range(0, 255)]
        public byte IsoLevel = 2;

        public bool FlatShading;

        public Material Material => _material;

        [SerializeField]
        private Material _material;

        public GrassModule GrassModulePrefab;

        public int AllocateChunkCount = 512;

        public VoxelTerrainLODSettings LODSettings => _lodSettings;
        public VoxelTerrainLoaderSettings LoaderSettings => _loaderSettings;

        [SerializeField] private VoxelTerrainLODSettings _lodSettings;
        [SerializeField] private VoxelTerrainLoaderSettings _loaderSettings;

        public VoxelTerrainObjectsDatabase ObjectsDatabase => _objectsDatabase;
        [SerializeField] private VoxelTerrainObjectsDatabase _objectsDatabase;
    }

}
