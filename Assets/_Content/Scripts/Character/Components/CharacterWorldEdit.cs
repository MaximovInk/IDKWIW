using MaximovInk.VoxelEngine;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.IDKWIW
{
    public class CharacterWorldEdit : MonoBehaviour, ICharacterComponent
    {
        public LayerMask LayerMask;

        private CharacterComponents _components;
        public void Initialize(CharacterComponents componentsRef)
        {
            _components = componentsRef;


        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                var main = _components.Camera.transform;

                var hit = Physics.Raycast(main.position, main.forward, out RaycastHit hitInfo, 5f, LayerMask);

                if (hit)
                {
                    var terrainChunk = hitInfo.transform.GetComponent<VoxelChunk>();

                    if (terrainChunk != null)
                    {
                        var terrain = terrainChunk.Terrain;

                        var pos = terrain.WorldToGrid(hitInfo.point);

                        for (int i = -2; i < 2; i++)
                        {
                            for (int j = -2; j < 2; j++)
                            {
                                for (int k = -2; k < 2; k++)
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
