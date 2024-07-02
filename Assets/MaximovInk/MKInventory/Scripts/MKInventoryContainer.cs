using System;
using System.Linq;
using UnityEngine;

namespace MaximovInk
{
    public enum MKInventoryContainerType
    {
        None,
        Main,
        Hotbar,
        Chest
    }

    public class MKInventoryContainer : MonoBehaviour
    {
        public MKInventoryContainerType Type => _containerType;
        public MKSlot Selected => _selectedSlot;

        public event Action<MKSlot, MKSlot> OnSelect;
        public event Action<MKSlot> OnDeselect;

        private MKSlot[] _slots;

        [SerializeField] private bool _alwaysNeedBeSelected;
        [SerializeField] public MKInventoryContainerType _containerType;

        private MKSlot _selectedSlot;
        private MKSlot _previousSlot;

        private void Awake()
        {
            OnSelect += MKInventoryManager.Instance.OnSelect;
            OnDeselect += MKInventoryManager.Instance.OnDeselect;

            Initialize();
        }

        private void OnDestroy()
        {
            OnSelect -= MKInventoryManager.Instance.OnSelect;
            OnDeselect -= MKInventoryManager.Instance.OnDeselect;
        }

        public void SetType(MKInventoryContainerType type)
        {
            _containerType = type;
        }

        private bool Initialize()
        {
            _slots = GetComponentsInChildren<MKSlot>();

            if (_slots == null || _slots.Length == 0)
            {
                Destroy(gameObject);
                return false;
            }

            if (_alwaysNeedBeSelected)
            {
                Select(0);
            }

            return true;
        }

        public bool Add(MKItem item)
        {
            if (_slots == null && !Initialize()) return false;

            if (_slots == null || _slots.Length == 0) return false;

            foreach (var slot in _slots)
            {
                if (!slot.PlaceItem(item)) continue;

                if(slot == _selectedSlot)
                    Select(slot);

                return true;
            }

            return false;
        }

        public bool IsSelected(MKSlot slot)
        {
            return _selectedSlot == slot;
        }

        public void Select(MKSlot slot)
        {
            _previousSlot = _selectedSlot;

            _selectedSlot = slot;

            if (_previousSlot != null)
                OnDeselect?.Invoke(_previousSlot);
            OnSelect?.Invoke(_previousSlot, _selectedSlot);


            UpdateAll();
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _slots.Length) return;

            var slot = _slots[index];

            Select(slot);
        }

        public void UpdateSelected()
        {
            if(_selectedSlot != null)
                Select(_selectedSlot);
        }

        public void UpdateAll()
        {
            foreach (var slot in _slots)
            {
                slot.UpdateUI();
            }
        }

    }
}
