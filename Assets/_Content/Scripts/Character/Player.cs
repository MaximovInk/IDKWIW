
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class Player : CharacterController
    {
        [SerializeField] private Vector2 _mouseSens = Vector2.one;

        private void Awake()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void HandleInput()
        {
            var input = new CharacterInput();

            input.MoveValue = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            if (Input.GetKeyDown(KeyCode.F))
            {
                Cursor.visible = !Cursor.visible;

                Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
            }

            if (input.MoveValue.magnitude > 1f)
                input.MoveValue.Normalize();

            if (Input.GetKeyDown(KeyCode.Space) && IsGround)
            {
                input.IsInvokeJump = true;
            }

            input.IsCrouch = Input.GetKey(KeyCode.LeftControl);
            input.IsSprint = Input.GetKey(KeyCode.LeftShift);

            if (Cursor.lockState == CursorLockMode.None)
                input.LookValue = Vector2.zero;
            else
                input.LookValue = new Vector2(Input.GetAxis("Mouse X") * _mouseSens.x * Time.deltaTime * 100f, -Input.GetAxis("Mouse Y") * _mouseSens.y * Time.deltaTime * 100f);

            input.LookAround = Input.GetKey(KeyCode.LeftAlt);

            input.InvokeChangeCamera = Input.GetKeyDown(KeyCode.C);

            input.ScrollDelta -= Input.mouseScrollDelta.y * Time.deltaTime * 70f;

            input.IsAiming = Input.GetMouseButton(1);
            input.IsFire = Input.GetMouseButton(0);

            if (input.IsAiming)
                input.IsSprint = false;

            if (input.IsCrouch)
                input.IsSprint = false;

            ProcessInput(input);
        }

        protected override void Update()
        {
            if (!IsOwner) return;

            HandleInput();

            base.Update();

        }
    }
}
