using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;

namespace PlayRecorder
{

    [CustomEditor(typeof(RecordingManager), true)]
    public class RecordManagerEditor : Editor
    {
        public class RecordManagerDuplicates
        {
            public string descriptor;
            public List<RecordComponent> components = new List<RecordComponent>();
        }

        List<RecordManagerDuplicates> componentNames = new List<RecordManagerDuplicates>();

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || serializedObject.FindProperty("_recording").boolValue);

            if(GUILayout.Button("Start Recording"))
            {
                ((RecordingManager)serializedObject.targetObject).StartRecording();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !serializedObject.FindProperty("_recording").boolValue);

            if(GUILayout.Button("End Recording"))
            {
                ((RecordingManager)serializedObject.targetObject).StopRecording();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < serializedObject.FindProperty("components").arraySize; i++)
            {
                if (serializedObject.FindProperty("components").GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    serializedObject.FindProperty("components").DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    i--;
                }
            }



            componentNames.Clear();
            for (int i = 0; i < serializedObject.FindProperty("components").arraySize; i++)
            {
                string d = ((RecordComponent)serializedObject.FindProperty("components").GetArrayElementAtIndex(i).objectReferenceValue).descriptor;

                int ind = componentNames.FindIndex(x => x.descriptor == d);

                if(ind != -1)
                {
                    componentNames[ind].components.Add((RecordComponent)serializedObject.FindProperty("components").GetArrayElementAtIndex(i).objectReferenceValue);
                }
                else
                {
                    componentNames.Add(new RecordManagerDuplicates()
                    {
                        descriptor = d,
                        components = new List<RecordComponent>() { (RecordComponent)serializedObject.FindProperty("components").GetArrayElementAtIndex(i).objectReferenceValue }
                    });
                }

            }
            bool duplicates = false;
            for (int i = 0; i < componentNames.Count; i++)
            {
                if(componentNames[i].components.Count > 1 || Regex.Replace(componentNames[i].descriptor, @"\s+", "") == "")
                {
                    duplicates = true;
                    serializedObject.FindProperty("_duplicateItems").boolValue = duplicates;
                }
            }

            if(duplicates)
            {
                EditorGUILayout.HelpBox("You have duplicate or invalid descriptors for some components. Please fix, you will not be able to start recording until all issues are rectified.", MessageType.Error);
                for (int i = 0; i < componentNames.Count; i++)
                {
                    if (componentNames[i].components.Count > 1 || Regex.Replace(componentNames[i].descriptor, @"\s+", "") == "")
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(componentNames[i].descriptor,EditorStyles.helpBox);

                        EditorGUILayout.LabelField("(" + componentNames[i].components.Count + ")", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();
                        for (int j = 0; j < componentNames[i].components.Count; j++)
                        {
                            EditorGUILayout.ObjectField(componentNames[i].components[j], typeof(RecordComponent));
                        }
                    }
                }
            }


            DrawDefaultInspector();
        }

    }
}