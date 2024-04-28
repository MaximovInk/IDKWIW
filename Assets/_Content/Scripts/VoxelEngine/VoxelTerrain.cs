using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public struct VoxelTerrainLODSettings
    {
        public ChunkLODParameter[] LODs;

        public float Delay;

        [HideInInspector]
        public float _timer;

        public int LODFreeChunk;
    }

    [System.Serializable]
    public struct VoxelTerrainLoaderSettings
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

    public class VoxelTerrain : MonoBehaviour
    {
        public event Action<VoxelChunk> OnChunkLoaded;

        public const int ChunkSize = 16;
        public const int HalfChunkSize = ChunkSize / 2;
        public const int DoubleChunkSize = ChunkSize * 2;
        public const int BlockSize = 2;


        public const int ChunkBlockSize = BlockSize * ChunkSize;
        public const int HalfChunkBlockSize = ChunkBlockSize / 2;
        public const int ChunkBlockSizeSqr = ChunkBlockSize * ChunkBlockSize;

        public Transform Target;

        [Range(0,255)]
        public byte IsoLevel = 2;

        private readonly Dictionary<int3, VoxelChunk> _chunksCache = new();

        public bool FlatShading;

        public Dictionary<int3, VoxelChunk> ChunksCache=>_chunksCache;

        private int3 _chunkCachedPos = new(int.MaxValue, int.MaxValue, int.MaxValue);
        private VoxelChunk _chunkLastUsed;

        public Material Material => _material;

        [SerializeField]
        private Material _material;

        [SerializeField]
        private int _allocateChunkCount;

        [SerializeField] private VoxelTerrainLODSettings _lodSettings;
        [SerializeField] private VoxelTerrainLoaderSettings _loaderSettings;

        private Stack<VoxelChunk> _freeChunks = new Stack<VoxelChunk>();

        private void Awake()
        {
            for (int i = 0; i < _allocateChunkCount; i++)
            {
                AllocateChunk(new int3(256+i, 0, 0));
            }

            CacheLODSettings();
            CacheLoaderSettings();
        }

        private void CacheLoaderSettings()
        {
            var chunkAroundUpdate = _loaderSettings.ChunkAroundUpdate;

            var xSize = Mathf.CeilToInt(chunkAroundUpdate.x / 2f);
            var ySize = chunkAroundUpdate.y;
            var zSize = Mathf.CeilToInt(chunkAroundUpdate.z / 2f);

            var xMin = chunkAroundUpdate.x - xSize;
            //var yMin = _chunkAroundUpdate.y - ySize;
            var zMin = chunkAroundUpdate.z - zSize;

            _loaderSettings._loaderMin = new int3(xMin, -1, zMin);

            _loaderSettings._loaderMax = new int3(xSize, ySize, zSize);
        }

        private void CacheLODSettings()
        {
            for (int i = 0; i < _lodSettings.LODs.Length; i++)
            {
                var lod = _lodSettings.LODs[i];
                lod.DistanceDoubled = lod.DistanceInChunks * lod.DistanceDoubled;
                _lodSettings.LODs[i] = lod;
            }

            _lodSettings.LODs = _lodSettings.LODs.OrderByDescending(n => n.DistanceInChunks).ToArray();

            _lodSettings._timer -= _lodSettings._timer / 2f;
        }

        private void Update()
        {
            if (Target == null) return;

            _lodSettings._timer += Time.deltaTime;
            _loaderSettings._timer += Time.deltaTime;

            if (_lodSettings._timer > _lodSettings.Delay)
            {
                _lodSettings._timer = 0f;

                UpdateChunksState();
            }

            if (_loaderSettings._timer > _lodSettings.Delay)
            {
                _loaderSettings._timer = 0f;

                LoadAroundChunks();
            }
        }

        private void UpdateChunksState()
        {
            if(_chunksCache.Count == 0)return;

            //SortChunksByLod();

            _freeChunks.Clear();

            foreach (var chunk in _chunksCache.Values)
            {
                var changed = UpdateChunkLOD(chunk);

                chunk.IsFree = chunk.LOD >= _lodSettings.LODFreeChunk;

                if(chunk.IsFree)
                    _freeChunks.Push(chunk);

                if (changed) return;
            }
        }

        private Vector3 GetTargetPos()
        {
            return Target.position + Target.forward * HalfChunkBlockSize;
        }

        private bool UpdateChunkLOD(VoxelChunk chunk)
        {
            var distance = Vector3.Distance(chunk.transform.position, GetTargetPos()) /
                           ChunkBlockSize;

            chunk.DistanceToTarget = distance;


            int lod = 1;

            for (int i = 0; i < _lodSettings.LODs.Length; i++)
            {
                if (distance >= _lodSettings.LODs[i].DistanceInChunks)
                {
                    lod = _lodSettings.LODs[i].LOD;
                    break;
                }
            }

            lod = VoxelUtility.ValidateLodValue(lod);


            if (chunk.LOD != lod)
            {
                chunk.LOD = lod;
                _lodSettings._timer = _lodSettings.Delay / 1.2f;

                return true;
            }


            return false;
        }

        private void LoadAroundChunks()
        {
            if (_freeChunks.Count == 0) return;

            var min = _loaderSettings._loaderMin;
            var max = _loaderSettings._loaderMax;
           
            var origin = WorldToChunkPosition(GetTargetPos());

            for (var ix = -min.x; ix <= max.x; ix++)
            {
                for (var iy = min.y; iy <= max.y; iy++)
                {
                    for (var iz = -min.z; iz <= max.z; iz++)
                    {
                        if(_freeChunks.Count == 0) return;

                        var currentPos = origin + new int3(ix, iy, iz);

                        if (GetChunkByPos(currentPos, false) != null) continue;

                        var freeChunk = _freeChunks.Pop();

                        if (freeChunk == null) return;

                        freeChunk.IsFree = false;

                        if (UnloadChunk(freeChunk.Position) != freeChunk)
                        {
                            Debug.Log("ERROReee");
                        }

                        freeChunk.Position = currentPos;

                        LoadChunk(currentPos, freeChunk);

                    }
                }
            }

        }

        #region ChunkManipulation


        private void ClearLastCacheUsed()
        {
            _chunkLastUsed = null;
            _chunkCachedPos = new(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        public VoxelChunk UnloadChunk(int3 position)
        {
            _chunksCache.TryGetValue(position, out var chunk);

            if (chunk == null)
                return null;

            chunk.ClearMesh();

            ClearLastCacheUsed();

            _chunksCache.Remove(position);
            
            return chunk;
        }

        public bool LoadChunk(int3 position, VoxelChunk chunk)
        {
            if (chunk == null) return false;

            var hasChunk = _chunksCache.TryGetValue(position, out _);

            if (hasChunk) return false;

            ClearLastCacheUsed();

            chunk.Position = position;
            chunk.UpdatePosition();
            chunk.Clear();

            _chunksCache[position] = chunk;

            OnChunkLoaded?.Invoke(chunk);

            for (int ix = -1; ix <= 0; ix++)
            {
                for (int iy = -1; iy <= 0; iy++)
                {
                    for (int iz = -1; iz <= 0; iz++)
                    {
                        if(ix == 0 && iy == 0 && iz == 0)continue;

                        var ch = GetChunkByPos(position + new int3(ix,iy,iz),false);

                        if(ch == null) continue;

                        ch.SetIsDirty();
                    }
                }   
            }

            return true;
        }

        private void BuildChunkCache()
        {
            _chunksCache.Clear();
            for (var i = 0; i < transform.childCount; ++i)
            {
                var chunk = transform.GetChild(i).GetComponent<VoxelChunk>();
                if (chunk)
                {
                    _chunksCache[chunk.Position] = chunk;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VoxelChunk GetOrCreateChunk(int3 gridPos, bool autoCreate = true)
        {
            var chunkPos = GridToChunk(gridPos);

            return GetChunkByPos(chunkPos, autoCreate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VoxelChunk GetChunkByPos(int3 chunkPos, bool autoCreate)
        {
            //Return last used chunk if pos is equals
            if ((math.all(chunkPos == _chunkCachedPos)) && (_chunkLastUsed != null))
                return _chunkLastUsed;

            //Try find chunk in cache
            _chunksCache.TryGetValue(chunkPos, out var chunk);

            //If autoCreate, allocate new chunk
            if (chunk == null && autoCreate)
            {
                chunk = AllocateChunk(chunkPos);
            }

            _chunkLastUsed = chunk;
            _chunkCachedPos = chunkPos;

            return chunk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VoxelChunk GetChunkByPos(Vector3Int chunkPos, bool autoCreate)
        {
            var posInt = new int3(chunkPos.x, chunkPos.y, chunkPos.z);

            return GetChunkByPos(posInt, autoCreate);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public VoxelChunk AllocateChunk(int3 chunkPos)
        {
            var go = new GameObject($"CHUNK");
            go.transform.SetParent(transform);
            go.transform.SetLocalPositionAndRotation(
                new Vector3(
                    chunkPos.x * ChunkBlockSize,
                    chunkPos.y * ChunkBlockSize,
                    chunkPos.z * ChunkBlockSize),
                Quaternion.identity);
            go.transform.localScale = Vector3.one;
            var chunk = go.AddComponent<VoxelChunk>();
            chunk.Terrain = this;
            chunk.Position = chunkPos;
            chunk.Initialize();
            _chunksCache[chunkPos] = chunk;
            return chunk;
        }

        #endregion

        #region Data

        public int GetBlockID(string blockID)
        {
            return VoxelDatabase.GetID(blockID);
        }

        public bool SetBlock(string blockID, int3 position)
        {
            var chunk = GetOrCreateChunk(position);

            if (string.IsNullOrEmpty(blockID))
            {
                return chunk.SetBlock(0, PositionToChunk(position));
            }

            var index = VoxelDatabase.GetID(blockID);

            return chunk.SetBlock((ushort)(index), PositionToChunk(position));
        }

        public ushort GetBlock(int3 position)
        {
            var chunk = GetOrCreateChunk(position);

            return chunk.GetBlock( PositionToChunk(position));
        }

        public void SetValue(byte value, int3 position)
        {
            var chunk = GetOrCreateChunk(position);

            chunk.SetValue( PositionToChunk(position), value);
        }

        #endregion

        #region PositionConvertation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int3 PositionToChunk(int3 globalGridPos)
        {
            var xInvert = globalGridPos.x < 0;
            var yInvert = globalGridPos.y < 0;
            var zInvert = globalGridPos.z < 0;

            globalGridPos.x = (xInvert ? -globalGridPos.x - 1 : globalGridPos.x) % ChunkSize;
            globalGridPos.y = (yInvert ? -globalGridPos.y - 1 : globalGridPos.y) % ChunkSize;
            globalGridPos.z = (zInvert ? -globalGridPos.z - 1 : globalGridPos.z) % ChunkSize;

            if (xInvert) globalGridPos.x = ChunkSize - 1 - globalGridPos.x;
            if (yInvert) globalGridPos.y = ChunkSize - 1 - globalGridPos.y;
            if (zInvert) globalGridPos.z = ChunkSize - 1 - globalGridPos.z;

            return globalGridPos;
        }

        private int3 GridToChunk(int3 gridPos)
        {
            var chunkX = (gridPos.x < 0 ? (gridPos.x + 1 - ChunkSize) : gridPos.x) / ChunkSize;
            var chunkY = (gridPos.y < 0 ? (gridPos.y + 1 - ChunkSize) : gridPos.y) / ChunkSize;
            var chunkZ = (gridPos.z < 0 ? (gridPos.z + 1 - ChunkSize) : gridPos.z) / ChunkSize;

            return new int3(chunkX, chunkY, chunkZ);
        }

        public int3 WorldToGrid(Vector3 worldPos)
        {
            var localPos = transform.InverseTransformPoint(worldPos);

            return LocalToGrid(localPos);
        }

        private int3 LocalToGrid(Vector3 localPos)
        {
            return new int3(
                Mathf.FloorToInt((localPos.x) / BlockSize),
                Mathf.FloorToInt((localPos.y) / BlockSize),
                Mathf.FloorToInt((localPos.z) / BlockSize));
        }

        private int3 LocalToChunk(Vector3 localPos)
        {
            var chunkSize = ChunkSize * BlockSize;

            return new int3(
                Mathf.FloorToInt(localPos.x / chunkSize), 
                Mathf.FloorToInt(localPos.y / chunkSize),
                Mathf.FloorToInt(localPos.z / chunkSize)
                );
        }

        public int3 WorldToChunkPosition(Vector3 worldPos)
        {
            var localPos = transform.InverseTransformPoint(worldPos);
            
            return LocalToChunk(localPos);
        }

        #endregion

        public void UpdateImmediately()
        {
            BuildChunkCache();
            foreach (var chunk in _chunksCache)
            {
                chunk.Value.UpdateImmediately();
            }
        }

        public void Clear()
        {
            foreach (var chunk in _chunksCache)
            {
                chunk.Value.Clear();
            }
        }
    }
}
