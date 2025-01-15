using System;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public class BasePass : TerrainGeneratePass
    {
        private BaseTerrainGenerator _generator;

        private Vector2Int _offset;
        private float _globalScale;

        private float _currentTemp;
        private float _currentHum;

        private float[] _biomeWeights = Array.Empty<float>();

        private MKBiome[] _biomes;

        public BasePass(BaseTerrainGenerator generator)
        {
            _generator = generator;

            _offset =new Vector2Int((int)_generator.Offset.x, (int)_generator.Offset.y);
            _globalScale = _generator.GlobalScale;
        }

        private void ScaleAtMovePos(ref float x, ref float y)
        {
            x = (x + _offset.x) / _globalScale;
            y = (y + _offset.y) / _globalScale;
        }

        public override void Generate(IChunkDataInterface data, int stage = -1)
        {
            var chunkPos = data.Position;

            var chunkSize = data.ChunkSize;

            var gridOrigin = chunkSize * chunkPos;

            _biomes = _generator.Terrain.Data.Biomes;

            for (int ix = 0; ix < chunkSize.x; ix++)
            {
                for (int iz = 0; iz < chunkSize.z; iz++)
                {
                    var sampleX = ix + gridOrigin.x;
                    var sampleY = iz + gridOrigin.z;

                    _currentTemp = _generator.TempNoise.Evaluate(sampleX,sampleY);
                    if(stage == 0)
                    {
                        var pos = new int3(ix, 1, iz);

                        data.Set(
                          pos,
                            0,
                          _generator.TempNoise.Color.Evaluate(_currentTemp),
                          255
                          );

                        continue;
                    }

                    _currentHum = _generator.HumNoise.Evaluate(sampleX, sampleY);
                    if(stage == 1)
                    {
                        var pos = new int3(ix, 1, iz);

                        data.Set(
                          pos,
                          0,
                          _generator.HumNoise.Color.Evaluate(_currentHum),
                          255
                          );

                        continue;
                    }

                    CalculateBiomeWeights();

                    var info = GetBiomeInfo(sampleX, sampleY);

                    var height = info.Value;

                    height *= _generator.Amplitude;

                    if (height <= 0) continue;

                    for (int iy = 0; iy < chunkSize.y && iy < height; iy++)
                    {
                        var pos = new int3(ix, iy, iz);
                        var value = Mathf.Clamp01((height - iy) / (chunkSize.y));

                        data.Set(
                            pos,
                            (ushort)(info.PreferBiomeIndex + 1),
                            info.Color,
                            (byte)(value * 255f)
                            ); 
                    }


                }
            }
        }


        private void CalculateBiomeWeights()
        {
            var biomeCount = _biomes.Length;

            if (biomeCount == 0) return;

            if (_biomeWeights == null || _biomeWeights.Length != biomeCount)
                _biomeWeights = new float[biomeCount];

            var maxt = -1f;
            var maxi = 0;
            var sum = 0f;

            for (int i = 0; i < biomeCount; i++)
            {
                Vector2 d = new Vector2(_biomes[i].Temperature - _currentTemp, _biomes[i].Humidity - _currentHum);
                d.x *= _generator.TempInfluence; // temperature has a high influence on biome matching
                d.y *= _generator.HumidityInfluence; // crank up the humidity difference also to make biome borders less fuzzy
                _biomeWeights[i] = Mathf.Max(0, 1.0f - (d.x * d.x + d.y * d.y) * 0.1f);
                // record highest weight
                if (_biomeWeights[i] > maxt)
                {
                    maxi = i;
                    maxt = _biomeWeights[i];
                }
                sum += _biomeWeights[i];
            }

            if (sum > .001)
            {
                // normalize the weights so they add up to 1
                sum = 1.0f / sum;
                for (int i = 0; i < biomeCount; i++)
                    _biomeWeights[i] *= sum;
            }
            else
            {
                // sum of all weights is very close to zero, just zero all weights and set the highest to 1.0
                // this helped with artifacts at biome borders
                for (int i = 0; i < biomeCount; i++)
                    _biomeWeights[i] = 0.0f;
                _biomeWeights[maxi] = 1.0f;
            }

        }

        private HeightCellInfo GetBiomeInfo(float x, float y)
        {
            var hBlended = 0.0f;
            var bestIndex = 0;
            var bestWeight = 0f;
            var h = 0f;

            if (_biomeWeights.Length == 0)
            {
                //return new BiomeInfo() { IsUnderwater = false, PreferBiomeIndex = -1, Color = Color.white };
                return new HeightCellInfo()
                {
                    IsUnderwater = false,
                    PreferBiomeIndex = -1, 
                    Color = Color.magenta 
                };
            }
            bestWeight = _biomeWeights[bestIndex];

            for (int i = 1; i < _biomeWeights.Length; i++)
            {
                var curW = _biomeWeights[i];
                if (curW > bestWeight)
                {
                    bestIndex = i;
                    bestWeight = curW;
                }
            }

            h = _biomes[bestIndex].Height.Evaluate(x,y) * bestWeight;

            hBlended = h;

            var color = GetColor(_biomes[bestIndex], h);

            for (int i = 0; i < _biomeWeights.Length; i++)
            {
                if (i == bestIndex) continue;

                var weight = _biomeWeights[i];

                h = _biomes[i].Height.Evaluate(x, y);

                var tt = Mathf.Clamp01(weight * _generator.HeightBlendHeight);

                hBlended += h;

                var c = GetColor(_biomes[i], h);

                color = Color.Lerp(color, c, weight);
            }

            var isUnderwater = hBlended < _generator.WaterLevel;

            return new HeightCellInfo()
            {
                IsUnderwater = isUnderwater,
                PreferBiomeIndex = bestIndex,
                PreferBiome2 = bestIndex,
                Color = isUnderwater?_generator.WaterColor : color,
                Value = hBlended,
                X = x,
                Y = y
            };

        }

        private Color GetColor(MKBiome biome, float t)
        {
            var c = biome.Color.Evaluate(t);

            return c;
        }

    }

    public interface IMKNoise
    {
        public int Seed { get; }

        public Vector2 Scale { get; }
        public Vector2 Offset { get; }

        public float Evaluate(float x, float y);

    }

    [System.Serializable]
    public class MKNoiseBase : IMKNoise
    {
        public int Seed => _seed;
        [SerializeField]
        private int _seed;

        private int _lastSeed;

        public Vector2 Scale => _scale;
        [SerializeField]
        private Vector2 _scale = Vector2.one;

        public Vector2 Offset => _offset;
        [SerializeField]
        private Vector2 _offset;

        public int Octaves = 4;
        [Range(0, 1)]
        public float Persistance = 0.5f;
        [Min(1f)]
        public float Lacunarity = 2f;

        [HideInInspector, SerializeField]
        private Vector2[] _offsets;

        public Gradient Color = new Gradient()
        {
            alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f,0f),
                new GradientAlphaKey(1f,1f),
            },

            colorKeys = new GradientColorKey[] { 
                new GradientColorKey(UnityEngine.Color.black, 0f),
                new GradientColorKey(UnityEngine.Color.white, 1f),
            }
        };

        public float Evaluate(float x, float y)
        {
            if (_offsets.Length != Octaves || _lastSeed != Seed)
            {
                CalculateOffsets();
                _lastSeed = Seed;
            }

            var frequency = 1f;
            var amplitude = 1f;
            var value = 0f;

            for (int i = 0; i < Octaves; i++)
            {
                var dX = Scale.x * frequency;
                var dY = Scale.y * frequency;

                dX = Mathf.Max(dX, 0.001f);
                dY = Mathf.Max(dY, 0.001f);

                var sampleX = (x + Offset.x + _offsets[i].x) / dX;
                var sampleY = (y + Offset.y + _offsets[i].y) / dY;

                var sample = MKNoiseUtils.Gradient(sampleX, sampleY, Seed) * 2 - 1;

                value += amplitude * sample;

                amplitude *= Persistance;
                frequency *= Lacunarity;
            }

            return value;
        }

        public void CalculateOffsets()
        {
            if (_offsets == null || _offsets.Length != Octaves)
                _offsets = new Vector2[Octaves];

            var rnd = new System.Random(Seed);

            for (int i = 0; i < _offsets.Length; i++)
            {
                float offsetX = rnd.Next(-10000, 10000);
                float offsetY = rnd.Next(-10000, 10000);

                _offsets[i] = new Vector2(offsetX, offsetY);
            }


        }
    }

    public class BaseTerrainGenerator : MonoBehaviour, ITerrainGenerator, ITerrainPasses
    {
        [Range(0.01f, 2f)]
        public float GlobalScale = 1.0f;

        public Vector2 Offset;

        public float Amplitude { get => _amplitude; }

        [SerializeField] private float _amplitude = 1f;

        public VoxelTerrain Terrain => _terrain;

        [SerializeField]
        private VoxelTerrain _terrain;

        //public MKNoiseBase HeightPassNoise;
        public MKNoiseBase TempNoise;
        public MKNoiseBase HumNoise;

        public float TempInfluence = 10f;
        public float HumidityInfluence = 5f;
        public float HeightBlendHeight = 1f;
        public float WaterLevel;
        public Color WaterColor;


        public void Generate(IChunkDataInterface data)
        {
            for (int i = 0; i < Passes.Length; i++)
            {
                Passes[i].Generate(data);
            }
        }

        public IMKNoise[] GetNoises
        {
            get
            {
                return new IMKNoise[]
                {
                    TempNoise,
                    HumNoise
                };
            }
        }

        public TerrainGeneratePass[] Passes
        {
            get
            {
                return new TerrainGeneratePass[]
                {
                    new BasePass(this)
                };
            }
        }
    }
}
