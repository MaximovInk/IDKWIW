using UnityEngine;

namespace MaximovInk
{
    [System.Serializable]
    public struct KickbackWeaponData
    {
        public bool KickbackEnabled;

        public float KickBackReturnSpeed;
        public float KickBackSpeed;

        public float MaxKickBackRotation;
        public float MaxKickBackPosition;
    }

    public class Weapon : MonoBehaviour
    {
        [HideInInspector] public MKCharacter Owner;
        public Transform AimPoint => _aimPoint;
        public KickbackWeaponData KickbackWeaponData => _kickbackWeaponData;
        public Transform SecondArmTarget => _secondArmTarget;
        public WeaponType Type => _type;

        public string ID;

        [SerializeField] private Transform _aimPoint;
        [SerializeField] protected Transform _secondArmTarget;
        [SerializeField] protected WeaponType _type;
        [SerializeField] private float _fireDelay = 0.01f;

        protected float FireTimer = 0f;

        [SerializeField] private KickbackWeaponData _kickbackWeaponData;

        public virtual void Fire()
        {
            FireTimer = _fireDelay;
        }

        protected virtual void Update()
        {
            if (FireTimer > 0f)
            {
                FireTimer -= Time.deltaTime;
            }
        }
    }
}
