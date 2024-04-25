using System;
using System.Linq;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [RequireComponent(typeof(VoxelTerrain))]
    public class ChunksLoader : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        private VoxelTerrain _terrain;

        //[Range(0,1)]
        //[SerializeField] private float _frequency;

        [Range(0, 10)]
        [SerializeField] private float _lodMultiplier = 1f;

        [SerializeField] private float _delay;
        private float _timer;

        private void Awake()
        {
            _terrain = GetComponent<VoxelTerrain>();
        }

        private void Update()
        {
            if(_target == null)
                return;

           // _delay = (1f - _frequency) * 5f;

            _timer += Time.deltaTime;

            if (_timer > _delay)
            {
                _timer = 0f;

                UpdateChunks();
            }

        }

        private VoxelChunk[] _chunks;

        private void UpdateChunks()
        {
            if(_chunks== null || _chunks.Length != transform.childCount)
                _chunks = GetComponentsInChildren<VoxelChunk>();

            if (_chunks == null || _chunks.Length == 0) return;

            _chunks = _chunks.OrderBy(n => n.LOD).ToArray();

            var avgSize = (_terrain.ChunkSize.x + _terrain.ChunkSize.y + _terrain.ChunkSize.z) / 3f;

            var targetPos = _target.position;

            for (int i = 0; i < _chunks.Length; i++)
            {
                var chunk = _chunks[i];

                var distance = Vector3.Distance(targetPos, chunk.transform.position);

                var lod = chunk.ValidateLodValue((int)(distance / (avgSize * _lodMultiplier)));

                if (_chunks[i].LOD != lod)
                {
                    _chunks[i].LOD = lod;

                    Debug.Log("Update");

                    _timer = _delay / 1.2f;

                    return;
                }
            }
        }
    }
}
