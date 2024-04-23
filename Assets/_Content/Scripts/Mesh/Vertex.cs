using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk
{
    public struct Vertex
    {
        public Vector3 Position;
        public Vector2 UV;
        public Color Color;

        public int2 id;

        public int SubMeshID;
    }
}