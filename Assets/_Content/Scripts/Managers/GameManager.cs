using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class GameManager : MonoBehaviourSingleton<GameManager>
    {
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

    }
}
