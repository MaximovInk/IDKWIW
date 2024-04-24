using System.Linq;

namespace MaximovInk.VoxelEngine
{
    public class ChunkData
    {
        public ushort[] Blocks;
        public byte[] Value;

        public ChunkData(int width, int height, int depth)
        {
            Blocks = new ushort[width * height * depth];
            Value = new byte[width * height * depth];

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

        public bool IsEmpty()
        {
            return Blocks.All(t => t <= 0);
        }

        public int ArraySize => Blocks.Length;

    }
}
