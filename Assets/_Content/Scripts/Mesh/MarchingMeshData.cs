using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
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

            Mesh.SetVertices(Vertices);
            Mesh.SetTriangles(Triangles, 0);

            //Mesh.SetUVs(0, Uvs);
            //Mesh.SetColors(Colors);
            //Mesh.SetNormals(Normals);

          //  Mesh.RecalculateNormals();
          //  Mesh.RecalculateTangents();
        }

        public Mesh GetMesh()
        {
            return Mesh;
        }
    }
}
