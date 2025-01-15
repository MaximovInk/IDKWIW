using NUnit.Framework;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace MaximovInk.VoxelEngine
{

    [BurstCompile]
    public struct MarchingCubesJob : IJob
    {
        private const int ChunkSize = VoxelTerrain.ChunkSize;
        private const float BlockSize = VoxelTerrain.BlockSize;

        //Current chunk data
        public NativeArray<ushort> BlockData; //0 - empty
        public NativeArray<byte> Values; //isoValue (density)
        public NativeArray<Color> Colors; // colors of blocks

        public int LOD;
        public bool EnableSmoothing;


        public float IsoLevel; //iso level 0-1
        public byte IsoLevelByte; //same iso level (0-255)

        //Neighbors (If chunk neighbor not exists, LOD same as current Chunk LOD and array lenght = 0)
        public NativeArray<byte> DensityForwardNeighbor;
        public int LODForward;
        public NativeArray<byte> DensityRightNeighbor;
        public int LODRight;
        public NativeArray<byte> DensityTopNeighbor;
        public int LODTop;

        public NativeArray<byte> DensityForwardRightTopNeighbor;
        public int LODForwardRightTop;
        public NativeArray<byte> DensityTopRightNeighbor;
        public int LODTopRight;
        public NativeArray<byte> DensityForwardRightNeighbor;
        public int LODForwardRight;
        public NativeArray<byte> DensityForwardTopNeighbor;
        public int LODForwardTop;

        //Result of job
        public NativeList<float3> OutputVertices;
        public NativeList<float3> OutputNormals;
        public NativeList<float4> OutputColors;
        public NativeList<float2> OutputUVs;
        public NativeList<int> OutputTriangles;
        public NativeList<ushort> VertexToIndex;

        public bool IsEmpty;
        public bool IsFull;



        private Color GetColor(int blockId)
        {
            return Colors[blockId];
        }

        private int3 GetOffset(int index)
        {
            // Используем заранее определённый NativeArray для хранения смещений
            switch (index)
            {
                case 0: return new int3(0, 0, 0);
                case 1: return new int3(1, 0, 0);
                case 2: return new int3(1, 1, 0);
                case 3: return new int3(0, 1, 0);
                case 4: return new int3(0, 0, 1);
                case 5: return new int3(1, 0, 1);
                case 6: return new int3(1, 1, 1);
                case 7: return new int3(0, 1, 1);
                default: return new int3(0, 0, 0);
            }
        }

        public void Execute()
        {
            /* // Проверка, пустой ли чанк или полностью заполнен
             if (IsEmpty || IsFull)
                 return;*/

            int chunkSize = ChunkSize >> LOD;
            float blockSize = BlockSize * (1 << LOD);
            float3 blockSize3 = new float3(blockSize, blockSize, blockSize);

            NativeArray<float> densities = new NativeArray<float>(8, Allocator.Temp);
            NativeArray<float4> colors = new NativeArray<float4>(8, Allocator.Temp);

            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        var pos = new int3(x, y, z);
                        var index = VoxelUtility.PosToIndexInt(pos);

                        if (BlockData[index] > 0)
                        {
                            IsEmpty = false;
                        }
                        else
                        {
                            IsFull = false;
                        }


                       // float[] densities = new float[8];
                      //  float4[] colors = new float4[8];

                        for (int i = 0; i < 8; i++)
                        {
                            int3 corner = GetOffset(i) + new int3(x, y, z);
                            densities[i] = GetDensity(corner);
                            colors[i] = GetVertexColor(corner);
                        }

                        int cubeIndex = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            if (densities[i] >= IsoLevel)
                                cubeIndex |= (1 << i);
                        }

                        if (cubeIndex == 0 || cubeIndex == 255)
                            continue;

                        GenerateCellGeometry(x, y, z, cubeIndex, densities, colors, blockSize);
                    }
                }
            }

            densities.Dispose();
            colors.Dispose();
        }

        private float GetDensity(int3 corner)
        {
            if (corner.x >= 0 && corner.x < ChunkSize &&
                corner.y >= 0 && corner.y < ChunkSize &&
                corner.z >= 0 && corner.z < ChunkSize)
            {
                int index = corner.x + ChunkSize * (corner.y + ChunkSize * corner.z);
                return Values[index] / 255f;
            }

            return GetNeighborDensity(corner);
        }

        private float4 GetVertexColor(int3 corner)
        {
            if (corner.x >= 0 && corner.x < ChunkSize &&
                corner.y >= 0 && corner.y < ChunkSize &&
                corner.z >= 0 && corner.z < ChunkSize)
            {
                int index = corner.x + ChunkSize * (corner.y + ChunkSize * corner.z);
                return new float4(Colors[index].r, Colors[index].g, Colors[index].b, Colors[index].a);
            }

            return new float4(0,0,0,0); // Возврат стандартного цвета для отсутствующих данных
        }

        private float GetNeighborDensity(int3 corner)
        {
            return 0f; // Заменить на логику обработки соседних чанков
        }

        private void GenerateCellGeometry(int x, int y, int z, int cubeIndex, NativeArray<float> densities, NativeArray<float4> colors, float blockSize)
        {
            int[] edges = TriTable[cubeIndex];

            int offset = cubeIndex * 16;
            for (int i = 0; edges[i] != -1; i += 3)
            {
                float3 v0 = InterpolateVertex(edges[i], densities, x, y, z, blockSize);
                float3 v1 = InterpolateVertex(edges[i + 1], densities, x, y, z, blockSize);
                float3 v2 = InterpolateVertex(edges[i + 2], densities, x, y, z, blockSize);

                float4 c0 = InterpolateColor(edges[i], densities, colors);
                float4 c1 = InterpolateColor(edges[i + 1], densities, colors);
                float4 c2 = InterpolateColor(edges[i + 2], densities, colors);

                float3 normal = math.normalize(math.cross(v1 - v0, v2 - v0));

                OutputVertices.Add(v0);
                OutputVertices.Add(v1);
                OutputVertices.Add(v2);

                OutputNormals.Add(normal);
                OutputNormals.Add(normal);
                OutputNormals.Add(normal);

                OutputColors.Add(c0);
                OutputColors.Add(c1);
                OutputColors.Add(c2);

                OutputTriangles.Add(OutputVertices.Length - 3);
                OutputTriangles.Add(OutputVertices.Length - 2);
                OutputTriangles.Add(OutputVertices.Length - 1);
            }
        }

        private float3 InterpolateVertex(int edgeIndex, NativeArray<float> densities, int x, int y, int z, float3 blockSize)
        {
            int3 v0 = Offsets[EdgeConnections[edgeIndex][0]] + new int3(x, y, z);
            int3 v1 = Offsets[EdgeConnections[edgeIndex][1]] + new int3(x, y, z);

            float t = math.unlerp(densities[EdgeConnections[edgeIndex][0]], densities[EdgeConnections[edgeIndex][1]], IsoLevel);
            float3 p0 = v0 * blockSize;
            float3 p1 = v1 * blockSize;

            return math.lerp(p0, p1, t);
        }

        private float4 InterpolateColor(int edgeIndex, NativeArray<float> densities, NativeArray<float4> colors)
        {
            int idx0 = EdgeConnections[edgeIndex][0];
            int idx1 = EdgeConnections[edgeIndex][1];

            float t = math.unlerp(densities[idx0], densities[idx1], IsoLevel);
            return math.lerp(colors[idx0], colors[idx1], t);
        }



        //References to tables
        private static readonly byte[] RegularCellClass = TransvoxelTables.RegularCellClass;
        private static readonly RegularCellData[] RegularCellData = TransvoxelTables.RegularCellData;
        private static readonly ushort[][] RegularVertexData = TransvoxelTables.RegularVertexData;
        private static readonly byte[] TransitionCellClass = TransvoxelTables.TransitionCellClass;
        private static readonly TransitionCellData[] TransitionCellData = TransvoxelTables.TransitionCellData;
        private static readonly ushort[][] TransitionVertexData = TransvoxelTables.TransitionVertexData;
        private static readonly byte[] TransitionCornerData = TransvoxelTables.TransitionCornerData;

        private static readonly int[][] EdgeConnections = MarchingCubesTables.EdgeConnections;
        private static readonly int3[] Offsets = MarchingCubesTables.Offsets;
        private static readonly int[][] TriTable = MarchingCubesTables.TriTable;
        private static readonly float3[] CornerOffsets = MarchingCubesTables.CornerOffsets;
    }
}