using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using static Unity.Collections.AllocatorManager;

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
            var chunk = GetOrCreateChunk(position);

            if (string.IsNullOrEmpty(blockID))
            {
                chunk.SetBlock(0, PositionToChunk(position));

                return;
            }

            var index = VoxelDatabase.GetID(blockID);

            if (chunk.SetBlock((ushort)(index), PositionToChunk(position)))
            {
                //UpdateBitmask(position);
                //UpdateNeighborsBitmask(position);
            }
        }

        public ushort GetBlock(int3 position)
        {
            var chunk = GetOrCreateChunk(position);

            return chunk.GetBlock( PositionToChunk(position));
        }

        public void SetValue(float value, int3 position)
        {
            var chunk = GetOrCreateChunk(position);

            chunk.SetValue( PositionToChunk(position), value);
        }

        #endregion

        #region Bitmask

        public void SetBitmask(int3 position, byte bitmask)
        {
            var chunk = GetOrCreateChunk(position);

            if (chunk == null)
                return;

            var coords = PositionToChunk(position);

            if (chunk.GetBitmask(coords) != bitmask)
                chunk.SetBitmask(coords, bitmask);
        }

        public void UpdateNeighborsBitmask(int3 position)
        {
            int x = position.x;
            int y = position.y;
            int z = position.z;

            for (int ix = x - 1; ix < x + 2; ix++)
            {
                for (int iy = y - 1; iy < y + 2; iy++)
                {
                    for (int iz = z - 1; iz < z + 2; iz++)
                    {
                        if (ix == x && iy == y && iz == z)
                            continue;

                        UpdateBitmask(new int3(ix,iy,iz));
                    }
                }
            }
        }

        public byte GetBitmask(int3 position)
        {
            var chunk = GetOrCreateChunk(position);

            if (chunk == null) return 0;

            var coords = PositionToChunk(position);

            return chunk.GetBitmask(coords);
        }

        public void UpdateBitmask(int3 position)
        {
            SetBitmask(position, CalculateBitmask(position));
        }

        public byte CalculateBitmask(int3 position)
        {
            var tileID = GetBlock(position);

            if (tileID == 0)
                return 0;

            byte bitmask = 0;

            if (GetBlock(position + new int3(0, 0, 1)) > 0)
                bitmask.AddBit(Bitmasking.MarchingMask.Forward);

            if (GetBlock(position + new int3(1, 0, 0)) > 0)
                bitmask.AddBit(Bitmasking.MarchingMask.Right);

            if (GetBlock(position + new int3(0, 1, 0)) > 0)
                bitmask.AddBit(Bitmasking.MarchingMask.Top);

            if (GetBlock(position + new int3(0, 1, 1)) > 0)
                bitmask.AddBit(Bitmasking.MarchingMask.ForwardTop);

            if (GetBlock(position + new int3(1, 0, 1)) > 0)
                bitmask.AddBit(Bitmasking.MarchingMask.ForwardRight);

            if (GetBlock(position + new int3(1, 1, 0)) > 0)
                bitmask.AddBit(Bitmasking.MarchingMask.TopRight);

            if (GetBlock(position + new int3(1, 1, 1)) > 0)
                bitmask.AddBit(Bitmasking.MarchingMask.ForwardTopRight);

            return bitmask;
        }


        #endregion

        #region PositionConvertation

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int3 PositionToChunk(int3 globalGridPos)
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
