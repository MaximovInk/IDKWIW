using System;
using System.Collections.Generic;
using UnityEngine;

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

        public void ClearMesh()
        {
            _mesh.Clear();
        }

        private void InitializeRenderer()
        {
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
