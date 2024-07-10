using System.Collections.Generic;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{
    public class GrassModule : MonoBehaviour
    {
        //private ModelGrass _grass;

        private Grass _grass;

        List<Vector3> pointCache = new List<Vector3>();

        private void Awake()
        {
             _grass = GetComponent<Grass>();
            _grass.scale = new Vector3(2, 2, 2);
            
            if (_grass == null)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize(HeightMapData heightMap)
        {
   if(_grass == null) _grass = GetComponent<Grass>();


            _grass.InstancePointData = new InstancePointData();
            

            var step = VoxelTerrain.BlockSize;
            var stepH = VoxelTerrain.ChunkSize;


            _grass.InititializeNEW();

            var offset = step - step * (25 / 255f);

            for (int i = 0; i < heightMap.data.GetLength(0); i++)
            {
                for (int j = 0; j < heightMap.data.GetLength(1); j++)
                {
                    var h = heightMap.data[i, j] * step;

                    if (Icaria.Engine.Procedural.IcariaNoise.GradientNoise((i + transform.position.x)/250f, (j + transform.position.z)/ 250f, 0) > 0.25f) continue;

                    //var point = transform.position + new Vector3(step * i, h * stepH, step * j);

                    var pointOffset = new Vector3(Random.Range(-1, 1) * step, 0, Random.Range(-1, 1) * step);

                    var point = transform.position + new Vector3(step * i, h * stepH - offset, step * j) + pointOffset;

                    _grass.InstancePointData.GetPointsAdjacentChunks(point, ref pointCache);

                   // if (Random.Range(0, 1f) > 0.5f) continue;

                    _grass.InstancePointData.AddPointsToChunk(
                               PoissonDiscSampling.GeneratePointsInDisc(pointCache, point, 3f)
                           );
                }
            }
 

            _grass.UpdateInstancePointsBuffer();

           // Debug.Log(_grass.InstancePointData.TotalPointAmount);
          
        }

       
    }
}
