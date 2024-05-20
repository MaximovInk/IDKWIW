using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterInteract : NetworkBehaviour
    {
        public LayerMask LayerMask;
        public float Distance = 10f;

        private IInteractable _currentInteractable;

        private CharacterController _controller;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _controller = GetComponent<CharacterController>();

            _controller.OnInputEvent += Controller_OnInputEvent;
        }

        private void Controller_OnInputEvent(CharacterInput obj)
        {
            _invokeInteract |= obj.IsInvokeInteract;
        }

        private bool _invokeInteract;

        private void FixedUpdate()
        {
            _currentInteractable = null;

            var main = _controller.Camera.transform;

            var hit = Physics.Raycast(main.position, main.forward, out RaycastHit hitInfo, Distance, LayerMask);

            if (hit)
            {
                _currentInteractable = hitInfo.collider.transform.GetComponent<IInteractable>();
            }

            if (_invokeInteract)
            {
                if (_currentInteractable != null)
                {
                    _controller.InteractWith(_currentInteractable);
                }

            }

            _invokeInteract = false;

        }
    }
}