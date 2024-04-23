using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class BulletProjectile : NetworkBehaviour
    {
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _rigidbody.velocity = transform.forward * 100f;
            _rigidbody.isKinematic = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            DestroyServerRPC();
        }

        [ServerRpc]
        private void DestroyServerRPC()
        {
            Destroy(gameObject);
        }
    }
}