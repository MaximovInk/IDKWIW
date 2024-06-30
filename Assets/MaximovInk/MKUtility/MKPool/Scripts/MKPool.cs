using System.Collections.Generic;
using UnityEngine;


namespace MaximovInk
{

    public class MKPool : MonoBehaviourSingleton<MKPool>
    {
        [SerializeField] private MKPoolInfo _info;

        private readonly Dictionary<string, GameObject[]> _instantiated = new();

        public GameObject GetByID(string ID)
        {
            var pool = _instantiated[ID];

            var first = pool[0];

            foreach (var instance in pool)
            {
                if (instance.activeSelf) continue;

                return instance;
            }

            return first;
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

                InitPool(poolParent.transform, info);
            }
        }

        private void InitPool(Transform parent, ObjectInfo info)
        {
            var pool = new GameObject[info.Capacity];

            for (int i = 0; i < info.Capacity; i++)
            {
                var instance = Instantiate(info.Prefab, parent);
                instance.SetActive(false);
                pool[i] = instance;
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