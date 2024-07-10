using Icaria.Engine.Procedural;
using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public class HeightMapData
    {
        public float[,] data;

        public HeightMapData(int w,int h)
        {
            data = new float[w,h];
        }
    }

    public partial class VoxelChunk
    {
        // private GrassModule _grassModule;

        private List<Vector3> grassPoints = new();
        private List<Vector3> grassNormals = new();




        private Matrix4x4[] matrices;
        private MaterialPropertyBlock block;

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

            if (!_isVisible) return;

            Graphics.DrawMeshInstanced(Terrain.GrassMesh, 0, Terrain.GrassMaterial, matrices, population, block);
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

        private void GrassGenerate()
        {
            /*
               if (_grassModule != null)
               {
                   Destroy(_grassModule.gameObject);
               }*/
            grassPoints.Clear();
            grassNormals.Clear();
            population = 0;

            if (_data.IsEmpty()) return;
            if (_data.IsFull()) return;

            if (LOD != 1) return;

            var before = Random.state;

            var seed = (int)((Position.x + 0.23f) * 2f + (Position.z - 0.435f) / 2f);

            Random.InitState(seed);

            

            if (Terrain.Data.GrassModulePrefab != null)
            {
                /*
                  _grassModule = Instantiate(Terrain.Data.GrassModulePrefab, transform);
                 _grassModule.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);*/

                /*
                 var heightMap = new HeightMapData(VoxelTerrain.ChunkSize, VoxelTerrain.ChunkSize);

                for (int i = 0; i < VoxelTerrain.ChunkSize; i++)
                {
                    for (int j = 0; j < VoxelTerrain.ChunkSize; j++)
                    {
                        heightMap.data[i,j] = GetHeightPixelAt(i, j);
                    }
                }*/

                var triangles = _mesh.triangles;
                var vertices = _mesh.vertices;
                var normals = _mesh.normals;

                var scaleNoise = 200f;

                for (int i = 0; i < triangles.Length; i+=3)
                {
                  

                    var P1 = vertices[triangles[i]];
                    var P2 = vertices[triangles[i  + 1]];
                    var P3 = vertices[triangles[i  + 2]];

                    var noiseVal = IcariaNoise.GradientNoise((P1.x + Position.x) / scaleNoise, (P2.z + Position.z) / scaleNoise);

                    if (noiseVal < -0.5f) continue;


                    var N1 = normals[triangles[i ]];
                    var N2 = normals[triangles[i  + 1]];
                    var N3 = normals[triangles[i  + 2]];

                    var center = ((P1 + P2 + P3) / 3);
                    var faceNormal = ((N1 + N2 + N3) / 3);

                   

                    var rangeAddative = Random.Range(0, 5);
                    var radius = Random.Range(1f, 3f);
                    var radius1 = Random.Range(1f, 3f);


                    for (int j = 0; j < rangeAddative; j++)
                    {
                        var offset = transform.position + center;

                        offset += new Vector3(radius,0,radius1);

                        grassPoints.Add(offset);
                        grassNormals.Add(faceNormal);
                    }

                    /*
                     var addPoints = PoissonDiscSampling.GeneratePointsInDisc(grassPoints, transform.position + center, 3f);

                    for (int j = 0; j < addPoints.Count; j++)
                    {
                        grassPoints.Add(addPoints[j]);
                        grassNormals.Add(faceNormal);
                    }*/
                }

                population = grassPoints.Count;



                matrices = new Matrix4x4[population];
                //Vector4[] colors = new Vector4[population];

                block = new MaterialPropertyBlock();

                var range = Terrain.Range;
                var yrandom = Terrain.YRandom;
                var grassScale = Terrain.GrassScale;
                var offsett = Terrain.grassOffset;

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

                   // colors[i] = Color.Lerp(Color.red, Color.blue, Random.value);
                }





                // _grassModule.Initialize(heightMap);


            }


            Random.state = before;
        }
    
        
    } 
}
