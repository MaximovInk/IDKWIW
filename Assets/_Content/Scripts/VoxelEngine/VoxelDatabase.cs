using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public class VoxelData
    {
        public string Name;
        public Vector4 UV;
        public Color VertexColor;
    }

    public static class VoxelDatabase
    {
        private static readonly Dictionary<ushort, VoxelData> Voxels = new();
        private static readonly Dictionary<string, ushort> VoxelsByString = new();

        public static void RegisterBlock(VoxelData voxelTile, bool replace = false)
        {
            if (Voxels.Any(n => n.Value.Name == voxelTile.Name))
            {
                if (replace)
                {
                    var oldData = Voxels.First(n => n.Value.Name == voxelTile.Name);

                    Voxels[oldData.Key] = voxelTile;
                }
                else
                {
                    Debug.LogError("Tile is already registered in database:" + voxelTile.Name);
                }

                return;
            }

            Voxels.Add((ushort)Voxels.Count, voxelTile);


            BuildCache();
        }

        public static ushort GetID(string name)
        {
            return VoxelsByString[name];
        }

        public static VoxelData GetVoxel(string name)
        {
            return Voxels.FirstOrDefault(n => n.Value.Name == name).Value;
        }

        private static void BuildCache()
        {
            VoxelsByString.Clear();

            for (var i = 0; i < Voxels.Count; i++)
            {
                var voxel = Voxels.ElementAt(i).Value;

                VoxelsByString[voxel.Name] = (ushort)(i + 1);
            }
        }

        public static List<VoxelData> GetAllVoxels()
        {
            return Voxels.Values.ToList();
        }

        public static VoxelData GetVoxel(ushort id)
        {
            id--;

            if (id > Voxels.Count) id = 0;

            return Voxels[id];
        }

        private static void RegisterDefaultBlocks()
        {
            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Sand", 
                VertexColor = new Color(147/255f,112/255f,63/255f)});

            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Dirt", 
                VertexColor = new Color(0.5f, 0.4f, 0.3f) });

            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Grass", 
                VertexColor = new Color(30/255f, 61/255f, 48/255f)});

            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Rock",
                VertexColor = Color.gray});

            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Snow",
                VertexColor = Color.white});

        }

        static VoxelDatabase()
        {
            RegisterDefaultBlocks();
        }
    }
}
