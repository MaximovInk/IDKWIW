using Icaria.Engine.Procedural;
using System.Runtime.CompilerServices;

namespace MaximovInk.VoxelEngine
{
    public struct MKCellularResult
    {
        /// <summary> The distance to the closest cell center.</summary>
        public readonly float d0;
        /// <summary> The distance to the second-closest cell center.</summary>
        public readonly float d1;
        /// <summary> A random 0 - 1 value for each cell. </summary>
        public readonly float r;

        public MKCellularResult(float d0, float d1, float r)
        {
            this.d0 = d0;
            this.d1 = d1;
            this.r = r;
        }
    }

    public static class MKNoiseUtility
    {
        private const float RANGE = 0.6f;
        private const float RANGE_D = RANGE * 2F;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Gradient(float x, float y, int seed = 0)
        {
            //return Mathf.PerlinNoise(x, y);
            return (IcariaNoise.GradientNoise(x, y, seed) + RANGE) / RANGE_D;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MKCellularResult Cellular(float x, float y, int seed = 0)
        {
            var t = IcariaNoise.CellularNoise(x, y, seed);

            return new MKCellularResult(t.d0,t.d1,t.r);
        }
    }
}
