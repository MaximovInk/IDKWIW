using System;
using Icaria.Engine.Procedural;
using UnityEngine;

namespace MaximovInk
{
    [System.Serializable]
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
        public MKBiomeData[] Biomes;

        public Color NullColor = Color.magenta;

        public FilterMode FilterMode;

        public bool Colored = true;

        public int Width = 256;
        public int Height = 256;

        public float BiomeRangeH = 0.2f;
        public float BiomeRangeT = 0.2f;

        public NoisePreviewType Preview;

        public Texture2D Result => _result;
        private Texture2D _result;



        private float GetNoise(NoiseContainer noise, float x, float y)
        {
            x = (x + (int)OffsetX) / GlobalScale;
            y = (y + (int)OffsetY) / GlobalScale;

            return noise.Evaluate(x, y, Seed);
        }

        private MKBiomeData GetBiomeAt(int x, int y)
        {
            if (Biomes == null) return default;
            if (Biomes.Length == 0) return default;

            var h = GetNoise(Humidity, x, y);
            var t = GetNoise(Temperature, x, y);

            for (int i = 1; i < Biomes.Length; i++)
            {
                var biome = Biomes[i];

                var tD = Mathf.Abs(t - biome.Temperature);
                var hD = Mathf.Abs(h - biome.Humidity);

                if (tD < BiomeRangeT && hD < BiomeRangeH)
                    return biome;
            }

            return Biomes[0];
        }

        private Color GetColorBW(float t)
        {
            return Color.Lerp(Color.white, Color.black, t);
        }

        private Color GetColorAtPosition(int x, int y)
        {
            switch (Preview)
            {
                case NoisePreviewType.All:

                    var biome = GetBiomeAt(x, y);

                    return biome.GetColor(GetNoise(biome.HeightData, x, y), Colored);


                    //return GetColorBW(GetNoise(Main, x, y));

                    break;
                case NoisePreviewType.Humidity:
                    return Humidity.GetColor(GetNoise(Humidity, x, y), Colored);
                    break;
                case NoisePreviewType.Temperature:
                    return Temperature.GetColor(GetNoise(Temperature, x, y), Colored);
                    break;
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

            if(IsChangeCurve)
                height = Curve.Evaluate(height);

            height = Mathf.Clamp01(height);

            return height;
        }


        public Color GetColor(float value, bool colored)
        {
            if (!colored)
                return Color.Lerp( Color.white, Color.black, value);

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


                    value =  IcariaNoise.CellularNoise(x + 0.1459f, y + 0.2146f, seed + Seed).r;
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
        [Range(0f,1f)]
        public float Humidity;
        [Range(0f,1f)]
        public float Temperature;

        public NoiseContainer HeightData;

        public Gradient Gradient;

        public Color GetColor(float t, bool colored)
        {
            if(Gradient == null)return Color.black;

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
        All,
        Humidity,
        Temperature,
    }

    [ExecuteInEditMode]
    public class GenerationGeneric : MonoBehaviour
    {
        public MKTerrainGeneration TerrainGeneration;

        private void Awake()
        {
            TerrainGeneration.Generate();
        }

        private void OnValidate()
        {
            TerrainGeneration.Generate();
        }

        private void Update()
        {
           TerrainGeneration.Update();
        }

    }
}