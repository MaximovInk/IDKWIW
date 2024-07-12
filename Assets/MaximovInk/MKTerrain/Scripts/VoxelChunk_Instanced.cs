
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public struct MeshProperties
    {
        public Matrix4x4 mat;
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }

    [System.Serializable]
    public class InstancedInfo
    {
        public MeshProperties[] Properties;
        public int DrawCount;

        public ComputeBuffer MeshPropertiesBuffer;
        public ComputeBuffer ArgsBuffer;

        public Mesh Mesh;
        public Material DrawMaterial;

        public void Dispose()
        {
            MeshPropertiesBuffer?.Release();
            MeshPropertiesBuffer = null;

            ArgsBuffer?.Release();
            ArgsBuffer = null;
            DrawCount = 0;
        }
    }

    public partial class VoxelChunk
    {
        public event Action OnInstancedRequestBuild;

        private List<InstancedInfo> _instancedDrawList = new();

        private Bounds _instancedBounds;

        private void InstancedAwake()
        {
            OnMeshGenerated += RebuildInstanced;

            OnUpdateModules += InstancedOnUpdateModules;
            OnDisableModules += DisposeInstanced;
        }

        private void DisposeInstanced()
        {
            for (int i = 0; i < _instancedDrawList.Count; i++)
            {
                _instancedDrawList[i].Dispose();
            }

            _instancedDrawList.Clear();
        }

        private void InstancedOnUpdateModules()
        {
            for (int i = 0; i < _instancedDrawList.Count; i++)
            {
                var instancedInfo = _instancedDrawList[i];
                if (instancedInfo.DrawCount == 0) continue;
                if (instancedInfo.ArgsBuffer == null) continue;

                Graphics.DrawMeshInstancedIndirect(instancedInfo.Mesh, 0, instancedInfo.DrawMaterial, _instancedBounds, instancedInfo.ArgsBuffer);
            }
        }

        private void InstancedCalculateBounds()
        {
            var chunkSize = VoxelTerrain.ChunkBlockSize;
            var size = new Vector3(chunkSize, chunkSize, chunkSize);
            _instancedBounds = new Bounds(transform.position + size / 2f, size);
        }

        private void RebuildInstanced()
        {
            DisposeInstanced();

            InstancedCalculateBounds();

            OnInstancedRequestBuild?.Invoke();
        }

        public void AddInstancedInfo(InstancedInfo info)
        {
            _instancedDrawList.Add(info);

        }
    }
}
