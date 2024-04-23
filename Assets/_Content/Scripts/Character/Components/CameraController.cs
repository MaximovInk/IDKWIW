using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MaximovInk.IDKWIW
{
    public class CameraController : MonoBehaviour, ICharacterComponent
    {
        public event Action<bool> OnModeChanged;

        public float MaxDistance = 7f;
        public float MinDistance = 1f;

        private const float MaxAngle = 85f;

        public Camera ActiveCamera => _camera;
        private Camera _camera;

        [SerializeField] private Vector3 _thirdPersonOffset = new(1, 0.5f, 0);

        public bool IsFirstPerson => _isFirstPerson;

        private CharacterComponents _components;

        private float _distance = 5f;

        private float _cameraYaw;
        private float _cameraPitch;

        private bool _isFirstPerson = true;

        private Vector3 _thirdPersonOffsetTarget;

        private const float ZOffset = 0.5f;

        [SerializeField] private LayerMask _wallMask;

        [SerializeField] private float _bobbingAmount = 0.05f;
        [SerializeField] private float _headBobSpeed = 14f;

        private Vector3 _initPosition;

        private float _offsetPitch;

        private void CharacterController_OnInputEvent(CharacterInput currentInput)
        {
            if (currentInput.InvokeChangeCamera)
                Toggle();

            _cameraYaw += currentInput.LookValue.y;
            _cameraYaw = Mathf.Clamp(_cameraYaw, -MaxAngle, MaxAngle);

            if(currentInput.LookAround)
                _offsetPitch += currentInput.LookValue.x;
            else if (_offsetPitch != 0)
            {
                _cameraPitch -= _offsetPitch;
                _offsetPitch = 0f;
            }


            _cameraPitch += currentInput.LookValue.x;

            if (!IsFirstPerson)
            {
                if(currentInput.ScrollDelta != 0f)
                    _distance += currentInput.ScrollDelta;

                _thirdPersonOffsetTarget = Vector3.Lerp(_thirdPersonOffsetTarget,
                    currentInput.LookAround ? Vector3.zero : _thirdPersonOffset, Time.deltaTime * 20f);
            }
        }

        private float _headBobTimer = 0f;

        private void HeadBobLogic()
        {
            bool isMoving = _components.Controller.IsMoving && _components.Controller.IsGround;

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

        public void Toggle()
        {
            _isFirstPerson = !_isFirstPerson;

            _camera.transform.localPosition = Vector3.zero;

            //_cameraPitch = 0f;
            _offsetPitch = 0f;

            if (_isFirstPerson)
            {
                transform.SetParent(_components.Controller.transform);
                transform.localRotation = Quaternion.identity;
                transform.localPosition = _initPosition;
            }
            else
            {
                transform.SetParent(null);
                //transform.forward = _components.Controller.transform.forward;
                //transform.position = _components.Controller.transform.position - _components.Controller.transform.forward;
            }

            OnModeChanged?.Invoke(_isFirstPerson);
            Controller_OnCrouch(_components.Controller.IsCrouch);
        }

        public void Initialize(CharacterComponents componentsRef)
        {
            _components = componentsRef;

            _camera = GetComponentInChildren<Camera>();

            if (!componentsRef.Controller.IsOwner)
            {
                gameObject.SetActive(false);

                return;
            }
            else
            {
                _initPosition = transform.localPosition;
            }

            _components.Controller.OnInputEvent += CharacterController_OnInputEvent;
            _components.Controller.OnCrouch += Controller_OnCrouch;

        }

        private const float CrouchOffset = 0.35f;

        private void Controller_OnCrouch(bool obj)
        {
            if (!IsFirstPerson) return;

            var offset = obj ? -CrouchOffset : 0f;

            transform.localPosition = _initPosition + new Vector3(0, offset, 0);
        }

        private void LateUpdate()
        {
            Debug.DrawRay(transform.position, transform.forward * 10f, Color.green);
            Debug.DrawRay(_components.Controller.transform.position, _components.Controller.transform.forward * 10f, Color.red);

            if (_isFirstPerson)
            {
                transform.localEulerAngles = new Vector3(_cameraYaw, 0, 0);

                HeadBobLogic();
            }
            else
            {
                _distance = Mathf.Clamp(_distance, MinDistance, MaxDistance);

                var rotation = Quaternion.Euler(_cameraYaw, _cameraPitch, 0);

                transform.rotation = rotation;

                var playerPos = _components.Controller.transform.position;
                var direction = (transform.position - playerPos).normalized;

                var ray = new Ray(playerPos, direction);

                transform.position = playerPos + rotation *
                                     (Physics.Raycast(ray, out RaycastHit hit, _distance + ZOffset, _wallMask)
                                         ? new Vector3(0, 0, -(hit.distance - ZOffset))
                                         : new Vector3(0, 0, -_distance));

                transform.LookAt(_components.Controller.transform);

                _camera.transform.localPosition = _thirdPersonOffsetTarget;

            }
        }
    }
}
