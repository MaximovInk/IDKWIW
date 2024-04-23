using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk
{
    public class MarchingMeshData
    {
        private Mesh Mesh
        {
            get
            {
                if (_mesh == null)
                {
                    _mesh = new Mesh();
                }

                return _mesh;
            }
        }

        [NonSerialized] public List<Vector3> Vertices = new();
        [NonSerialized] public List<int> Triangles = new();
        [NonSerialized] public List<Vector2> Uvs = new();
        [NonSerialized] public List<Color32> Colors = new();
        [NonSerialized] public List<Vector3> Normals = new();
        [NonSerialized] private Mesh _mesh;

        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
            Uvs.Clear();
            Colors.Clear();
            Normals.Clear();
        }


        public void ApplyToMesh()
        {
            Mesh.Clear();
            Mesh.vertices = Vertices.ToArray();

            Mesh.triangles = Triangles.ToArray();

            Mesh.uv = Uvs.ToArray();
            Mesh.colors32 = Colors.ToArray();
            Mesh.normals = Normals.ToArray();

            Mesh.RecalculateNormals();
            Mesh.RecalculateTangents();
        }

        public Mesh GetMesh()
        {
            return Mesh;
        }
    }
}
