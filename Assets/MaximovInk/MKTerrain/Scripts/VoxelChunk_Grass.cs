using Icaria.Engine.Procedural;
using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public class HeightMapData
    {
        public float[,] data;

        public HeightMapData(int w, int h)
        {
            data = new float[w, h];
        }
    }

    public partial class VoxelChunk
    {
        private List<Vector3> grassPoints = new();
        private List<Vector3> grassNormals = new();
        private List<Color> grassColors = new();

        private Matrix4x4[] matrices;
        private MaterialPropertyBlock block;

        private ComputeBuffer meshPropertiesBuffer;
        private ComputeBuffer argsBuffer;

        private Material _grassMaterial;

        private struct MeshProperties
        {
            public Matrix4x4 mat;
            public Vector4 color;

            public static int Size()
            {
                return
                    sizeof(float) * 4 * 4 + // matrix;
                    sizeof(float) * 4;      // color;
            }
        }

        private Bounds bounds;

        public int population;

        private void Awake()
        {
            OnInitializeModules += VoxelChunk_OnInitializeModules;
            OnDestroyModules += VoxelChunk_OnDestroyModules;

            OnMeshGenerated += VoxelChunk_OnMeshGenerated;

            OnUpdateModules += VoxelChunk_OnUpdateModules;
        }

        private void VoxelChunk_OnUpdateModules()
        {
            if (population == 0) return;

            //if (!_isVisible) return;

            //Graphics.DrawMeshInstanced(Terrain.GrassMesh, 0, Terrain.GrassMaterial, matrices, population, block);

            if (argsBuffer == null) return;

            Graphics.DrawMeshInstancedIndirect(Terrain.GrassMesh, 0, _grassMaterial, bounds, argsBuffer);
            //Graphics.DrawMeshInstancedIndirect(Terrain.GrassMesh, 0, Terrain.GrassMaterial, bounds, argsBuffer);
        }

        private bool _isVisible;

        private void OnBecameInvisible()
        {
            _isVisible = false;
        }

        private void OnBecameVisible()
        {
            _isVisible = true;
        }

        private void VoxelChunk_OnMeshGenerated()
        {
            GrassGenerate();
        }

        private void VoxelChunk_OnDestroyModules()
        {
            /*
               if (_grassModule != null)
              {
                  Destroy(_grassModule.gameObject);
              }*/

            OnInitializeModules -= VoxelChunk_OnInitializeModules;
            OnDestroyModules -= VoxelChunk_OnDestroyModules;
            OnMeshGenerated -= VoxelChunk_OnMeshGenerated;
            OnUpdateModules -= VoxelChunk_OnUpdateModules;
        }

        private bool _grassGenerated = false;

        private void VoxelChunk_OnInitializeModules()
        {
            //GrassGenerate();
        }

        private float GetHeightPixelAt(int x, int y)
        {
            int maxIndex = 0;

            for (int i = 0; i < ChunkSize; i++)
            {
                var pos = new Unity.Mathematics.int3(x, i, y);

                var block = GetBlock(pos);

                var valueBlock = GetValue(pos);

                if (block > 0 && valueBlock > Terrain.Data.IsoLevel) maxIndex = i;
            }

            var valuePos = new Unity.Mathematics.int3(x, maxIndex, y);

            var value = GetValue(valuePos) / 255f;

            return (float)(maxIndex + value) / ChunkSize;
        }

        private void OnDrawGizmosSelected()
        {
            for (int i = 0; i < grassPoints.Count; i++)
            {
                // Gizmos.DrawWireSphere(grassPoints[i], 0.2f);
                Gizmos.DrawLine(grassPoints[i], grassPoints[i] + grassNormals[i]);
            }
        }

        private void ClearGrass()
        {
            grassPoints.Clear();
            grassNormals.Clear();
            grassColors.Clear();
            population = 0;
        }

        private void OnDisable()
        {
            if (meshPropertiesBuffer != null)
            {
                meshPropertiesBuffer.Release();
            }
            meshPropertiesBuffer = null;

            if (argsBuffer != null)
            {
                argsBuffer.Release();
            }
            argsBuffer = null;
        }

        private void GenerateGrassPoints()
        {

            var triangles = _mesh.triangles;
            var vertices = _mesh.vertices;
            var normals = _mesh.normals;
            var colors = _mesh.colors;

            var scaleNoise = 200f;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var P1 = vertices[triangles[i]];
                var P2 = vertices[triangles[i + 1]];
                var P3 = vertices[triangles[i + 2]];

                var noiseVal = IcariaNoise.GradientNoise((P1.x + Position.x) / scaleNoise, (P2.z + Position.z) / scaleNoise);

                if (noiseVal < -0.5f) continue;

                var N1 = normals[triangles[i]];
                var N2 = normals[triangles[i + 1]];
                var N3 = normals[triangles[i + 2]];

                var color = colors[triangles[i]];

                var center = ((P1 + P2 + P3) / 3);
                var faceNormal = ((N1 + N2 + N3) / 3);

                var rangeAddative = Random.Range(0, 5);
                var radius = Random.Range(1f, 3f);
                var radius1 = Random.Range(1f, 3f);

                for (int j = 0; j < rangeAddative; j++)
                {
                    //var offset = transform.position + center;
                    var offset = center;

                    offset += new Vector3(radius, 0, radius1);

                    grassPoints.Add(offset);
                    grassNormals.Add(faceNormal);
                    grassColors.Add(color);
                }
            }

            population = grassPoints.Count;


        }

        private void GenerateMatrices()
        {
            matrices = new Matrix4x4[population];
            //Vector4[] colors = new Vector4[population];

            block = new MaterialPropertyBlock();

            var range = Terrain.Range;
            var yrandom = Terrain.YRandom;
            var grassScale = Terrain.GrassScale;
            var offsett = Terrain.grassOffset;

            //var bounds = new Bounds(transform.position, Vector3.one);

            //var min = bounds.min;
            //var max = bounds.max;

            for (int i = 0; i < population; i++)
            {
                // Build matrix.
                Vector3 position =
                    grassPoints[i] +
                    new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range)) +
                    grassNormals[i] * offsett;

                Quaternion rotation =
                    Quaternion.FromToRotation(grassNormals[i], Vector3.up) *
                    Quaternion.Euler(0f, Random.Range(-180, 180), 0f);

                Vector3 scale = grassScale;
                scale.y = Random.Range(yrandom.x, yrandom.y);

                var mat = Matrix4x4.TRS(position, rotation, scale);

                matrices[i] = mat;

                //min = Vector3.Min(min, position - scale);
                //max = Vector3.Max(max, position + scale);

            }

        }

        private void InitializeBuffers()
        {
            if (population == 0) return;

            var mesh = Terrain.GrassMesh;
            _grassMaterial = new Material(Terrain.GrassMaterial);

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            // Arguments for drawing mesh.
            // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
            args[0] = (uint)mesh.GetIndexCount(0);
            args[1] = (uint)population;
            args[2] = (uint)mesh.GetIndexStart(0);
            args[3] = (uint)mesh.GetBaseVertex(0);
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            MeshProperties[] properties = new MeshProperties[population];

            var range = Terrain.Range;
            var yrandom = Terrain.YRandom;
            var grassScale = Terrain.GrassScale;
            var offsett = Terrain.grassOffset;


            var chunkSize = VoxelTerrain.ChunkBlockSize;

            var size = new Vector3(chunkSize, chunkSize, chunkSize)/2f;

            for (int i = 0; i < population; i++)
            {
                MeshProperties props = new MeshProperties();
                Vector3 position =
                     grassPoints[i] +
                     new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range)) +
                     grassNormals[i] * offsett - size;

                Quaternion rotation =
                    Quaternion.FromToRotation(grassNormals[i], Vector3.up) *
                    Quaternion.Euler(0f, Random.Range(-180, 180), 0f);

                Vector3 scale = grassScale;
                scale.y *= Random.Range(yrandom.x, yrandom.y);

                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = grassColors[i];

                properties[i] = props;
            }

            meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
            meshPropertiesBuffer.SetData(properties);
            _grassMaterial.SetBuffer("_Properties", meshPropertiesBuffer);
            //Terrain.GrassMaterial.SetBuffer("_Properties", meshPropertiesBuffer);


        }

        private void OnDrawGizmos()
        {
            if (population == 0 || LOD != 1) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void GrassGenerate()
        {
            ClearGrass();

            if (_data.IsEmpty()) return;
            if (_data.IsFull()) return;

            if (LOD != 1) return;

            var before = Random.state;

            var seed = (int)((Position.x + 0.23f) * 2f + (Position.z - 0.435f) / 2f);

            Random.InitState(seed);

            var chunkSize = VoxelTerrain.ChunkBlockSize;

            var size = new Vector3(chunkSize, chunkSize, chunkSize);

            bounds = new Bounds(transform.position + size / 2f, size);

            GenerateGrassPoints();

            //GenerateMatrices();

            InitializeBuffers();

            Random.state = before;
        }


    }
}
