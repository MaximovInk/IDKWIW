using System;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public class TexturePreview
    {

        public Texture2D Texture => _texture;
        private Texture2D _texture;

        public int Width = 256;
        public int Height = 256;

        public bool InvokeGenerate;
        public bool InvokeRepaint;

        public FilterMode FilterMode;

        public void Validate()
        {
            if (_texture != null && _texture.width == Width && _texture.height == Height &&
                _texture.filterMode == FilterMode) return;


            _texture = new Texture2D(Width, Height)
            {
                filterMode = FilterMode
            };

            InvokeGenerate = true;
        }
    }

    public enum MKNoiseType
    {
        Result,
        Temp,
        Humidity,
        Ground,
        BiomeColor,
        Height
    }

    [System.Serializable]
    public struct ObjectSpawnData
    {
        public string ID;
        public float Chance;

    }

    [ExecuteInEditMode]
    public class MKTerrainGeneration : MonoBehaviour
    {

        public TexturePreview Preview = new ();

        [Range(0.01f, 2f)]
        public float GlobalScale = 1.0f;

        public Vector2 Offset;

        public MKNoise Ground = new();

        public MKNoise Temperature = new();
        public MKNoise Humidity = new();

        public MKNoiseType PreviewType;

        public float testMin = 0f;
        public float testMax = 1f;

        public float TempInfluence = 10f;
        public float HumidityInfluence = 5f;

        public MKBiome[] Biomes = Array.Empty<MKBiome>();

        [SerializeField]
        private float[] _biomeWeights = Array.Empty<float>();

        [SerializeField] private VoxelTerrain _terrain;

        public float TempColorApply = 1f;

        private void Awake()
        {
            GeneratePreview();

            _terrain = GetComponent<VoxelTerrain>();

            if (_terrain != null)
                _terrain.OnChunkLoaded += GenerateData;

        }

        private static int3 ChunkSize => VoxelTerrain.ChunkSize;

        public float HeightBlendHeight = 1f;

        [SerializeField] private float Amplitude = 40f;

        public float WaterLevel;
        public Color WaterColor;

        public ObjectSpawnData[] Objects;

        private void GenerateData(VoxelChunk chunk)
        {
            var chunkPos = chunk.Position;

            var gridOrigin = ChunkSize * chunkPos;
            PreGeneration();

            for (int ix = 0; ix < ChunkSize.x; ix++)
            {
                for (int iz = 0; iz < ChunkSize.z; iz++)
                {
                    var sampleX = ix + gridOrigin.x;
                    var sampleY = iz + gridOrigin.z;

                    _currentTemp = GetNoise(sampleX, sampleY, Temperature);
                    _currentHum = GetNoise(sampleX, sampleY, Humidity);

                    CalculateBiomeWeights();

                    var color = GetBiomeColor(sampleX, sampleY, out var t, true);

                    var height = t;

                    height *= Amplitude;

                    height -= gridOrigin.y;

                    if (height <= 0) continue;

                    for (int iy = 0; iy < ChunkSize.y && iy < height; iy++)
                    {
                        var pos = new int3(ix, iy, iz);

                        chunk.SetBlock(1, pos);
                        chunk.SetColor(color, pos);

                        var value = Mathf.Clamp01((height - iy) / (ChunkSize.y));

                        chunk.SetValue(pos, (byte)(value * 255f));
                    }

                }
            }
        }

        private void OnValidate()
        {
            GeneratePreview();
        }

        private void ScaleAtMovePos(ref float x, ref float y)
        {
            x = (x + (int)Offset.x) / GlobalScale;
            y = (y + (int)Offset.y) / GlobalScale;
        }

        private void GeneratePreviewT()
        {
            PreGeneration();

            for (int ix = 0; ix < Preview.Width; ix++)
            {
                for (int iy = 0; iy < Preview.Height; iy++)
                {
                    var t = GetNoise(ix, iy);

                    var color = GetColor(ix,iy,t);

                    testMin = Mathf.Min(testMin, t);
                    testMax = Mathf.Max(testMax, t);

                    Preview.Texture.SetPixel(ix, iy, color);
                }
            }

        }

        private void PreGeneration()
        {
            switch (PreviewType)
            {
                case MKNoiseType.Result:
                    Ground.CalculateOffsets();
                    Temperature.CalculateOffsets();
                    Humidity.CalculateOffsets();
                    break;
                case MKNoiseType.Temp:
                    Temperature.CalculateOffsets();
                    break;
                case MKNoiseType.Humidity:
                    Humidity.CalculateOffsets();
                    break;
                case MKNoiseType.Ground:
                    Ground.CalculateOffsets();
                    break;
                case MKNoiseType.BiomeColor:
                    break;
                case MKNoiseType.Height:
                    Ground.CalculateOffsets();
                    Temperature.CalculateOffsets();
                    Humidity.CalculateOffsets();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CalculateBiomeWeights()
        {
            var biomeCount = Biomes?.Length ?? 0;

            if (biomeCount == 0) return;

            if(_biomeWeights == null || _biomeWeights.Length != biomeCount)
                _biomeWeights = new float[biomeCount];

            var maxt = -1f;
            var maxi = 0;
            var sum = 0f;

            for (int i = 0; i < biomeCount; i++)
            {
                Vector2 d = new Vector2(Biomes[i].Temperature - _currentTemp, Biomes[i].Humidity - _currentHum);
                d.x *= TempInfluence; // temperature has a high influence on biome matching
                d.y *= HumidityInfluence; // crank up the humidity difference also to make biome borders less fuzzy
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

        private Color GetColor(MKBiome biome, float t)
        {
            var c = biome.Color.Evaluate(t);

            if (biome.TemperatureApply)
            {
                c = Color.Lerp(c, Temperature.GetColor(_currentTemp), TempColorApply);
            }

            return c;
        }

        private Color GetBiomeColor(float x, float y, out float hBlended, bool height=false)
        {
            hBlended = 0.0f;
            var bestIndex = 0;
            var bestWeight = 0f;
            var h = 0f;

            if(_biomeWeights.Length == 0)return Color.white;
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

            if (height)
                h = GetNoise(x,y,Biomes[bestIndex].Terrain)* bestWeight;

            hBlended = (height ? h : GetNoise(x, y, Biomes[bestIndex].Terrain) * bestWeight)  * Biomes[bestIndex].Amplitude + Biomes[bestIndex].BaseHeight * bestWeight;

            var color = GetColor(Biomes[bestIndex], h);

            for (int i = 0; i < _biomeWeights.Length; i++)
            {
                if(i == bestIndex)continue;

                var weight = _biomeWeights[i];

                if(height)
                    h = GetNoise(x, y, Biomes[i].Terrain);

                var tt = Mathf.Clamp01(weight * HeightBlendHeight);

                hBlended += (height ? h : GetNoise(x, y, Biomes[i].Terrain)) * tt * Biomes[i].Amplitude + Biomes[i].BaseHeight * tt;

                var c = GetColor(Biomes[i], h);

                color = Color.Lerp(color, c, weight);
            }

            if(hBlended < WaterLevel)
            {
                return WaterColor;
            }
            
            return color;
        }

        private float GetNoise(float x, float y, MKNoise noise)
        {
            ScaleAtMovePos(ref x, ref y);

            var value = (noise.Evaluate(x, y) + 1f)/2f;

            return value;
        }

        private float GetNoise(float x, float y)
        {
            ScaleAtMovePos(ref x, ref y);

            var value = PreviewType switch
            {
                MKNoiseType.Result => Ground.Evaluate(x, y),
                MKNoiseType.Temp => Temperature.Evaluate(x, y),
                MKNoiseType.Humidity => Humidity.Evaluate(x, y),
                MKNoiseType.Ground => Ground.Evaluate(x, y),
                _ => 0f
            };

            value = (value + 1) / 2f;

            return value;
        }

        private float _currentTemp = 0f;
        private float _currentHum = 0f;

        private Color GetColor(float x, float y, float t)
        {
            //var t = GetNoise(x, y);
            _currentTemp = GetNoise(x, y, Temperature);
            _currentHum = GetNoise(x, y, Humidity);

            CalculateBiomeWeights();

            switch (PreviewType)
            {
                case MKNoiseType.Result:

                    return GetBiomeColor(x,y,out _, true);

                    break;
                case MKNoiseType.Temp:
                    return Temperature.GetColor(t);
                    break;
                case MKNoiseType.Humidity:
                    return Humidity.GetColor(t);
                    break;
                case MKNoiseType.Ground:
                    return Ground.GetColor(t);
                    break;
                case MKNoiseType.BiomeColor:

                    return GetBiomeColor(x,y,out _,false);

                    break;
                case MKNoiseType.Height:

                    // var t = GetNoise(ix + gridOrigin.x, iz + gridOrigin.z);

                    // var color = GetColor(ix + gridOrigin.x, iz + gridOrigin.z, t);

                    var color = GetBiomeColor(x, y, out var newT, true);

                    return Color.Lerp(Color.black, Color.white, newT);

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Color.black;
        }

        private void GeneratePreview()
        {
            Preview.Validate();

            testMin = 1f;
            testMax = 0f;

            GeneratePreviewT();

            Preview.Texture.Apply();

            Preview.InvokeGenerate = false;
            Preview.InvokeRepaint = true;
        }

        private void Update()
        {
            if(Preview.InvokeGenerate)
                GeneratePreview();
        }
    }

}