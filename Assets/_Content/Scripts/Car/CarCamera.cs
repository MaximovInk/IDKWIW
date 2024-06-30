namespace MaximovInk.IDKWIW
{
    public class CarCamera : BaseCameraController
    {
        private void Awake()
        {
            GetComponentInParent<CarController>().OnInput += CarCamera_OnInput;

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