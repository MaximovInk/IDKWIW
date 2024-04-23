using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Serialization;

namespace MaximovInk
{
    public class FPSCounter : MonoBehaviour
    {
        private const float UPDATE_INTERVAL = 0.2f;

        private StringBuilder _text;
        [FormerlySerializedAs("text")] public TextMeshProUGUI _textMesh;

        private float _lastInterval;
        private float _frames = 0;

        private float _framesavtick = 0;
        private float _framesav = 0.0f;

        private void Start()
        {
            _lastInterval = Time.realtimeSinceStartup;
            _frames = 0;
            _framesav = 0;
            _text = new StringBuilder();
            _text.Capacity = 200;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            /*
             _text.AppendFormat("\nOS: {0}(RAM: {1} mb)\nGPU: {2}({3} mb)\nCPU: {4}({6}x{5} MHz)",
                SystemInfo.operatingSystem,
                SystemInfo.systemMemorySize,
                SystemInfo.graphicsDeviceName,
                SystemInfo.graphicsMemorySize,
                SystemInfo.processorType,
                SystemInfo.processorFrequency,
                SystemInfo.processorCount
            );
             */
        }

        private void Update()
        {
            ++_frames;

            var timeNow = Time.realtimeSinceStartup;

            if (!(timeNow > _lastInterval + UPDATE_INTERVAL)) return;

            var fps = _frames / (timeNow - _lastInterval);
            var ms = 1000.0f / Mathf.Max(fps, 0.00001f);

            ++_framesavtick;
            _framesav += fps;
            var fpsav = _framesav / _framesavtick;

            _text.Length = 0;

            _text.
                AppendFormat("Time: {0,0:F1} ms\nFPS current {1,0:F1}\nFPS average {2,0:F1}\n", ms, fps, fpsav)
                .AppendFormat("\nRAM usage: {0} mb\n",
                    Profiler.usedHeapSizeLong / 1048576);
            

            _textMesh.text = _text.ToString();
            _frames = 0;
            _lastInterval = timeNow;
        }
    }
}