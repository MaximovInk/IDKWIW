namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public struct ChunkNeighbors
    {
        public VoxelChunk Forward;
        public VoxelChunk Top;
        public VoxelChunk Right;

        public VoxelChunk ForwardTop;
        public VoxelChunk ForwardRight;
        public VoxelChunk ForwardTopRight;

        public VoxelChunk TopRight;
    }
}