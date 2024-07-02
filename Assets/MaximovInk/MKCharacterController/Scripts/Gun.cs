using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace MaximovInk
{
    [System.Serializable]
    public struct WeaponMuzzle
    {
        public Light Light;
        public ParticleSystem Effect;
    }

    public class Gun : Weapon
    {
        public Transform BulletPos => _bulletPos;
        [SerializeField] private Transform _bulletPos;



        private MK3DAudioSource _gunSource;

        [SerializeField] private float _minVolume = 0.3f;
        [SerializeField] private float _maxVolume = 0.6f;

        [SerializeField] private WeaponMuzzle _muzzle;

        private void Awake()
        {
            _gunSource = GetComponent<MK3DAudioSource>();

            _muzzle.Light.enabled = false;
        }

        public override void Fire()
        {
            if (FireTimer > 0f) return;

            Fire(Owner.CurrentAimIsHit.Value, Owner.CurrentAimHitPoint.Value, Owner.CurrentAimHitNormal.Value);

            base.Fire();
        }


        private void Fire(bool isHit, Vector3 hitPoint, Vector3 hitNormal)
        {
            _gunSource.MasterValue = Random.Range(_minVolume, _maxVolume);
            _gunSource.Play();

            if (isHit)
            {
                var pool = MKPool.Instance;

                var vfx = pool.GetByID("Smoke");
                vfx.gameObject.SetActive(true);
                vfx.transform.position = hitPoint;
                vfx.transform.up = hitNormal;

                var visual = vfx.GetComponent<VFXControl>();
                visual.Play();
                visual.AutoHideOnEmpty = true;
                visual.LifeTime = 0.2f;

                var hole = pool.GetByID("BulletHole");
                hole.gameObject.SetActive(true);
                hole.transform.position = hitPoint + hitNormal * .1f;
                hole.transform.forward = -hitNormal;
                hole.transform.Rotate(Vector3.forward, Random.Range(0f, 360f));

                pool.HideAfterTime(hole,1f);
            }

            if (Owner != null)
            {
                Owner.KickBack();
            }

            _muzzle.Light.enabled = true;

            _muzzle.Effect.Play();
        }

        protected override void Update()
        {
            base.Update();

            if (FireTimer <= 0f && _muzzle.Light.enabled)
            {
                _muzzle.Light.enabled = false;
            }
        }
    }


}