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
        public List<Color> Colors;

        public MeshDataStruct(int3 size)
        {
            var capacity = size.x * size.y * size.z * 16;

            Vertices = new List<Vector3>(capacity);
            Normals = new List<Vector3>(capacity);
            Triangles = new List<int>(capacity);
            Colors = new List<Color>(capacity);
        }

        public void Clear()
        {
            Vertices.Clear();
            Normals.Clear();
            Triangles.Clear();
            Colors.Clear();
        }

        public void ApplyToMesh(Mesh mesh)
        {
            mesh.Clear();

            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetTriangles(Triangles, 0);
            mesh.SetColors(Colors);
        }
    }
}
