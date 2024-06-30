using UnityEditor;
using UnityEngine;

namespace MaximovInk
{
    
[CustomEditor(typeof(MK3DAudioSource))]
public class MK3DAudioSourceEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Play"))
        {
            (target as MK3DAudioSource)?.Play();
        }

        base.OnInspectorGUI();
    }
}
}
