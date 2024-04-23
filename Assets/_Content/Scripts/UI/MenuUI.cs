using UnityEngine;
using UnityEngine.UI;

namespace MaximovInk.IDKWIW
{
    public class MenuUI : MonoBehaviour
    {
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _clientButton;

        [SerializeField] private GameObject _lobbyObject;

        private void Awake()
        {
            _hostButton.onClick.AddListener(() => { 
                GameManager.Instance.NetworkManager.StartHost();
                _lobbyObject.gameObject.SetActive(false);
            });
            _clientButton.onClick.AddListener(() => {
                GameManager.Instance.NetworkManager.StartClient();
                _lobbyObject.gameObject.SetActive(false); 
            });
        }

        

    }
}
