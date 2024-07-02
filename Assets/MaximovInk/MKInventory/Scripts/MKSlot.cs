using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MaximovInk
{
    public class MKSlot : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerDownHandler, IPointerClickHandler, IPointerUpHandler
    {
        public MKInventoryContainer Container => _container;

        private static Vector2 dragOffset;
        private static Vector2 iconInitPosition;
        private static bool isCurrentDrag;

        public MKItem Item => _item;

        public bool IsSelected => _container != null && _container.IsSelected(this);

        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private Slider _durabilitySlider;
        [SerializeField] private Image _itemIcon;
        [SerializeField] private GameObject _selectObject;

        private MKInventoryContainer _container;

        private MKItem _item;

        private void Awake()
        {
            _container = GetComponentInParent<MKInventoryContainer>();

            UpdateUI();
        }

        private void Update()
        {
            if (isCurrentDrag)
            {
                dragOffset = Vector2.Lerp(dragOffset, Vector2.zero, Time.deltaTime);
            }

        }

        public void UpdateUI()
        {
            var hasItem = _item != null;

            _countText.gameObject.SetActive(hasItem);
            _durabilitySlider.gameObject.SetActive(hasItem);
            _itemIcon.gameObject.SetActive(hasItem);
            _selectObject.gameObject.SetActive(IsSelected);

            if (!hasItem) return;

            _countText.text = _item.Count.ToString();

            var data = _item.Data;

            if(!data.IsDurable || Math.Abs(data.MaximumDurability - _item.Durability) < 0.05f)
                _durabilitySlider.gameObject.SetActive(false);

            _durabilitySlider.value = _item.Durability / data.MaximumDurability;

            _itemIcon.sprite = data.Icon;

        }

        public bool PlaceItem(MKItem other)
        {
            if (_item == null)
            {
                _item = other;

                UpdateUI();

                if (IsSelected)
                    Container.Select(this);

                return true;
            }

            if (!other.CanStack(_item)) return false;

            _item = other.Stack(_item);

            if (IsSelected)
                Container.Select(this);

            return true;
        }



        public MKItem Swap(MKItem from)
        {
            var temp = _item;
            _item = from;

            UpdateUI();

            if(IsSelected)
                Container.Select(this);

            return temp;
        }

        public void Select()
        {
            if (_container != null)
                _container.Select(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Select();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
           
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            
        }

        public void OnBeginDrag(PointerEventData eventData)
        {

            isCurrentDrag = _item != null;

            if (!isCurrentDrag) return;

            iconInitPosition = _itemIcon.rectTransform.position;

            dragOffset = iconInitPosition - eventData.position;

            _itemIcon.transform.SetParent(_itemIcon.rectTransform.root);

        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isCurrentDrag) return;

            if (eventData.pointerCurrentRaycast.isValid)
            {
                if (_item != null)
                {
                    var endSlot = eventData.pointerCurrentRaycast.gameObject.GetComponent<MKSlot>();

                    if (endSlot != null)
                    {
                        _item = endSlot.PlaceItem(_item) ? null : endSlot.Swap(_item);

                        if (IsSelected)
                        {
                            _container.Select(endSlot);
                        }
                    }

                   
                }
            }

            _itemIcon.transform.SetParent(transform);
            _itemIcon.rectTransform.position = iconInitPosition;

            UpdateUI();

            isCurrentDrag = false;

           
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isCurrentDrag) return;

            _itemIcon.rectTransform.position = dragOffset + eventData.position;

        }

    }
}
