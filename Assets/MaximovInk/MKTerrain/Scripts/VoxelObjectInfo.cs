
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public struct VoxelObjectInstanceInfo
    {
        public VoxelObjectInfo Info;
        public VoxelObjectData Data;

        public GameObject Instance;
    }

    public struct VoxelObjectInfo
    {
        public Vector3 Position;
        public string PrefabID;
        public Quaternion Rotation;
        public Vector3 Scale;
    }
}
