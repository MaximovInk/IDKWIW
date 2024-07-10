
using UnityEngine;

namespace MaximovInk
{
    public class InstanceTestUtility : MonoBehaviour
    {
        public Transform Prefab;

        public Vector2 Spacing = Vector2.one;

        public int SpawnX = 100;
        public int SpawnY = 100;

        private void Awake()
        {
            Spawn();
        }

        public void Spawn()
        {
            MKUtils.DestroyAllChildren(transform);

            var maxX = SpawnX / 2;
            var minX = -maxX;

            var maxY = SpawnY / 2;
            var minY = -maxY;

            for (int i = minX; i < maxX; i++)
            {
                for (int j = minY; j < maxY; j++)
                {
                    var offset = new Vector3(Spacing.x * i, 0, Spacing.y * j);

                    var instance = Instantiate(Prefab, transform);
                    instance.localPosition = offset;
                }
            }

        }

    }
}
