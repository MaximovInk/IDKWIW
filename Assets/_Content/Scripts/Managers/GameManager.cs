using System;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
        public event Action<Camera> OnCameraChanged;

        public NetworkManager NetworkManager
        {
            get
            {
                if(_networkManager == null)
                    _networkManager = GetComponent<NetworkManager>();

                return _networkManager;
            }
        }

        private NetworkManager _networkManager;


        public WeaponSystem WeaponSystem => _weaponSystem;

        [SerializeField]
        private WeaponSystem _weaponSystem;

        public Camera CurrentCamera => _currentCamera;

        [SerializeField] private Camera _currentCamera;

        public void SetCurrentCamera(Camera camera)
        {
            _currentCamera = camera;
            OnCameraChanged?.Invoke(camera);
        }



    }
}
