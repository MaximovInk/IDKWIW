using UnityEngine;

namespace MaximovInk
{
    public enum ItemType
    {
        None,
        Consumable,
        Ammo,
        Weapon,
        Custom
    }

    [System.Serializable]
    public struct MKItemData
    {
        public string ID;
        public Sprite Icon;

        public ItemType Type;

        public bool IsDurable;
        public float MaximumDurability;
        public bool CanDurabilityStack;

        public int MaxCount;

        public GameObject CustomModel;

        public int CustomDataInt;
        public string CustomDataString;

    }
}
