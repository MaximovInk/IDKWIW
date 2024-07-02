using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

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
        public Camera Camera => _camera;

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
        private bool _isInteract;

        private int _jumpHash;
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
        private Weapon _currentWeapon;

        [SerializeField] private LayerMask _aimMask;
        [SerializeField] private Transform _worldPoint;
        [SerializeField] private float _maxRayDistance = 50f;

        public NetworkVariable<Vector3> CurrentAimHitPoint = new(writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<Vector3> CurrentAimHitNormal = new(writePerm: NetworkVariableWritePermission.Owner);
        public NetworkVariable<bool> CurrentAimIsHit = new(writePerm: NetworkVariableWritePermission.Owner);
        public RaycastHit CurrentAimHitResult;

        public NetworkVariable<int> WeaponIndex = new(-1, writePerm: NetworkVariableWritePermission.Owner, readPerm: NetworkVariableReadPermission.Everyone);

        [SerializeField] private float _aimRigWeight = 0f;
        private Vector3 _aimTargetPosition;
        [SerializeField] private TwoBoneIKConstraint _secondHandConstraint;
        [SerializeField] private Transform _secondHandIK;

        [SerializeField] private Rig _aimRig;
        [SerializeField] private Rig _headRig;
        [SerializeField] private Transform _headIk;

        
        [SerializeField] private Transform _kickbackTransform;
        [SerializeField] private Vector3 _kickBackInitPos;

        private readonly RaycastHit[] _results = new RaycastHit[20];

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

            WeaponIndex.Value = -1;

            Cursor.lockState = CursorLockMode.Locked;

            _secondHandConstraint.weight = 0f;
            _aimRigWeight = 0f;
            _kickBackInitPos = _kickbackTransform.localPosition;

            _camera.transform.SetParent(null);

            MKCharacterManager.Instance.Current = this;
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
            bool invokeLockWithoutReset = Input.GetKeyDown(KeyCode.H);

            if (Input.GetKeyDown(KeyCode.J) || invokeLockWithoutReset)
            {
                _lockInput = !_lockInput;

                if (!invokeLockWithoutReset)
                {
                    _isCrouch = default;
                    _isSprint = default;
                    _invokeJump = default;
                    _changeCameraInvoke = default;
                    _scrollDelta = default;
                    _lookAround = default;
                    _invokeFire = default;
                    _isAiming = default;
                }

                _moveInput = default;
                _lookInput = default;
            }

            if (_lockInput)
                return;
            

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
            _isInteract = Input.GetKeyDown(KeyCode.E);

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                //WeaponIndex.Value = 1;

                //SetCurrentWeaponServerRpc(1);
                // SetCurrentWeapon(_testGun);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
               // WeaponIndex.Value = 0;

                //SetCurrentWeaponServerRpc(2);
                //SetCurrentWeapon(null);
            }
        }

        private void SetCurrentWeapon(int index)
        {
            var gun = WeaponManager.Instance.Database.Get(index);

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
            

            if (!_lookAround)
            {
                var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                var ray = _camera.ScreenPointToRay(screenCenter);

                 var hits = Physics.RaycastNonAlloc(ray, _results, 999f, _aimMask, QueryTriggerInteraction.Ignore);
                 var foundPoint = false;
                 var closestIndex = 0;
                float closestDist = Mathf.Infinity;

                //_currentAimPoint = default;

                for (int i = 0; i < hits; i++)
                {
                    if (_results[i].collider.transform.root == transform)
                    {
                        continue;
                    }

                    if (foundPoint)
                    {
                        if (_results[i].distance < closestDist)
                        {
                            closestIndex = i;
                            closestDist = _results[i].distance;
                        }

                    }
                    else
                    {
                        closestDist = _results[i].distance;

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
                    _aimTargetPosition = _results[closestIndex].point;

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
                        CurrentAimHitResult = _results[closestIndex];
                        CurrentAimHitPoint.Value = CurrentAimHitResult.point;
                        CurrentAimHitNormal.Value = CurrentAimHitResult.normal;
                        CurrentAimIsHit.Value = true;
                    }
                  
                    
                }

                _worldPoint.position = _aimTargetPosition;
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

        public void KickBack()
        {
            if (!_currentWeapon.KickbackWeaponData.KickbackEnabled) return;

            var rot = _kickbackTransform.localRotation;
            var pos = _kickbackTransform.localPosition;
            var maxRot = _currentWeapon.KickbackWeaponData.MaxKickBackRotation;
            var maxPos = _currentWeapon.KickbackWeaponData.MaxKickBackPosition;
            
            var speed = _currentWeapon.KickbackWeaponData.KickBackSpeed;

            rot = Quaternion.Lerp(rot, Quaternion.Euler(maxRot, rot.y, rot.z), Time.deltaTime * speed);
            pos = Vector3.Lerp(pos,  _kickBackInitPos +new Vector3(0, maxPos,0 ), Time.deltaTime * speed);

            _kickbackTransform.SetLocalPositionAndRotation(pos, rot);
        }

        private void AimLogic()
        {
            _aimRig.weight = Mathf.Lerp(_aimRig.weight, _aimRigWeight, Time.deltaTime * 15f);
            _animator.SetLayerWeight(1, _aimRig.weight);

            if (_currentWeapon != null && _currentWeapon.SecondArmTarget != null)
            {
                var armTarget = _currentWeapon.SecondArmTarget;

                _secondHandIK.SetPositionAndRotation(armTarget.position, armTarget.rotation);
            }

            if (_currentWeapon != null && !_isAiming)
            {
                _headIk.transform.position = _currentWeapon.AimPoint.transform.position;

                _headRig.weight = Mathf.Lerp(_headRig.weight, 1f, Time.deltaTime * 5f);
            }
            else
            {
                _headIk.transform.position = _aimTargetPosition;

                _headRig.weight = Mathf.Lerp(_headRig.weight, 0.3f, Time.deltaTime * 5f);
            }

            if (!IsOwner) return;

            _animator.SetBool(_isAimingHash, _isAiming);
        }

        private void ChangeCamera()
        {
            _isFirstPerson = !_isFirstPerson;

            //_camera.transform.SetParent(_isFirstPerson ? transform : null);
        }

        private void ResetKickBack()
        {
            var rot = _kickbackTransform.localRotation;
            var pos = _kickbackTransform.localPosition;

            var speed = 5f;

            if(_currentWeapon != null)
                speed = _currentWeapon.KickbackWeaponData.KickBackReturnSpeed;

            rot = Quaternion.Lerp(rot, Quaternion.identity, Time.deltaTime * speed);
            pos = Vector3.Lerp(pos, _kickBackInitPos, Time.deltaTime * speed);

            _kickbackTransform.SetLocalPositionAndRotation(pos, rot);
        }

        private void Update()
        {
            CalculateAimPoint();
            AimLogic();

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

            if (_isInteract && CurrentAimIsHit.Value)
            {
                var interactObject = CurrentAimHitResult.transform.root.GetComponent<IInteractable>();

                if (interactObject != null)
                {
                    interactObject.Interact(this);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!IsOwner) return;

            CheckGround();

            _rigidbody.drag = _isGround ? _groundDrag : _airDrag;

            if (!_lookAround && Mathf.Abs(_yawOffset) <= 0.07f)
            {
                var lastRot = transform.eulerAngles;
                lastRot.y = Mathf.LerpAngle(lastRot.y, _camera.transform.eulerAngles.y, Time.fixedDeltaTime * 20f);
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

            ResetKickBack();

            
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