using MaximovInk.VoxelEngine;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterWorldEdit : NetworkBehaviour
    {
        public LayerMask LayerMask;

        private CharacterController _controller;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                var main = _controller.Camera.transform;

                var hit = Physics.Raycast(main.position, main.forward, out RaycastHit hitInfo, 5f, LayerMask);

                if (hit)
                {
                    var terrainChunk = hitInfo.transform.GetComponent<VoxelChunk>();

                    if (terrainChunk != null)
                    {
                        var terrain = terrainChunk.Terrain;

                        var pos = terrain.WorldToGrid(hitInfo.point);

                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                for (int k = -1; k <= 1; k++)
                                {
                                    var newPos = pos + new int3(i, j, k);

                                    terrain.SetBlock(string.Empty, newPos);
                                    terrain.SetValue(0, newPos);
                                }
                            }
                        }

              
                    }
                }
            }

           
        }
    }
}
