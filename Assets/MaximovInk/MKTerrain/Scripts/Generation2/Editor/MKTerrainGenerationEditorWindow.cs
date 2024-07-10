using System;
using MaximovInk;
using UnityEditor;
using UnityEngine;

namespace MaximovInk.VoxelEngine
{

    public class MKTerrainGenerationEditorWindow : EditorWindow
    {
        private MKTerrainGeneration _generation;

        [MenuItem("MaximovInk/TerrainGeneration2")]
        public static void Show()
        {
            MKTerrainGenerationEditorWindow wnd = GetWindow<MKTerrainGenerationEditorWindow>();
            wnd.titleContent = new GUIContent("Generation");
        }

        private void OnGUI()
        {
            _generation =
                EditorGUILayout.ObjectField("Target", _generation, typeof(MKTerrainGeneration), allowSceneObjects: true)
                    as MKTerrainGeneration;

            if (_generation == null) return;

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            GUILayout.EndVertical();

            var rect = GUILayoutUtility.GetLastRect();

            if (_generation.Preview.Texture != null)
                EditorGUI.DrawPreviewTexture(rect, _generation.Preview.Texture);


        }

        private void OnInspectorUpdate()
        {
            if (_generation == null) return;

            if (_generation.Preview.InvokeRepaint)
            {
                Repaint();

                _generation.Preview.InvokeRepaint = false;
            }
        }
    }
}