using System;
using MaximovInk.VoxelEngine;
using Unity.Mathematics;
using UnityEngine;

namespace Assets._Content.Scripts
{
    public class Explosion : MonoBehaviour
    {
        [SerializeField]
        private float _radius;

        [SerializeField]
        private GameObject _particlePrefab;

        [SerializeField]
        private bool _explodeOnStart;

        [SerializeField] private bool _invokeExplode;

        [SerializeField]
        private LayerMask _layerMask;

        private void Start()
        {
            if (_explodeOnStart)
            {
                Explode();
            }
        }

        private void Update()
        {
            if(_invokeExplode)Explode();
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, _radius);
        }

        public void Explode()
        {
            _invokeExplode = false;

            if (_particlePrefab != null)
            {
                var particleInstance = Instantiate(_particlePrefab);
                particleInstance.transform.position = transform.position;
            }

            var raycast = Physics.OverlapSphere(transform.position, _radius, _layerMask);

            if (raycast.Length > 0)
            {
                for (int i = 0; i < raycast.Length; i++)
                {
                    var chunk = raycast[i].GetComponent<VoxelChunk>();

                    if(chunk == null)continue;

                    var terrain = chunk.Terrain;

                    if (terrain == null) continue;


                    var gridPos = terrain.WorldToGrid(transform.position);

                    var gridRadius = Mathf.CeilToInt(_radius / VoxelTerrain.BlockSize);

                    for (int ix = -gridRadius; ix < gridRadius; ix++)
                    {
                        for (int iy = -gridRadius; iy < gridRadius; iy++)
                        {
                            for (int iz = -gridRadius; iz < gridRadius; iz++)
                            {
                                var offset = new int3(ix, iy, iz);

                                var erasePos = gridPos + offset;

                                if(Vector3.Distance(Vector3.zero, new Vector3(ix,iy,iz)) > gridRadius)continue;
                                

                                terrain.SetBlock(string.Empty, erasePos);
                                terrain.SetValue(0, erasePos);

                            }
                        }
                    }

                }
            }

            Destroy(gameObject);
        }
    }
}
