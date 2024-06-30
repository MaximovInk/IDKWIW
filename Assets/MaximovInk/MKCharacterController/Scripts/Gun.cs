using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using Random = UnityEngine.Random;

namespace MaximovInk
{

    public class Gun : MonoBehaviour
    {
        public Transform BulletPos => _bulletPos;

        [SerializeField] private Transform _bulletPos;

        public Transform SecondArmTarget => _secondArmTarget;

        [SerializeField] private Transform _secondArmTarget;

        //[SerializeField] private AudioClip _fireClip;

        private MK3DAudioSource _gunSource;

        [SerializeField] private float _fireDelay = 0.01f;

        [SerializeField] private float _minVolume = 0.3f;
        [SerializeField] private float _maxVolume = 0.6f;

        public MKCharacter Owner;

        private float _fireTimer = 0f;

        private void Awake()
        {
            _gunSource = GetComponent<MK3DAudioSource>();
        }


        public void Fire()
        {
            Fire(Owner.CurrentAimIsHit.Value, Owner.CurrentAimHitPoint.Value, Owner.CurrentAimHitNormal.Value);

            //Debug.Log("Fire");
           // FireServerRpc();
        }

        /*
          [ServerRpc]
         public void FireServerRpc()
         {
             //Debug.Log("FireServer");
             FireClientRpc();
         }

         [ClientRpc]
         public void FireClientRpc()
         {

             if (Owner == null) return;

             Fire(Owner.CurrentAimIsHit.Value, Owner.CurrentAimHitPoint.Value, Owner.CurrentAimHitNormal.Value);
         }
         */

        private void Fire(bool isHit, Vector3 hitPoint, Vector3 hitNormal)
        {
          

            if (_fireTimer > 0f) return;

            

            _gunSource.MasterValue = Random.Range(_minVolume, _maxVolume);
            _gunSource.Play();
            _fireTimer = _fireDelay;

            // var dir = (aimPos - _bulletPos.position);

            //var distance = dir.magnitude;

            // var ray = new Ray(_bulletPos.position, dir);

            // Debug.DrawRay(ray.origin,ray.direction * distance, Color.red, 1f);

            if (isHit)
            {
                var pool = MKPool.Instance;

                // Debug.Log($"Hit : {info.Hit.collider.gameObject.name}");

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


        }



        private void Update()
        {
            if (_fireTimer > 0f)
            {
                _fireTimer -= Time.deltaTime;


            }
        }
    }


}