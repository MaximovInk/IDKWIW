using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MaximovInk.IDKWIW
{
    public class CameraController : BaseCameraController
    {
        [SerializeField] private float _bobbingAmount = 0.05f;
        [SerializeField] private float _headBobSpeed = 14f;
        [SerializeField] private bool _headBobLogic = true;
        private float _headBobTimer = 0f;

        private const float CrouchOffset = 0.35f;
        private CharacterController _controller;

        private void CharacterController_OnInputEvent(CharacterInput currentInput)
        {
            HandleInput(new CameraLookInput()
            {
                InvokeChangeCamera = currentInput.InvokeChangeCamera,
                LookAround = currentInput.LookAround,
                LookValue = currentInput.LookValue,
                ScrollDelta = currentInput.ScrollDelta
            });
        }

        private void HeadBobLogic()
        {
            if (!_headBobLogic) return;

            bool isMoving = _controller.IsMoving && _controller.IsGround;

            if (isMoving)
            {
                _headBobTimer += Time.deltaTime * _headBobSpeed;

                _camera.transform.localPosition = new Vector3(0, Mathf.Sin(_headBobTimer) * _bobbingAmount, 0);
            }
            else
            {
                _headBobTimer = 0f;

                _camera.transform.localPosition = new Vector3(0,
                    Mathf.Lerp(_camera.transform.localPosition.y, 0, Time.deltaTime * _headBobSpeed), 0);
            }

        }

        public override void Toggle()
        {
            base.Toggle();
            Controller_OnCrouch(_controller.IsCrouch);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner)
            {
                _cameraObject.gameObject.SetActive(false);
                return;
            }

            _controller = GetComponent<CharacterController>();
            _initPosition = _cameraObject.transform.localPosition;

            _controller.OnInputEvent += CharacterController_OnInputEvent;
            _controller.OnCrouch += Controller_OnCrouch;

            _target = _controller.transform;
        }

        private void Controller_OnCrouch(bool obj)
        {
            if (!IsFirstPerson) return;

            var offset = obj ? -CrouchOffset : 0f;

            _cameraObject.transform.localPosition = _initPosition + new Vector3(0, offset, 0);
        }

    }
}
