using Unity.Netcode;
using UnityEngine;

namespace MaximovInk
{
    public class MKInventoryNetwork : NetworkBehaviour
    {
        [SerializeField] private MKWorldItem _worldItemPrefab;

        [SerializeField] private float _worldItemPushForce = 500f;

        public void Drop(MKItem item, Vector3 pos, Vector3 look)
        {
            DropServerRpc(item.ItemID, item.Count, item.Durability, pos, look);
        }

        [ServerRpc(RequireOwnership = false)]
        public void DropServerRpc(string ID, int count, float durability, Vector3 pos, Vector3 look)
        {
            if (!IsServer) return;

            //Debug.Log($"Drop {ID} {count} {durability}");

            var instance = Instantiate(_worldItemPrefab, pos,
                Quaternion.identity);
            instance.Init(new MKItem
            {
                ItemID = ID,
                Count = count,
                Durability = durability,
            });

            instance.GetComponent<NetworkObject>().Spawn(true);


            var rb = instance.GetComponent<Rigidbody>();

            if (rb != null)
            {
                

                rb.AddForce(look * _worldItemPushForce);
            }

            instance.transform.forward = look;


            //DropClientRpc(ID,count,durability, pos,look);
        }

        /*
           [ClientRpc]
          private void DropClientRpc(string ID, int count, float durability, Vector3 pos, Vector3 look)
          {

          }

         */

    }
}
