using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class LocalPlayerInput : NetworkBehaviour
    {
        public IPlayer CurrentInputTarget
        {
            get => _currentInputTarget;
            set
            {
                SendNullInput();
                _currentInputTarget = value;
            }

        }

        private IPlayer _currentInputTarget;

        public bool LockInput;

        [SerializeField] private PlayerInputType _debugType;

        [SerializeField] private Vector2 _mouseSens = Vector2.one;

        private void SendNullInput()
        {
            if (_currentInputTarget == null) return;

            var input = new CharacterInput();

            _currentInputTarget.HandleInput(input);
        }

        private void Awake()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            CurrentInputTarget = GetComponent<IPlayer>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(!IsOwner)gameObject.SetActive(false);
        }

        private void Update()
        {
            if(!IsOwner) return;

            if (CurrentInputTarget == null)
                return;

            if(LockInput)return;

            var input = new CharacterInput
            {
                MoveValue = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"))
            };

            if (Input.GetKeyDown(KeyCode.F))
            {
                Cursor.visible = !Cursor.visible;

                Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
            }

            if (input.MoveValue.magnitude > 1f)
                input.MoveValue.Normalize();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                input.IsInvokeJump = true;
            }

            input.IsCrouch = Input.GetKey(KeyCode.LeftControl);
            input.IsSprint = Input.GetKey(KeyCode.LeftShift);

            if (Cursor.lockState == CursorLockMode.None)
                input.LookValue = Vector2.zero;
            else
                input.LookValue = new Vector2(Input.GetAxisRaw("Mouse X") * _mouseSens.x, -Input.GetAxisRaw("Mouse Y") * _mouseSens.y);

            input.LookAround = Input.GetKey(KeyCode.LeftAlt);

            input.InvokeChangeCamera = Input.GetKeyDown(KeyCode.C);

            input.ScrollDelta -= Input.mouseScrollDelta.y;

            input.IsAiming = Input.GetMouseButton(1);
            input.IsFire = Input.GetMouseButton(0);

            if (input.IsAiming)
                input.IsSprint = false;

            if (input.IsCrouch)
                input.IsSprint = false;

            input.IsInvokeInteract = Input.GetKeyDown(KeyCode.E);

            input.QuitVehicle = Input.GetKeyDown(KeyCode.G);

            input.VehicleBrake = Input.GetKey(KeyCode.Space);

            if(input.QuitVehicle)
                Debug.Log("FF");
            

            CurrentInputTarget.HandleInput(input);

            _debugType = CurrentInputTarget.GetInputType();
        }
    }
}
