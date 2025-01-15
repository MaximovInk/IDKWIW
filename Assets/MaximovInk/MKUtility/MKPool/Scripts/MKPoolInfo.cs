using System.Linq;
using UnityEngine;

namespace MaximovInk
{
    [System.Serializable]
    public struct ObjectInfo
    {
        public string ID;
        public GameObject Prefab;
        public int Capacity;

        public bool AutoCapacity;
    }

    [CreateAssetMenu(fileName = "MKPool", menuName = "MaximovInk/MKPool")]
    public class MKPoolInfo : ScriptableObject
    {
        public ObjectInfo[] Get() => _prefabs;

        [SerializeField] private ObjectInfo[] _prefabs;

        public ObjectInfo Info(string ID)
        {
            return _prefabs.FirstOrDefault(n => n.ID == ID);
        }

    }
}