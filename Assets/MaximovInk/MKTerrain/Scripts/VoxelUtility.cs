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
        public static int3 LocalGridToGlobalGrid(VoxelChunk chunk, int3 localPos)
        {
            return new int3(
                localPos.x + chunk.Position.x * VoxelTerrain.ChunkSize,
                localPos.y + chunk.Position.y * VoxelTerrain.ChunkSize,
                localPos.z + chunk.Position.z * VoxelTerrain.ChunkSize);
        }

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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawChunkBounds(VoxelChunk chunk)
        {
            var chunkSize = VoxelTerrain.DoubleChunkSize;

            var halfSize = chunkSize / 2;

            var blockSize = VoxelTerrain.BlockSize;

            var chunkBlockSize = chunkSize * blockSize;
            var chunkBlockHalfSize = halfSize * blockSize;

            DrawBox(chunk.transform.position + new Vector3(
                     chunkBlockHalfSize, chunkBlockHalfSize, chunkBlockHalfSize),
                Quaternion.identity, new Vector3(chunkBlockSize, chunkBlockSize, chunkBlockSize), Color.blue);

        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValidateLodValue(int value)
        {
            var limit = VoxelTerrain.ChunkSize;

            if (value <= 1)
            {
                value = 1;
                return value;
            }

            if (value > limit)
            {
                value = limit;
                return value;
            }

            if (value is 16 or 8 or 4 or 2)
                return value;

            if (value > 16)
                value = 16;
            else if (value > 8)
                value = 8;
            else if (value > 4)
                value = 4;
            else
                value = 2;

            return value;
        }

        private static void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
        {
            // create matrix
            Matrix4x4 m = new Matrix4x4();
            m.SetTRS(pos, rot, scale);

            var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
            var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
            var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
            var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));

            var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
            var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
            var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
            var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));

            Debug.DrawLine(point1, point2, c);
            Debug.DrawLine(point2, point3, c);
            Debug.DrawLine(point3, point4, c);
            Debug.DrawLine(point4, point1, c);
            Debug.DrawLine(point5, point6, c);
            Debug.DrawLine(point6, point7, c);
            Debug.DrawLine(point7, point8, c);
            Debug.DrawLine(point8, point5, c);
            Debug.DrawLine(point1, point5, c);
            Debug.DrawLine(point2, point6, c);
            Debug.DrawLine(point3, point7, c);
            Debug.DrawLine(point4, point8, c);


        }

    }
}
