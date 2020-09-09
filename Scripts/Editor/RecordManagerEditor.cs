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

        static string componentString = "_components";

        GUIStyle iconButtonStyle;

        Vector2 scrollPos;

        public class RecordManagerDuplicates
        {
            public string descriptor;
            public List<RecordComponent> components = new List<RecordComponent>();
        }

        List<RecordManagerDuplicates> componentNames = new List<RecordManagerDuplicates>();

        public override void OnInspectorGUI()
        {
            iconButtonStyle = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(2, 2, 2, 2),
                fixedHeight = 20
            };

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || serializedObject.FindProperty("_recording").boolValue);

            if(GUILayout.Button("Start Recording"))
            {
                ((RecordingManager)serializedObject.targetObject).StartRecording();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !serializedObject.FindProperty("_recording").boolValue);

            if(GUILayout.Button("Stop Recording"))
            {
                ((RecordingManager)serializedObject.targetObject).StopRecording();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < serializedObject.FindProperty(componentString).arraySize; i++)
            {
                if (serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    serializedObject.FindProperty(componentString).DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    i--;
                }
            }

            componentNames.Clear();
            for (int i = 0; i < serializedObject.FindProperty(componentString).arraySize; i++)
            {
                string d = ((RecordComponent)serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue).descriptor;

                int ind = componentNames.FindIndex(x => x.descriptor == d);

                if(ind != -1)
                {
                    componentNames[ind].components.Add((RecordComponent)serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue);
                }
                else
                {
                    componentNames.Add(new RecordManagerDuplicates()
                    {
                        descriptor = d,
                        components = new List<RecordComponent>() { (RecordComponent)serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue }
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
                            EditorGUILayout.ObjectField(componentNames[i].components[j], typeof(RecordComponent), true);
                        }
                    }
                }
            }

            DrawDefaultInspector();

            EditorGUILayout.BeginHorizontal();

            if(serializedObject.FindProperty(componentString).isExpanded = EditorGUILayout.Foldout(serializedObject.FindProperty(componentString).isExpanded,new GUIContent("Recording Components ("+ serializedObject.FindProperty(componentString).arraySize+")", "All recording components currently found within the scene."),true))
            {
                RefreshComponentsButton();
                EditorGUILayout.EndHorizontal();

                GUIContent hierarchyButton = EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow");
                hierarchyButton.tooltip = "Ping the object within the scene hierarchy.";

                GUIContent recordLabel = new GUIContent("Record", "Decides whether this component will be used during the next recording. Does not affect playback.");

                GUIStyle s = new GUIStyle(EditorStyles.boldLabel);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
                for (int i = 0; i < serializedObject.FindProperty(componentString).arraySize; i++)
                {
                    RecordComponent r = (RecordComponent)serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue;

                    if (r.required)
                    {
                        s.normal.textColor = Color.black;
                    }
                    else
                    {
                        s.normal.textColor = Color.grey;
                        s.active.textColor = Color.grey;
                        s.hover.textColor = Color.grey;
                    }

                    if (serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        serializedObject.FindProperty(componentString).DeleteArrayElementAtIndex(i);
                        i--;
                        continue;
                    }

                    if(i > 0)
                    {
                        EditorUtils.DrawUILine(Color.grey, 1, 4);
                    }

                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button(hierarchyButton, iconButtonStyle, GUILayout.Width(20)))
                    {
                        EditorGUIUtility.PingObject(serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue);
                    }
                    if(GUILayout.Button(((RecordComponent)serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue).descriptor + " ("+ serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue.name+")", s))
                    {
                        EditorGUIUtility.PingObject(serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue);
                    }

                    if(GUILayout.Button(recordLabel,EditorStyles.label,GUILayout.Width(46)))
                    {
                        r.required = !r.required;
                    }
                    r.required = EditorGUILayout.Toggle(r.required,GUILayout.Width(14));

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.LabelField(new GUIContent("Type: " + serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue.GetType().ToString()));
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                RefreshComponentsButton();
                EditorGUILayout.EndHorizontal();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void RefreshComponentsButton()
        {
            GUIContent rf = EditorGUIUtility.IconContent("Refresh");
            rf.text = "Refresh";
            rf.tooltip = "Finds all the current components within the scene in case automatic additions fail.";
            if (GUILayout.Button(rf,iconButtonStyle,GUILayout.Width(68)))
            {
                ((RecordingManager)serializedObject.targetObject).RefreshComponents();
            }
        }

    }
}