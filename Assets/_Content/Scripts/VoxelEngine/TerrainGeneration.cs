using System.Collections.Generic;
using System.Linq;
using Icaria.Engine.Procedural;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public struct NoiseGenerator
    {
        [Range(0,1)]
        public float Value;

        public Vector2 Scale;
        public Vector2 Offset;
    }

    [System.Serializable]
    public struct Biome
    {
        public string BlockID;

        public int FromHeight;
    }

    [System.Serializable]
    public struct ObjectSpawnData
    {
        public GameObject[] Prefabs;

        public float Chance;

        public bool RandomRot;

        public bool RandomScale;

        public float MinSize;
        public float MaxSize;

        public int LODMax;

        public bool BlockRequire;
        public string BlockRequireID;
    }

    public class TerrainGeneration : MonoBehaviour
    {
        [SerializeField]
        private List<NoiseGenerator> _noiseGenerators;

        [SerializeField] private List<Biome> _biomes;

        [SerializeField]
        private float _amplitude;

        [SerializeField] private int _seed;

        private VoxelTerrain _terrain;

        [SerializeField] private ObjectSpawnData[] _objects;

        private void Start()
        {
            _terrain = GetComponent<VoxelTerrain>();

            _terrain.OnChunkLoaded += GenerateData;
            _terrain.OnMeshGenerated += GenerateObjects;
        }


        private void GenerateObjects(VoxelChunk chunk)
        {

            var vertices = chunk.Mesh.vertices;
            var normals = chunk.Mesh.normals;
            var colors = chunk.Mesh.colors;

            MKUtils.DestroyAllChildren(chunk.transform);

            var chunkPos = chunk.transform.position;

            var offsetRange = VoxelTerrain.BlockSize / 2f;

            /*
             vertices[i].GetHashCode()
             */

            for (int i = 0; i < vertices.Length; i++)
            {
                var index = chunk.VertexToIndex[i];

                Random.InitState(chunk.Position.GetHashCode() + index.GetHashCode());

                var objectIndex = Random.Range(0, _objects.Length);

                var objData = _objects[objectIndex];

                if (chunk.LOD > objData.LODMax)
                    continue;

                var requireColor = VoxelDatabase.GetVoxel(objData.BlockRequireID).VertexColor;

                if (objData.BlockRequire)
                {
                    var col = colors[i];

                    var distance = Mathf.Abs(requireColor.r + requireColor.g + requireColor.b - col.r - col.g - col.b);

                    if (distance > 0.1) continue;
                }

                var vertexPos = vertices[i];

                //Random.InitState(chunk.Position.GetHashCode() + objData.GetHashCode() + vertices[i].GetHashCode());

                var randomValue = Random.Range(0, 1f);

                if (randomValue > objData.Chance) continue;

                var prefab = objData.Prefabs[Random.Range(0, objData.Prefabs.Length)];

                var objectPos = vertexPos + chunkPos +
                                new Vector3(Random.Range(-offsetRange, offsetRange),
                                    0,
                                    Random.Range(-offsetRange, offsetRange)
                                );

                var instance = Instantiate(prefab, chunk.transform, true);

                instance.transform.position = objectPos;

                if (objData.RandomRot)
                {
                    instance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                }

                if (objData.RandomScale)
                {
                    instance.transform.localScale =
                        Random.Range(objData.MinSize, objData.MaxSize) * instance.transform.localScale;
                }

                instance.transform.up = normals[i];

            }


            /*
             
            for (int objectIndex = 0; objectIndex < _objects.Length; objectIndex++)
            {
                Random.InitState(chunk.Position.GetHashCode() + objectIndex.GetHashCode());

                var objData = _objects[objectIndex];

                if (chunk.LOD > objData.LODMax)
                    continue;

                var requireColor = VoxelDatabase.GetVoxel(objData.BlockRequireID).VertexColor;

                for (int i = 0; i < vertices.Length; i++)
                {

                    if (objData.BlockRequire)
                    {
                        var col = colors[i];

                        var distance = Mathf.Abs(requireColor.r + requireColor.g + requireColor.b - col.r - col.g - col.b);

                        if (distance > 0.1) continue;
                    }

                    var vertexPos = vertices[i];

                    var randomValue = Random.Range(0, 1f);

                    if (randomValue > objData.Chance) continue;

                    var objectPos = vertexPos + chunkPos +
                                    new Vector3(Random.Range(-offsetRange, offsetRange),
                                        0,
                                        Random.Range(-offsetRange, offsetRange)
                                    );

                    var instance = Instantiate(objData.Prefab, chunk.transform, true);

                    instance.transform.position = objectPos;

                    if (objData.RandomRot)
                    {
                        instance.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    }

                    if (objData.RandomScale)
                    {
                        instance.transform.localScale =
                            Random.Range(objData.MinSize, objData.MaxSize) * instance.transform.localScale;
                    }

                    instance.transform.up = normals[i];
                }

            }

             */






        }

        public float GetHeight(float x, float y)
        {
            double height = 0;

            var divider = 0f;

            for (int i = 0; i < _noiseGenerators.Count; i++)
            {
                var data = _noiseGenerators[i];

                var nx = (data.Offset.x + x) / data.Scale.x;
                var ny = (data.Offset.y + y) / data.Scale.y;


                height += data.Value * (IcariaNoise.GradientNoise(nx, ny, _seed) + 1)/2f;
                divider += data.Value;
            }

            divider = Mathf.Max(0.01f, divider);

            height /= divider;

            height *= _amplitude;

            return (float)height;
        }

        private string GetBlockId(float height)
        {
            _biomes = _biomes.OrderByDescending(n => n.FromHeight).ToList();

            for (int i = 0; i < _biomes.Count; i++)
            {
                if (height > _biomes[i].FromHeight)
                    return _biomes[i].BlockID;
            }

            return "Dirt";
        }

        private static int3 ChunkSize => VoxelTerrain.ChunkSize;

        private void GenerateData(VoxelChunk chunk)
        {
            var chunkPos = chunk.Position;

           
            var gridOrigin = ChunkSize * chunkPos;

           
            for (int ix = 0; ix < ChunkSize.x; ix++)
            {
                for (int iz = 0; iz < ChunkSize.z; iz++)
                {
                    var height = GetHeight(ix + gridOrigin.x, iz + gridOrigin.z) - gridOrigin.y;


                    if (height <= 0) continue;

                    for (int iy = 0;  iy < ChunkSize.y && iy < height; iy++)
                    {
                        var blockID = GetBlockId(iy + gridOrigin.y);

                        var pos = new int3(ix, iy, iz);

                        if (string.IsNullOrEmpty(blockID))
                        {
                            chunk.SetBlock(0, pos);
                        }

                        var index = VoxelDatabase.GetID(blockID);

                        chunk.SetBlock((ushort)(index), pos);

                        var value = Mathf.Clamp01((height - iy) / (ChunkSize.y));

                        chunk.SetValue(pos, (byte)(value * 255f));
                    }


                    

                }
            }


           

          

        }

    }
}