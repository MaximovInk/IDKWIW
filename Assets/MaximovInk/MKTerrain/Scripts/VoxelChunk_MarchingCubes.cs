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

        public NativeList<ushort> VertexToBlockIndex => _vertexToBlockIndex;
        private NativeList<ushort> _vertexToBlockIndex;

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
            _vertexToBlockIndex = new NativeList<ushort>(0, Allocator.Persistent);
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

            _vertexToBlockIndex.Dispose();
        }

        private JobHandle _handle;
        private MarchingCubesJob _currentJob;

        private bool _isRunning;

        private NativeArray<byte> GetNeighborData(VoxelChunk neighbor)
        {
            return neighbor != null && !neighbor.IsEmpty() 
                ? new NativeArray<byte>(neighbor._data.Value, Allocator.TempJob) 
                : new NativeArray<byte>(0, Allocator.TempJob);
        }

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
            _vertexToBlockIndex.Clear();

            _currentJob = new MarchingCubesJob
            {
                BlockData = new NativeArray<ushort>(_data.Blocks, Allocator.TempJob),
                Values = new NativeArray<byte>(_data.Value, Allocator.TempJob),
                Colors = new NativeArray<Color>(_data.Colors, Allocator.TempJob),
                LOD = _lod,
                EnableSmoothing = !Terrain.FlatShading,
                IsoLevelByte = Terrain.IsoLevel,
                IsoLevel = Terrain.IsoLevel / 255f,
                OutputVertices = _outputVertices,
                OutputNormals = _outputNormals,
                OutputColors = _outputColors,
                OutputTriangles = _outputTriangles,
                VertexToIndex = _vertexToBlockIndex,
                OutputUVs = _outputUvs
            };

            _currentJob.DensityForwardNeighbor = HasNeighbor(_neighbors.Forward) ? new NativeArray<byte>(_neighbors.Forward._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.LODForward = HasNeighbor(_neighbors.Forward) ? _neighbors.Forward.LOD : _lod;
            
            _currentJob.DensityRightNeighbor = HasNeighbor(_neighbors.Right) ? new NativeArray<byte>(_neighbors.Right._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.LODRight = HasNeighbor(_neighbors.Right) ? _neighbors.Right.LOD : _lod;
           
            _currentJob.DensityTopNeighbor = HasNeighbor(_neighbors.Top) ? new NativeArray<byte>(_neighbors.Top._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.LODTop = HasNeighbor(_neighbors.Top) ? _neighbors.Top.LOD : _lod;
            
            _currentJob.DensityForwardRightNeighbor = HasNeighbor(_neighbors.ForwardRight) ? new NativeArray<byte>(_neighbors.ForwardRight._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.LODForwardRight = HasNeighbor(_neighbors.ForwardRight) ? _neighbors.ForwardRight.LOD : _lod;
           
            _currentJob.DensityTopRightNeighbor = HasNeighbor(_neighbors.TopRight) ? new NativeArray<byte>(_neighbors.TopRight._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.LODTopRight = HasNeighbor(_neighbors.TopRight) ? _neighbors.TopRight.LOD : _lod;
           
            _currentJob.DensityForwardRightTopNeighbor = HasNeighbor(_neighbors.ForwardTopRight) ? new NativeArray<byte>(_neighbors.ForwardTopRight._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.LODForwardRightTop = HasNeighbor(_neighbors.ForwardTopRight) ? _neighbors.ForwardTopRight.LOD : _lod;
           
            _currentJob.DensityForwardTopNeighbor = HasNeighbor(_neighbors.ForwardTop) ? new NativeArray<byte>(_neighbors.ForwardTop._data.Value, Allocator.TempJob) : new NativeArray<byte>(0, Allocator.TempJob);
            _currentJob.LODForwardTop = HasNeighbor(_neighbors.ForwardTop) ? _neighbors.ForwardTop.LOD : _lod;

            _handle = _currentJob.Schedule();

            StartCoroutine(WaitFor(_handle));
        }

        private bool HasNeighbor(VoxelChunk chunk)
        {
            return chunk != null && !chunk.IsEmpty();
        }


        IEnumerator WaitFor(JobHandle job)
        {
            yield return new WaitUntil(() => job.IsCompleted);

            job.Complete();

            _isEmpty = _currentJob.IsEmpty;
            _isFull = _currentJob.IsFull;

            _currentJob.DensityForwardNeighbor.Dispose();
            _currentJob.DensityRightNeighbor.Dispose();
            _currentJob.DensityTopNeighbor.Dispose();

            _currentJob.DensityForwardRightNeighbor.Dispose();
            _currentJob.DensityTopRightNeighbor.Dispose();
            _currentJob.DensityForwardRightTopNeighbor.Dispose();
            _currentJob.DensityForwardTopNeighbor.Dispose();

            _currentJob.Colors.Dispose();
            _currentJob.BlockData.Dispose();
            _currentJob.Values.Dispose();

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
