using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace MaximovInk
{
    /*
      [System.Serializable]
     public struct AimPointInfo
     {
         public bool IsHit;
         public RaycastHit Hit;
         public float Distance;
     }
     */

    [RequireComponent(typeof(Rigidbody))]
    public class MKCharacter : NetworkBehaviour
    {
        public event Action<bool> OnGroundChangedEvent;

        public bool IsStrafe => (Mathf.Abs(_moveInput.x)) > Mathf.Abs(_moveInput.y);
        public bool IsCrouch => _isCrouch;
        public bool IsMoving => _rigidbody.velocity.magnitude > 0.2f;
        public bool IsGround => _isGround;

        public Animator Animator => _animator;
        public Rigidbody Rigidbody => _rigidbody;
        public CapsuleCollider Collider => _collider;

        public float GetMouseSensX() => _mouseSens * _mouseSensAxis.x;
        public float GetMouseSensY() => _mouseSens * _mouseSensAxis.y;

        [SerializeField] private float _entityHeight = 1.5f;
        [SerializeField] private float _slopeLimit = 60f;
        [SerializeField] private float _jumpHeight = 1f;
        [SerializeField] private LayerMask _groundLayerMask;
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _sprintMultiplier = 2f;
        [SerializeField] private float _crouchMultiplier = 0.7f;
        [SerializeField] private float _airMultiplier = 0.5f;
        [SerializeField] private float _airDrag = 0.1f;
        [SerializeField] private float _groundDrag = 1f;

        [SerializeField] private float _velocityBlendSpeed = 6f;

        [SerializeField] private float _mouseSens = 3f;
        [SerializeField] private Vector2 _mouseSensAxis = Vector2.one;
        [SerializeField] private float _minPitch = -89;
        [SerializeField] private float _maxPitch = 75;

        [SerializeField] private Transform _cameraRoot;
        [SerializeField] private Camera _camera;

        private float _pitch;
        private float _yaw;

        private Rigidbody _rigidbody;
        private CapsuleCollider _collider;
        private Animator _animator;

        private float _halfHeight;
        private float _playerInitialScale;
        private RigidbodyConstraints _defaultConstraints;

        private bool _isGround;
        private RaycastHit _groundHitInfo;
        private float _currentSlopeAngle;

        private Vector3 _velocity;

        private Vector2 _moveInput;
        private bool _isSprint;
        private bool _isCrouch;
        private bool _invokeJump;
        private Vector2 _lookInput;
        private bool _changeCameraInvoke;
        private float _scrollDelta;
        private bool _lookAround;
        private bool _isAiming;
        private bool _invokeFire;
        private bool _lockInput;

        private int _jumpHash;
        private int _fallingHash;
        private int _groundedHash;
        private int _xVelHash;
        private int _yVelHash;
        private int _isCrouchHash;
        private int _isAimingHash;

        private float _xVelAnim;
        private float _yVelAnim;

        public float MaxDistance = 7f;
        public float MinDistance = 1f;

        private bool _isFirstPerson = true;
        protected float _distance = 5f;
        private Transform _cameraTarget;
        [SerializeField] private Vector3 _targetOffset;
        [SerializeField] private Vector3 _lookAroundTargetOffset;
        private float _yawOffset;

        [SerializeField]
        private Transform _weaponParent;
        private Gun _currentWeapon;

        [SerializeField] private LayerMask _aimMask;
        [SerializeField] private Transform _worldPoint;
        [SerializeField] private float _maxRayDistance = 50f;
        [SerializeField] private bool _alwaysAim;
       // [SerializeField] private AimPointInfo _currentAimPoint;

        public NetworkVariable<Vector3> CurrentAimHitPoint = new(writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<Vector3> CurrentAimHitNormal = new(writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> CurrentAimIsHit = new(writePerm: NetworkVariableWritePermission.Owner);

        public NetworkVariable<int> WeaponIndex = new(writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);

        [SerializeField] private float _aimRigWeight = 0f;
        private Vector3 _aimTargetPosition;
        [SerializeField] private TwoBoneIKConstraint _secondHandConstraint;
        [SerializeField] private Transform _secondHandIK;

        [SerializeField] private Rig _aimRig;
        [SerializeField] private Transform _headIk;


        [SerializeField]
        private Gun _testGun;

        private RaycastHit[] results = new RaycastHit[20];

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<CapsuleCollider>();
            _animator = GetComponentInChildren<Animator>();
            _playerInitialScale = _collider.height;
            _defaultConstraints = _rigidbody.constraints;

            _xVelHash = Animator.StringToHash("X_Velocity");
            _yVelHash = Animator.StringToHash("Y_Velocity");
            _jumpHash = Animator.StringToHash("Jump");
            _fallingHash = Animator.StringToHash("Falling");
            _groundedHash = Animator.StringToHash("Grounded");
            _isCrouchHash = Animator.StringToHash("IsCrouch");
            _isAimingHash = Animator.StringToHash("IsAiming");

            OnGroundChangedEvent += MKCharacter_OnGroundChangedEvent;

            _cameraTarget = transform;
        }

        private void MKCharacter_OnGroundChangedEvent(bool obj)
        {
            _animator.SetBool(_groundedHash, obj);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            GetComponent<AudioListener>().enabled = IsOwner;

            _camera.gameObject.SetActive(IsOwner);

            WeaponIndex.OnValueChanged += OnWeaponChanged;

            SetCurrentWeapon(WeaponIndex.Value);

            if (!IsOwner) return;

            Cursor.lockState = CursorLockMode.Locked;

            _secondHandConstraint.weight = 0f;
            _aimRigWeight = 0f;
        }

        public override void OnNetworkDespawn()
        {
            WeaponIndex.OnValueChanged -= OnWeaponChanged;
            base.OnNetworkDespawn();
        }

        private void OnWeaponChanged(int prev, int curr)
        {
            SetCurrentWeapon(curr);
        }

        private void CheckGround()
        {
            _halfHeight = _entityHeight / 2f;

            var prevGround = _isGround;
            _isGround = Physics.Raycast(transform.position, Vector3.down, out _groundHitInfo, _halfHeight, _groundLayerMask);

            if (_isGround)
                _isGround = (Vector3.Angle(Vector3.up, _groundHitInfo.normal) <= _slopeLimit);

            if (prevGround != _isGround)
                OnGroundChangedEvent?.Invoke(_isGround);
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.G))
                _lockInput = !_lockInput;

            if (_lockInput)
            {
                _moveInput = default;
                _lookInput = default;
                _isCrouch = default;
                _isSprint = default;
                _invokeJump = default;
                _changeCameraInvoke = default;
                _scrollDelta = default;
                _lookAround = default;
                _invokeFire = default;
                _isAiming = default;

                return;
            }

            _moveInput = new Vector2(
                Input.GetAxis("Horizontal"), 
                Input.GetAxis("Vertical"));
            _lookInput = new Vector2(
                Input.GetAxis("Mouse X") * GetMouseSensX(),
                Input.GetAxis("Mouse Y") * GetMouseSensY());
             _isCrouch = Input.GetKey(KeyCode.LeftControl);
            _isSprint = Input.GetKey(KeyCode.LeftShift);
            _invokeJump = Input.GetKeyDown(KeyCode.Space);
            _changeCameraInvoke = Input.GetKeyDown(KeyCode.C);
            _scrollDelta = -Input.mouseScrollDelta.y;
            _lookAround = Input.GetKey(KeyCode.LeftAlt) && !_isFirstPerson;
            _invokeFire = Input.GetMouseButton(0);

            _isAiming = Input.GetMouseButton(1);

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                WeaponIndex.Value = 1;

                //SetCurrentWeaponServerRpc(1);
                // SetCurrentWeapon(_testGun);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                WeaponIndex.Value = 0;

                //SetCurrentWeaponServerRpc(2);
                //SetCurrentWeapon(null);
            }
        }

        [ServerRpc]
        private void SetCurrentWeaponServerRpc(int index)
        {
           
            SetCurrentWeaponClientRpc(index);
        }

        [ClientRpc]
        private void SetCurrentWeaponClientRpc(int index)
        {
            

        }

        private void SetCurrentWeapon(int index)
        {
            var gun = index == 1 ? _testGun : null;

            if (_currentWeapon != null)
                Destroy(_currentWeapon.gameObject);

            if (gun != null)
            {
                _currentWeapon = Instantiate(gun, transform);
                _currentWeapon.Owner = this;
            }

            _secondHandConstraint.weight = 0f;

            if (_currentWeapon != null)
            {
                _secondHandConstraint.weight = _currentWeapon.SecondArmTarget != null ? 1f : 0f;

            }

            _aimRigWeight = gun != null ? 1f : 0f;
        }


        private Vector3 GetSlopeMoveDir(Vector3 moveDirection)
        {
            return Vector3.ProjectOnPlane(moveDirection, _groundHitInfo.normal).normalized;
        }
        private bool IsOnSlope(float angle)
        {
            return angle < _slopeLimit && _currentSlopeAngle != 0;
        }

        private float GetMoveForce()
        {
            var force = _speed;

            if (!_isGround)
                force *= _airMultiplier;
            if (_isSprint)
                force *= _sprintMultiplier;
            if (_isCrouch)
                force *= _crouchMultiplier;

            return force;
        }

        private Vector3 GetMoveDirection()
        {
            var input = _moveInput;

            var moveDirection = transform.forward * input.y + transform.right * input.x;

            // therefore
            _currentSlopeAngle = Vector3.Angle(Vector3.up, _groundHitInfo.normal);

            if (IsOnSlope(_currentSlopeAngle))
            {
                var slopedMove = GetSlopeMoveDir(moveDirection);

                return slopedMove;
            }
            else
            {
                return moveDirection;
            }
        }

        private void ApplyMove()
        {
            _velocity = Vector3.zero;

            var input = _moveInput;

            var moveDirection = GetMoveDirection();

            var force = GetMoveForce();

            if (moveDirection.magnitude>0.1f)
            {
                var rbVelocity = _rigidbody.velocity;
                _velocity.y = rbVelocity.y;

                _velocity = moveDirection * force;

                var xVelDiff = _velocity.x - rbVelocity.x;
                var zVelDiff = _velocity.z - rbVelocity.z;

                _rigidbody.AddForce(new Vector3(xVelDiff, 0, zVelDiff), ForceMode.VelocityChange);
            }

            _xVelAnim = Mathf.Lerp(_xVelAnim, input.x * force, Time.fixedDeltaTime * _velocityBlendSpeed);
            _yVelAnim = Mathf.Lerp(_yVelAnim, input.y * force, Time.fixedDeltaTime * _velocityBlendSpeed);

            _animator.SetFloat(_xVelHash, _xVelAnim);
            _animator.SetFloat(_yVelHash, _yVelAnim);
            _animator.speed = Mathf.Clamp(force / 6f, 1f, 3f);
            _animator.SetBool(_isCrouchHash, _isCrouch);
        }

        private void Jump()
        {
            _isGround = false;
            OnGroundChangedEvent?.Invoke(_isGround);

            var velocity = _rigidbody.velocity;
            var jumpForce = Mathf.Sqrt(-2.0f * Physics.gravity.y * _jumpHeight);
            velocity.y = jumpForce;

            velocity = new Vector3(velocity.x, jumpForce, velocity.z);
            _rigidbody.velocity = velocity;

            _animator.SetTrigger(_jumpHash);
        }

        private void CalculateRotation()
        {

            _yaw += _lookInput.x;
            if (_lookAround)
                _yawOffset += _lookInput.x;

            _pitch -= _lookInput.y;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        private void CalculateAimPoint()
        {
            /*
             if (!IsOwner)
            {
                _aimTargetPosition = _worldPoint.position;
            }
            else 
             */

            if (!_lookAround)
            {
                var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                var ray = _camera.ScreenPointToRay(screenCenter);

                 var hits = Physics.RaycastNonAlloc(ray, results, 999f, _aimMask, QueryTriggerInteraction.Ignore);
                 var foundPoint = false;
                 var closestIndex = 0;
                float closestDist = Mathf.Infinity;

                //_currentAimPoint = default;

                for (int i = 0; i < hits; i++)
                {
                    if (results[i].collider.transform.root == transform)
                    {
                        continue;
                    }

                    if (foundPoint)
                    {
                        if (results[i].distance < closestDist)
                        {
                            closestIndex = i;
                            closestDist = results[i].distance;
                        }

                    }
                    else
                    {
                        closestDist = results[i].distance;

                    }
                    foundPoint = true;

                }

                if (!foundPoint)
                {
                    _aimTargetPosition = _camera.transform.position + ray.direction * _maxRayDistance;
                    if (IsOwner)
                    {
                        CurrentAimIsHit.Value = false;
                    }

                }
                else
                {
                    _aimTargetPosition = results[closestIndex].point;

                    /*
                     _currentAimPoint = new AimPointInfo()
                    {
                        Hit = results[closestIndex],
                        IsHit = true,
                        Distance = closestDist,
                    };
                     */

                    if (IsOwner)
                    {
                        CurrentAimHitPoint.Value = results[closestIndex].point;
                        CurrentAimHitNormal.Value = results[closestIndex].normal;
                        CurrentAimIsHit.Value = true;

                    }
                  
                    
                }

                _worldPoint.position = _aimTargetPosition;
            }
        }

        private void UpdateAnimation()
        {
            CalculateAimPoint();
            AimLogic();
        }

        private void Update()
        {
            UpdateAnimation();

            if (!IsOwner) return;

            HandleInput();

            if (_invokeJump && _isGround) Jump();

            CalculateRotation();

            if (_changeCameraInvoke)
                ChangeCamera();

            if (_invokeFire)
            {
                FireServerRpc();
            }
        }

        [ServerRpc]
        private void FireServerRpc()
        {
            FireClientRpc();
        }

        [ClientRpc]
        private void FireClientRpc()
        {

            if(_currentWeapon != null)
                _currentWeapon.Fire();
        }

        private void AimLogic()
        {
            _aimRig.weight = Mathf.Lerp(_aimRig.weight, _aimRigWeight, Time.deltaTime * 15f);
            _animator.SetLayerWeight(1, _aimRig.weight);

            if (_currentWeapon != null && _currentWeapon.SecondArmTarget != null)
            {
                var target = _currentWeapon.SecondArmTarget;

                _secondHandIK.SetPositionAndRotation(target.position, target.rotation);
            }

            _headIk.transform.position = _aimTargetPosition;

            if (!IsOwner) return;

            _animator.SetBool(_isAimingHash, _isAiming);
        }

        private void ChangeCamera()
        {
            _isFirstPerson = !_isFirstPerson;

            _camera.transform.SetParent(_isFirstPerson ? transform : null);
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            CheckGround();

            _rigidbody.drag = _isGround ? _groundDrag : _airDrag;

            if (!_lookAround && Mathf.Abs(_yawOffset) <= 0.07f)
            {
                var lastRot = transform.eulerAngles;
                lastRot.y = Mathf.LerpAngle(lastRot.y, _camera.transform.eulerAngles.y, Time.deltaTime * 20f);
                transform.eulerAngles = lastRot;
            }

            ApplyMove();

        }

        private void LateUpdate()
        {
            if (_isFirstPerson)
            {
                _camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0);

                _camera.transform.position = _cameraRoot.position;
            }
            else
            {
                if (_scrollDelta != 0f)
                    _distance += _scrollDelta;

                if (!_lookAround && Mathf.Abs(_yawOffset) > 0.05f)
                {
                    var yawLerpVal = Mathf.Lerp(_yawOffset, 0, Time.deltaTime * 15f);

                    _yaw -= _yawOffset - yawLerpVal;
                    _yawOffset = yawLerpVal;

                   // Debug.Log("reset");
                }

                _distance = Mathf.Clamp(_distance, MinDistance, MaxDistance);

                var rotation = Quaternion.Euler(_pitch, _yaw, 0);

                var offset = Vector3.zero;

                //var offset = _lookAround ? _lookAroundTargetOffset : _targetOffset;

                if (_lookAround)
                {
                    offset = _lookAroundTargetOffset;
                }
                else
                {
                    offset = _targetOffset;
                    offset = _cameraTarget.forward * offset.z + 
                             _cameraTarget.right * offset.x + 
                             _cameraTarget.up * offset.y;
                }


                var targetPos = _cameraTarget.position + offset;
                var direction = (_camera.transform.position - targetPos).normalized;
                var ray = new Ray(targetPos, direction);
                _camera.transform.position = targetPos + rotation * new Vector3(0, 0, -_distance);
                _camera.transform.LookAt(targetPos);
            }

            if (_currentWeapon != null)
            {
                _currentWeapon.transform.SetPositionAndRotation(_weaponParent.position, _weaponParent.rotation);
            }

           
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

        private void OnApplicationFocus(bool hasFocus)
        {
            if (IsOwner && hasFocus)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

    }
}