
using System;
using Unity.Netcode;
using UnityEngine;


public struct CameraLookInput
{
    public bool LookAround;
    public bool InvokeChangeCamera;
    public Vector2 LookValue;
    public float ScrollDelta;
}

public class BaseCameraController : NetworkBehaviour
{
    public event Action<bool> OnModeChanged;

    public float MaxDistance = 7f;
    public float MinDistance = 1f;

    protected const float MaxAngle = 85f;

    public Camera ActiveCamera => _camera;
    protected Camera _camera;

    [SerializeField] protected Vector3 _thirdPersonOffset = new(1, 0.5f, 0);

    public bool IsFirstPerson => _isFirstPerson;

    protected float _distance = 5f;

    protected float _cameraYaw;
    protected float _cameraPitch;

    protected bool _isFirstPerson = true;

    protected Vector3 _thirdPersonOffsetTarget;

    protected const float ZOffset = 0.5f;

    [SerializeField] protected LayerMask _wallMask;

    public void SetTarget(Transform newTarget)
    {
        _target = newTarget;
    }

    [SerializeField] protected Transform _target;
    [SerializeField] protected Transform _cameraObject;

    protected Vector3 _initPosition;

    protected float _offsetPitch;

    [SerializeField] protected bool _checkWalls = true;
    [SerializeField] protected bool _fovVelocity = true;

    [SerializeField] protected float _minFov;
    [SerializeField] protected float _maxFov;
    [SerializeField] protected float _fovVelocityMax;



    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        _camera = _cameraObject.GetComponentInChildren<Camera>();
        _initPosition = _cameraObject.transform.localPosition;
        _isFirstPerson = true;
    }


    protected virtual void HandleInput(CameraLookInput currentInput)
    {
        if (currentInput.InvokeChangeCamera)
            Toggle();

        _cameraYaw += currentInput.LookValue.y;
        _cameraYaw = Mathf.Clamp(_cameraYaw, -MaxAngle, MaxAngle);

        if (currentInput.LookAround)
            _offsetPitch += currentInput.LookValue.x;
        else if (_offsetPitch != 0)
        {
            _cameraPitch -= _offsetPitch;
            _offsetPitch = 0f;
        }


        _cameraPitch += currentInput.LookValue.x;

        if (!IsFirstPerson)
        {
            if (currentInput.ScrollDelta != 0f)
                _distance += currentInput.ScrollDelta;

            _thirdPersonOffsetTarget = Vector3.Lerp(_thirdPersonOffsetTarget,
                currentInput.LookAround ? Vector3.zero : _thirdPersonOffset, Time.deltaTime * 20f);
        }
    }

    public virtual void Toggle()
    {
        _isFirstPerson = !_isFirstPerson;

        _camera.transform.localPosition = Vector3.zero;

        _offsetPitch = 0f;

        if (_isFirstPerson)
        {
            _cameraObject.transform.SetParent(_target.transform);
            _cameraObject.transform.localRotation = Quaternion.identity;
            _cameraObject.transform.localPosition = _initPosition;
        }
        else
        {
            _cameraObject.transform.SetParent(null);
        }

        OnModeChanged?.Invoke(_isFirstPerson);
    }

    protected virtual void FirstPersonLogic()
    {
        if (RotateFirstPersonY())
        {
            _cameraObject.transform.localEulerAngles = new Vector3(_cameraYaw, _cameraPitch, 0);
            return;
        }
        _cameraObject.transform.localEulerAngles = new Vector3(_cameraYaw, 0, 0);
    }

    protected virtual void ThirdPersonLogic()
    {
        _distance = Mathf.Clamp(_distance, MinDistance, MaxDistance);

        var rotation = Quaternion.Euler(_cameraYaw, _cameraPitch, 0);

        _cameraObject.transform.rotation = rotation;

        var playerPos = _target.position;
        var direction = (_cameraObject.transform.position - playerPos).normalized;

        var ray = new Ray(playerPos, direction);


        if (_checkWalls)
        {
            _cameraObject.transform.position = playerPos + rotation *
                (Physics.Raycast(ray, out RaycastHit hit, _distance + ZOffset, _wallMask)
                    ? new Vector3(0, 0, -(hit.distance - ZOffset))
                    : new Vector3(0, 0, -_distance));
        }
        else
        {
            _cameraObject.transform.position = playerPos + rotation * new Vector3(0, 0, -_distance);
        }



        _cameraObject.transform.LookAt(_target.transform);

        _camera.transform.localPosition = _thirdPersonOffsetTarget;
    }
    
    private void LateUpdate()
    {
        if (_target == null) return;

        if (_isFirstPerson)
        {
          FirstPersonLogic();
        }
        else
        {
           ThirdPersonLogic();
        }

        if (_lastPos != _cameraObject.transform.position)
        {
            CalculateVelocity();
        }

        UpdateFov();

    }

    private Vector3 _lastPos;
    private float _velocity;

    private void CalculateVelocity()
    {
        var pos = _cameraObject.transform.position;

        _velocity = Mathf.Lerp(_velocity, (pos - _lastPos).magnitude / Time.deltaTime, 0.5f);

        _lastPos = pos;
    }

    private void UpdateFov()
    {
        var t = _velocity / _fovVelocityMax;

        t = Mathf.Clamp01(t);

        var newVal = Mathf.Lerp(_minFov, _maxFov, t);

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, newVal, 2f*Time.deltaTime);
    }

    protected virtual bool RotateFirstPersonY()
    {
        return false;
    }
}

