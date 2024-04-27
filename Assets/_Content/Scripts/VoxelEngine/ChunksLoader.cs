using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.UI.Image;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public struct ChunkLODParameter
    {
        public int LOD;
        public float DistanceInChunks;
    }

    [RequireComponent(typeof(VoxelTerrain))]
    public class ChunksLoader : MonoBehaviour
    {
        public Transform Target
        {
            get => _target; set => _target = value;
        }

        [SerializeField] private Transform _target;

        private VoxelTerrain _terrain;


        [SerializeField] private float _delay;
        private float _timer;

        [SerializeField] private ChunkLODParameter[] LODs;

        [SerializeField] private int _lodFreeChunk;

        private Stack<VoxelChunk> _freeChunks = new Stack<VoxelChunk>(16*16);

        [SerializeField] private int3 _chunkAroundUpdate = new int3(2,1,2);

        private void Awake()
        {
            _terrain = GetComponent<VoxelTerrain>();
        }

        private void Start()
        {
            _chunks = _terrain.ChunksCache.Values.ToArray();
        }

        private void Update()
        {
            if(_target == null)
                return;

            _timer += Time.deltaTime;

            if (_timer > _delay)
            {
                _timer = 0f;

                UpdateChunksLOD();
                UpdateChunksPositions();
            }
        }

        private VoxelChunk[] _chunks;

        /*
         private void ValidateChunksArray()
        {
            if (_chunks == null || _chunks.Length != transform.childCount)
                _chunks = GetComponentsInChildren<VoxelChunk>();
        }

         */



        private void SortChunksByLod()
        {
            _chunks = _chunks.OrderBy(n => n.LOD).ToArray();
        }

        private void SortLODsByDistance()
        {
            LODs = LODs.OrderByDescending(n => n.DistanceInChunks).ToArray();
        }

        private float GetChunkDistanceStepAverage()
        {
            return (_terrain.ChunkSize.x + _terrain.ChunkSize.z) / 2f * (_terrain.BlockSize.x + _terrain.BlockSize.z) / 2f;
        }

        private bool UpdateChunkLOD(VoxelChunk chunk, Vector3 targetPos, float lodStep)
        {
            var player = new float2(targetPos.x, targetPos.z);

            var chunkPosD = new float2(chunk.transform.position.x, chunk.transform.position.z);

            var distance = math.distance(player, chunkPosD) / lodStep;

            int lod = 1;

            for (int i = 0; i < LODs.Length; i++)
            {
                if (distance >= LODs[i].DistanceInChunks)
                {
                    lod = LODs[i].LOD;
                    break;
                }
            }

            lod = chunk.ValidateLodValue(lod);

            if (chunk.LOD != lod)
            {
                chunk.LOD = lod;
                _timer = _delay / 1.2f;

                return true;
            }

            return false;
        }

        private void UpdateChunksLOD()
        {
            if (_chunks == null || _chunks.Length == 0) return;

            SortChunksByLod();
            SortLODsByDistance();

            var lodStep = GetChunkDistanceStepAverage();

            var targetPos = _target.position;

            _freeChunks.Clear();

            for (int i = 0; i < _chunks.Length; i++)
            {
                var chunk = _chunks[i];

                var changed = UpdateChunkLOD(chunk, targetPos, lodStep);

                if (chunk.LOD >= _lodFreeChunk)
                {
                    _freeChunks.Push(chunk);
                    chunk.IsFree = false;
                }

                if (changed) return;
            }
        }


        private void UpdateChunksPositions()
        {
            if(_freeChunks.Count == 0) return;

            var xSize = Mathf.CeilToInt(_chunkAroundUpdate.x / 2f);
            var ySize = _chunkAroundUpdate.y;
            var zSize = Mathf.CeilToInt(_chunkAroundUpdate.z / 2f);

            var xMin = _chunkAroundUpdate.x - xSize;
            //var yMin = _chunkAroundUpdate.y - ySize;
            var zMin = _chunkAroundUpdate.z - zSize;

            var origin = _terrain.WorldToChunkPosition(_target.position);

            for (int ix = -xMin; ix <= xSize; ix++)
            {
                for (int iy = -1; iy <= ySize; iy++)
                {
                    for (int iz = -zMin; iz <= zSize; iz++)
                    {
                        if (_freeChunks.Count == 0) return;

                        var currentPos = origin + new int3(ix, iy, iz);

                        if(_terrain.GetChunkByPos(currentPos,false) != null) continue;

                        var chunk = _freeChunks.Pop();

                        if (chunk == null) return;

                        chunk.IsFree = true;

                        var gettedChunk = _terrain.UnloadChunk(chunk.Position);

                        if (gettedChunk != chunk)
                        {
                            continue;
                        }

                        gettedChunk.Position = currentPos;

                        _terrain.LoadChunk(currentPos, gettedChunk);
                    }
                }
            }
        }
        
    }
}
