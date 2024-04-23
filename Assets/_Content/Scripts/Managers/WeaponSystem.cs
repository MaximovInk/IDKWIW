using MaximovInk.IDKWIW;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk
{
    public class WeaponSystem : NetworkBehaviour
    {
        public float SwaySmooth => _swaySmooth;
        public float WeaponMoveAmount => _weaponMoveAmount;
        public float WeaponRotateAmount => _weaponRotateAmount;

        [SerializeField] private float _swaySmooth;
        [SerializeField] private float _weaponMoveAmount;
        [SerializeField] private float _weaponRotateAmount;

        [SerializeField] private BulletProjectile _bulletProjectilePrefab;

        public void SpawnBullet(Vector3 initPos, Vector3 aimDir)
        {
            SpawnBulletServerRPC(initPos, aimDir);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SpawnBulletServerRPC(Vector3 initPos, Vector3 aimDir)
        {
            var bullet = Instantiate(_bulletProjectilePrefab, initPos, Quaternion.LookRotation(aimDir, Vector3.up));
            bullet.GetComponent<NetworkObject>().Spawn();

            Destroy(bullet.gameObject, 10f);
        }

    }
}
