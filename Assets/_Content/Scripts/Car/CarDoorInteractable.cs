using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CarDoorInteractable : NetworkBehaviour, IInteractable
    {
        private CarController _controller;

        private void Awake()
        {
            _controller = GetComponentInParent<CarController>();
        }

        public void Interact(CharacterController source)
        {
            var input = source.GetComponent<LocalPlayerInput>();

            if (input != null)
            {
                _controller.SetPlayerTo(source, 0);
            }
        }
    }
}
