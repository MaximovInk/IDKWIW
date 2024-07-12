using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{

    public class VoxelTerrain : MonoBehaviour
    {
        public const int ChunkSize = 16;
        public const int HalfChunkSize = ChunkSize / 2;
        public const int DoubleChunkSize = ChunkSize * 2;
        public const float BlockSize = 3;

        public const float ChunkBlockSize = BlockSize * ChunkSize;
        public const float HalfChunkBlockSize = ChunkBlockSize / 2;
        public const float QuartChunkBlockSize = ChunkBlockSize / 4;
        public const float ChunkBlockSizeSqr = ChunkBlockSize * ChunkBlockSize;

        public event Action<VoxelChunk> OnChunkLoaded;
        public event Action<VoxelChunk> OnMeshGenerated;

        public VoxelTerrainData Data => _data;
        public VoxelTerrainLODSettings LodSettings => _data.LODSettings;
        public VoxelTerrainLoaderSettings LoaderSettings => _data.LoaderSettings;
        public Material Material => _data.Material;
        public byte IsoLevel => _data.IsoLevel;
        public Dictionary<int3, VoxelChunk> ChunksCache => _chunksCache;
        public bool FlatShading => _data.FlatShading;

        public Transform Target;

        [SerializeField] private VoxelTerrainData _data;

        private Vector3 _lastAppliedLODPosition;
        private Vector3 _lastAppliedLoadPosition;

        private int3 _chunkCachedPos = new(int.MaxValue, int.MaxValue, int.MaxValue);
        private VoxelChunk _chunkLastUsed;

        private readonly Dictionary<int3, VoxelChunk> _chunksCache = new();
        private readonly Stack<VoxelChunk> _freeChunks = new();

        private void Awake()
        {
            for (int i = 0; i < _data.AllocateChunkCount; i++)
            {
                AllocateChunk(new int3(256+i, 0, 0));
            }

            CacheLODSettings();
            CacheLoaderSettings();

            _invokeUpdateLOD = true;
            _invokeUpdatePosition = true;
        }

        private void CacheLoaderSettings()
        {
            var chunkAroundUpdate = LoaderSettings.ChunkAroundUpdate;

            var xSize = Mathf.CeilToInt(chunkAroundUpdate.x / 2f);
            var ySize = chunkAroundUpdate.y;
            var zSize = Mathf.CeilToInt(chunkAroundUpdate.z / 2f);

            var xMin = chunkAroundUpdate.x - xSize;
            //var yMin = _chunkAroundUpdate.y - ySize;
            var zMin = chunkAroundUpdate.z - zSize;

            LoaderSettings._loaderMin = new int3(xMin, -1, zMin);

            LoaderSettings._loaderMax = new int3(xSize, ySize, zSize);
        }

        private void CacheLODSettings()
        {
            for (int i = 0; i < LodSettings.LODs.Length; i++)
            {
                var lod = LodSettings.LODs[i];
                lod.DistanceDoubled = lod.DistanceInChunks * lod.DistanceDoubled;
                LodSettings.LODs[i] = lod;
            }

            LodSettings.LODs = LodSettings.LODs.OrderByDescending(n => n.DistanceInChunks).ToArray();

            LodSettings._timer -= LodSettings._timer / 2f;
        }

        private bool _invokeUpdateLOD;
        private bool _invokeUpdatePosition;

        private void Update()
        {
            if (Target == null) return;
            if (_chunksCache.Count == 0) return;

            if (!_invokeUpdateLOD && Vector3.Distance(_lastAppliedLODPosition, Target.position) > QuartChunkBlockSize)
            {
                _lastAppliedLODPosition = Target.position;
                _invokeUpdateLOD = true;
            }

            if (!_invokeUpdatePosition && Vector3.Distance(_lastAppliedLoadPosition, Target.position) > QuartChunkBlockSize)
            {
                _lastAppliedLoadPosition = Target.position;
                _invokeUpdatePosition = true;
            }

            LodSettings._timer += Time.deltaTime;
            LoaderSettings._timer += Time.deltaTime;

            if (LodSettings._timer > LodSettings.Delay)
            {
                if (_invokeUpdateLOD)
                {
                    _invokeUpdateLOD = false;
                    UpdateChunkLOD();
                }
                    
                LodSettings._timer = 0f;
            }

            


            if (LoaderSettings._timer > LodSettings.Delay)
            {
                UpdateChunksState();

                if (_invokeUpdatePosition)
                {
                    _invokeUpdatePosition = false;
                    LoadAroundChunks();
                }

                LoaderSettings._timer = 0f;
            }
        }

        private void UpdateChunkLOD()
        {
            foreach (var chunk in _chunksCache.Values)
            {
                if (UpdateChunkLOD(chunk))
                {
                    _invokeUpdateLOD = true;
                    return;
                }
            }
        }

        private void UpdateChunksState()
        {
            //SortChunksByLod();

            _freeChunks.Clear();

            foreach (var chunk in _chunksCache.Values)
            {
                var changed = UpdateChunkLOD(chunk);

                chunk.IsFree = chunk.LOD >= LodSettings.LODFreeChunk || ((chunk.IsFull() || chunk.IsEmpty()) && !chunk.IsLoaded);

                if(chunk.IsFree)
                    _freeChunks.Push(chunk);

                if (changed)
                {
                    _invokeUpdateLOD = true;
                    _invokeUpdatePosition = true;
                    return;
                }
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

            for (int i = 0; i < LodSettings.LODs.Length; i++)
            {
                if (distance >= LodSettings.LODs[i].DistanceInChunks)
                {
                    lod = LodSettings.LODs[i].LOD;
                    break;
                }
            }

            lod = VoxelUtility.ValidateLodValue(lod);


            if (chunk.LOD != lod)
            {
                chunk.LOD = lod;
                LodSettings._timer = LodSettings.Delay / 1.2f;

                return true;
            }


            return false;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            /*
              var originRaw = WorldToChunkPosition(GetTargetPos());
             var origin = new Vector3(originRaw.x, originRaw.y, originRaw.z);

             var min = LoaderSettings._loaderMin;
             var max = LoaderSettings._loaderMax;

             var chunkSize = ChunkBlockSize;

             var size = new Vector3(chunkSize, chunkSize, chunkSize);
             var halfSize = size / 2f;

             var bounds = new Bounds(origin + size, size);

             for (var ix = -min.x; ix <= max.x; ix++)
             {
                 for (var iy = min.y; iy <= max.y; iy++)
                 {
                     for (var iz = -min.z; iz <= max.z; iz++)
                     {
                         var currentPos = new Vector3(ix* chunkSize + halfSize.x, iy* chunkSize + halfSize.y, iz* chunkSize + halfSize.z);

                         Gizmos.color = Color.green;
                         Gizmos.DrawWireCube(transform.position + bounds.center + currentPos, bounds.size);
                     }
                 }
             }*/
        }

        private void LoadAroundChunks()
        {
            if (_freeChunks.Count == 0) return;

            var min = LoaderSettings._loaderMin;
            var max = LoaderSettings._loaderMax;
           
            var origin = WorldToChunkPosition(GetTargetPos());

            foreach (var chunk in _chunksCache.Values)
            {
                if (chunk.Position.Equals(origin)) continue;

                bool isAlreadyloaded = false;

                for (var ix = -min.x; ix <= max.x; ix++)
                {
                    for (var iy = min.y; iy < max.y; iy++)
                    {
                        for (var iz = -min.z; iz <= max.z; iz++)
                        {
                            var currentPos = origin + new int3(ix, iy, iz);

                            if (chunk.Position.Equals(currentPos) && !(chunk.IsFull() || chunk.IsEmpty())) isAlreadyloaded = true;
                        }
                    }
                }



                if (isAlreadyloaded) continue;

                chunk.IsLoaded = false;
            }

            
             for (var ix = -min.x; ix <= max.x; ix++)
            {
                for (var iy = min.y; iy < max.y; iy++)
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

                        freeChunk.IsLoaded = true;

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

            UpdateNearestChunks(position);

            return true;
        }

        private void UpdateNearestChunks(int3 position)
        {
            for (int ix = -1; ix <= 0; ix++)
            {
                for (int iy = -1; iy <= 0; iy++)
                {
                    for (int iz = -1; iz <= 0; iz++)
                    {
                        if (ix == 0 && iy == 0 && iz == 0) continue;

                        var ch = GetChunkByPos(position + new int3(ix, iy, iz), false);

                        if (ch == null) continue;

                        ch.SetIsDirty();
                    }
                }
            }

        }

        private void UpdateNearestChunks(int3 position, int3 min, int3 max)
        {
            for (int ix = min.x; ix <= max.x; ix++)
            {
                for (int iy = min.y; iy <= max.y; iy++)
                {
                    for (int iz = min.z; iz <= max.z; iz++)
                    {
                        if (ix == 0 && iy == 0 && iz == 0) continue;

                        var ch = GetChunkByPos(position + new int3(ix, iy, iz), false);

                        if (ch == null) continue;

                        ch.SetIsDirty();
                    }
                }
            }

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

            //TODO: Unsubscribe
            chunk.OnMeshGenerated += () => { OnMeshGenerated?.Invoke(chunk); };

            return chunk;
        }

        #endregion

        #region Data

        public int GetBlockID(string blockID)
        {
            return VoxelDatabase.GetID(blockID);
        }

        private void UpdateNearestChunksIfNeed(int3 chunkPos, int3 positionInChunk)
        {
            var min = int3.zero;
            var max = int3.zero;

            if (positionInChunk.x == 0)
                min.x = -1;
            else if (positionInChunk.x == ChunkSize - 1)
                max.x = 1;

            if (positionInChunk.y == 0)
                min.y = -1;
            else if(positionInChunk.y == ChunkSize - 1)
                max.y = 1;
            
            if(positionInChunk.z == 0)
                min.z = -1;
            else if(positionInChunk.z == ChunkSize - 1)
                max.z = 1;

            if (math.all(min == int3.zero) && math.all(max == int3.zero))
                return;

            UpdateNearestChunks(chunkPos, min, max);

        }

        public bool SetBlock(string blockID, int3 position)
        {
            var chunk = GetOrCreateChunk(position);

            var index = string.IsNullOrEmpty(blockID) ? 0 : VoxelDatabase.GetID(blockID);

            var chunkInsideGridPos = PositionToChunk(position);

            var changed = chunk.SetBlock((ushort)(index), chunkInsideGridPos);

            if (!changed) return false;

            

            var chunkPos = GridToChunk(position);

            UpdateNearestChunksIfNeed(chunkPos, chunkInsideGridPos);

            return true;
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

        private Vector3 GridToLocal(int3 position)
        {
            return new Vector3(
                (position.x) * BlockSize,
                (position.y) * BlockSize,
                (position.z) * BlockSize);
        }

        public Vector3 GridToWorld(int3 gridPos)
        {
            var localPos = GridToLocal(gridPos);

            var worldPos = transform.TransformPoint(localPos);

            return worldPos;
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
