using System;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk
{
    public class MKInventoryManager : MonoBehaviourSingleton<MKInventoryManager>
    {
        public MKSlot SlotPrefab => _slotPrefab;
        public MKItemDatabase Database => _database;

        [SerializeField] private MKItemDatabase _database;

        [SerializeField] private MKSlot _slotPrefab;

        [SerializeField] private MKInventoryContainer _mainContainer;
        [SerializeField] private MKInventoryContainer _hotbarContainer;

        [SerializeField] private MKInventoryNetwork _inventoryNetwork;

        private void Awake()
        {
            _inventoryNetwork = GetComponent<MKInventoryNetwork>();

            MKCharacterManager.Instance.OnInitialized += Instance_OnInitialized;
        }

        private void OnDestroy()
        {
            MKCharacterManager.Instance.OnInitialized -= Instance_OnInitialized;
        }

        private void Instance_OnInitialized(MKCharacter obj)
        {
            if(obj == null)return;


            _hotbarContainer.UpdateSelected();
        }

        public bool Collect(MKWorldItem worldItem)
        {
            if (_hotbarContainer.Add(worldItem.Contains))
                return true;
            if (_mainContainer.Add(worldItem.Contains))
                return true;

            return false;
        }

        public void OnSelect(MKSlot prev, MKSlot slot)
        {
            if (slot.Item == null) return;

            var data = slot.Item.Data;

            if (slot.Container.Type == MKInventoryContainerType.Hotbar)
            {
                if (data.Type == ItemType.Weapon)
                {
                    if (MKCharacterManager.Instance.IsValid)
                    {
                        Debug.Log($"{slot.Item.ItemID} {data.CustomDataInt}");

                        MKCharacterManager.Instance.Current.WeaponIndex.Value = data.CustomDataInt;

                        slot.Container.OnDeselect += ResetWeapon;
                    }
                }
            }
        }

        private void ResetWeapon(MKSlot slot)
        {
            if (MKCharacterManager.Instance.IsValid)
            {
                MKCharacterManager.Instance.Current.WeaponIndex.Value = -1;
            }
            slot.Container.OnDeselect -= ResetWeapon;
        }

        public void OnDeselect(MKSlot slot)
        {
        }

        public void Drop(MKSlot slot)
        {
            if (slot.Item == null) return;

            var character = MKCharacterManager.Instance.Current;

            if (character == null) return;

            var camTransform = character.Camera.transform;
            var look = camTransform.forward;
            var pos = camTransform.position + look * 0.1f;

            _inventoryNetwork.Drop(slot.Item, pos, look);

            slot.Swap(null);
        }

        
    }
}
