using Unity.VisualScripting;
using UnityEngine;

namespace MaximovInk
{
    public class MK3DAudioSource : MonoBehaviour
    {
        public MK3DAudioClip Clip;

        [Range(0,1)]
        public float MasterValue = 1f;

        private void PlayClip(MKAudioClipInfo info, float distance)
        {
            var vol = 0f;

            if (info.Distance < 1)
                info.Distance = 1;


            if(distance > info.Distance + info.Range)
                distance = info.Distance + info.Range;

            if(distance < info.Distance - info.Range)
                distance = info.Distance - info.Range;

            if (distance > info.Distance)
            {
                distance -= info.Distance;

                vol = 1f - distance / info.Range;

            }

            if (distance < info.Distance)
            {
                distance -= (info.Distance - info.Range);

                vol = distance / info.Range;
            }

            info.Distance = Mathf.Clamp(info.Distance, 0.01f, 9999f);

            MK3DAudioPool.Instance.PlayAudioSource(new AudioPlayInfo()
            {
                volume = vol * info.MasterVolume,
                pitch = 1f,
                position = transform.position,
                data = info
            });
        }

        public void Play()
        {
            if (Camera.main == null) return;

            var distance = Vector3.Distance(Camera.main.transform.position, transform.position);

          

            if (distance < 1)
            {
                distance = 1;
            }

            MK3DAudioPool.Instance.LowerAllPriority();

            foreach (var t in Clip.MKAudioClipInfo)
            {
                PlayClip(t, distance);

                /*
                 var info = t;

                if (info.Distance < 1)
                    info.Distance = 1;

                var currentDistance = distance;

                var infoDistance = Mathf.Clamp(info.Distance, 0.01f, 9999f);
                 */



                //var distanceVol = Mathf.Abs( currentDistance / infoDistance);



                /*
                 if (currentDistance > infoDistance)
                {
                    distanceVol = Mathf.Abs( infoDistance / currentDistance);
                }

                distanceVol = Mathf.Clamp01(distanceVol);

                distanceVol = info.VolumeCurve.Evaluate(distanceVol) * MasterValue;


                distanceVol = Mathf.Clamp(distanceVol, 0, MasterValue);

                Debug.Log(distanceVol);

                MK3DAudioPool.Instance.PlayAudioSource(new AudioPlayInfo()
                {
                    volume = distanceVol,
                    pitch = 1f,
                    position = transform.position,
                    data = info
                });
                 */
            }
        }

    }
}