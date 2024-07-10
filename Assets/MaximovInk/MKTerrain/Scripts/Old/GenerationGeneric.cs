using System;
using Icaria.Engine.Procedural;
using UnityEngine;
using SneakySquirrelLabs.MinMaxRangeAttribute;
using Unity.Mathematics;

namespace MaximovInk.VoxelEngine.Old
{


    [Serializable]
    public class MKTerrainGeneration
    {
        public int Seed;
        [Range(0.01f, 1f)]
        public float GlobalScale = 1.0f;

        public float OffsetX;
        public float OffsetY;

        public NoiseContainer Humidity;
        public NoiseContainer Temperature;
        public NoiseContainer Main;
        public NoiseContainer HeightNoise;

        public MKBiomeData[] Biomes;

        public Color NullColor = Color.magenta;

        public FilterMode FilterMode;

        public bool Colored = true;

        public int Width = 256;
        public int Height = 256;

       // public float BiomeRangeH = 0.2f;
       // public float BiomeRangeT = 0.2f;
       // public float BiomeRangeM= 0.25f;

        public NoisePreviewType Preview;

        public float WaterLine;
        public Gradient WaterGradient;

        public Texture2D Result => _result;
        private Texture2D _result;

        public float BiomeApply = 0.5f;

        public float TemperatureApply = 0.5f;

        private float GetNoise(NoiseContainer noise, float x, float y)
        {
            x = (x + (int)OffsetX) / GlobalScale;
            y = (y + (int)OffsetY) / GlobalScale;

            return noise.Evaluate(x, y, Seed);
        }

        private bool IsInRange(float v, Vector2 range)
        {
            return v < range.y && v > range.x;
        }

        public float GetRangeDistance(float v, Vector2 range)
        {
            var center = (range.x + range.y) / 2f;

            if (v > range.y)
                return (v - center);
            if (v < range.x)
                return (center - v);

            return Mathf.Clamp01(Mathf.Abs(v - center));
        }


        private MKBiomeData GetBiomeAt(int x, int y, out float h, out float t, out float m)
        {
            if (Biomes == null)
            {
                h = 0;
                t = 0;
                m = 0;
                return default;
            }
            if (Biomes.Length == 0)
            {
                h = 0;
                t = 0;
                m = 0;
                return default;
            }

            h = GetNoise(Humidity, x, y);
            t = GetNoise(Temperature, x, y);
            m = GetNoise(Main, x, y);

            //var biomesN = new float[Biomes.Length];

            var bestIndex = 0;
            var bestDistance = 0f;

            for (int i = 0; i < Biomes.Length; i++)
            {
                var biome = Biomes[i];

                /*


                 if (IsInRange(h, biome.HumidityRange) &&
                     IsInRange(t, biome.TemperatureRange) &&
                     IsInRange(m, biome.HeightRange))
                     return biome;

                 */
                var currentD = (1f - GetRangeDistance(h, biome.HumidityRange) +
                    1f - GetRangeDistance(t, biome.TemperatureRange) +
                    1f - GetRangeDistance(m, biome.HeightRange)) / 3f;

                if (bestDistance < currentD)
                {
                    bestDistance = currentD;
                    bestIndex = i;
                }

            }

            return Biomes[bestIndex];

            // return Biomes[0];
        }


        private MKBiomeData GetBiomeAt(int x, int y)
        {
            if (Biomes == null) return default;
            if (Biomes.Length == 0) return default;

            var h = GetNoise(Humidity, x, y);
            var t = GetNoise(Temperature, x, y);
            var m = GetNoise(Main, x, y);

            //var biomesN = new float[Biomes.Length];

            var bestIndex = 0;
            var bestDistance = 0f;
            
            for (int i = 0; i < Biomes.Length; i++)
            {
                var biome = Biomes[i];

                /*
                 

                 if (IsInRange(h, biome.HumidityRange) && 
                     IsInRange(t, biome.TemperatureRange) && 
                     IsInRange(m, biome.HeightRange))
                     return biome;

                 */
                var currentD = (1f-GetRangeDistance(h, biome.HumidityRange) +
                              1f-GetRangeDistance(t, biome.TemperatureRange) +
                              1f-GetRangeDistance(m, biome.HeightRange))/3f;

                if (bestDistance < currentD)
                {
                    bestDistance = currentD;
                    bestIndex = i;
                }

            }

            return Biomes[bestIndex];

           // return Biomes[0];
        }

        private Color GetColorBW(float t)
        {
            return Color.Lerp(Color.white, Color.black, t);
        }

        private Color GetColorAtPosition(int x, int y)
        {
            return GetColorAtPosition(x, y, out _);
        }

        private Color GetColorAtPosition(int x, int y, out float noise)
        {
            var biome = GetBiomeAt(x, y,out var h, out var t, out var m);
            var mainH = GetNoise(Main, x, y);
            var biomeH = GetNoise(biome.HeightData, x, y);

            var total = BiomeApply + 1f;

            var resultH = (mainH + biomeH * BiomeApply)/ total;

            resultH = Mathf.Clamp01(resultH);

            noise = resultH;

            switch (Preview)
            {
                case NoisePreviewType.Result:
                   
                    if (mainH <= WaterLine && WaterGradient != null)
                    {
                        noise = mainH;
                        return WaterGradient.Evaluate(mainH / WaterLine);
                    }

                    var c =  biome.GetColor(mainH, Colored);

                    if (biome.ColorTemperature)
                    {
                        c = Color.Lerp(c, Temperature.GetColor(t, true), TemperatureApply);

                       // c *= Temperature.GetColor(t,true);
                    }

                    return c;
                case NoisePreviewType.Humidity:
                    return Humidity.GetColor(GetNoise(Humidity, x, y), Colored);
                case NoisePreviewType.Temperature:
                    return Temperature.GetColor(GetNoise(Temperature, x, y), Colored);
                case NoisePreviewType.MainTerrain:
                    return Main.GetColor(GetNoise(Main, x, y), Colored);
                case NoisePreviewType.BiomeTerrain:
                    return biome.GetColor(biomeH, Colored);
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return NullColor;
        }

        public void GenerateTexture()
        {
            if (_result == null || _result.width != Width || _result.height != Height)
            {
                _result = new Texture2D(Height, Width);
            }

            _result.filterMode = FilterMode;

            for (int ix = 0; ix < Width; ix++)
            {
                for (int iy = 0; iy < Height; iy++)
                {
                    var color = GetColorAtPosition(ix, iy);

                    _result.SetPixel(ix, iy, color);
                }
            }

            _result.Apply();

            Painted = false;
        }

        public Color GetAtPos(int x, int y, out float value)
        {
            return GetColorAtPosition(x, y, out value);
        }

        private bool _invokeGeneration;

        public bool Painted = false;

        public void Generate() => _invokeGeneration = true;

        public void Update()
        {
            if (_invokeGeneration)
            {
                _invokeGeneration = false;
                GenerateTexture();
            }
        }
    }

    [ExecuteInEditMode]
    public class GenerationGeneric : MonoBehaviour
    {
        public MKTerrainGeneration TerrainGeneration;


        private VoxelTerrain _terrain;

        [SerializeField] private bool _preview;

        private void Awake()
        {
            _terrain = GetComponent<VoxelTerrain>();

            if(_terrain != null)
                _terrain.OnChunkLoaded += GenerateData;

            if(_preview)
                TerrainGeneration.Generate();


        }

        private static int3 ChunkSize => VoxelTerrain.ChunkSize;

        [SerializeField] private float Amplitude = 40f;

        private void GenerateData(VoxelChunk chunk)
        {
            var chunkPos = chunk.Position;


            var gridOrigin = ChunkSize * chunkPos;


            for (int ix = 0; ix < ChunkSize.x; ix++)
            {
                for (int iz = 0; iz < ChunkSize.z; iz++)
                {
                   // var height = GetHeight(ix + gridOrigin.x, iz + gridOrigin.z) - gridOrigin.y;
                    var color = TerrainGeneration.GetAtPos(ix + gridOrigin.x, iz + gridOrigin.z, out float height);

                    height = Mathf.Clamp01(height);

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
            if (_preview)
                TerrainGeneration.Generate();
        }

        private void Update()
        {
            if (_preview)
                TerrainGeneration.Update();
        }

    }



    [Serializable]
    public struct NoiseContainer
    {
        public Color Min;
        public Color Max;

        public NoiseData[] Data;

        public bool IsChangeCurve;
        public AnimationCurve Curve;

        public float Evaluate(float x, float y, int seed = 0)
        {
            var height = 0f;
            var divider = 0f;

            if (Data == null || Data.Length == 0)
                return 0f;

            for (var index = 0; index < Data.Length; index++)
            {
                var data = Data[index];
                var nx = (data.Offset.x + x) / data.Scale.x;
                var ny = (data.Offset.y + y) / data.Scale.y;

                height += data.Value * data.Evaluate(nx, ny, seed);
                divider += data.Value;
            }

            divider = Mathf.Max(0.01f, divider);

            height /= divider;

            if (IsChangeCurve)
                height = Curve.Evaluate(height);

            height = Mathf.Clamp01(height);

            return height;
        }


        public Color GetColor(float value, bool colored)
        {
            if (!colored)
                return Color.Lerp(Color.white, Color.black, value);

            return Color.Lerp(Min, Max, value);
        }
    }

    public enum NoiseType
    {
        Gradient,
        Celluar
    }

    [Serializable]
    public struct NoiseData
    {
        public Vector2 Scale;
        public Vector2 Offset;

        public int Seed;

        public float Multiplier;
        public float Value;

        public NoiseType Type;

        [Range(0f, 20f)]
        public float Round;

        public float Evaluate(float x, float y, int seed)
        {
            var value = 0f;

            switch (Type)
            {
                case NoiseType.Gradient:
                    value = (IcariaNoise.GradientNoise(x + 0.1459f, y + 0.2146f, seed + Seed) + 1f) / 2f * Multiplier;
                    //return Mathf.PerlinNoise(x, y);
                    break;
                case NoiseType.Celluar:


                    value = IcariaNoise.CellularNoise(x + 0.1459f, y + 0.2146f, seed + Seed).r;
                    break;
            }

            if (Round > 0.01f)
            {
                value = Mathf.Round(value * Round) / Round;
            }

            return value;
        }
    }

    [Serializable]
    public struct MKBiomeData
    {
        //[Range(0f, 1f)]
        //public float Humidity;
        //[Range(0f, 1f)]
        //public float Temperature;

        [MinMaxRange(0f,1f)]
        public Vector2 HumidityRange;
        [MinMaxRange(0f, 1f)]
        public Vector2 TemperatureRange;
        [MinMaxRange(0f, 1f)]
        public Vector2 HeightRange;

        public NoiseContainer HeightData;

        public bool ColorTemperature;

        public Gradient Gradient;

        public Color GetColor(float t, bool colored)
        {
            if (Gradient == null) return Color.black;

            t = Mathf.Clamp01(t);

            var c = Gradient.Evaluate(t);

            if (!colored)
            {
                Color.RGBToHSV(c, out var h, out var s, out float v);

                s = 0f;

                return Color.HSVToRGB(h, s, v);

            }

            return c;
        }
    }

    public enum NoisePreviewType
    {
        Result,
        MainTerrain,
        Humidity,
        Temperature,
        BiomeTerrain
    }

}