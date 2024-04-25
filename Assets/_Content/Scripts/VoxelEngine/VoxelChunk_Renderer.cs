using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Profiling;
using static UnityEngine.Mesh;
using Debug = UnityEngine.Debug;

namespace MaximovInk.VoxelEngine
{
    public partial class VoxelChunk
    {
        //private MarchingMeshData _meshData;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        private Mesh _mesh;

        private MeshDataStruct _meshData;

        private void InitializeRenderer()
        {
            smoothedVerticesCache = new Dictionary<Vector3, int>(ChunkSize.x * ChunkSize.y * ChunkSize.z * 16);
            
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshFilter = GetComponent<MeshFilter>();

            _meshData = new MeshDataStruct(ChunkSize);

            _mesh = new Mesh();

            _meshFilter.mesh = _mesh;
            _meshCollider = GetComponent<MeshCollider>();

            ApplyMaterials();

            _isDirty = true;
        }

        private void ApplyMesh()
        {
            _meshData.ApplyToMesh(_mesh);
        }

        private void ApplyMaterials()
        {
            _meshRenderer.material = Terrain.Material;
        }
    }
}
