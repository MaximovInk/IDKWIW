using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace MaximovInk.IDKWIW
{
    public class CharacterAim : NetworkBehaviour
    {
        [SerializeField] private LayerMask _aimMask;

        [SerializeField] private Transform _worldPoint;

        [SerializeField] private float _maxRayDistance = 50f;

        [SerializeField] private Transform _spawnBulletPosition;

        [SerializeField] private Transform _headIk;

        [SerializeField] private bool _alwaysAim;

        [SerializeField] private Rig _aimRig;

        private Vector3 _targetPosition;

        public bool IsAiming => _isAiming.Value;

        private readonly NetworkVariable<bool> _isAiming = new(writePerm: NetworkVariableWritePermission.Owner);

        private float _timer = 0f;

        private readonly float _fireDelay = 0.1f;

        private CharacterController _controller;

        private void Update()
        {
            _timer += Time.deltaTime;

            if (!_controller.IsOwner) {
                _targetPosition = _worldPoint.position;
            }
            else
            {
                var cam = _controller.Camera;

                if (cam == null) return;

                var screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
                var ray = cam.ScreenPointToRay(screenCenter);

                if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, _aimMask))
                    _targetPosition = raycastHit.point;
                else
                    _targetPosition = cam.transform.position + ray.direction * _maxRayDistance;

                _worldPoint.position = _targetPosition;
            }

            if(_headIk != null)
                _headIk.transform.position = _targetPosition;

            _aimRig.weight = Mathf.Lerp(_aimRig.weight, _isAiming.Value ? 1f : 0f, Time.deltaTime*20f);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _controller = GetComponent<CharacterController>();

            _controller.OnInputEvent += Controller_OnInputEvent;
        }

        private void Controller_OnInputEvent(CharacterInput input)
        {
            _isAiming.Value = (input.IsAiming || _alwaysAim) && !_controller.CurrentInput.LookAround;

            if (_isAiming.Value && input.IsFire && _timer > _fireDelay)
            {
                _timer = 0f;

                var position = _spawnBulletPosition.position;

                var aimDir = (_targetPosition - position).normalized;
                GameManager.Instance.WeaponSystem.SpawnBullet(position, aimDir);
            }
        }

       
    }
}
