using System;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterGraphics : NetworkBehaviour
    {

        [SerializeField] private GameObject _graphics;
        [SerializeField] private Transform _meshParent;
        [SerializeField] private Transform _accessoriesParent;
        [SerializeField] private Transform _bodyParent;
        [SerializeField] private Transform _customization;
        [SerializeField] private Transform _armors;

        private Transform _head;
        private Transform _neck;
        private Transform _chest;
        private Transform _arms;
        private Transform _hands;
        private Transform _legs;
        private Transform _feet;

        private Transform _face;
        private Transform _hair;
        private Transform _beards;
        private Transform _moustaches;

        private Vector3 InitPos;

        public bool IsLookingToMove { get; private set; }

        private void Init()
        {
            if (_head == null)
                InitPos = _meshParent.transform.localPosition;

            _head = _bodyParent.Find("Head");
            _neck = _bodyParent.Find("Neck");
            _chest = _bodyParent.Find("Chest");
            _arms = _bodyParent.Find("Arms");
            _hands = _bodyParent.Find("Hands");
            _legs = _bodyParent.Find("Legs");
            _feet = _bodyParent.Find("Feet");

            _face = _customization.Find("Face");
            _hair = _customization.Find("Hair");
            _beards = _customization.Find("Beards");
            _moustaches = _customization.Find("Moustaches");

           
        }

        public void HideHead(bool hide)
        {
            Init();

            hide = !hide;

            _head.gameObject.SetActive(hide);
            _neck.gameObject.SetActive(hide);
            _chest.gameObject.SetActive(hide);

            _face.gameObject.SetActive(hide);
            _hair.gameObject.SetActive(hide);
            _beards.gameObject.SetActive(hide);
            _moustaches.gameObject.SetActive(hide);

            _armors.gameObject.SetActive(hide);
            _legs.gameObject.SetActive(hide);
            _feet.gameObject.SetActive(hide);

            if (hide)
                _meshParent.transform.localPosition = InitPos;
            else
                _meshParent.transform.localPosition = InitPos + new Vector3(0, 0.1f, -0.3f);

            _accessoriesParent.gameObject.SetActive(hide);
        }

        private Vector3 _initPos;
        private CharacterController _controller;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _initPos = _graphics.transform.localPosition;
            _controller = GetComponent<CharacterController>();

            if (IsOwner)
            {
                HideHead(_controller.CameraController.IsFirstPerson);

                _controller.CameraController.OnModeChanged += HideHead;
            }

            _controller.OnCrouch += Controller_OnCrouch;
            _controller.CameraController.OnModeChanged += CameraController_OnModeChanged;
        }

        private void CameraController_OnModeChanged(bool obj)
        {
            Controller_OnCrouch(_controller.IsCrouch);
        }

        private void Controller_OnCrouch(bool obj)
        {
            if (!_controller.CameraController.IsFirstPerson)
            {
                _graphics.transform.localPosition = _initPos;

                return;
            }

            _graphics.transform.localPosition = obj ? InitPos + new Vector3(0, -0.35f, 0) : InitPos;
        }



        private void Update()
        {
            IsLookingToMove = false;

            if (!_controller.IsGround || _controller.CameraController.IsFirstPerson)
            {
                _graphics.transform.localRotation = Quaternion.identity;

                return;
            }

            if (_controller.Aim.IsAiming && !_controller.IsStrafe)
            {
                _graphics.transform.localRotation = Quaternion.Lerp(_graphics.transform.localRotation, Quaternion.identity, Time.deltaTime * 10f);

                return;
            }

            if (!_controller.IsMoving)
            {
                _graphics.transform.localRotation = Quaternion.Lerp(_graphics.transform.localRotation, Quaternion.identity, Time.deltaTime * 10f);

                return;
            }

            var oldRotation = _graphics.transform.rotation;

            _graphics.transform.forward = _controller.Rigidbody.velocity.normalized;

            var newRotation = _graphics.transform.rotation;

            newRotation.x = oldRotation.x;
            newRotation.z = oldRotation.z;

            _graphics.transform.rotation = newRotation;

            IsLookingToMove = true;
        }
    }
}
