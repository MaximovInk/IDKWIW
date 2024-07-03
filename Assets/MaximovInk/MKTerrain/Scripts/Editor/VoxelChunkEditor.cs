using System.Collections;
using System.Collections.Generic;
using MaximovInk.VoxelEngine;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxelChunk))]
public class VoxelChunkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Generate"))
        {
            (target as VoxelChunk)?.SetIsDirty();
        }


        base.OnInspectorGUI();
    }
}
