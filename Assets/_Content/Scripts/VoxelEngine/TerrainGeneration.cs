using System.Collections.Generic;
using System.Linq;
using NoiseTest;
using Unity.Mathematics;
using UnityEngine;

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

    public class TerrainGeneration : MonoBehaviour
    {
        [SerializeField]
        private List<NoiseGenerator> _noiseGenerators;

        [SerializeField] private List<Biome> _biomes;

        [SerializeField]
        private float _amplitude;

        [SerializeField] private Vector2 Size;

        [SerializeField] private int _seed;

        [SerializeField]
        private float _genDelay = 0.1f;

        [SerializeField] private int _minY;

        [SerializeField] private bool _centred;

        private float _timer;

        private bool _changed = false;

        private VoxelTerrain _terrain;

        private OpenSimplexNoise _noise;

        public bool FlatShading;

        private void OnValidate()
        {
            _changed = true;
        }

        private void Awake()
        {
            _terrain = GetComponent<VoxelTerrain>();
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_changed && _timer > _genDelay)
            {
                _terrain.FlatShading = FlatShading;

                _timer = 0f;

                _changed = false;

                Generate();
            }
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

                height += (_noise.Evaluate(nx,ny) + 1)/2f;
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
        
        public void Generate()
        {
            _terrain.Clear();

            _noise = new OpenSimplexNoise((long)(_seed));

            var max = Size / 2f;
            var min = -(Size - max);

            if (!_centred)
            {
                min = Vector2.zero;
                max = Size;
            }

            var maxY = _amplitude;

            for (int ix = (int)(min.x) ; ix < max.x; ix++)
            {
                for (int iz = (int)(min.y); iz < max.y; iz++)
                {
                    var height = GetHeight(ix, iz);

                    if (height < 0f)
                    {
                        Debug.Log(height);
                    }

                    for (int iy = 0; iy < height; iy++)
                    {
                        var blockId = GetBlockId(iy);

                        var pos = new int3(ix, iy, iz);

                        _terrain.SetBlock(blockId, pos);

                        var value = Mathf.Clamp01((height - iy) / (_amplitude + _minY + _terrain.IsoLevel));

                        _terrain.SetValue((byte)(value * 255f), pos);
                    }

                }
            }


        }
    }
}