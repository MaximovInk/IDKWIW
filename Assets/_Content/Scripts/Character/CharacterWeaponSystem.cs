using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public enum HandleType
    {
        None,
        Default,
        Pistol,
        Rifle,
    }

    public class CharacterWeaponSystem : NetworkBehaviour
    {
        public bool IsAnyHandling => _isWeaponHandleDebug;
        public HandleType HandleType => _handleType;

        private bool _isWeaponHandleDebug = true;

        private HandleType _handleType;


        [SerializeField] private WeaponInstance _currentWeapon;

        private CharacterController _controller;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _controller = GetComponent<CharacterController>();

            _handleType = HandleType.Rifle;

            _isWeaponHandleDebug = true;

            _currentWeapon.Init(_controller);
        }


    }
}
