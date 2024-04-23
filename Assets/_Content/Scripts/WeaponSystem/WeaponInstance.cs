using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace MaximovInk.IDKWIW
{
    public class WeaponInstance : MonoBehaviour
    {
        private Quaternion _initRotation;

        public Vector3 _axisUp = Vector3.up;
        public Vector3 _axisRight = Vector3.forward;

        private CharacterController _controller;

        public void Init(CharacterController controller)
        {
            _controller= controller;
            _initRotation = transform.localRotation;
        }


        private void Update()
        {
            if (_controller == null) return;

            //var move = _controller.Rigidbody.velocity / _controller.MaxCurrentSpeed;

            var rot = _controller.CurrentInput.LookValue;

            var moveAmount = GameManager.Instance.WeaponSystem.WeaponMoveAmount;
            var rotateAmount = GameManager.Instance.WeaponSystem.WeaponRotateAmount;
            var swaySmooth = GameManager.Instance.WeaponSystem.SwaySmooth;

            // var value = move * moveAmount;

            // transform.localRotation = Quaternion.Euler(value.y * rotateAmount, 0, value.x * rotateAmount);

            // transform.localRotation = Quaternion.Euler(value.y * rotateAmount, ort, value.x * rotateAmount);

            Quaternion rotationX = Quaternion.AngleAxis(-rot.y * rotateAmount, _axisRight);
            Quaternion rotationY = Quaternion.AngleAxis(rot.x * rotateAmount, _axisUp);

            Quaternion targetRotation = rotationX * rotationY;

           transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, swaySmooth * Time.deltaTime);
        }
    }
}
