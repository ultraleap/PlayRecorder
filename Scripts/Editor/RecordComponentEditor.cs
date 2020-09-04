using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace PlayRecorder
{

    [CustomEditor(typeof(RecordComponent), true)]
    public class RecordComponentEditor : Editor
    {

        bool descriptorCheck = true, isUnique = true;
        string currentDescriptor = "";

        RecordingManager manager = null;

        public override void OnInspectorGUI()
        {
            currentDescriptor = serializedObject.FindProperty("_descriptor").stringValue;

            if (manager == null)
            {
                manager = FindObjectOfType<RecordingManager>();
                manager.AddComponent((RecordComponent)serializedObject.targetObject);
            }

            if(descriptorCheck && manager != null)
            {
                descriptorCheck = false;
                isUnique = manager.CheckUniqueDescriptor((RecordComponent)serializedObject.targetObject);
            }

            GUIStyle errorStyle = new GUIStyle(EditorStyles.boldLabel);
            errorStyle.normal.textColor = new Color(255f*0.8f, 0, 0);
            if(manager == null)
            {
                EditorGUILayout.LabelField("No recording manager in scene. Please add one.");
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("_descriptor"), new GUIContent("Descriptor", "This needs to be unique."));

            if(!isUnique || Regex.Replace(serializedObject.FindProperty("_descriptor").stringValue, @"\s+", "") == "")
            {
                EditorGUILayout.LabelField("Current descriptor is not unique. Please change this to make it unique.",errorStyle);
            }

            serializedObject.FindProperty("_required").boolValue = EditorGUILayout.Toggle(new GUIContent("Required for Recording", "Decides whether this component will be used during the next recording. Does not affect playback."), serializedObject.FindProperty("_required").boolValue);

            serializedObject.ApplyModifiedProperties();
            DrawDefaultInspector();
            if(currentDescriptor != serializedObject.FindProperty("_descriptor").stringValue)
            {
                descriptorCheck = true;
            }
        }
    }
}
