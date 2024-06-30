using UnityEngine;
using UnityEngine.Serialization;

namespace MaximovInk
{

    [System.Serializable]
    public struct AudioPlayInfo
    {
        public float volume;
        public float pitch;
        public Vector3 position;

        [FormerlySerializedAs("infoData")] public MKAudioClipInfo data;

    }

    public class MK3DAudioPool : MonoBehaviourSingletonAuto<MK3DAudioPool>
    {
        [SerializeField] private int _poolInitialSize = 100;

        private AudioSource[] _audioSources;

        private void Awake()
        {
            InitPool();
        }

        private void InitPool()
        {
            MKUtils.DestroyAllChildren(transform);

            _audioSources = new AudioSource[_poolInitialSize];

            for (int i = 0; i < _poolInitialSize; i++)
            {
                var go = new GameObject($"AudioSource[{i}] pooled");

                go.transform.SetParent(transform);
                go.gameObject.SetActive(false);

                _audioSources[i] = go.AddComponent<AudioSource>();

                _audioSources[i].playOnAwake = false;
                _audioSources[i].loop = false;
            }

        }

        private int firstCounter = 0;

        private AudioSource GetUnused()
        {
            var first = _audioSources[firstCounter];

            for (int i = 0; i < _audioSources.Length; i++)
            {
                if (_audioSources[i].gameObject.activeSelf)continue;

                return _audioSources[i];
            }

            firstCounter++;

            if (firstCounter >= _poolInitialSize)
                firstCounter = 0;

            return first;
        }

        public void LowerAllPriority()
        {
            for (int i = 0; i < _audioSources.Length; i++)
            {
                _audioSources[i].priority++;
            }
        }

        public void PlayAudioSource(AudioPlayInfo info)
        {
            var source = GetUnused();
            source.gameObject.SetActive(true);
            source.clip = info.data.Source;
            source.volume = info.volume * info.data.MasterVolume;
            source.pitch = info.pitch;
            source.spatialBlend = info.data.SpatialBlend;

            source.priority = 128;

            source.Play();

            if (info.data.Source != null)
            {
                var length = info.data.Source.length;

                this.Invoke(() =>
                {
                    source.clip = null;
                    source.gameObject.SetActive(false);

                }, length);
            }
           
        }

    }
}
