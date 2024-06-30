using Unity.Netcode;

namespace MaximovInk
{
    public class MKNetworkManager : MonoBehaviourSingleton<MKNetworkManager>
    {
        public NetworkManager NetworkManager => _networkManager;

        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();

        }

        public void StartHost() => _networkManager.StartHost();

        public void StartClient() => _networkManager.StartClient();
    }
}
