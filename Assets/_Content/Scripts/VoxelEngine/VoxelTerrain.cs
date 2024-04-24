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
        public int3 ChunkSize;
        public float3 BlockSize;

        public float IsoLevel = 0.5f;

        private readonly Dictionary<int3, VoxelChunk> _chunksCache = new();

        public bool FlatShading;

        private int3 _chunkCachedPos = new(int.MaxValue, int.MaxValue, int.MaxValue);
        private VoxelChunk _chunkLastUsed;

        public void UpdateImmediately()
        {
            BuildChunkCache();
            foreach (var chunk in _chunksCache)
            {
                chunk.Value.UpdateImmediately();
            }
        }

        #region ChunkManipulation

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
        private VoxelChunk GetOrCreateChunk(int gx, int gy, int gz, bool autoCreate = true)
        {
            return GetOrCreateChunk(new int3(gx, gy, gz), autoCreate);
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
        private VoxelChunk AllocateChunk(int3 chunkPos)
        {
            var go = new GameObject($"[Instance] Chunk {chunkPos}");
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

        public void SetBlock(string blockID, int3 position)
        {
            var chunk = GetOrCreateChunk(position.x, position.y, position.z);

            if (string.IsNullOrEmpty(blockID))
            {
                chunk.SetBlock(0, PositionToChunk(position));

                return;
            }

            var index = VoxelDatabase.GetID(blockID);

            chunk.SetBlock((ushort)(index), PositionToChunk(position));
        }

        public void SetValue(float value, int3 position)
        {
            var chunk = GetOrCreateChunk(position.x, position.y, position.z);

            chunk.SetValue( PositionToChunk(position), value);
        }

        #endregion

        #region PositionConvertation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int3 PositionToChunk(int3 globalGridPos)
        {
            var xInvert = globalGridPos.x < 0;
            var yInvert = globalGridPos.y < 0;
            var zInvert = globalGridPos.z < 0;

            globalGridPos.x = (globalGridPos.x < 0 ? -globalGridPos.x - 1 : globalGridPos.x) % ChunkSize.x;
            globalGridPos.y = (globalGridPos.y < 0 ? -globalGridPos.y - 1 : globalGridPos.y) % ChunkSize.y;
            globalGridPos.z = (globalGridPos.z < 0 ? -globalGridPos.z - 1 : globalGridPos.z) % ChunkSize.z;

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
