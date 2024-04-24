using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Mesh;

namespace MaximovInk
{
    public class MeshDataStruct
    {
        public List<Vector3> Vertices;
        public List<Vector3> Normals;
        public List<int> Triangles;

        public MeshDataStruct(int3 size)
        {
            Vertices = new List<Vector3>(size.x * size.y * size.z * 16);
            Normals = new List<Vector3>(size.x * size.y * size.z * 16);
            Triangles = new List<int>(size.x * size.y * size.z * 16);
        }

        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            Triangles.Clear();
        }

        public void ApplyToMesh(Mesh mesh)
        {
            mesh.Clear();

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetTriangles(Triangles, 0);
        }
    }
}
