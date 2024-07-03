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

        public Color Color;
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

        private void Start()
        {
            _terrain = GetComponent<VoxelTerrain>();

            _terrain.OnChunkLoaded += GenerateData;
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