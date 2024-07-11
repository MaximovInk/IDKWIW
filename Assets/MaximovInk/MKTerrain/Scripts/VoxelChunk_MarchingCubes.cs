using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using Unity.Collections;
using Unity.Jobs;
using System.Collections;

namespace MaximovInk.VoxelEngine
{
    public partial class VoxelChunk
    {
        private NativeParallelHashMap<float3, int> smoothedVerticesCache;
        private NativeArray<float> cubeValues;

        private NativeList<float3> _outputVertices;
        private NativeList<float3> _outputNormals;
        private NativeList<float4> _outputColors;
        private NativeList<int> _outputTriangles;
        private NativeList<float2> _outputUvs;

        private NativeArray<float4> _blockColors;


        public NativeList<int> VertexToIndex => _vertexToIndex;
        private NativeList<int> _vertexToIndex;

        private float _isoLevel;

        private void InitializeMarchingCubes()
        {
            smoothedVerticesCache =
                new NativeParallelHashMap<float3, int>(ChunkSize * ChunkSize * ChunkSize * 16, Allocator.Persistent);
            cubeValues = new NativeArray<float>(8, Allocator.Persistent);

            var voxels = VoxelDatabase.GetAllVoxels();
            _blockColors = new NativeArray<float4>(voxels.Count, Allocator.Persistent);

            for (int i = 0; i < voxels.Count; i++)
            {
                var c = voxels[i].VertexColor;
                _blockColors[i] = new float4(c.r, c.g, c.b, c.a);
            }

            _outputVertices = new NativeList<float3>(0, Allocator.Persistent);
            _outputNormals = new NativeList<float3>(0, Allocator.Persistent);
            _outputColors = new NativeList<float4>(0, Allocator.Persistent);
            _outputTriangles = new NativeList<int>(0, Allocator.Persistent);
            _outputUvs = new NativeList<float2>(0, Allocator.Persistent);
            _vertexToIndex = new NativeList<int>(0, Allocator.Persistent);
        }

        private void OnMarchingCubesDestroy()
        {
            _handle.Complete();
            
            smoothedVerticesCache.Dispose();
            cubeValues.Dispose();

            _blockColors.Dispose();

            _outputColors.Dispose();
            _outputTriangles.Dispose();
            _outputVertices.Dispose();
            _outputNormals.Dispose();
            _outputUvs.Dispose();

            _vertexToIndex.Dispose();
        }

        private JobHandle _handle;
        private MarchingCubesJob _currentJob;

        private bool _isRunning;

        private void Generate()
        {
            if (_isRunning || !_isInitialized)
            {
                _isDirty = true;

                return;
            }

            _isRunning = true;

            ValidateLodValue();

            smoothedVerticesCache.Clear();

            _outputColors.Clear();
            _outputVertices.Clear();
            _outputTriangles.Clear();
            _outputNormals.Clear();
            _outputUvs.Clear();
            _vertexToIndex.Clear();

            _currentJob = new MarchingCubesJob
            {
                smoothedVerticesCache = smoothedVerticesCache,
                cubeValues = cubeValues,
                _data = new NativeArray<ushort>(_data.Blocks, Allocator.TempJob),
                _values = new NativeArray<byte>(_data.Value, Allocator.TempJob),
                _colors = new NativeArray<Color>(_data.Colors, Allocator.TempJob),
                lod = _lod,
                smoothing = !Terrain.FlatShading,
                _isoLevelByte = Terrain.IsoLevel,
                _isoLevel = Terrain.IsoLevel / 255f,
                _blockColors = _blockColors,
                OutputVertices = _outputVertices,
                OutputNormals = _outputNormals,
                OutputColors = _outputColors,
                OutputTriangles = _outputTriangles,
                VertexToIndex = _vertexToIndex,
                OutputUVs = _outputUvs
            };

            _currentJob.valuesForward = _neighbors.Forward != null && !_neighbors.Forward.IsEmpty() ? new NativeArray<byte>(_neighbors.Forward._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.valuesRight = _neighbors.Right != null && !_neighbors.Right.IsEmpty() ? new NativeArray<byte>(_neighbors.Right._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.valuesTop = _neighbors.Top != null && !_neighbors.Top.IsEmpty() ? new NativeArray<byte>(_neighbors.Top._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);

            _currentJob.valuesForwardRight = _neighbors.ForwardRight != null && !_neighbors.ForwardRight.IsEmpty() ? new NativeArray<byte>(_neighbors.ForwardRight._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.valuesTopRight = _neighbors.TopRight != null && !_neighbors.TopRight.IsEmpty() ? new NativeArray<byte>(_neighbors.TopRight._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.valuesForwardRightTop = _neighbors.ForwardTopRight != null && !_neighbors.ForwardTopRight.IsEmpty() ? new NativeArray<byte>(_neighbors.ForwardTopRight._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.valuesForwardTop = _neighbors.ForwardTop != null && !_neighbors.ForwardTop.IsEmpty() ? new NativeArray<byte>(_neighbors.ForwardTop._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);

            _handle = _currentJob.Schedule();

            StartCoroutine(WaitFor(_handle));
        }


        IEnumerator WaitFor(JobHandle job)
        {
            yield return new WaitUntil(() => job.IsCompleted);

            job.Complete();

            _isEmpty = _currentJob._isEmpty;
            _isFull = _currentJob._isFull;

            _currentJob.valuesForward.Dispose();
            _currentJob.valuesRight.Dispose();
            _currentJob.valuesTop.Dispose();

            _currentJob.valuesForwardRight.Dispose();
            _currentJob.valuesTopRight.Dispose();
            _currentJob.valuesForwardRightTop.Dispose();
            _currentJob.valuesForwardTop.Dispose();

            _currentJob._colors.Dispose();
            _currentJob._data.Dispose();
            _currentJob._values.Dispose();

            _mesh.Clear();

            _mesh.SetVertices(_currentJob.OutputVertices.AsArray());
            _mesh.SetNormals(_currentJob.OutputNormals.AsArray());
            _mesh.SetColors(_currentJob.OutputColors.AsArray());
            _mesh.SetIndices(_currentJob.OutputTriangles.AsArray(), MeshTopology.Triangles, 0);
            _mesh.SetUVs(2, _outputUvs.AsArray());

            // if (!Terrain.FlatShading)
            //_mesh.RecalculateNormals(180, 0.5f);

            _meshCollider.sharedMesh = _mesh.vertexCount == 0 ? null : _mesh;

            _isRunning = false;

            OnMeshGenerated?.Invoke();
        }

        private void CacheNeighbors()
        {
            Profiler.BeginSample("Neighbors");

            Vector3Int pos = new Vector3Int(Position.x, Position.y, Position.z);

                _neighbors.Forward = Terrain.GetChunkByPos(pos + Vector3Int.forward, false);

                _neighbors.Top = Terrain.GetChunkByPos(pos + Vector3Int.up, false);

                _neighbors.Right = Terrain.GetChunkByPos(pos + Vector3Int.right, false);

                _neighbors.ForwardTop = Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.up, false);

                _neighbors.ForwardRight = Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.right, false);

                _neighbors.TopRight = Terrain.GetChunkByPos(pos + Vector3Int.up + Vector3Int.right, false);

                _neighbors.ForwardTopRight =
                    Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.up + Vector3Int.right, false);
             
            Profiler.EndSample();
        }

       
    }
}
