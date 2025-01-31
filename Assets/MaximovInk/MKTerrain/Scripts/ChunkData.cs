﻿using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public class ChunkData : IDisposable
    {
        public NativeArray<ushort> Blocks;
        public NativeArray<byte> Value;
        public NativeArray<Color> Colors;

        public ChunkData(int width, int height, int depth)
        {
            Blocks = new NativeArray<ushort>(width*height*depth, Allocator.Persistent);
            Value = new NativeArray<byte>(width*height*depth, Allocator.Persistent);
            Colors = new NativeArray<Color>(width*height*depth, Allocator.Persistent);

            Clear();
        }

        public void Clear()
        {
            for (var i = 0; i < Blocks.Length; i++)
            {
                Blocks[i] = 0;
                Value[i] = 0;
            }
        }

        public bool IsFull()
        {
            return Blocks.All(t => t > 0);
        }

        public bool IsEmpty()
        {
            return Blocks.All(t => t <= 0);
        }

        public int ArraySize => Blocks.Length;

        public void Dispose()
        {
            Blocks.Dispose();
            Value.Dispose();
            Colors.Dispose();
        }

    }
}
