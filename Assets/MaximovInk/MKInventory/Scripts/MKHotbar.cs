using System;
using UnityEngine;

namespace MaximovInk
{
    public class MKHotbar : MonoBehaviour
    {
        private KeyCode[] keyCodes = new KeyCode[]
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2, 
            KeyCode.Alpha3, 
            KeyCode.Alpha4, 
            KeyCode.Alpha5, 
            KeyCode.Alpha6, 
            KeyCode.Alpha7, 
            KeyCode.Alpha8, 
            KeyCode.Alpha9,
        };

        private MKInventoryContainer _container;

        private void Awake()
        {
            _container = GetComponent<MKInventoryContainer>();
            _container.SetType(MKInventoryContainerType.Hotbar);
            
            if(_container == null) Destroy(this);
        }

        private void Update()
        {
            if (!MKCharacterManager.Instance.IsValid) return;
            if (!MKCharacterManager.Instance.Current.IsOwner) return;

            for (int i = 0; i < keyCodes.Length; i++)
            {
                var key = keyCodes[i];

                if (Input.GetKeyDown(key))
                {
                    _container.Select(i);
                }
            }


            if (Input.GetKeyDown(KeyCode.G))
            {
                var slot = _container.Selected;

                MKInventoryManager.Instance.Drop(slot);
            }
        }
    }
}
