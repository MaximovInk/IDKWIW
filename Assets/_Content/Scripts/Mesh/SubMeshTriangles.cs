using System;
using System.Collections.Generic;

namespace MaximovInk
{
    [Serializable]
    public class SubMeshTriangles
    {
        public int _id;
        public List<int> _triangles;

        public SubMeshTriangles(int iD)
        {
            _id = iD;
            _triangles = new List<int>();
        }
    }
}