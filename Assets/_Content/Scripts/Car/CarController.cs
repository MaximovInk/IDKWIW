using System;
using System.Collections;
using System.Collections.Generic;
using MaximovInk.IDKWIW;
using Unity.Netcode;
using UnityEngine;
using static Unity.VisualScripting.Member;
using CharacterController = MaximovInk.IDKWIW.CharacterController;


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
    }

    public class CarController : NetworkBehaviour, IPlayer
    {
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

        private CarCamera _carCamera;

        private Vector2 _moveInput;
        private bool _invokeBrake;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _rigidbody.centerOfMass = _centerOfMass;

            _carCamera = GetComponentInChildren<CarCamera>();
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

        }

        public void UnsedPlayer(CharacterController controller)
        {
            if (IsOwner)
            {
                controller.UnsetVehicle();

                _carCamera.gameObject.SetActive(false);
            }
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

            var rot = _sterringWheel.localRotation;

            var angles = rot.eulerAngles;

            var newRot = Quaternion.Euler(angles.x, angles.y, steerAngle * _steeringWheelAmount);

            _sterringWheel.localRotation = Quaternion.Lerp(rot, newRot, 0.6f);
        }

        private void AnimateWheels()
        {
            foreach (var wheel in _wheels)
            {
                wheel.Collider.GetWorldPose(out var position,out var rotation);

                wheel.Graphics.transform.SetPositionAndRotation(position,rotation);

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
        }
    }


}

