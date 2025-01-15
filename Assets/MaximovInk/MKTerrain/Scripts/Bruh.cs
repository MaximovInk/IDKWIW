using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace MaximovInk.VoxelEngine
{
    public class Bruh : MonoBehaviour
    {
        public VoxelTerrain Terrain;

        public float Height;

        public Color color;

        private void Awake()
        {
            Terrain.OnChunkLoaded += Terrain_OnChunkLoaded;
        }

        private static int3 ChunkSize => VoxelTerrain.ChunkSize;
        private void Terrain_OnChunkLoaded(VoxelChunk chunk)
        {
            var chunkPos = chunk.Position;

            var gridOrigin = ChunkSize * chunkPos;

            var height = Height;

            for (int ix = 0; ix < ChunkSize.x; ix++)
            {
                for (int iz = 0; iz < ChunkSize.z; iz++)
                {
                    height -= gridOrigin.y;

                    if (height <= 0) continue;

                    for (int iy = 0; iy < ChunkSize.y && iy < height; iy++)
                    {
                        var pos = new int3(ix, iy, iz);
                        chunk.SetBlock((ushort)(0 + 1), pos);
                        chunk.SetColor(color, pos);

                        var value = Mathf.Clamp01((height - iy) / (ChunkSize.y));

                        chunk.SetValue(pos, (byte)(value * 255f));


                    }
                }
            }
        }
    }
}
