
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MaximovInk.VoxelEngine
{
    [System.Serializable]
    public class TexturePreview
    {

        public Texture2D Texture => _texture;
        private Texture2D _texture;

        public int Width = 256;
        public int Height = 256;

        public bool InvokeGenerate;
        public bool InvokeRepaint;

        public FilterMode FilterMode;

        public void Validate()
        {
            if (_texture != null && _texture.width == Width && _texture.height == Height &&
                _texture.filterMode == FilterMode) return;


            _texture = new Texture2D(Width, Height)
            {
                filterMode = FilterMode
            };

            InvokeGenerate = true;
        }
    }

    public enum MKNoiseType
    {
        Result,
        Temp,
        Humidity,
        Ground,
        BiomeColor,
        Height,
        ObjectsV1
    }

    [System.Serializable]
    public struct ObjectSpawnData
    {
        public string ID;
        public float Chance;

    }

    [ExecuteInEditMode]
    public class MKTerrainGeneration : MonoBehaviour
    {
        public TexturePreview Preview = new ();

        [Range(0.01f, 2f)]
        public float GlobalScale = 1.0f;

        public Vector2 Offset;

        public MKNoise Ground = new();

        public MKNoise Temperature = new();
        public MKNoise Humidity = new();

        public MKNoise ObjectsV1 = new();

        public MKNoiseType PreviewType;

        public float testMin = 0f;
        public float testMax = 1f;

        public float TempInfluence = 10f;
        public float HumidityInfluence = 5f;

        [SerializeField]
        private float[] _biomeWeights = Array.Empty<float>();

        [SerializeField] private VoxelTerrain _terrain;

        public float TempColorApply = 1f;

        private void Awake()
        {
            GeneratePreview();

            _terrain = GetComponent<VoxelTerrain>();

            if (_terrain != null)
            {
               // _terrain.OnChunkLoaded += GenerateHeight;
              //  _terrain.OnMeshGenerated += GeneratePost;
            }

        }

        List<int3> positionsAdded = new List<int3>();


        private void GeneratePost(VoxelChunk chunk)
        {

            if(chunk.LOD < 8 && chunk.PreviousLOD < chunk.LOD)
                return;

            chunk.DestroyAllObjects();

            var chunkPos = chunk.Position;
            var gridOrigin = ChunkSize * chunkPos;
            positionsAdded.Clear();


            for (int i = 0; i < chunk.Mesh.vertexCount; i++)
            {
                var v = chunk.Mesh.vertices[i];

                var bIndex = chunk.VertexToBlockIndex[i];

                var pos = VoxelUtility.IndexToPos(bIndex);

                if (positionsAdded.Contains(pos)) continue;
                positionsAdded.Add(pos);

                var topCheck = new int3(pos.x, pos.y + 2, pos.z);

                bool topIsEmpty;
                if (topCheck.y >= ChunkSize.y-1)
                {
                    var globalPos = VoxelUtility.LocalGridToGlobalGrid(chunk, topCheck);
                    topIsEmpty = _terrain.GetBlock(globalPos) == 0;
                }
                else
                {
                    topIsEmpty = chunk.GetBlock(topCheck) == 0;
                }

                if (topIsEmpty)
                {
                    var sampleX = pos.x + gridOrigin.x;
                    var sampleY = pos.z + gridOrigin.z;

                    var p = ScaleAtPos(new Vector2(sampleX, sampleY));
                    var d0 = 0f;
                    var rV = ObjectsV1.EvaluateCellular(p.x, p.y, ref d0);

                    
                    if (rV > 0.5f && d0 < 0.5f)
                    {
                       // Debug.Log("topEmpty");


                       var posOffset = new Vector3(
                           Random.Range(-1,1),0,
                           Random.Range(-1,1));

                        chunk.AddObject(new VoxelObjectInfo()
                        {
                            PrefabID = "tree",
                            Position = v+ posOffset,
                            Scale = Vector3.one,
                            Rotation =Quaternion.identity
                        });
                    }

                }



            }

        }

        private static int3 ChunkSize => VoxelTerrain.ChunkSize;

        public float HeightBlendHeight = 1f;

        [SerializeField] private float Amplitude = 40f;

        public float WaterLevel;
        public Color WaterColor;

        public ObjectSpawnData[] Objects;

        private MKBiome[] _cachedBiomes;

        private void GenerateHeight(VoxelChunk chunk)
        {
            var chunkPos = chunk.Position;

            var gridOrigin = ChunkSize * chunkPos;
            PreGeneration();

            //var isoLevelF = chunk.Terrain.IsoLevel/255f;
            //var isoLevelFMinus = 1f- isoLevelF;

            for (int ix = 0; ix < ChunkSize.x; ix++)
            {
                for (int iz = 0; iz < ChunkSize.z; iz++)
                {
                    var sampleX = ix + gridOrigin.x;
                    var sampleY = iz + gridOrigin.z;

                    _currentTemp = GetNoise(sampleX, sampleY, Temperature);
                    _currentHum = GetNoise(sampleX, sampleY, Humidity);

                    CalculateBiomeWeights();

                    var info = GetBiomeInfo(sampleX, sampleY, out var t, true);


                    var height = t;

                    height *= Amplitude;

                    height -= gridOrigin.y;

                    if (height <= 0) continue;

                    //bool canSpawnObject = height < ChunkSize.y;

                    var topH = 0f;

                    for (int iy = 0; iy < ChunkSize.y && iy < height; iy++)
                    {
                        var pos = new int3(ix, iy, iz);

                        chunk.SetBlock((ushort)(info.PreferBiomeIndex + 1), pos);

                        chunk.SetColor(info.Color, pos);

                        var value = Mathf.Clamp01((height - iy) / (ChunkSize.y));

                        chunk.SetValue(pos, (byte)(value * 255f));

                        topH = Mathf.Max(iy, topH);
                    }

                    /*
                    if (canSpawnObject)
                    {
                        var p = ScaleAtPos(new Vector2(sampleX, sampleY));
                        var d0 = 0f;
                        var rV = ObjectsV1.EvaluateCellular(p.x, p.y, ref d0);

                        if (rV > 0.5f && d0 < 0.01f)
                        {
                            Debug.Log(topH);

                            //chunk.GridToLocal(new Vector3(p.x, p.y, height)

                            //3
                            //0,098
                            //0,0902

                            //37.61
                            //42



                            //4,39

                            chunk.AddObject(new VoxelObjectInfo()
                            {
                                PrefabID = "tree",
                                Position = chunk.GridToLocal(new Vector3(ix, topH, iz)),
                                Rotation = Quaternion.identity,
                                Scale = Vector3.one
                            });
                        }
                    }*/

                }
            }

        }

        private Vector2 ScaleAtPos(Vector2 input)
        {
            ScaleAtMovePos(ref input.x, ref input.y);
            return input;
        }

        private void OnValidate()
        {
            GeneratePreview();
        }

        private void ScaleAtMovePos(ref float x, ref float y)
        {
            x = (x + (int)Offset.x) / GlobalScale;
            y = (y + (int)Offset.y) / GlobalScale;
        }

        private void GeneratePreviewT()
        {
            PreGeneration();

            for (int ix = 0; ix < Preview.Width; ix++)
            {
                for (int iy = 0; iy < Preview.Height; iy++)
                {
                    var t = GetNoise(ix, iy);

                    var color = GetColor(ix,iy,t);

                    testMin = Mathf.Min(testMin, t);
                    testMax = Mathf.Max(testMax, t);

                    Preview.Texture.SetPixel(ix, iy, color);
                }
            }

        }

        private void PreGeneration()
        {
            switch (PreviewType)
            {
                case MKNoiseType.Result:
                    Ground.CalculateOffsets();
                    Temperature.CalculateOffsets();
                    Humidity.CalculateOffsets();
                    break;
                case MKNoiseType.Temp:
                    Temperature.CalculateOffsets();
                    break;
                case MKNoiseType.Humidity:
                    Humidity.CalculateOffsets();
                    break;
                case MKNoiseType.Ground:
                    Ground.CalculateOffsets();
                    break;
                case MKNoiseType.BiomeColor:
                    break;
                case MKNoiseType.Height:
                    Ground.CalculateOffsets();
                    Temperature.CalculateOffsets();
                    Humidity.CalculateOffsets();
                    break;
                case MKNoiseType.ObjectsV1:
                    ObjectsV1.CalculateOffsets();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CalculateBiomeWeights()
        {
            var biomeCount = _cachedBiomes?.Length ?? 0;

            if (biomeCount == 0) return;

            if(_biomeWeights == null || _biomeWeights.Length != biomeCount)
                _biomeWeights = new float[biomeCount];

            var maxt = -1f;
            var maxi = 0;
            var sum = 0f;

            for (int i = 0; i < biomeCount; i++)
            {
                Vector2 d = new Vector2(_cachedBiomes[i].Temperature - _currentTemp, _cachedBiomes[i].Humidity - _currentHum);
                d.x *= TempInfluence; // temperature has a high influence on biome matching
                d.y *= HumidityInfluence; // crank up the humidity difference also to make biome borders less fuzzy
                _biomeWeights[i] = Mathf.Max(0, 1.0f - (d.x * d.x + d.y * d.y) * 0.1f);
                // record highest weight
                if (_biomeWeights[i] > maxt)
                {
                    maxi = i;
                    maxt = _biomeWeights[i];
                }
                sum += _biomeWeights[i];
            }

            if (sum > .001)
            {
                // normalize the weights so they add up to 1
                sum = 1.0f / sum;
                for (int i = 0; i < biomeCount; i++)
                    _biomeWeights[i] *= sum;
            }
            else
            {
                // sum of all weights is very close to zero, just zero all weights and set the highest to 1.0
                // this helped with artifacts at biome borders
                for (int i = 0; i < biomeCount; i++)
                    _biomeWeights[i] = 0.0f;
                _biomeWeights[maxi] = 1.0f;
            }

        }

        private Color GetColor(MKBiome biome, float t)
        {
            var c = biome.Color.Evaluate(t);

            if (biome.TemperatureApply)
            {
                c = Color.Lerp(c, Temperature.GetColor(_currentTemp), TempColorApply);
            }

            return c;
        }

        private struct BiomeInfo
        {
            public int PreferBiomeIndex;
            public bool IsUnderwater;
            public Color Color;
        }

        private BiomeInfo GetBiomeInfo(float x, float y, out float hBlended, bool height=false)
        {
            hBlended = 0.0f;
            var bestIndex = 0;
            var bestWeight = 0f;
            var h = 0f;

            if (_biomeWeights.Length == 0)
            {
                return new BiomeInfo() { IsUnderwater = false, PreferBiomeIndex = -1, Color = Color.white };

            }
            bestWeight = _biomeWeights[bestIndex];

            for (int i = 1; i < _biomeWeights.Length; i++)
            {
                var curW = _biomeWeights[i];
                if (curW > bestWeight)
                {
                    bestIndex = i; 
                    bestWeight = curW;
                }
            }



            if (height)
                h = GetNoise(x,y, _cachedBiomes[bestIndex].Terrain)* bestWeight;

            hBlended = (height ? h : GetNoise(x, y, _cachedBiomes[bestIndex].Terrain) * bestWeight)  * _cachedBiomes[bestIndex].Amplitude + _cachedBiomes[bestIndex].BaseHeight * bestWeight;

            var color = GetColor(_cachedBiomes[bestIndex], h);

            for (int i = 0; i < _biomeWeights.Length; i++)
            {
                if(i == bestIndex)continue;

                var weight = _biomeWeights[i];

                if(height)
                    h = GetNoise(x, y, _cachedBiomes[i].Terrain);

                var tt = Mathf.Clamp01(weight * HeightBlendHeight);

                hBlended += (height ? h : GetNoise(x, y, _cachedBiomes[i].Terrain)) * tt * _cachedBiomes[i].Amplitude + _cachedBiomes[i].BaseHeight * tt;

                var c = GetColor(_cachedBiomes[i], h);

                color = Color.Lerp(color, c, weight);
            }

            if (hBlended < WaterLevel)
            {
                return new BiomeInfo() { IsUnderwater = true, PreferBiomeIndex = bestIndex, Color = WaterColor };
            }

            return new BiomeInfo() { IsUnderwater = false, PreferBiomeIndex = bestIndex, Color = color };
        }

        private float GetNoise(float x, float y, MKNoise noise)
        {
            ScaleAtMovePos(ref x, ref y);

            var value = (noise.Evaluate(x, y) + 1f)/2f;

            return value;
        }

        private float GetNoise(float x, float y)
        {
            ScaleAtMovePos(ref x, ref y);

            var value = PreviewType switch
            {
                MKNoiseType.Result => Ground.Evaluate(x, y),
                MKNoiseType.Temp => Temperature.Evaluate(x, y),
                MKNoiseType.Humidity => Humidity.Evaluate(x, y),
                MKNoiseType.Ground => Ground.Evaluate(x, y),
                MKNoiseType.ObjectsV1 => ObjectsV1.Evaluate(x,y),
                _ => 0f
            };

            value = (value + 1) / 2f;

            return value;
        }

        private float _currentTemp = 0f;
        private float _currentHum = 0f;

        private Color GetColor(float x, float y, float t)
        {
            //var t = GetNoise(x, y);
            _currentTemp = GetNoise(x, y, Temperature);
            _currentHum = GetNoise(x, y, Humidity);

            CalculateBiomeWeights();

            switch (PreviewType)
            {
                case MKNoiseType.Result:

                    return GetBiomeInfo(x,y,out _, true).Color;

                    break;
                case MKNoiseType.Temp:
                    return Temperature.GetColor(t);
                    break;
                case MKNoiseType.Humidity:
                    return Humidity.GetColor(t);
                    break;
                case MKNoiseType.Ground:
                    return Ground.GetColor(t);
                    break;
                case MKNoiseType.BiomeColor:

                    return GetBiomeInfo(x,y,out _,false).Color;

                    break;
                case MKNoiseType.Height:

                    // var t = GetNoise(ix + gridOrigin.x, iz + gridOrigin.z);

                    // var color = GetColor(ix + gridOrigin.x, iz + gridOrigin.z, t);

                    GetBiomeInfo(x, y, out var newT, true);

                    return Color.Lerp(Color.black, Color.white, newT);

                case MKNoiseType.ObjectsV1:
                    return ObjectsV1.GetColor(t);

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Color.black;
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

        private void Update()
        {
            if(Preview.InvokeGenerate)
                GeneratePreview();
        }
    }

}