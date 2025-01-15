using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class PreviewGeneration : MonoBehaviour, IChunkDataInterface
    {
        [SerializeField]
        private BaseTerrainGenerator _generator;
        [SerializeField]
        private VoxelTerrain _terrain;

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            var mesh = new Mesh();


            mesh.vertices = new Vector3[4]
{
    new Vector3(0, 0, 0),
    new Vector3(1, 0, 0),
    new Vector3(0, 1, 0),
    new Vector3(1, 1, 0)
};
            int[] tris = new int[6]
{
    // lower left triangle
    0, 2, 1,
    // upper right triangle
    2, 3, 1
};
            mesh.triangles = tris;
            Vector3[] normals = new Vector3[4]
{
    -Vector3.forward,
    -Vector3.forward,
    -Vector3.forward,
    -Vector3.forward
};
            mesh.normals = normals;
            Vector2[] uv = new Vector2[4]
{
      new Vector2(0, 0),
      new Vector2(1, 0),
      new Vector2(0, 1),
      new Vector2(1, 1)
};
            mesh.uv = uv;


            _meshFilter.mesh = mesh;
        }

        private bool _isDirty;

        public TexturePreview Preview;


        public float testMin = 0f;
        public float testMax = 1f;

        public void Update()
        {
            if (_isDirty || Preview.InvokeGenerate)
            {
                _isDirty = false;

                GeneratePreview();

                Preview.Validate();

                _meshRenderer.material.mainTexture = Preview.Texture;
            }
        }

        private MKBiome[] _cachedBiomes;

        private void GeneratePreviewT()
        {
            _generator.Generate(this);

        }

        private void GeneratePreview()
        {
            if (_terrain == null)
            {
                Debug.LogError("Generation aborted. Terrain is null");
                return;
            }
            if (_terrain.Data == null)
            {
                Debug.LogError("Generation aborted. TerrainData is null");
                return;
            }
            _cachedBiomes = _terrain.Data.Biomes;
            if (_cachedBiomes == null)
            {
                Debug.LogError("Generation aborted. TerrainData Biomes is null");
                return;
            }

            Preview.Validate();

            testMin = 1f;
            testMax = 0f;

            GeneratePreviewT();

            Preview.Texture.Apply();

            Preview.InvokeGenerate = false;
            Preview.InvokeRepaint = true;
        }

        public int3 Position => int3.zero;
        public int3 ChunkSize => VoxelTerrain.ChunkSize;
        public int LOD => 1;
        public int PreviousLOD => 1;

        public void Set(int3 position, ushort blockID, Color color, byte density)
        {
            Preview.Texture.SetPixel(position.x, position.y, color);
        }

        public void ClearAllObjects()
        {
            throw new System.NotImplementedException();
        }

        public void AddObject(VoxelObjectInfo info)
        {
            throw new System.NotImplementedException();
        }
    }
}
