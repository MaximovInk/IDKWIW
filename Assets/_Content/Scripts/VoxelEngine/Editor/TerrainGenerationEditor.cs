using System.Collections;
using System.Collections.Generic;
using MaximovInk.VoxelEngine;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainGeneration))]
public class TerrainGenerationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("Generate"))
        {
            (target as TerrainGeneration)?.Generate();
        }

        base.OnInspectorGUI();

    }
}
