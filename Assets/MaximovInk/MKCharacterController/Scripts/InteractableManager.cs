using TMPro;
using UnityEngine;

namespace MaximovInk
{
    public class InteractableManager : MonoBehaviourSingleton<InteractableManager>
    {
        private Transform _canvas;
        private TextMeshProUGUI _infoText;

        public void SetTargetInfoTo(IInteractable interactabe)
        {
            _canvas.gameObject.SetActive(true);
            _canvas.transform.position = interactabe.GetPosition();
            _infoText.text = interactabe.GetInfoText();
        }

        public void DisableInfo()
        {
            _canvas.gameObject.SetActive(false);
        }

    }
}
