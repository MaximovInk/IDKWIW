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

       // private Matrix4x4[] matrices;
       // private MaterialPropertyBlock block;

        //private ComputeBuffer meshPropertiesBuffer;
       // private ComputeBuffer argsBuffer;

       // private Material _grassMaterial;

        private void GrassAwake()
        {
            OnInstancedRequestBuild += GrassBuildInstanced;
        }


       // private int _grassDublicatesCount = 0;
        private void GenerateGrassPoints()
        {
            grassPoints.Clear();
            grassNormals.Clear();
            grassColors.Clear();

            var triangles = _mesh.triangles;
            var vertices = _mesh.vertices;
            var normals = _mesh.normals;
            var colors = _mesh.colors;

            var scaleNoise = 200f;

            var grassDublicatesCount = Terrain.Data.GrassData.DublicatesCount;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var tIndex = triangles[i];
                var tIndex1 = triangles[i + 1];
                var tIndex2 = triangles[i + 2];

                var P1 = vertices[tIndex];
                var P2 = vertices[tIndex1];
                var P3 = vertices[tIndex2];

                if(tIndex >= _vertexToBlockIndex.Length) continue;
                if (tIndex1 >= _vertexToBlockIndex.Length) continue;
                if (tIndex2 >= _vertexToBlockIndex.Length) continue;

                if(_vertexToBlockIndex[tIndex] >= _data.Blocks.Length) continue;
                if(_vertexToBlockIndex[tIndex1] >= _data.Blocks.Length) continue;
                if(_vertexToBlockIndex[tIndex2] >= _data.Blocks.Length) continue;

                if (!Terrain.Data.GetBiomeFromBlock(_data.Blocks[_vertexToBlockIndex[tIndex]]-1).GenGrass &&
                    !Terrain.Data.GetBiomeFromBlock(_data.Blocks[_vertexToBlockIndex[tIndex1]]-1).GenGrass &&
                    !Terrain.Data.GetBiomeFromBlock(_data.Blocks[_vertexToBlockIndex[tIndex2]]-1).GenGrass)
                    continue;
                
                //if (_data.Blocks[_vertexToBlockIndex[tIndex]])

                var noiseVal = IcariaNoise.GradientNoise((P1.x + Position.x) / scaleNoise, (P2.z + Position.z) / scaleNoise);

                if (noiseVal < -0.5f) continue;

                var N1 = normals[tIndex];
                var N2 = normals[tIndex1];
                var N3 = normals[tIndex2];

                var color = colors[tIndex];

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

                    for (int k = 0; k < grassDublicatesCount; k++)
                    {
                        grassPoints.Add(offset);
                        grassNormals.Add(faceNormal);
                        grassColors.Add(color);
                    }
                }
            }
        }



        private void GrassBuildInstanced()
        {
            GenerateGrassPoints();

            InitializeBuffers();
        }


        private void InitializeBuffers()
        {

            var population = grassPoints.Count;

            if (population == 0) return;

            var data = Terrain.Data.GrassData;

            var mesh = data.GrassMesh;
            var material = new Material(data.GrassMaterial);
            var grassDublicatesCount = data.DublicatesCount;

            uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
            // Arguments for drawing mesh.
            // 0 == number of triangle indices, 1 == population, others are only relevant if drawing submeshes.
            args[0] = (uint)mesh.GetIndexCount(0);
            args[1] = (uint)population;
            args[2] = (uint)mesh.GetIndexStart(0);
            args[3] = (uint)mesh.GetBaseVertex(0);
            var argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            argsBuffer.SetData(args);

            MeshProperties[] properties = new MeshProperties[population];

            var range = data.Range;
            var yrandom = data.YRandom;
            var grassScale = data.GrassScale;
            var offsett = data.grassOffset;

            var chunkSize = VoxelTerrain.ChunkBlockSize;

            var size = new Vector3(chunkSize, chunkSize, chunkSize) / 2f;

            Vector3 position = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            Vector3 scale = Vector3.one;

            int rotateIndex = 0;

            float rotateAngle = 90 / Mathf.Max(grassDublicatesCount, 1);

            for (int i = 0; i < population; i += grassDublicatesCount + 1)
            {
                MeshProperties props = new MeshProperties();

                if (rotateIndex == 0)
                {
                    position =
                   grassPoints[i] +
                   new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range)) +
                   grassNormals[i] * offsett - size;

                    rotation =
                        Quaternion.FromToRotation(grassNormals[i], Vector3.up) *
                        Quaternion.Euler(0f, Random.Range(-180, 180), 0f);

                    scale = grassScale;
                    scale.y *= Random.Range(yrandom.x, yrandom.y);
                }
                else
                {
                    rotation *= Quaternion.Euler(0, rotateAngle, 0);
                }

                rotateIndex++;


                props.mat = Matrix4x4.TRS(position, rotation, scale);
                props.color = grassColors[i];

                properties[i] = props;

                if (rotateIndex == grassDublicatesCount + 1)
                    rotateIndex = 0;
            }

            var meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
            meshPropertiesBuffer.SetData(properties);
            material.SetBuffer("_Properties", meshPropertiesBuffer);

            AddInstancedInfo(new InstancedInfo
            {
                ArgsBuffer = argsBuffer,
                DrawCount = population,
                DrawMaterial = material,
                Mesh = mesh,
                MeshPropertiesBuffer = meshPropertiesBuffer,
                Properties = properties
            });

        }


        /*
          private void Awake()
         {
             OnInitializeModules += VoxelChunk_OnInitializeModules;
             OnDestroyModules += VoxelChunk_OnDestroyModules;

             OnMeshGenerated += VoxelChunk_OnMeshGenerated;

             OnUpdateModules += VoxelChunk_OnUpdateModules;
         }*/

        /*
         private void VoxelChunk_OnUpdateModules()
        {
            if (population == 0) return;

            //if (!_isVisible) return;

            //Graphics.DrawMeshInstanced(Terrain.GrassMesh, 0, Terrain.GrassMaterial, matrices, population, block);

            if (argsBuffer == null) return;

            var data = Terrain.Data.GrassData;

            Graphics.DrawMeshInstancedIndirect(data.GrassMesh, 0, _grassMaterial, bounds, argsBuffer);
            //Graphics.DrawMeshInstancedIndirect(Terrain.GrassMesh, 0, Terrain.GrassMaterial, bounds, argsBuffer);
        }*/


        /*
          private void VoxelChunk_OnMeshGenerated()
         {
             GrassGenerate();
         }*/
        /*

                private void VoxelChunk_OnDestroyModules()
                {
                    OnInitializeModules -= VoxelChunk_OnInitializeModules;
                    OnDestroyModules -= VoxelChunk_OnDestroyModules;
                    OnMeshGenerated -= VoxelChunk_OnMeshGenerated;
                    OnUpdateModules -= VoxelChunk_OnUpdateModules;
                }*/

        // private bool _grassGenerated = false;

        /* private void VoxelChunk_OnInitializeModules()
         {
             //GrassGenerate();
         }
 */
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


        /*
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
         */

        /*
         

         


           private void InitializeBuffers()
           {

               if (population == 0) return;

               var data = Terrain.Data.GrassData;

               var mesh = data.GrassMesh;
               _grassMaterial = new Material(data.GrassMaterial);

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

               var range = data.Range;
               var yrandom = data.YRandom;
               var grassScale = data.GrassScale;
               var offsett = data.grassOffset;

               var chunkSize = VoxelTerrain.ChunkBlockSize;

               var size = new Vector3(chunkSize, chunkSize, chunkSize)/2f;

               Vector3 position = Vector3.zero;
               Quaternion rotation = Quaternion.identity;
               Vector3 scale = Vector3.one;

               int rotateIndex = 0;

               float rotateAngle = 90 / Mathf.Max(_grassDublicatesCount, 1);

               for (int i = 0; i < population; i+= _grassDublicatesCount+1)
               {
                   MeshProperties props = new MeshProperties();

                   if (rotateIndex == 0)
                   {
                       position =
                      grassPoints[i] +
                      new Vector3(Random.Range(-range, range), 0, Random.Range(-range, range)) +
                      grassNormals[i] * offsett - size;

                       rotation =
                           Quaternion.FromToRotation(grassNormals[i], Vector3.up) *
                           Quaternion.Euler(0f, Random.Range(-180, 180), 0f);

                       scale = grassScale;
                       scale.y *= Random.Range(yrandom.x, yrandom.y);
                   }
                   else
                   {
                       rotation *= Quaternion.Euler(0, rotateAngle, 0);
                   }

                   rotateIndex++;


                   props.mat = Matrix4x4.TRS(position, rotation, scale);
                   props.color = grassColors[i];

                   properties[i] = props;

                   if (rotateIndex == _grassDublicatesCount+1)
                       rotateIndex = 0;
               }

               meshPropertiesBuffer = new ComputeBuffer(population, MeshProperties.Size());
               meshPropertiesBuffer.SetData(properties);
               _grassMaterial.SetBuffer("_Properties", meshPropertiesBuffer);


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
   */

    }
}
