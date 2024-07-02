using Unity.Netcode;
using UnityEngine;

namespace MaximovInk
{
    public class MKWorldItem : NetworkBehaviour, IInteractable
    {
        public MKItem Contains => _contains;

        [SerializeField] private MKItem _contains;

        public MKItem Collect()
        {
            CollectServerRpc();

            return _contains;
        }

        [ServerRpc(RequireOwnership = false)]
        private void CollectServerRpc()
        {
            Destroy(gameObject);
        }

        public void Init(MKItem data)
        {
            _contains = data;

            UpdateModel();
        }

        private void Awake()
        {
            UpdateModel();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            UpdateModel();

            if(IsServer)
                NetworkManager.Singleton.OnClientConnectedCallback += Singleton_OnClientConnectedCallback;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsServer)
                NetworkManager.Singleton.OnClientConnectedCallback -= Singleton_OnClientConnectedCallback;
        }

        private void Singleton_OnClientConnectedCallback(ulong obj)
        {
            UpdateModelClientRpc(_contains.ItemID, _contains.Count, _contains.Durability);
        }

        private void UpdateModel()
        {
            if (!IsServer) return;

            //UpdateModelServerRpc();   

            if (_contains == null) return;

            UpdateModelClientRpc(_contains.ItemID, _contains.Count, _contains.Durability);
        }


        [ClientRpc]
        private void UpdateModelClientRpc(string ID, int count, float durability)
        {
            _contains = new MKItem()
            {
                ItemID = ID,
                Durability = durability,
                Count = count,
            };

            MKUtils.DestroyAllChildren(transform);

            if (_contains == null) return;

            var data = _contains.Data;

            if (data.CustomModel == null) return;

            var customModel = Instantiate(data.CustomModel, transform);

            customModel.transform.localRotation = Quaternion.identity;
            customModel.transform.localPosition = Vector3.zero;

            var modelCollider = customModel.GetComponent<Collider>();

            if (modelCollider != null)
            {
                modelCollider.enabled = true;
            }

            _contains = new MKItem() { ItemID = ID, Count = count, Durability = durability };
        }

        public void Interact(MKCharacter character)
        {
            if (character.IsOwner && MKInventoryManager.Instance.Collect(this))
            {
                Collect();
            }
        }

        public string GetInfoText()
        {
            return _contains != null ? _contains.ItemID : "nullItem";
        }

        public Vector3 GetPosition()
        {
            return transform.position;
        }
    }
}
