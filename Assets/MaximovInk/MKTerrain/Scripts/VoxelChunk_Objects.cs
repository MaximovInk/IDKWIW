using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public partial class VoxelChunk
    {
        public List<VoxelObjectInfo> Objects => _objects;
        private List<VoxelObjectInfo> _objects = new();
       
        public void DestroyAll()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i].Instance == null)
                    continue;

                Destroy(_objects[i].Instance);
            }

            _objects.Clear();
        }

        public bool AddObject(VoxelObjectInfo info)
        {
            var prefab = Terrain.Data.ObjectsDatabase.Get(info.PrefabID);

            if (prefab == null) return false;

            var instance = Instantiate(prefab, transform);
            instance.transform.SetLocalPositionAndRotation(info.Position, info.Rotation);
            instance.transform.localScale = info.Scale;

            info.Instance = instance;

            _objects.Add(info);

            return true;
        }

    }
}
