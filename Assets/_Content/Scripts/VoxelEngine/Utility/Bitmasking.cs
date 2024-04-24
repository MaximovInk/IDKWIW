using System.Runtime.CompilerServices;

namespace MaximovInk.VoxelEngine
{
    public static class Bitmasking
    {
        public enum MarchingMask : byte
        {
            None = 0,
            Forward = 1,
            Right = 2,
            Top = 4,
            ForwardTop = 8,
            ForwardRight = 16,
            TopRight = 32,
            ForwardTopRight = 64
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte AddBit(this byte bitmask, MarchingMask value)
        {
            return AddBit(bitmask, (byte)value);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte AddBit(this byte bitmask, byte value)
        {
            bitmask |= value;

            return bitmask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasBit(this byte bitmask, byte position)
        {
            return (bitmask & position) == position;
        }

    }
}
