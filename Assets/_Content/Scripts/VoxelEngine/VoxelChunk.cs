using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace MaximovInk.VoxelEngine
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class VoxelChunk : MonoBehaviour
    {
        public int3 ChunkSize => Terrain.ChunkSize;
        public float BlockSize => Terrain.BlockSize;

        public int3 Position;
        public VoxelTerrain Terrain;

        private MarchingMeshData _meshData;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;


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
                transform.localPosition = new Vector3(
                    Position.x * ChunkSize.x * BlockSize,
                    Position.y * ChunkSize.y * BlockSize,
                    Position.z * ChunkSize.z * BlockSize
                );

                _isDirty = false;
                Generate();
            }

            if (_invokeApplyMesh)
            {
                _invokeApplyMesh = false;
                _meshData.ApplyToMesh();


                _meshCollider.sharedMesh = _meshData.GetMesh();

            }
        }

        private void OnDestroy()
        {
            _isDestroyed = true;
            _data.Dispose();
        }

        #endregion

        #region Main

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();
            _meshData = new MarchingMeshData();
            _meshFilter.mesh = _meshData.GetMesh();
            _meshCollider = GetComponent<MeshCollider>();


            _data = new ChunkData(ChunkSize.x, ChunkSize.y, ChunkSize.z);

            ApplyMaterials();

          

            _isDirty = true;
        }

        public void UpdateImmediately()
        {
            _isDirty = true;
            Update();
        }

        #endregion

        #region Data

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
            Vector3Int pos = new Vector3Int(Position.x, Position.y, Position.z);

            if (_neighbors.Forward == null)
            {
                _neighbors.Forward = Terrain.GetChunkByPos(pos + Vector3Int.forward, false);
            }

            if (_neighbors.Top == null)
            {
                _neighbors.Top = Terrain.GetChunkByPos(pos + Vector3Int.up, false);
            }

            if (_neighbors.Right == null)
            {
                _neighbors.Right = Terrain.GetChunkByPos(pos + Vector3Int.right, false);
            }

            if (_neighbors.ForwardTop == null)
            {
                _neighbors.ForwardTop = Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.up, false);
            }

            if (_neighbors.ForwardRight == null)
            {
                _neighbors.ForwardRight = Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.right, false);
            }

            if (_neighbors.TopRight == null)
            {
                _neighbors.TopRight = Terrain.GetChunkByPos(pos + Vector3Int.up + Vector3Int.right, false);
            }

            if (_neighbors.ForwardTopRight == null)
            {
                _neighbors.ForwardTopRight =
                    Terrain.GetChunkByPos(pos + Vector3Int.forward + Vector3Int.up + Vector3Int.right, false);
            }
        }

        private VoxelChunk GetChunkOverflowCheck(ref int3 pos, out bool overflow)
        {
            var right = pos.x >= ChunkSize.x;
            var top = pos.y >= ChunkSize.y;
            var forward = pos.z >= ChunkSize.z;

            overflow = right || top || forward;

            if (!overflow) return this;

            CacheNeighbors();

            VoxelChunk targetChunk = null;

            if (right && top && forward)
                targetChunk = _neighbors.ForwardTopRight;
            else if (right && top)
                targetChunk = _neighbors.TopRight;
            else if(forward && top)
                targetChunk = _neighbors.ForwardTop;
            else if(forward && right)
                targetChunk = _neighbors.ForwardRight;
            else if(forward)
                targetChunk = _neighbors.Forward;
            else if(top)
                targetChunk = _neighbors.Top;
            else if(right)
                targetChunk = _neighbors.Right;

            if (targetChunk == null)
            {
                return this;
            }

            if (right)
                pos.x -= ChunkSize.x;

            if (top)
                pos.y -= ChunkSize.y;

            if (forward)
                pos.z -= ChunkSize.z;

            return targetChunk;
        }

        public ushort GetBlock(int3 position, bool autoNeighbor = false)
        {
            if (autoNeighbor)
            {
                var chunk = GetChunkOverflowCheck(ref position, out var overflow);

                if (overflow && chunk == this) return 0;

                return chunk.GetBlock(position, false);
            }

            if (position.x >= ChunkSize.x || position.y >= ChunkSize.y || position.z >= ChunkSize.z)
            {
                return 0;
            }

            var index = VoxelUtility.PosToIndexInt(position);
            return _data.Blocks[index];
        }
        public ushort GetBlock(int x, int y, int z, bool autoNeighbor = false)
        {
            return GetBlock(new int3(x, y, z), autoNeighbor);
        }

        public float GetValue(int3 position, bool autoNeighbor =true)
        {
            if (autoNeighbor)
            {
                var chunk = GetChunkOverflowCheck(ref position, out var overflow);

                if (overflow && chunk == this) return 0;

                return chunk.GetValue(position, false);
            }

            if (position.x >= ChunkSize.x || position.y >= ChunkSize.y || position.z >= ChunkSize.z)
            {
                return 0;
            }

            var index = VoxelUtility.PosToIndexInt(position);
            return _data.Blocks[index];

        }
        public float GetValue(int x, int y, int z, bool autoNeighbor = false)
        {
            return GetValue(new int3(x, y, z), autoNeighbor);
        }

        #endregion

        private void ApplyMaterials()
        {
            var blocks = VoxelDatabase.GetAllVoxels();

            _meshRenderer.material = blocks[0].Material;
        }

        #region Generation

        private readonly float _isoLevel = 0.5f;

        private int GetConfiguration(int x, int y, int z, ref float[] cubeValues)
        {
            int cubeIndex = 0;

            var offsets = MarchingCubesTables.cornerOffsetsInt;

            for (int i = 0; i < 8; i++)
            {
                var offset = offsets[i];

                var value = GetValue(x + offset.x, y + offset.y, z + offset.z, true);

                cubeValues[i] = value;

                if (cubeValues[i] < _isoLevel)
                {
                    cubeIndex |= 1 << i;
                }
            }

            return cubeIndex;
        }

        private float3 InterpolateEdges(float3 edgeVertex1, float valueAtVertex1, float3 edgeVertex2, float valueAtVertex2)
        {
            if (Mathf.Abs(valueAtVertex1) < 0.00001f)
                return edgeVertex1;

            if (Mathf.Abs(valueAtVertex2) < 0.00001f)
                return edgeVertex2;

            if (Mathf.Abs(valueAtVertex1 - valueAtVertex2) < 0.00001f)
                return edgeVertex1;

            var t = (_isoLevel - valueAtVertex1) / (valueAtVertex2 - valueAtVertex1);

            return edgeVertex1 + t * (edgeVertex2 - edgeVertex1);
        }
        public void Clear()
        {
            _data.Clear();
            _isDirty = true;
        }

        private readonly object _lockObject = new object();

        private void Generate()
        {
            _meshData.Clear();

            //Generate

            var thread = new Thread(() =>
            {
                lock (_lockObject)
                {
                    var triTable = MarchingCubesTables.triTable;
                    var edgeConnections = MarchingCubesTables.edgeConnections;
                    var cornerOffsets = MarchingCubesTables.cornerOffsets;

                    var cubeValues = new float[8];

                    //var triangles = new List<Triangle>();

                    var vertices = new Dictionary<float3, int>();
                    var normals = new List<float3>();

                    var unstagedVertices = new List<Vertex>();
                    var triangles = new List<int>();

                    var smoothing = !Terrain.FlatShading;

                    for (var index = 0; index < _data.ArraySize; index++)
                    {
                        var posInt = VoxelUtility.IndexToPos(index);

                        var pos = VoxelUtility.IndexToPosFloat(index) * BlockSize;

                        var cubeIndex = GetConfiguration(posInt.x, posInt.y, posInt.z, ref cubeValues);

                        if (cubeIndex is 0 or 255)
                        {
                            continue;
                        }

                        var edges = triTable[cubeIndex];

                        for (var i = 0; edges[i] != -1 && i < 12; i += 3)
                        {
                            var e00 = edgeConnections[edges[i]][0];
                            var e01 = edgeConnections[edges[i]][1];

                            var e10 = edgeConnections[edges[i + 1]][0];
                            var e11 = edgeConnections[edges[i + 1]][1];

                            var e20 = edgeConnections[edges[i + 2]][0];
                            var e21 = edgeConnections[edges[i + 2]][1];

                            var a = InterpolateEdges(cornerOffsets[e00], cubeValues[e00], cornerOffsets[e01],
                                cubeValues[e01]) + pos;
                            var b = InterpolateEdges(cornerOffsets[e10], cubeValues[e10], cornerOffsets[e11],
                                cubeValues[e11]) + pos;

                            var c = InterpolateEdges(cornerOffsets[e20], cubeValues[e20], cornerOffsets[e21],
                                cubeValues[e21]) + pos;

                            if (!a.Equals(b) && !a.Equals(c) && !b.Equals(c))
                            {
                                float3 normal = math.normalize(math.cross(b - a, c - a));

                                if (smoothing)
                                {
                                    if (vertices.TryGetValue(c, out var pointC))
                                    {
                                        triangles.Add(pointC);
                                    }
                                    else
                                    {
                                        var idx = vertices.Count;
                                        vertices.Add(c, idx);
                                        normals.Add(normal);

                                        triangles.Add(idx);
                                    }

                                    if (vertices.TryGetValue(a, out var pointA))
                                    {
                                        triangles.Add(pointA);
                                    }
                                    else
                                    {
                                        var idx = vertices.Count;
                                        vertices.Add(a, idx);
                                        normals.Add(normal);

                                        triangles.Add(idx);
                                    }

                                    if (vertices.TryGetValue(b, out var pointB))
                                    {
                                        triangles.Add(pointB);
                                    }
                                    else
                                    {
                                        var idx = vertices.Count;
                                        vertices.Add(b, idx);
                                        normals.Add(normal);

                                        triangles.Add(idx);
                                    }

                                }
                                else
                                {
                                    triangles.Add(unstagedVertices.Count);
                                    triangles.Add(unstagedVertices.Count + 1);
                                    triangles.Add(unstagedVertices.Count + 2);

                                    unstagedVertices.Add(new Vertex() { Position = a, Color = Color.white, UV = Vector2.one });
                                    unstagedVertices.Add(new Vertex() { Position = b, Color = Color.white, UV = Vector2.one });
                                    unstagedVertices.Add(new Vertex() { Position = c, Color = Color.white, UV = Vector2.one });

                                    normals.Add(normal);
                                    normals.Add(normal);
                                    normals.Add(normal);
                                }
                            }
                        }
                    }

                    if (smoothing)
                    {
                        _meshData.Vertices = vertices.Select(n => (Vector3)n.Key).ToList();
                    }
                    else
                    {
                        _meshData.Vertices = unstagedVertices.Select(n => n.Position).ToList();
                    }

                    _meshData.Normals = normals.Select(n => new Vector3(n.x, n.y, n.z)).ToList();
                    _meshData.Triangles = triangles;
                }

                _invokeApplyMesh = true;
            });

            thread.Start();


        }

        #endregion
    }

    public struct ChunkNeighbors
    {
        public VoxelChunk Forward;
        public VoxelChunk Top;
        public VoxelChunk Right;

        public VoxelChunk ForwardTop;
        public VoxelChunk ForwardRight;
        public VoxelChunk ForwardTopRight;

        public VoxelChunk TopRight;
    }
}
