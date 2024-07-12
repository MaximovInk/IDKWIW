using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public class MKBiome 
    {
       // [MinMaxRange(0f, 1f)]
       // public Vector2 HumidityRange;
       // [MinMaxRange(0f, 1f)]
       // public Vector2 TemperatureRange;
        //[MinMaxRange(0f, 1f)]
       // public Vector2 HeightRange;

       public float Temperature;
       public float Humidity;

        public Gradient Color;

        public MKNoise Terrain;

        public bool TemperatureApply;

        public float Amplitude = 1f;

        public float BaseHeight = 0f;
    }
}
