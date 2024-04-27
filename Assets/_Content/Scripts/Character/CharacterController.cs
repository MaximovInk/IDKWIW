using System;
using MaximovInk.VoxelEngine;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterController : NetworkBehaviour
    {
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

        public CharacterComponents Components;

        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _sprintMultiplier = 2f;
        [SerializeField] private float _crouchMultiplier = 0.7f;

        [SerializeField] private float _entityHeight = 1.5f;
        [SerializeField] private float _jumpHeight = 1f;
        [SerializeField] private float _airDrag = 0.1f;
        [SerializeField] private float _groundDrag = 1f;
        [SerializeField] private float _crouchScaleMultiplier = 0.5f;

        [SerializeField] private LayerMask _groundLayerMask;

        private Vector3 _velocity;
        private float _halfHeight;
        private bool _isGround;

        private float _playerInitialScale;
        private float _crouchScale;
        private bool _crouchPrev;

        private CharacterInput _currentInput;
        private Rigidbody _rigidbody;

        private CapsuleCollider _collider;

        private void OnGroundChanged(bool newValue)
        {
            _rigidbody.drag = newValue ? _groundDrag : _airDrag;
        }

        private void FindComponents()
        {
            Components = new CharacterComponents
            {
                Controller = this,
                Graphics = GetComponentInChildren<CharacterGraphics>(),
                Animator = GetComponentInChildren<CharacterAnimator>(),
                CameraController = GetComponentInChildren<CameraController>(),
                WeaponSystem = GetComponentInChildren<CharacterWeaponSystem>(),
                Aim = GetComponentInChildren<CharacterAim>(),
            };
        }

        private void InitializeComponents()
        {
            var componentsToInitialize = GetComponentsInChildren<ICharacterComponent>();

            foreach (var component in componentsToInitialize)
            {
                component.Initialize(Components);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _collider = GetComponent<CapsuleCollider>();

            _playerInitialScale = _collider.height;
            _rigidbody = GetComponent<Rigidbody>();

            FindComponents();

            InitializeComponents();

            _crouchScale = _playerInitialScale * _crouchScaleMultiplier;
            OnGroundChangedEvent += OnGroundChanged;

            GetComponent<AudioListener>().enabled=IsOwner;

            if (IsOwner)
            {
                FindObjectOfType<ChunksLoader>().Target = transform;

            }
        }


        private void CheckGround()
        {
            _halfHeight = _entityHeight / 2f;

            var prevGround = _isGround;
            _isGround = Physics.Raycast(transform.position, Vector3.down, _halfHeight, _groundLayerMask);
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
            if (_currentInput.LookAround && !Components.CameraController.IsFirstPerson) return;

            _rigidbody.rotation *= Quaternion.Euler(0, _currentInput.LookValue.x, 0);

           
        }

        private void ApplyMove()
        {
            _velocity = Vector3.zero;

            var input = _currentInput.MoveValue;

            if (input.x != 0 || input.y != 0)
            {

                _velocity += transform.forward * input.y;
                _velocity += transform.right * input.x;

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
            if (!IsOwner || !Components.IsInitialized) return;

           

            Components.Animator.UpdateAnimation();
        }

    }
}