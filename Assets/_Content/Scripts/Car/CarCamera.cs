
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CarCamera : BaseCameraController
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            GetComponentInParent<CarController>().OnInput += CarCamera_OnInput;
            Debug.Log(GetComponentInParent<CarController>() == null);
        }

        private void CarCamera_OnInput(CharacterInput currentInput)
        {
          

            HandleInput(new CameraLookInput()
            {
                InvokeChangeCamera = currentInput.InvokeChangeCamera,
                LookAround = currentInput.LookAround,
                LookValue = currentInput.LookValue,
                ScrollDelta = currentInput.ScrollDelta
            });
        }


        protected override bool RotateFirstPersonY()
        {
            return true;
        }
    }


}