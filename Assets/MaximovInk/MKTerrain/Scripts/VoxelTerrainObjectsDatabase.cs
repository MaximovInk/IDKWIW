using System;
using System.Linq;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public struct VoxelObjectData
    {
        public string CustomID;

        public bool UsePool;
        public GameObject Prefab;

        public Vector3 MinScale;
        public Vector3 MaxScale;

        public float YRotationRange;
        public float XRotationRange;
        public float ZRotationRange;

        public bool IsValid => Prefab != null || UsePool;

    }

    [CreateAssetMenu(menuName ="MaximovInk/VoxelObjectsDB", fileName ="VoxelObjectsDB")]
    public class VoxelTerrainObjectsDatabase : ScriptableObject
    {
        public VoxelObjectData[] Data;

        //public GameObject[] Prefabs;

        

        public VoxelObjectData Get(string ID)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                var data = Data[i];

                if (string.IsNullOrEmpty(Data[i].CustomID) && !data.UsePool)
                {
                    data.CustomID = Data[i].Prefab.name;
                    Data[i] = data;
                }

                if (string.Equals(data.CustomID, ID, StringComparison.InvariantCultureIgnoreCase))
                {
                    return data;
                }

            }

            return default;
        }
    }
}
