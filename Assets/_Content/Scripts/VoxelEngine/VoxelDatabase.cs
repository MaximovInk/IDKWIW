using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public class VoxelData
    {
        public string Name;
        public Material Material;
        public Vector4 UV;
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
            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Default", Material = Resources.Load<Material>("Materials/Default") });
            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Type1", Material = Resources.Load<Material>("Materials/Type1") });
            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Type2", Material = Resources.Load<Material>("Materials/Type2") });
            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Sand", Material = Resources.Load<Material>("Materials/Sand") });
            RegisterBlock(new VoxelData() { UV = new Vector4(0, 0, 1, 1), Name = "Rock", Material = Resources.Load<Material>("Materials/Rock") });

        }

        static VoxelDatabase()
        {
            RegisterDefaultBlocks();
        }
    }
}
