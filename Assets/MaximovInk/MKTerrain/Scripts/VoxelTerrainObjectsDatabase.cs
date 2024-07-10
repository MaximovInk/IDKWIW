using System.Linq;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [CreateAssetMenu(menuName ="MaximovInk/VoxelObjectsDB", fileName ="VoxelObjectsDB")]
    public class VoxelTerrainObjectsDatabase : ScriptableObject
    {
        public GameObject[] Prefabs;

        public GameObject Get(string ID) => Prefabs.FirstOrDefault(n => n.name == ID);
    }
}
