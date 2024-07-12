using System;
using Icaria.Engine.Procedural;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace MaximovInk.VoxelEngine
{
    public enum NoiseType
    {
        Gradient = 0,
        Cellular = 1
    }

    [System.Serializable]
    public class MKNoise
    {
        public int Octaves = 4;
        [Range(0,1)]
        public float Persistance = 0.5f;
        [Min(1f)]
        public float Lacunarity = 2f;

        [Space]

        public int Seed;
        [Range(0f, 1000f)] public float Round;

        public float ScaleX = 1f;
        public float ScaleY = 1f;
        public float OffsetX;
        public float OffsetY;

        [Space] 
        
        public Gradient Color = new();

        [Space]

        public bool IsCustomOctaves;
        public NoiseOctave[] CustomOctaves = Array.Empty<NoiseOctave>();

        [HideInInspector,SerializeField]
        private Vector2[] _offsets;

        public Color GetColor(float t)
        {
            //t = Mathf.Clamp01(t);

            return Color.Evaluate(t);
        }

        private float CustomEvaluate(float x, float y)
        {
            var value = 0f;
            var divider = 0f;

            foreach (var octave in CustomOctaves)
            {
                var nx = (x+ octave.OffsetX) / octave.ScaleX;
                var ny = (y + octave.OffsetY) / octave.ScaleY ;

                value += octave.Weight * octave.Evaluate(nx, ny, Seed);
                divider += octave.Weight;
            }

            divider = Mathf.Max(0.01f, divider);

            value /= divider;

            return value;

        }

        private float DefaultEvaluate(float x,float y)
        {
            if (_offsets.Length != Octaves) return 0f;

            var frequency = 1f;
            var amplitude = 1f;
            var value = 0f;

            for (int i = 0; i < Octaves; i++)
            {
                var sampleX = (x+ OffsetX + _offsets[i].x) / ScaleX * frequency ;
                var sampleY = (y + OffsetY + _offsets[i].y) / ScaleY * frequency ;

                var sample = MKNoiseUtils.Gradient(sampleX, sampleY, Seed) * 2 - 1;
                
                value += amplitude * sample;

                amplitude *= Persistance;
                frequency *= Lacunarity;
            }

            return value;
        }

        public void CalculateOffsets()
        {
            if (IsCustomOctaves) return;

            if (_offsets== null || _offsets.Length != Octaves)
                _offsets = new Vector2[Octaves];

            var rnd = new System.Random(Seed);

            for (int i = 0; i < _offsets.Length; i++)
            {
                float offsetX = rnd.Next(-10000, 10000);
                float offsetY = rnd.Next(-10000, 10000);

                _offsets[i] = new Vector2(offsetX, offsetY);
            }


        }

        public float Evaluate(float x, float y)
        {
            var value = IsCustomOctaves ? CustomEvaluate(x, y) : DefaultEvaluate(x, y);

            if (Round > 0.01f)
            {
                value = Mathf.Round(value * Round) / Round;
            }

            return value;
        }
    }

    [System.Serializable]
    public class NoiseOctave
    {
        [Min(0.001f)] public float ScaleX = 1f;
        [Min(0.001f)] public float ScaleY = 1f;

        public float OffsetX = 0f;

        public float OffsetY = 0f;

        public NoiseType Type = NoiseType.Gradient;
        [Range(0f, 1000f)] public float Round;
        public int Seed;

        public float Weight = 1f;

        public float Evaluate(float x, float y, int seed = 0)
        {
            var value = 0f;

            seed += Seed;

            value = Type switch
            {
                NoiseType.Gradient => MKNoiseUtils.Gradient(x, y, seed),
                NoiseType.Cellular => MKNoiseUtils.Cellular(x, y, seed),
                _ => value
            };

            if (Round > 0.01f)
            {
                value = Mathf.Round(value * Round) / Round;
            }

            value = Mathf.Clamp01(value);

            return value;
        }
    }

    public static class MKNoiseUtils
    {

        private const float RANGE = 0.6f;
        private const float RANGE_D = RANGE * 2F;

        public static float Gradient(float x, float y, int seed = 0)
        {
            return Mathf.PerlinNoise(x, y);
            //return (IcariaNoise.GradientNoise(x, y, seed) + RANGE) / RANGE_D;
        }

        public static float Cellular(float x, float y, int seed = 0)
        {
            return IcariaNoise.CellularNoise(x, y, seed).r;
        }
    }
}

