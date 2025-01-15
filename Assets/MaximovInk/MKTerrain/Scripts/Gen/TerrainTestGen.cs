using MaximovInk.VoxelEngine;
using Unity.Mathematics;
using UnityEngine;

namespace MaximovInk
{
    public class TerrainTestGen : MonoBehaviour
    {
        [SerializeField] private VoxelTerrain _terrain;

        [SerializeField] private Vector3 _startZoneStart;
        [SerializeField] private Color _startZoneColor;

        private void Start()
        {
            for (var ix = -_startZoneStart.x; ix < _startZoneStart.x; ix++)
            {
                for (var iy = _startZoneStart.y; iy < _startZoneStart.y; iy++)
                {
                    for (var iz = -_startZoneStart.z; iz < _startZoneStart.z; iz++)
                    {
                        var pos = new int3((int)ix, (int)iy, (int)iz);

                        _terrain.SetBlock("Grass", pos);
                        _terrain.SetValue(1, pos);
                        _terrain.SetColor(_startZoneColor, pos);
                    }
                }
            }
            
        }
    }
}
