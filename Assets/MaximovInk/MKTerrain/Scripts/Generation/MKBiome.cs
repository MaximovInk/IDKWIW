using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public class MKBiome
    {
        public float Temperature;
        public float Humidity;

        public Gradient Color;
        public MKNoise Terrain;

        public MKNoiseBase Height;

        public bool TemperatureApply;

        public float Amplitude = 1f;
        public float BaseHeight = 0f;

        public bool GenGrass;
        public bool GenTrees;



        public string[] Tags;
    }
}
