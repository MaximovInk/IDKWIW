using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace MaximovInk
{
    public class MKLobbyUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        private void Awake()
        {
            NetworkManager.Singleton.OnClientStarted += Singleton_OnClientStarted;

            _hostButton.onClick.AddListener(() => {
                NetworkManager.Singleton.StartHost();
               
            });
            _clientButton.onClick.AddListener(() => {
                NetworkManager.Singleton.StartClient();
            });
        }

        private void Singleton_OnClientStarted()
        {
            gameObject.SetActive(false);
        }
    }
}
