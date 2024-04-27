using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;

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

        public int LOD
        {
            get => _lod;
            set
            {
                _isDirty = _lod != value;

                _lod = value;

                ValidateLodValue();
            }
        }

        [SerializeField]
        private int _lod;


        public bool IsFree = true;

        #region UnityMessages

        private void Update()
        {
            if (_isDestroyed)
                return;

            VoxelUtility.DrawChunkBounds(this);

            if (_isDirty)
            {
                CacheNeighbors();

                UpdatePosition();

                _isDirty = false;


                Generate();
            }

            if (_invokeApplyMesh && _currentThread is not { IsAlive: true }) 
            {
                Profiler.BeginSample("ApplyMesh");
                _invokeApplyMesh = false;


                Profiler.BeginSample("Send data to mesh");
                ApplyMesh();
                Profiler.EndSample();


                Profiler.BeginSample("Recalculate normals");
               // if (!Terrain.FlatShading)
                    //_mesh.RecalculateNormals(180, 0.5f);
                Profiler.EndSample();

                if (_mesh.vertexCount == 0)
                {
                    _meshCollider.sharedMesh = null;
                }
                else
                    _meshCollider.sharedMesh = _mesh;
                Profiler.EndSample();

                

                //EditorApplication.isPaused = true;
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

            InitializeMarchingCubes();

            InitializeRenderer();
            LOD = 1;

            IsFree = true;

            UpdatePosition();

            _isEmpty = true;
        }

        public void UpdateImmediately()
        {
            _isDirty = true;
            Update();
        }

        public void UpdatePosition()
        {
            transform.localPosition = new Vector3(
                Position.x * ChunkSize.x * BlockSize.x,
                Position.y * ChunkSize.y * BlockSize.y,
                Position.z * ChunkSize.z * BlockSize.z
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

        public int ValidateLodValue(int value)
        {
            var limit = Mathf.Min(ChunkSize.x, ChunkSize.z, ChunkSize.y);

            if (value <= 1)
            {
                value = 1;
                return value;
            }

            if (value > limit)
            {
                value = limit;
                return value;
            }

            if (value is 16 or 8 or 4 or 2)
                return value;

            if (value > 16)
                value = 16;
            else if (value > 8)
                value = 8;
            else if (value > 4)
                value = 4;
            else
                value = 2;

            return value;
        }

        private void ValidateLodValue()
        {
            _lod = ValidateLodValue(_lod);
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

        #endregion

    }
}
