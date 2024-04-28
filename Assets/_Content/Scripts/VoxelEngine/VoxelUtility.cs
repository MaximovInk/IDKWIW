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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawChunkBounds(VoxelChunk chunk)
        {
            var globalPos = chunk.Terrain.transform.position;

            var chunkSize = VoxelTerrain.DoubleChunkSize;

            var halfSize = chunkSize / 2;

            var chunkPos = chunk.Position;


            DrawBox(globalPos + new Vector3(
                    chunkPos.x * chunkSize + halfSize, chunkPos.y * chunkSize + halfSize, chunkPos.z * chunkSize + halfSize),
                Quaternion.identity, new Vector3(chunkSize, chunkSize, chunkSize), Color.blue);

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
