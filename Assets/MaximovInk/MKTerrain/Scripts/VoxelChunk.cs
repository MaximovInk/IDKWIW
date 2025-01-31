﻿using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace MaximovInk.VoxelEngine
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public partial class VoxelChunk : MonoBehaviour
    {
        public event Action OnMeshGenerated;
        public event Action OnPositionChanged;

        private event Action OnInitializeModules;
        private event Action OnUpdateModules;
        private event Action OnDestroyModules;
        private event Action OnDisableModules;

        public const int ChunkSize = VoxelTerrain.ChunkSize;
        public static float3 BlockSize => VoxelTerrain.BlockSize;

        public int3 Position
        {
            get => _position;

            set
            {
                var temp = _position;
                _position = value;
                if (!temp.Equals(_position))
                    OnPositionChanged?.Invoke();

            }
        }

        private int3 _position;
        public VoxelTerrain Terrain;

        private ChunkData _data;
      

        private bool _isInitialized;
        private bool _isDestroyed;
        private bool _isDirty;

        public ChunkNeighbors Neighbors => _neighbors;
        private ChunkNeighbors _neighbors;

        public int LOD
        {
            get => _lod;
            set
            {
                _isDirty = _lod != value;

                _lastLod = _lod;

                _lod = value;

                ValidateLodValue();
            }
        }

        public int PreviousLOD => _lastLod;
        private int _lastLod = 1;

        [SerializeField]
        private int _lod;


        public bool IsFree = true;

        public bool IsLoaded;

        #region UnityMessages

        private void Awake()
        {
            InstancedAwake();
            GrassAwake();
        }

        private void Update()
        {
            if (_isDestroyed)
                return;

            VoxelUtility.DrawChunkBounds(this);

            OnUpdateModules?.Invoke();

            if (_isDirty)
            {
                CacheNeighbors();

                UpdatePosition();

                _isDirty = false;


                Generate();
            }
        }

        private void OnDestroy()
        {
            OnDestroyModules?.Invoke();

            _isDestroyed = true;
            _data.Dispose();

            OnMarchingCubesDestroy();
        }

        private void OnDisable()
        {
            OnDisableModules?.Invoke();
        }

        #endregion

        #region Main


        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            _data = new ChunkData(ChunkSize, ChunkSize, ChunkSize);

            InitializeMarchingCubes();

            InitializeRenderer();
            LOD = 1;

            IsFree = true;

            UpdatePosition();

            _isEmpty = true;
            IsLoaded = false;

            OnInitializeModules?.Invoke();
        }

        public void UpdateImmediately()
        {
            _isDirty = true;
            Update();
        }

        public void UpdatePosition()
        {
            transform.localPosition = new Vector3(
                Position.x * ChunkSize * BlockSize.x,
                Position.y * ChunkSize * BlockSize.y,
                Position.z * ChunkSize * BlockSize.z
            );

            gameObject.name = $"[Instance] Chunk {Position}";
        }

        public void SetIsDirty()
        {
            _isDirty = true;
        }

        #endregion

        #region Data

        [SerializeField]
        private bool _isEmpty = false;
        [SerializeField]
        private bool _isFull = false;

        public bool IsFull()
        {
            return _isFull;
        }

        public bool IsEmpty()
        {
            return _isEmpty;
        }

        private void ValidateLodValue()
        {
            _lod = VoxelUtility.ValidateLodValue(_lod);
        }

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
            if (position.x >= ChunkSize || position.y >= ChunkSize || position.z >= ChunkSize)
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
            if (position.x >= ChunkSize || position.y >= ChunkSize || position.z >= ChunkSize)
            {
                return 0;
            }

            {
                var index = VoxelUtility.PosToIndexInt(position);
                return _data.Blocks[index];
            }
        }


        public bool SetColor(Color color, int3 position)
        {
            var index = VoxelUtility.PosToIndexInt(position);

            _data.Colors[index] = color;

            _isDirty = true;

            return true;
        }

        public bool SetBlock(ushort id, int3 position)
        {
            var index = VoxelUtility.PosToIndexInt(position);

            if (_data.Blocks[index] == id) return false;


            _data.Blocks[index] = id;

            _isDirty = true;

            return true;
        }

        #endregion

    }
}
