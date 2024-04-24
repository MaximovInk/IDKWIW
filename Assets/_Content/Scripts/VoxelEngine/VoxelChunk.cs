using System.Collections.Generic;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using static UnityEditor.PlayerSettings;

namespace MaximovInk.VoxelEngine
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public partial class VoxelChunk : MonoBehaviour
    {
        public int3 ChunkSize => Terrain.ChunkSize;
        public float3 BlockSize => Terrain.BlockSize;

        public int3 Position;
        public VoxelTerrain Terrain;

        private ChunkData _data;

        private bool _isInitialized;
        private bool _isDestroyed;
        private bool _isDirty;
        private bool _invokeApplyMesh;

        private ChunkNeighbors _neighbors;

        #region UnityMessages

        private void Update()
        {
            if (_isDestroyed)
                return;

            if (_isDirty)
            {
                CacheNeighbors();

                transform.localPosition = new Vector3(
                    Position.x * ChunkSize.x * BlockSize.x,
                    Position.y * ChunkSize.y * BlockSize.y,
                    Position.z * ChunkSize.z * BlockSize.z
                );

                _isDirty = false;
                _invokeApplyMesh = false;

                Generate();
            }

            if (_invokeApplyMesh)
            {
                Profiler.BeginSample("ApplyMesh");
                _invokeApplyMesh = false;


                Profiler.BeginSample("Send data to mesh");
                ApplyMesh();
                Profiler.EndSample();


                Profiler.BeginSample("Recalculate normals");
                //if (!Terrain.FlatShading)
                  //  _meshData.GetMesh().RecalculateNormals(180, 0.5f);
                Profiler.EndSample();

                //_meshCollider.sharedMesh = _meshData.GetMesh();
                Profiler.EndSample();


                EditorApplication.isPaused = true;
            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
        }

        #endregion

        #region Main

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            _data = new ChunkData(ChunkSize.x, ChunkSize.y, ChunkSize.z);

            InitializeRenderer();
        }

        public void UpdateImmediately()
        {
            _isDirty = true;
            Update();
        }

        #endregion

        #region Data

        public void Clear()
        {
            _data.Clear();
            _isDirty = true;
        }

        public bool SetValue(int3 position, byte value)
        {
            var index = VoxelUtility.PosToIndexInt(position);

            if (_data.Value[index] == value) return false;

            _data.Value[index] = value;
            _isDirty = true;

            return true;
        }

        public byte GetValue(int3 position)
        {
            if (position.x >= ChunkSize.x || position.y >= ChunkSize.y || position.z >= ChunkSize.z)
            {
                return 0;
            }

            {
                var index = VoxelUtility.PosToIndexInt(position);

                if (_data.Blocks[index] == 0) return 0;

                return _data.Value[index];
            }

        }

        public ushort GetBlock(int3 position)
        {
            if (position.x >= ChunkSize.x || position.y >= ChunkSize.y || position.z >= ChunkSize.z)
            {
                return 0;
            }

            {
                var index = VoxelUtility.PosToIndexInt(position);
                return _data.Blocks[index];
            }
        }

        public bool SetBlock(ushort id, int3 position)
        {
            var index = VoxelUtility.PosToIndexInt(position);

            if (_data.Blocks[index] == id) return false;

            _data.Blocks[index] = id;

            _isDirty = true;

            return true;
        }

        private void CacheNeighbors()
        {
            Profiler.BeginSample("Neighbors");

            Vector3Int pos = new Vector3Int(Position.x, Position.y, Position.z);

            if (_neighbors.Forward == null)
                _neighbors.Forward = Terrain.GetChunkByPos(pos + Vector3Int.forward, false);

            if (_neighbors.Top == null)
                _neighbors.Top = Terrain.GetChunkByPos(pos + Vector3Int.up, false);

            if (_neighbors.Right == null)
                _neighbors.Right = Terrain.GetChunkByPos(pos + Vector3Int.right, false);

            if (_neighbors.ForwardTop == null)
                _neighbors.ForwardTop = Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.up, false);

            if (_neighbors.ForwardRight == null)
                _neighbors.ForwardRight = Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.right, false);

            if (_neighbors.TopRight == null)
                _neighbors.TopRight = Terrain.GetChunkByPos(pos + Vector3Int.up + Vector3Int.right, false);

            if (_neighbors.ForwardTopRight == null)
                _neighbors.ForwardTopRight =
                    Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.up + Vector3Int.right, false);

            Profiler.EndSample();
        }

        #endregion

    }
}
