using System;
using MaximovInk.VoxelEngine;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterController : NetworkBehaviour
    {
        public Camera Camera => CameraController.ActiveCamera;

        public CharacterGraphics Graphics { get
            {
                if(_graphics == null)
                    _graphics = GetComponent<CharacterGraphics>();
                return _graphics;
            } }
        private CharacterGraphics _graphics;

        public CharacterAnimator Animator { get
            {
                if (_animator == null)
                    _animator = GetComponent<CharacterAnimator>();
                return _animator;

            } }
        private CharacterAnimator _animator;

        public CameraController CameraController
        {
            get
            {
                if (_cameraController == null)
                    _cameraController = GetComponent<CameraController>();
                return _cameraController;

            }
        }
        private CameraController _cameraController;

        public CharacterWeaponSystem WeaponSystem
        {
            get
            {
                if (_weaponSystem == null)
                    _weaponSystem = GetComponent<CharacterWeaponSystem>();
                return _weaponSystem;

            }
        }
        private CharacterWeaponSystem _weaponSystem;

        public CharacterAim Aim
        {
            get
            {
                if (_aim == null)
                    _aim = GetComponent<CharacterAim>();
                return _aim;

            }
        }
        private CharacterAim _aim;


        public event Action<bool> OnGroundChangedEvent;
        public event Action<CharacterInput> OnInputEvent;
        public event Action<bool> OnCrouch;

        public float MaxCurrentSpeed => _speed * SpeedMultiplier;
        public float SpeedMultiplier => _currentInput.IsSprint ? _sprintMultiplier : 1f;
        public bool IsStrafe => (Mathf.Abs(_currentInput.MoveValue.x)) > Mathf.Abs(_currentInput.MoveValue.y);
        public bool IsMoving => _rigidbody.velocity.magnitude > 0.2f;
        public bool IsGround => _isGround;
        public bool IsCrouch => _currentInput.IsCrouch;
        public CharacterInput CurrentInput => _currentInput;
        public Rigidbody Rigidbody => _rigidbody;

        public CapsuleCollider Collider => _collider;

        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _sprintMultiplier = 2f;
        [SerializeField] private float _crouchMultiplier = 0.7f;

        [SerializeField] private float _entityHeight = 1.5f;
        [SerializeField] private float _jumpHeight = 1f;
        [SerializeField] private float _airDrag = 0.1f;
        [SerializeField] private float _groundDrag = 1f;
        [SerializeField] private float _crouchScaleMultiplier = 0.5f;

        [SerializeField] private LayerMask _groundLayerMask;
        [SerializeField] private CapsuleCollider _collider;

        [SerializeField] private CarController _currentVehicle;

        private Vector3 _velocity;
        private float _halfHeight;
        private bool _isGround;

        private float _playerInitialScale;
        private float _crouchScale;
        private bool _crouchPrev;

        private CharacterInput _currentInput;
        private Rigidbody _rigidbody;

        private RaycastHit _groundHitInfo;

        [SerializeField] private float _slopeLimit = 60f;

        [SerializeField] private float _currentSlopeAngle;

        private RigidbodyConstraints _defaultConstraints;

        private void OnGroundChanged(bool newValue)
        {
            _rigidbody.drag = newValue ? _groundDrag : _airDrag;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _collider = GetComponent<CapsuleCollider>();
            _playerInitialScale = _collider.height;
            _rigidbody = GetComponent<Rigidbody>();
            _defaultConstraints = _rigidbody.constraints;

            _crouchScale = _playerInitialScale * _crouchScaleMultiplier;
            OnGroundChangedEvent += OnGroundChanged;

            GetComponent<AudioListener>().enabled=IsOwner;

            if (IsOwner)
            {
                var terrain = FindObjectOfType<VoxelTerrain>();

                if (terrain != null)
                    terrain.Target = transform;

                GameManager.Instance.SetCurrentCamera(Camera);
            }
        }

        public void FreezeRigidbody(bool isFreeze)
        {
            _rigidbody.constraints = isFreeze ? RigidbodyConstraints.FreezeAll : _defaultConstraints;
        }

        private void CheckGround()
        {
            _halfHeight = _entityHeight / 2f;

            var prevGround = _isGround;
            _isGround = Physics.Raycast(transform.position, Vector3.down, out _groundHitInfo, _halfHeight, _groundLayerMask);

            if(_isGround)
                _isGround = (Vector3.Angle(Vector3.up, _groundHitInfo.normal) <= _slopeLimit);

            if (prevGround != _isGround)
                OnGroundChangedEvent?.Invoke(_isGround);
        }

        private void Jump()
        {

            _isGround = false;
            OnGroundChanged(_isGround);

            var velocity = _rigidbody.velocity;
            var jumpForce = Mathf.Sqrt(-2.0f * Physics.gravity.y * _jumpHeight);
            velocity.y = jumpForce;

            velocity = new Vector3(velocity.x, jumpForce, velocity.z);
            _rigidbody.velocity = velocity;

        }

        private void ApplyLook()
        {
            if (_currentInput.LookAround && !CameraController.IsFirstPerson) return;

            _rigidbody.rotation *= Quaternion.Euler(0, _currentInput.LookValue.x, 0);
        }

        private Vector3 GetSlopeMoveDir(Vector3 moveDirection)
        {
            return Vector3.ProjectOnPlane(moveDirection, _groundHitInfo.normal).normalized;
        }

        private bool IsOnSlope(float angle)
        {
            return angle < _slopeLimit&& _currentSlopeAngle != 0;
        }

        private void ApplyMove()
        {
            _velocity = Vector3.zero;

            var input = _currentInput.MoveValue;

            if (input.x != 0 || input.y != 0)
            {
                var moveDirection = transform.forward * input.y + transform.right * input.x;

                // therefore
                _currentSlopeAngle = Vector3.Angle(Vector3.up, _groundHitInfo.normal);


                if (IsOnSlope(_currentSlopeAngle))
                {
                    var slopedMove = GetSlopeMoveDir(moveDirection);

                    _velocity += slopedMove;
                }
                else
                {
                    _velocity += moveDirection;

                }

                _velocity *= _speed;

                if (CurrentInput.IsSprint)
                    _velocity *= _sprintMultiplier;

                if (CurrentInput.IsCrouch)
                    _velocity *= _crouchMultiplier;

                _velocity.y = _rigidbody.velocity.y;

                _rigidbody.velocity = _velocity;
            }

        }

        private void Crouch()
        {
            _collider.height = IsCrouch ? _crouchScale : _playerInitialScale;

            var position = _collider.center;

            var offset = _playerInitialScale - _crouchScale;

            if (IsCrouch)
                position.y -= offset/2f;
            else
                position.y = 0;

            _collider.center = position;
        }

        public void ProcessInput(CharacterInput inputValue)
        {
            if (inputValue.IsInvokeJump && IsGround)
                Jump();

            

            _currentInput = inputValue;

            OnInputEvent?.Invoke(inputValue);
        }

        protected virtual void Update()
        {
            if (!IsOwner) return;

            if (CurrentInput.IsCrouch != _crouchPrev)
            {
                Crouch();
                OnCrouch?.Invoke(IsCrouch);
            }

            _crouchPrev = IsCrouch;

            ApplyLook();
        }

        protected virtual void FixedUpdate()
        {
            if (!IsOwner) return;

            CheckGround();

            ApplyMove();

        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying)
            {
                _halfHeight = _entityHeight / 2f;
            }

            Gizmos.color = _isGround ? Color.green : Color.red;

            Gizmos.DrawRay(transform.position, Vector3.down * _halfHeight);
        }

        protected virtual void LateUpdate()
        {
            if (!IsOwner) return;

            Animator.UpdateAnimation();
        }

        public virtual void InteractWith(IInteractable interactable)
        {
            interactable.Interact(this);
        }

        public void SetVehicle(CarController controller)
        {
            _currentVehicle = controller;
            FreezeRigidbody(true);
            _collider.enabled = false;
            CameraController.Object.gameObject.SetActive(false);

            GetComponent<LocalPlayerInput>().CurrentInputTarget = _currentVehicle;

        }

        public void UnsetVehicle()
        {
            GetComponent<LocalPlayerInput>().CurrentInputTarget = GetComponent<Player>();
            FreezeRigidbody(false);
            _collider.enabled = true;

            CameraController.Object.gameObject.SetActive(true);

            _currentVehicle = null;
        }
    }
}