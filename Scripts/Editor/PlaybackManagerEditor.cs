using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using PlayRecorder.Tools;

namespace PlayRecorder
{

    [CustomEditor(typeof(PlaybackManager), true)]
    public class PlaybackManagerEditor : Editor
    {

        Vector2 scrollPos;
        bool awaitingFileRefresh = false;

        static string recordedFilesVariable = "_recordedFiles", bindersVariable = "_binders", currentFileVariable = "_currentFile";

        string componentFilter = "";

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginHorizontal();


            serializedObject.FindProperty(recordedFilesVariable).isExpanded = EditorGUILayout.Foldout(serializedObject.FindProperty(recordedFilesVariable).isExpanded, "Recorded Files (" + serializedObject.FindProperty(recordedFilesVariable).arraySize + ")",true,Styles.foldoutBold);


            EditorGUI.BeginDisabledGroup(Application.isPlaying);


            if (GUILayout.Button("+", Styles.miniButton, GUILayout.Width(26)))
            {
                serializedObject.FindProperty(recordedFilesVariable).InsertArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize);
                serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize-1).objectReferenceValue = null;
                serializedObject.FindProperty(recordedFilesVariable).isExpanded = true;
                awaitingFileRefresh = true;
            }

            if (serializedObject.FindProperty("_changingFiles").boolValue)
            {
                GUILayout.Button(new GUIContent("Loading...", "This can take a while to process depending on your system, the number of files, and the recording complexity."), Styles.miniButtonGrey, GUILayout.Width(90));
            }
            else
            {
                if (GUILayout.Button(new GUIContent("Update Files", "This can take a while to process depending on your system, the number of files, and the recording complexity."), awaitingFileRefresh ? Styles.miniButtonBoldRed : Styles.miniButton, GUILayout.Width(90)))
                {
                    ((PlaybackManager)serializedObject.targetObject).ChangeFiles();
                    awaitingFileRefresh = false;
                }
            }
                

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (serializedObject.FindProperty(recordedFilesVariable).isExpanded)
            {
                for (int i = 0; i < serializedObject.FindProperty(recordedFilesVariable).arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue = EditorGUILayout.ObjectField("File " + (i + 1).ToString(), serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue, typeof(TextAsset), false);

                    EditorGUI.BeginDisabledGroup(Application.isPlaying);

                    if (GUILayout.Button("-",Styles.miniButton,GUILayout.Width(26)))
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

            EditorUtils.DrawUILine(Color.grey, 1, 4);

            EditorGUILayout.LabelField("Recorded Components ("+ serializedObject.FindProperty(bindersVariable).arraySize + ")", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            componentFilter = EditorGUILayout.TextField(new GUIContent("Filter Components", "Filter to specific components based upon their descriptor and component type."), componentFilter);
            if(GUILayout.Button("Clear Filter",Styles.miniButton,GUILayout.Width(90)))
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
                        EditorUtils.DrawUILine(Color.grey, 1, 4);
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
            if(GUILayout.Button("Expand All", Styles.miniButton))
            {
                ToggleExpanded(true);
            }
            if(GUILayout.Button("Collapse All", Styles.miniButton))
            {
                ToggleExpanded(false);
            }
            EditorGUILayout.EndHorizontal();

            EditorUtils.DrawUILine(Color.grey, 1, 4);

            EditorGUILayout.LabelField("Playback Parameters", EditorStyles.boldLabel);

            serializedObject.FindProperty("_playbackRate").floatValue = EditorGUILayout.Slider(new GUIContent("Playback Rate", "The rate/speed at which the recordings should play."), serializedObject.FindProperty("_playbackRate").floatValue, 0, 3.0f);
            serializedObject.FindProperty("_scrubWaitTime").floatValue = EditorGUILayout.Slider(new GUIContent("Scrubbing Wait Time", "The amount of time to wait before jumping to a specific point on the timeline."), serializedObject.FindProperty("_scrubWaitTime").floatValue, 0, 1.0f);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || serializedObject.FindProperty(recordedFilesVariable).arraySize == 0);

            EditorGUILayout.LabelField("Current Playback Information", EditorStyles.boldLabel);

            GUIContent playpause = new GUIContent();
            if(serializedObject.FindProperty("_paused").boolValue)
            {
                playpause.text = "Play";
            }
            else
            {
                playpause.text = "Pause";
            }
            if(GUILayout.Button(playpause))
            {
                ((PlaybackManager)serializedObject.targetObject).TogglePlaying();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("_playing").boolValue);
            int value = (int)EditorGUILayout.Slider(new GUIContent("Frame", "The current frame being played back from the current file."), Mathf.Clamp(serializedObject.FindProperty("_currentTickVal").intValue,0, serializedObject.FindProperty("_maxTickVal").intValue), 0, serializedObject.FindProperty("_maxTickVal").intValue);

            if(value != Mathf.Clamp(serializedObject.FindProperty("_currentTickVal").intValue, 0, serializedObject.FindProperty("_maxTickVal").intValue))
            {
                ((PlaybackManager)serializedObject.targetObject).ScrubTick(value);
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Frame Rate: " + serializedObject.FindProperty("_currentFrameRate").intValue.ToString());

            if (serializedObject.FindProperty("_currentFrameRate").intValue > 0)
            {
                EditorGUILayout.LabelField("Time: " + TimeUtil.ConvertToTime((double)serializedObject.FindProperty("_timeCounter").floatValue) + " / " + TimeUtil.ConvertToTime((double)serializedObject.FindProperty("_maxTickVal").intValue / (double)serializedObject.FindProperty("_currentFrameRate").intValue));
            }
            else
            {
                EditorGUILayout.LabelField("Time: 00:00:00 / 00:00:00");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
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