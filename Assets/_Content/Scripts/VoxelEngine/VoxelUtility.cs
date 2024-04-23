using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{ 
    public static class VoxelUtility
    {
        private const byte VOXEL_Y_SHIFT = 4;
        private const byte VOXEL_Z_SHIFT = 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PosToIndexInt(int3 pos)
        {
            return (pos.x | pos.y << VOXEL_Y_SHIFT | pos.z << VOXEL_Z_SHIFT);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 IndexToPosFloat(int index)
        {
            var blockX = index & 0xF;
            var blockY = (index >> VOXEL_Y_SHIFT) & 0xF;
            var blockZ = (index >> VOXEL_Z_SHIFT) & 0xF;
            return new float3(blockX, blockY, blockZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 IndexToPos(int index)
        {
            var blockX = index & 0xF;
            var blockY = (index >> VOXEL_Y_SHIFT) & 0xF;
            var blockZ = (index >> VOXEL_Z_SHIFT) & 0xF;
            return new int3(blockX, blockY, blockZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 TileStretchY(float4 uv, int3 scale)
        {
            return new Vector4(uv.x * scale.x, uv.y * scale.z, uv.z * scale.x, uv.w * scale.z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 TileStretchZ(float4 uv, int3 scale)
        {
            return new Vector4(uv.x * scale.x, uv.y * scale.y, uv.z * scale.x, uv.w * scale.y);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 TileStretchX(float4 uv, int3 scale)
        {
            return new Vector4(uv.x * scale.z, uv.y * scale.y, uv.z * scale.z, uv.w * scale.y);
        }

    }
}
