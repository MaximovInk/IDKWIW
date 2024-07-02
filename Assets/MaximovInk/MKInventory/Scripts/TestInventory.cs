using UnityEngine;

namespace MaximovInk
{
    public class TestInventory : MonoBehaviour
    {
        public MKItem[] Items;

        private void Awake()
        {
            var container = GetComponent<MKInventoryContainer>();

            foreach (var item in Items)
            {
                container.Add(item);
            }
        }
    }
}
