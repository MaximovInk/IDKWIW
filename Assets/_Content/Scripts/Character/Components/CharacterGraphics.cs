using System;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterGraphics : MonoBehaviour, ICharacterComponent
    {
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

        private CharacterComponents _components;

        private Vector3 _initPos;

        public void Initialize(CharacterComponents componentsRef)
        {
            _components = componentsRef;

            _initPos = transform.localPosition;

            if (componentsRef.Controller.IsOwner)
            {
                HideHead(componentsRef.CameraController.IsFirstPerson);

                componentsRef.CameraController.OnModeChanged += HideHead;
            }

            _components.Controller.OnCrouch += Controller_OnCrouch;
            _components.CameraController.OnModeChanged += CameraController_OnModeChanged;
        }

        private void CameraController_OnModeChanged(bool obj)
        {
            Controller_OnCrouch(_components.Controller.IsCrouch);
        }

        private void Controller_OnCrouch(bool obj)
        {
            if (!_components.CameraController.IsFirstPerson)
            {
                transform.localPosition = _initPos;

                return;
            }

            transform.localPosition = obj ? InitPos + new Vector3(0, -0.35f, 0) : InitPos;
        }



        private void Update()
        {
            IsLookingToMove = false;


            var graphicsTransform = transform;

            if (!_components.IsInitialized) return;

            if (!_components.Controller.IsGround || _components.CameraController.IsFirstPerson)
            {
                graphicsTransform.localRotation = Quaternion.identity;

                return;
            }

            if (_components.Aim.IsAiming && !_components.Controller.IsStrafe)
            {
                graphicsTransform.localRotation = Quaternion.Lerp(graphicsTransform.localRotation, Quaternion.identity, Time.deltaTime * 10f);

                return;
            }

            if (!_components.Controller.IsMoving)
            {
                graphicsTransform.localRotation = Quaternion.Lerp(graphicsTransform.localRotation, Quaternion.identity, Time.deltaTime * 10f);

                return;
            }

            var oldRotation = graphicsTransform.rotation;

            graphicsTransform.forward = _components.Controller.Rigidbody.velocity.normalized;

            var newRotation = graphicsTransform.rotation;

            newRotation.x = oldRotation.x;
            newRotation.z = oldRotation.z;

            graphicsTransform.rotation = newRotation;

            IsLookingToMove = true;
        }
    }
}
