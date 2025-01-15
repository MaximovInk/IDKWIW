using System.Collections.Generic;
using UnityEngine;


namespace MaximovInk
{

    public class MKPool : MonoBehaviourSingleton<MKPool>
    {
        [SerializeField] private MKPoolInfo _info;

        private readonly Dictionary<string, List<GameObject>> _instantiated = new();

        private readonly Dictionary<string, Transform> _parents = new();

        private GameObject InstanceNew(ObjectInfo info, Transform parent)
        {
            var instance = Instantiate(info.Prefab, parent);
            instance.SetActive(false);

            return instance;
        }

        public GameObject GetByID(string ID, bool activate = false)
        {
            var pool = _instantiated[ID];

            var foundValue = pool[0];

            var isFound = false;

            foreach (var instance in pool)
            {
                if (instance.activeSelf) continue;

                foundValue = instance;
                isFound = true;
            }

            if (!isFound)
            {
                var poolInfo = _info.Info(ID) ;

                if (poolInfo.AutoCapacity)
                {

                    var parent = _parents[ID];
                    var list = _instantiated[ID];

                    for (int i = 0; i < 10; i++)
                    {
                        list.Add(InstanceNew(poolInfo, parent));
                    }

                   

                }
            }



            if(activate)
                foundValue.SetActive(true);

           
            return foundValue;
        }

        private void Awake()
        {
            InitPools();
        }

        private void InitPools()
        {
            _instantiated.Clear();

            var prefabs = _info.Get();

            foreach (var info in prefabs)
            {
                var poolParent = new GameObject(info.ID);
                poolParent.transform.SetParent(transform);

                _parents[info.ID] = poolParent.transform;

                InitPool(poolParent.transform, info);
            }
        }

        private void InitPool(Transform parent, ObjectInfo info)
        {
            var pool = new List<GameObject>(info.Capacity);

            for (int i = 0; i < info.Capacity; i++)
            {
                var instance = Instantiate(info.Prefab, parent);
                instance.SetActive(false);
                pool.Add(instance);
            }

            _instantiated[info.ID] = pool;
        }

        public void HideAfterTime(GameObject go, float time = 1f)
        {
            this.Invoke(() =>
            {
                if (go == null) return;
                go.SetActive(false);
            }, time);

        }

    }

}