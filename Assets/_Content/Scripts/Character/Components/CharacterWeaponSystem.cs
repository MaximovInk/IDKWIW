﻿using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public enum HandleType
    {
        None,
        Default,
        Pistol,
        Rifle,
    }

    public class CharacterWeaponSystem : MonoBehaviour, ICharacterComponent
    {
        public bool IsAnyHandling => _isWeaponHandleDebug;
        public HandleType HandleType => _handleType;

        private bool _isWeaponHandleDebug = true;

        private HandleType _handleType;


        [SerializeField] private WeaponInstance _currentWeapon;

        private CharacterComponents _components;
        public void Initialize(CharacterComponents componentsRef)
        {
            _components = componentsRef;

            _handleType = HandleType.Rifle;

            _isWeaponHandleDebug = true;

            _currentWeapon.Init(_components.Controller);
        }

        private void Update()
        {
        }
    }
}
