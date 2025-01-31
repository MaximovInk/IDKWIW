﻿using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public partial class VoxelChunk
    {
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        public Mesh Mesh => _mesh;

        private Mesh _mesh;

        private MeshDataStruct _meshData;

        public float DistanceToTarget;

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

            DistanceToTarget = float.MaxValue;
        }


        private void ApplyMaterials()
        {
            _meshRenderer.material = Terrain.Material;
        }
    }
}
