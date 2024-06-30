using System;
using System.Collections.Generic;
using MaximovInk.IDKWIW;
using Unity.Netcode;
using UnityEngine;
using CharacterController 
    = MaximovInk.IDKWIW.CharacterController;

namespace MaximovInk
{
    public enum WheelAxle
    {
        Front,
        Rear
    }

    [System.Serializable]
    public struct Wheel
    {
        public Transform Graphics;
        public WheelCollider Collider;
        public WheelAxle Axle;

        public bool inverseRotDirection;

        [HideInInspector]
        public Transform _child;
        [HideInInspector]
        public Quaternion _initRotation;
    }

    public enum WheelAxis
    {
        X,
        Y,
        Z,
    }

    public class CarController : NetworkBehaviour, IPlayer
    {
        public event Action<CharacterInput> OnInput;

        [SerializeField] private float _maxAcceleration = 30.0f;
        [SerializeField] private float _brakeAcceleration = 50.0f;

        [SerializeField] private float _turnSensitivity = 1.0f;
        [SerializeField] private float _maxSteerAngle = 30.0f;

        [SerializeField] private Wheel[] _wheels;

        [SerializeField] private Vector3 _centerOfMass;

        [SerializeField] private Transform _sterringWheel;

        [SerializeField] private float _steeringWheelAmount = -2f;

        public Transform[] _playerPositions;

        private Rigidbody _rigidbody;

        [SerializeField] private CarCamera _carCamera;

        private List<CharacterController> _characterControllers = new List<CharacterController>();

        private Vector2 _moveInput;
        private bool _invokeBrake;

        [SerializeField] private WheelAxis _wheelAxis;

        private void Awake()
        {
            _carCamera = GetComponentInChildren<CarCamera>();
            if(_carCamera != null)
                _carCamera.gameObject.SetActive(false);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.centerOfMass = _centerOfMass;
            
            for (var index = 0; index < _wheels.Length; index++)
            {
                var wheel = _wheels[index];

                wheel._child = wheel.Graphics.GetChild(0);
                wheel._initRotation = wheel._child.localRotation;
               
                _wheels[index] = wheel;
            }
        }

        private void Move()
        {
            foreach (var wheel in _wheels)
            {
                wheel.Collider.motorTorque = _moveInput.y * _maxAcceleration;
            }
        }

        public void SetPlayerTo(CharacterController controller, int indexPos)
        {
            var pos = _playerPositions[indexPos];

            controller.SetVehicle(this);

            controller.transform.position = pos.transform.position;
            controller.transform.SetParent(pos);
       
            if (IsOwner)
            {
                _carCamera.gameObject.SetActive(true);
            }

            _characterControllers.Add(controller);

        }

        public void UnsedPlayer(CharacterController controller)
        {
            controller.UnsetVehicle();

            if (IsOwner)
            {
                _carCamera.gameObject.SetActive(false);
            }

            _characterControllers.Remove(controller);
        }

        private void FixedUpdate()
        {
            Move();
            Steer();
            
            UpdateBrake();
        }

        private void LateUpdate()
        {
            AnimateWheels();
        }

        private void UpdateBrake()
        {
            foreach (var wheel in _wheels)
            {
                wheel.Collider.brakeTorque = _invokeBrake ? _brakeAcceleration : 0;
            }
        }

        private void Steer()
        {

            var steerAngle = _moveInput.x * _turnSensitivity * _maxSteerAngle;

            foreach (var wheel in _wheels)
            {
                if(wheel.Axle != WheelAxle.Front)continue;

                wheel.Collider.steerAngle = Mathf.Lerp(wheel.Collider.steerAngle, steerAngle, 0.6f);
            }

            if (_sterringWheel == null) return;

            var rot = _sterringWheel.localRotation;

            var angles = rot.eulerAngles;

            var newRot = Quaternion.Euler(angles.x, angles.y, steerAngle * _steeringWheelAmount);

            _sterringWheel.localRotation = Quaternion.Lerp(rot, newRot, 0.6f);
        }

        private void RotateWheel(Transform target, Vector3 lastEuler, float rot, bool inverseDirection)
        {
            if (inverseDirection)
                rot *= -1;

            switch (_wheelAxis)
            {
                case WheelAxis.X:
                    target.localRotation = Quaternion.Euler(lastEuler.x + rot, lastEuler.y, lastEuler.z);
                    break;
                case WheelAxis.Y:
                    target.localRotation = Quaternion.Euler(lastEuler.x, lastEuler.y + rot, lastEuler.z);
                    break;
                case WheelAxis.Z:
                    target.localRotation = Quaternion.Euler(lastEuler.x, lastEuler.y, lastEuler.z + rot);
                    break;
            }

           
        }

        private void AnimateWheels()
        {
            foreach (var wheel in _wheels)
            {
                if(wheel._child == null) continue;

                wheel.Collider.GetWorldPose(out var position,out var rotation);

               // wheel._child.Rotate(wheel.Collider.rpm * 6.6f * Time.deltaTime, 0, 0, Space.Self);

               var chPrev = wheel._child.localRotation.eulerAngles;

                RotateWheel(wheel._child, chPrev, wheel.Collider.rotationSpeed * Time.deltaTime,wheel.inverseRotDirection);

                var prev = wheel.Graphics.localRotation.eulerAngles;

                wheel.Graphics.localRotation = Quaternion.Euler(prev.x, wheel.Collider.steerAngle, prev.z);
                wheel.Graphics.transform.position = position;
            }
        }

        public PlayerInputType GetInputType()
        {
            return PlayerInputType.Vehicle;
        }

        public void HandleInput(CharacterInput input)
        {
            _moveInput = input.MoveValue;

            _invokeBrake = input.VehicleBrake;

            //Debug.Log(input.LookValue);

            if (input.QuitVehicle)
            {
                for (int i = 0; i < _characterControllers.Count; i++)
                {
                    if (_characterControllers[i].IsOwner)
                    {
                        UnsedPlayer(_characterControllers[i]);
                        return;
                    }
                }


            }

            OnInput?.Invoke(input);
        }
    }


}

