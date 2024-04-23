using System;
using System.Collections;
using UnityEngine;

namespace MaximovInk
{
    public static class MKUtils
    {
        public static void Invoke(this MonoBehaviour mb, Action f, float delay)
        {
            mb.StartCoroutine(InvokeRoutine(f, delay));
        }

        private static IEnumerator InvokeRoutine(System.Action f, float delay)
        {
            yield return new WaitForSeconds(delay);
            f();
        }

        public static void InvokeAfterFrame(this MonoBehaviour mb, Action f)
        {
            mb.StartCoroutine(InvokeAfterFrameRoutine(f));
        }

        private static IEnumerator InvokeAfterFrameRoutine(System.Action f)
        {
            yield return new WaitForFixedUpdate();
            f();
        }

        public static void DestroyAllChildren(Transform target)
        {
            for (int i = target.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(target.GetChild(i).gameObject);
            }
        }
    }
}
