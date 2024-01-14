using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Spine.Unity.Editor
{
    [CustomEditor(typeof(VendorsGenerator))]
    public class VendorsGeneratorInspector : UnityEditor.Editor
    {
        readonly GUIContent SpawnGeometryButtonLabel = new GUIContent("Spawn Geometry");

        VendorsGenerator generator;

        private void OnEnable()
        {
            generator = (VendorsGenerator)target;
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("geoJson"), SpineInspectorUtility.TempContent("Geometry Json"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("verticesRotation"), SpineInspectorUtility.TempContent("Vertex Rotation"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("verticesScale"), SpineInspectorUtility.TempContent("Vertex Scale"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("flipNormal"), SpineInspectorUtility.TempContent("Flip Normal"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("GlobalScale"), SpineInspectorUtility.TempContent("Global Scale"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("animJsonList"), SpineInspectorUtility.TempContent("Animation File List"));

            if (SpineInspectorUtility.LargeCenteredButton(SpawnGeometryButtonLabel))
                generator.SpawnHierachy();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
