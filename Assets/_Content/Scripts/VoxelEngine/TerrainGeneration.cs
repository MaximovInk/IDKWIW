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

                height += data.Value * _noise.Evaluate((data.Offset.x + x) / data.Scale.x, (data.Offset.y + y) / data.Scale.y);
                divider += data.Value;
            }

            divider = Mathf.Max(0.01f, divider);

            height /= divider;

            height *= _amplitude;

            return (float)height;
        }

        private string GetBlockId(int height)
        {
            _biomes = _biomes.OrderByDescending(n => n.FromHeight).ToList();

            for (int i = 0; i < _biomes.Count; i++)
            {
                if (height > _biomes[i].FromHeight)
                    return _biomes[i].BlockID;
            }

            return "Default";
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

            for (int ix = (int)(min.x) ; ix < max.x; ix++)
            {
                for (int iz = (int)(min.y); iz < max.y; iz++)
                {
                    var height = Mathf.Max(_minY, _minY + GetHeight(ix, iz));
                    var heightInt = (int)height;

                    var reminder = height - heightInt;

                    for (int iy = 0; iy < heightInt; iy++)
                    {
                        var blockId = GetBlockId(iy);

                        var pos = new int3(ix, iy, iz);

                        _terrain.SetBlock(blockId, pos);

                        _terrain.SetValue((byte)((height - pos.y + _minY) / _amplitude * 255f), pos);
                    }

                }
            }


        }
    }
}
