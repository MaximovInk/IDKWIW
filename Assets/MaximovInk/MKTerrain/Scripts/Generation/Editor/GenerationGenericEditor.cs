using System;
using MaximovInk;
using UnityEditor;
using UnityEngine;

public class GenerationGenericEditor : EditorWindow
{
    private GenerationGeneric _generation;

    [MenuItem("MaximovInk/TerrainGeneration")]
    public static void ShowExample()
    {
        GenerationGenericEditor wnd = GetWindow<GenerationGenericEditor>();
        wnd.titleContent = new GUIContent("Generation");
    }

    private void OnGUI()
    {
        _generation = EditorGUILayout.ObjectField("Target", _generation, typeof(GenerationGeneric), allowSceneObjects:true) as GenerationGeneric;

        if (_generation == null) return;

        GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        GUILayout.EndVertical();

        var rect = GUILayoutUtility.GetLastRect();

        EditorGUI.DrawPreviewTexture(rect, _generation.TerrainGeneration.Result);

        
    }

    private void OnInspectorUpdate()
    {
        if (_generation == null) return;

        if (!_generation.TerrainGeneration.Painted)
        {
            Repaint();

            _generation.TerrainGeneration.Painted = true;
        }
    }
}
