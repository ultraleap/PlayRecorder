using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace PlayRecorder
{

    [CustomEditor(typeof(PlaybackManager), true)]
    public class PlaybackManagerEditor : Editor
    {

        Vector2 scrollPos;
        bool filesVisible = false, awaitingFileRefresh = false;

        static string recordedFilesVariable = "_recordedFiles", bindersVariable = "_binders", currentFileVariable = "_currentFile";

        string componentFilter = "";

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();

            GUIStyle s = new GUIStyle(EditorStyles.foldout);
            s.fontStyle = FontStyle.Bold;

            filesVisible = EditorGUILayout.Foldout(filesVisible, "Recorded Files (" + serializedObject.FindProperty(recordedFilesVariable).arraySize + ")",true,s);

            GUIStyle mb = new GUIStyle(EditorStyles.miniButton);
            mb.fontStyle = FontStyle.Bold;
            mb.fixedHeight = 18;
            GUIStyle mbN = new GUIStyle(EditorStyles.miniButton);
            mbN.fixedHeight = 18;
            mbN.fontStyle = FontStyle.Normal;

            EditorGUI.BeginDisabledGroup(Application.isPlaying);
            

            if (GUILayout.Button("+", mb, GUILayout.Width(26)))
            {
                serializedObject.FindProperty(recordedFilesVariable).InsertArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize);
                serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize-1).objectReferenceValue = null;
                filesVisible = true;
                awaitingFileRefresh = true;
            }

            GUIStyle mbR = new GUIStyle(EditorStyles.miniButton);
            mbR.fixedHeight = 18;
            mbR.fontStyle = FontStyle.Normal;
            if(awaitingFileRefresh)
            {
                mbR.fontStyle = FontStyle.Bold;
                mbR.normal.textColor = Color.red;
            }

            if (GUILayout.Button(new GUIContent("Update Files","This can take a while to process depending on your system, the number of files, and the recording complexity."), mbR, GUILayout.Width(90)))
            {
                ((PlaybackManager)serializedObject.targetObject).ChangeFiles();
                awaitingFileRefresh = false;
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (filesVisible)
            {
                for (int i = 0; i < serializedObject.FindProperty(recordedFilesVariable).arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue = EditorGUILayout.ObjectField("File " + (i + 1).ToString(), serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue, typeof(TextAsset), false);

                    EditorGUI.BeginDisabledGroup(Application.isPlaying);

                    if (GUILayout.Button("-",mb,GUILayout.Width(26)))
                    {
                        serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue = null;
                        serializedObject.FindProperty(recordedFilesVariable).DeleteArrayElementAtIndex(i);
                        awaitingFileRefresh = true;
                    }

                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (serializedObject.FindProperty(currentFileVariable).intValue != -1 && serializedObject.FindProperty(recordedFilesVariable).arraySize != 0)
            {
                if (serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(currentFileVariable).intValue).objectReferenceValue != null)
                {
                    EditorGUILayout.LabelField("Current file: " + ((TextAsset)serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(currentFileVariable).intValue).objectReferenceValue).name);
                }
            }
            
            DrawUILine(Color.grey, 1, 4);

            EditorGUILayout.LabelField("Recorded Components ("+ serializedObject.FindProperty(bindersVariable).arraySize + ")", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            componentFilter = EditorGUILayout.TextField(new GUIContent("Filter Components", "Filter to specific components based upon their descriptor and component type."), componentFilter);
            if(GUILayout.Button("Clear Filter",mbN,GUILayout.Width(90)))
            {
                componentFilter = "";
            }
            EditorGUILayout.EndHorizontal();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos,GUILayout.Height(400));
            int c = 0;
            for (int i = 0; i < serializedObject.FindProperty(bindersVariable).arraySize; i++)
            {
                if (componentFilter == "" || serializedObject.FindProperty(bindersVariable).GetArrayElementAtIndex(i).FindPropertyRelative("descriptor").stringValue.Contains(componentFilter,StringComparison.InvariantCultureIgnoreCase) || serializedObject.FindProperty(bindersVariable).GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Contains(componentFilter,StringComparison.InvariantCultureIgnoreCase))
                {
                    if (c > 0)
                    {
                        DrawUILine(Color.grey, 1, 4);
                    }
                    EditorGUILayout.PropertyField(serializedObject.FindProperty(bindersVariable).GetArrayElementAtIndex(i));
                    c++;
                }
            }
            if(c == 0 && serializedObject.FindProperty(bindersVariable).arraySize != 0)
            {
                EditorGUILayout.LabelField("No component descriptors match your filter term.");
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            if(GUILayout.Button("Expand All", mbN))
            {
                ToggleExpanded(true);
            }
            if(GUILayout.Button("Collapse All", mbN))
            {
                ToggleExpanded(false);
            }
            EditorGUILayout.EndHorizontal();

            DrawUILine(Color.grey, 1, 4);

            EditorGUILayout.LabelField("Playback Parameters", EditorStyles.boldLabel);

            serializedObject.FindProperty("_playbackRate").floatValue = EditorGUILayout.Slider(new GUIContent("Playback Rate", "The rate/speed at which the recordings should play."), serializedObject.FindProperty("_playbackRate").floatValue, 0, 3.0f);
            serializedObject.FindProperty("_scrubWaitTime").floatValue = EditorGUILayout.Slider(new GUIContent("Scrubbing Wait Time", "The amount of time to wait before jumping to a specific point on the timeline."), serializedObject.FindProperty("_scrubWaitTime").floatValue, 0, 1.0f);

            serializedObject.ApplyModifiedProperties();
        }

        public void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        void ToggleExpanded(bool expand)
        {
            for (int i = 0; i < serializedObject.FindProperty("_binders").arraySize; i++)
            {
                serializedObject.FindProperty("_binders").GetArrayElementAtIndex(i).isExpanded = expand;
            }
        }
    }

}