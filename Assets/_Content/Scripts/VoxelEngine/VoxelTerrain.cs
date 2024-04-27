using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public class VoxelTerrain : MonoBehaviour
    {
        public event Action<VoxelChunk> OnChunkLoaded;

        public int3 ChunkSize;
        public float3 BlockSize;

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

        private void Awake()
        {
            for (int i = 0; i < _allocateChunkCount; i++)
            {
                AllocateChunk(new int3(i, 0, 0));
            }

        }

        public void UpdateImmediately()
        {
            BuildChunkCache();
            foreach (var chunk in _chunksCache)
            {
                chunk.Value.UpdateImmediately();
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
                    chunkPos.x * ChunkSize.x * BlockSize.x,
                    chunkPos.y * ChunkSize.y * BlockSize.y,
                    chunkPos.z * ChunkSize.z * BlockSize.z),
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

            globalGridPos.x = (xInvert ? -globalGridPos.x - 1 : globalGridPos.x) % ChunkSize.x;
            globalGridPos.y = (yInvert ? -globalGridPos.y - 1 : globalGridPos.y) % ChunkSize.y;
            globalGridPos.z = (zInvert ? -globalGridPos.z - 1 : globalGridPos.z) % ChunkSize.z;

            if (xInvert) globalGridPos.x = ChunkSize.x - 1 - globalGridPos.x;
            if (yInvert) globalGridPos.y = ChunkSize.y - 1 - globalGridPos.y;
            if (zInvert) globalGridPos.z = ChunkSize.z - 1 - globalGridPos.z;

            return globalGridPos;
        }

        private int3 GridToChunk(int3 gridPos)
        {
            var chunkX = (gridPos.x < 0 ? (gridPos.x + 1 - ChunkSize.x) : gridPos.x) / ChunkSize.x;
            var chunkY = (gridPos.y < 0 ? (gridPos.y + 1 - ChunkSize.y) : gridPos.y) / ChunkSize.y;
            var chunkZ = (gridPos.z < 0 ? (gridPos.z + 1 - ChunkSize.z) : gridPos.z) / ChunkSize.z;

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
                Mathf.FloorToInt((localPos.x) / BlockSize.x),
                Mathf.FloorToInt((localPos.y) / BlockSize.y),
                Mathf.FloorToInt((localPos.z) / BlockSize.z));
        }

        private int3 LocalToChunk(Vector3 localPos)
        {
            var chunkSize = ChunkSize * BlockSize;

            return new int3(
                Mathf.FloorToInt(localPos.x / chunkSize.x), 
                Mathf.FloorToInt(localPos.y / chunkSize.y),
                Mathf.FloorToInt(localPos.z / chunkSize.z)
                );
        }

        public int3 WorldToChunkPosition(Vector3 worldPos)
        {
            var localPos = transform.InverseTransformPoint(worldPos);
            
            return LocalToChunk(localPos);
        }

        #endregion

        public void Clear()
        {
            foreach (var chunk in _chunksCache)
            {
                chunk.Value.Clear();
            }
        }
    }
}
