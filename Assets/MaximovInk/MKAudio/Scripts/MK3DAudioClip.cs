using UnityEngine;
using UnityEngine.Serialization;

namespace MaximovInk
{
    [System.Serializable]
    public struct MKAudioClipInfo
    {
        public AudioClip Source;
        public float Distance;
        public AnimationCurve VolumeCurve;
        public float SpatialBlend;
        public float MasterVolume;
        public float Range;
    }

    [CreateAssetMenu(fileName = "MKAudioClip", menuName = "MaximovInk/3DAudioClip")]
    public class MK3DAudioClip : ScriptableObject
    {
        public MKAudioClipInfo[] MKAudioClipInfo;


    }
}
