using UnityEngine;

namespace MaximovInk
{


    [RequireComponent(typeof(Animator))]
    public class IKFootPlacement : MonoBehaviour
    {
        private Animator _animator;

        [SerializeField] private LayerMask _groundMask;

        [SerializeField, Range(0f,1f)]
        private float _distance = 1f;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        private void FootLogic(AvatarIKGoal goal)
        {
            _animator.SetIKPositionWeight(goal, 1f);
            _animator.SetIKRotationWeight(goal, 1f);

            RaycastHit hit;
            Ray ray = new Ray(_animator.GetIKPosition(goal) + Vector3.up, Vector3.down);
            if (Physics.Raycast(ray, out hit, _distance + 1f, _groundMask))
            {
                Vector3 footPos = hit.point;
                footPos.y += _distance;
                _animator.SetIKPosition(goal, footPos);
                _animator.SetIKRotation(goal, Quaternion.LookRotation(transform.forward, hit.normal));

            }
        }

        private void OnAnimatorIK(int layerIndex)
        {
            FootLogic(AvatarIKGoal.LeftFoot);
            FootLogic(AvatarIKGoal.RightFoot);

            /*
              _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,1f);
             _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot,1f);

             RaycastHit hit;
             Ray ray = new Ray(_animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
             if (Physics.Raycast(ray, out hit, _distance + 1f, _groundMask))
             {
                 Vector3 footPos = hit.point;
                 footPos.y += _distance;
                 _animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPos);
                 _animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal));

             }

             */

        }
    }

}