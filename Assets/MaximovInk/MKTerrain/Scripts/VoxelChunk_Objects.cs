using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public partial class VoxelChunk
    {
        public List<VoxelObjectInstanceInfo> Objects => _objects;
        private List<VoxelObjectInstanceInfo> _objects = new();
       
        public void DestroyAllObjects()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i].Instance == null)
                    continue;

                if (_objects[i].Data.UsePool)
                {
                    _objects[i].Instance.SetActive(false);
                    _objects[i].Instance.transform.SetParent(null, false);
                }
                else
                {
                    Destroy(_objects[i].Instance);
                }
                
            }

            _objects.Clear();
        }

        public bool AddObject(VoxelObjectInfo info)
        {
            var data = Terrain.Data.ObjectsDatabase.Get(info.PrefabID);

            if (!data.IsValid) return false;

            var instance = data.UsePool ? MKPool.Instance.GetByID(data.CustomID,true) : Instantiate(data.Prefab, transform);

            instance.transform.SetParent(transform);

            var scale = new Vector3(
                Random.Range(data.MinScale.x, data.MaxScale.x) * info.Scale.x, 
                Random.Range(data.MinScale.y, data.MaxScale.y) * info.Scale.y,
                    Random.Range(data.MinScale.z, data.MaxScale.z) * info.Scale.z);

            var rotation = info.Rotation;

            rotation *= Quaternion.Euler(
                Random.Range(-data.XRotationRange,data.XRotationRange),
                Random.Range(-data.YRotationRange, data.YRotationRange),
                Random.Range(-data.ZRotationRange, data.ZRotationRange));


            instance.transform.SetLocalPositionAndRotation(info.Position, rotation);

            instance.transform.localScale = scale;
           // instance.transform.localPosition = new Vector3(0, 100f,0f);


            var instanceInfo = new VoxelObjectInstanceInfo
            {
                Info = info,
                Instance = instance,
                Data = data
            };

            _objects.Add(instanceInfo);

            return true;
        }

        public Vector3 GridToLocal(Vector3Int pos)
        {
            return new Vector3(
               BlockSize.x * pos.x,
               BlockSize.y * pos.y,
               BlockSize.z * pos.z
                );
        }

        public Vector3 GridToLocal(Vector3 pos)
        {
            return new Vector3(
               BlockSize.x * pos.x,
               BlockSize.y * pos.y,
               BlockSize.z * pos.z
            );
        }
    }
}
