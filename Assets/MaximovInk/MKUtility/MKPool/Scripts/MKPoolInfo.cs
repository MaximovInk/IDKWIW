using UnityEngine;

namespace MaximovInk
{
    [System.Serializable]
    public struct ObjectInfo
    {
        public string ID;
        public GameObject Prefab;
        public int Capacity;
    }

    [CreateAssetMenu(fileName = "MKPool", menuName = "MaximovInk/MKPool")]
    public class MKPoolInfo : ScriptableObject
    {
        public ObjectInfo[] Get() => _prefabs;

        [SerializeField] private ObjectInfo[] _prefabs;

    }
}