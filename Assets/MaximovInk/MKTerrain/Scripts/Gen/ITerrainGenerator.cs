
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;


namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public struct HeightCellInfo
    {
        public Color Color;
        [FormerlySerializedAs("PreferBiome")] public int PreferBiomeIndex;
        public int PreferBiome2;
        public bool IsUnderwater;

        public float X;
        public float Y;

        public float Value;
    }



    public interface IChunkDataInterface
    {
        public int3 Position { get; }
        public int3 ChunkSize { get; }
        public int LOD { get; }
        public int PreviousLOD { get; }

        public void Set(int3 position, ushort blockID, Color color, byte density);

        public void ClearAllObjects();
        public void AddObject(VoxelObjectInfo info);

    }

    public interface ITerrainGenerator
    {
        public float Amplitude { get; }

        public VoxelTerrain Terrain { get; }

       // public HeightCellInfo GenerateHeight(float x, float y);

        public void Generate(IChunkDataInterface data);
    }

    public abstract class TerrainGeneratePass
    {
        public abstract void Generate(IChunkDataInterface data, int stage = -1);


    }

    public interface ITerrainPasses
    {
        public IMKNoise[] GetNoises { get; }

        public TerrainGeneratePass[] Passes { get; }
    }
}