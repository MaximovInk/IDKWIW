using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk
{
    [Serializable]
    public class MeshData
    {
        public int SubMeshCount
        {
            get => _subMeshCount; set => _subMeshCount = value;
        }

        private int _subMeshCount;

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

        [NonSerialized] private List<Vector3> _vertices = new();
        [NonSerialized] private List<SubMeshTriangles> _subMeshTriangles = new();
        [NonSerialized] private List<Vector2> _uvs = new();
        [NonSerialized] private List<Color32> _colors = new();
        [NonSerialized] private List<Vector3> _normals = new();
        [NonSerialized] private Mesh _mesh;


        public void AddQuad(
            Vector3 vertex1,
            Vector3 vertex2,
            Vector3 vertex3,
            Vector3 vertex4,
            Vector4 uv,
            Color color,
            int subMeshID = 0
        )
        {
            var subMeshTriangle = GetSubMeshTriangles(subMeshID);

            subMeshTriangle._triangles.Add(_vertices.Count);
            subMeshTriangle._triangles.Add(_vertices.Count + 1);
            subMeshTriangle._triangles.Add(_vertices.Count + 2);
            subMeshTriangle._triangles.Add(_vertices.Count);
            subMeshTriangle._triangles.Add(_vertices.Count + 2);
            subMeshTriangle._triangles.Add(_vertices.Count + 3);

            _vertices.Add(vertex1);
            _vertices.Add(vertex2);
            _vertices.Add(vertex3);
            _vertices.Add(vertex4);

            _uvs.Add(new Vector2(uv.x, uv.y));
            _uvs.Add(new Vector2(uv.x, uv.w));
            _uvs.Add(new Vector2(uv.z, uv.w));
            _uvs.Add(new Vector2(uv.z, uv.y));

            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
            _colors.Add(color);
        }

        public void SetVertices(List<Vertex> vertices)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];

                _vertices.Add(vertex.Position);
                _colors.Add(vertex.Color);
                _uvs.Add(vertex.UV);
            }
        }

        public void SetTriangles(List<int> triangle)
        {
            var sub = GetSubMeshTriangles(0);

            sub._triangles = triangle;
        }

        private SubMeshTriangles GetSubMeshTriangles(int id)
        {
            var subMeshTriangle = _subMeshTriangles.Find(n => n._id == id);
            if (subMeshTriangle == null)
            {
               // _subMeshCount = Mathf.Max(_subMeshCount, id + 1);
                subMeshTriangle = new SubMeshTriangles(id);
                _subMeshTriangles.Add(subMeshTriangle);
            }

            return subMeshTriangle;
        }

        public void AddTriangle(Triangle triangle)
        {
            var subMeshTriangle = GetSubMeshTriangles(triangle.Vertex1.SubMeshID);

            subMeshTriangle._triangles.Add(_vertices.Count);
            subMeshTriangle._triangles.Add(_vertices.Count+1);
            subMeshTriangle._triangles.Add(_vertices.Count+2);

            _vertices.Add(triangle.Vertex1.Position);
            _vertices.Add(triangle.Vertex2.Position);
            _vertices.Add(triangle.Vertex3.Position);

            _uvs.Add(triangle.Vertex1.UV);
            _uvs.Add(triangle.Vertex2.UV);
            _uvs.Add(triangle.Vertex3.UV);

            _colors.Add(triangle.Vertex1.Color);
            _colors.Add(triangle.Vertex2.Color);
            _colors.Add(triangle.Vertex3.Color);
        }

        public void AddVertex(Vector3 vertex1, Vector2 uv, Color color, int subMeshID = 0)
        {
            var subMeshTriangle = GetSubMeshTriangles(subMeshID);

            subMeshTriangle._triangles.Add(_vertices.Count);

            _vertices.Add(vertex1);

            _uvs.Add(uv);

            _colors.Add(color);
        }

        public void Clear()
        {
            _subMeshCount = 0;
            _subMeshTriangles.Clear();
            _vertices.Clear();
            _uvs.Clear();
            _colors.Clear();
            _normals.Clear();
        }


        public void ApplyToMesh()
        {
            Mesh.Clear();
            Mesh.vertices = _vertices.ToArray();

            Mesh.subMeshCount = _subMeshCount;

            foreach (var triangle in _subMeshTriangles)
            {
                Mesh.SetTriangles(triangle._triangles.ToArray(), triangle._id);
            }

            Mesh.uv = _uvs.ToArray();
            Mesh.colors32 = _colors.ToArray();
            Mesh.normals = _normals.ToArray();
            Mesh.RecalculateNormals();
            Mesh.RecalculateTangents();

        }

        public Mesh GetMesh()
        {
            return Mesh;
        }
    }
}
