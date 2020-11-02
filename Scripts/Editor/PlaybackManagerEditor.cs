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
        
        Vector2 fileScrollPos,compopentScrollPos;

        static string recordedFilesVariable = "_recordedFiles", loadedFilesVariables = "_loadedRecordedFiles", dataCacheVariable = "_dataCacheNames", bindersVariable = "_binders", currentFileVariable = "_currentFile", awaitingRefreshVariable = "_awaitingFileRefresh";
        static string playingVariable = "_playing", changingFilesVariable = "_changingFiles";

        static string updateFilesDescription = "This can take a while to process depending on your system, the number of files, and the recording complexity. You may find certain features or options inaccessible until this button is pressed.";

        string componentFilter = "";

        int _recordedFilesArraySize = -1;



        GUIContent recordFoldoutGUI = new GUIContent("", "You can drag files onto this header to add them to the files list."),
            loadingButtonGUI = new GUIContent("Loading...", updateFilesDescription),
            updateFilesGUI = new GUIContent("Update Files", updateFilesDescription),
            filterComponentsGUI = new GUIContent("Filter Components", "Filter to specific components based upon their descriptor and component type."),
            playbackRateGUI = new GUIContent("Playback Rate", "The rate/speed at which the recordings should play."),
            scrubWaitGUI = new GUIContent("Scrubbing Wait Time", "The amount of time to wait before jumping to a specific point on the timeline.");


        private void Awake()
        {
            for (int i = 0; i < serializedObject.FindProperty(recordedFilesVariable).arraySize; i++)
            {
                if (serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    serializedObject.FindProperty(recordedFilesVariable).DeleteArrayElementAtIndex(i);
                }
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        public override void OnInspectorGUI()
        {

            EditorGUI.BeginChangeCheck();

            PlaybackLocker.SetPlayModeEnabled(!serializedObject.FindProperty(changingFilesVariable).boolValue && !serializedObject.FindProperty(awaitingRefreshVariable).boolValue);

            EditorGUI.BeginDisabledGroup(serializedObject.FindProperty(changingFilesVariable).boolValue);

            //EditorGUI.DrawRect(dragRect,Color.red);

            RecordedFiles();

            EditorUtils.DrawUILine(Color.grey, 1, 4);
            
            Playlists();

            EditorUtils.DrawUILine(Color.grey, 1, 4);

            RecordComponents();

            EditorUtils.DrawUILine(Color.grey, 1, 4);

            PlaybackControls();

            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        void ToggleExpanded(bool expand)
        {
            for (int i = 0; i < serializedObject.FindProperty("_binders").arraySize; i++)
            {
                serializedObject.FindProperty("_binders").GetArrayElementAtIndex(i).isExpanded = expand;
            }
        }

        void RecordedFiles()
        {
            EditorGUILayout.BeginHorizontal();

            Rect dragRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));

            recordFoldoutGUI.text = "Recorded Files (" + serializedObject.FindProperty(recordedFilesVariable).arraySize + ")";

            serializedObject.FindProperty(recordedFilesVariable).isExpanded = EditorGUI.Foldout(dragRect, serializedObject.FindProperty(recordedFilesVariable).isExpanded, recordFoldoutGUI, true, Styles.foldoutBold);

            if (dragRect.Contains(Event.current.mousePosition) && !serializedObject.FindProperty(changingFilesVariable).boolValue)
            {
                if (Event.current.type == EventType.DragUpdated)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
                else if (Event.current.type == EventType.DragPerform)
                {
                    int dragged = 0;
                    for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                    {
                        if (DragAndDrop.objectReferences[i].GetType() == typeof(TextAsset))
                        {
                            serializedObject.FindProperty(recordedFilesVariable).InsertArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize);
                            serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize - 1).objectReferenceValue = DragAndDrop.objectReferences[i];
                            dragged++;
                        }
                    }

                    Event.current.Use();

                    if (dragged > 0)
                    {
                        serializedObject.FindProperty(recordedFilesVariable).isExpanded = true;
                        serializedObject.FindProperty(awaitingRefreshVariable).boolValue = true;
                        serializedObject.ApplyModifiedProperties();
                        ((PlaybackManager)serializedObject.targetObject).RemoveDuplicateFiles();
                        GUIUtility.ExitGUI();
                    }

                }
            }

            if (serializedObject.FindProperty(changingFilesVariable).boolValue)
            {
                Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, (float)((PlaybackManager)serializedObject.targetObject).dataCacheCount / serializedObject.FindProperty(recordedFilesVariable).arraySize, "Parsing files...");
            }

            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            if (GUILayout.Button("+", Styles.miniButton, GUILayout.Width(26)))
            {
                serializedObject.FindProperty(recordedFilesVariable).InsertArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize);
                serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(recordedFilesVariable).arraySize - 1).objectReferenceValue = null;
                serializedObject.FindProperty(recordedFilesVariable).isExpanded = true;
                serializedObject.FindProperty(awaitingRefreshVariable).boolValue = true;
            }

            if (serializedObject.FindProperty("_changingFiles").boolValue)
            {
                GUILayout.Button(loadingButtonGUI, Styles.miniButtonGrey, GUILayout.Width(90));
            }
            else
            {
                if (GUILayout.Button(updateFilesGUI, serializedObject.FindProperty(awaitingRefreshVariable).boolValue ? Styles.miniButtonBoldRed : Styles.miniButton, GUILayout.Width(90)))
                {
                    serializedObject.FindProperty(awaitingRefreshVariable).boolValue = false;
                    serializedObject.ApplyModifiedProperties();
                    ((PlaybackManager)serializedObject.targetObject).ChangeFiles();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            if (serializedObject.FindProperty(recordedFilesVariable).isExpanded)
            {

                fileScrollPos = EditorGUILayout.BeginScrollView(fileScrollPos, GUILayout.Height(200));
                for (int i = 0; i < serializedObject.FindProperty(recordedFilesVariable).arraySize; i++)
                {
                    if (!(fileScrollPos.y - 40 <= (20 * (i - 1)) && fileScrollPos.y + 200 > (20 * i)))
                    {
                        EditorGUILayout.LabelField("");
                        continue;
                    }

                    if (serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        serializedObject.FindProperty(awaitingRefreshVariable).boolValue = true;
                    }

                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.BeginDisabledGroup(serializedObject.FindProperty(awaitingRefreshVariable).boolValue);

                    if (GUILayout.Button(new GUIContent(serializedObject.FindProperty(currentFileVariable).intValue == i ? "▶" + (i + 1).ToString() : (i + 1).ToString(), "Change the currently selected file to this file."), GUILayout.Width(32)))
                    {
                        ((PlaybackManager)serializedObject.targetObject).ChangeCurrentFile(i);
                        GUIUtility.ExitGUI();
                    }

                    EditorGUI.EndDisabledGroup();

                    string recName = "";
                    if (serializedObject.FindProperty(dataCacheVariable).arraySize > i)
                    {
                        recName = serializedObject.FindProperty(dataCacheVariable).GetArrayElementAtIndex(i).stringValue;
                    }

                    serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue = EditorGUILayout.ObjectField(recName.Length > 0 ? recName : (i + 1).ToString(), serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue, typeof(TextAsset), false);

                    EditorGUI.BeginDisabledGroup(Application.isPlaying);

                    if (GUILayout.Button("-", Styles.miniButton, GUILayout.Width(26)))
                    {
                        serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue = null;
                        serializedObject.FindProperty(recordedFilesVariable).DeleteArrayElementAtIndex(i);
                        serializedObject.FindProperty(awaitingRefreshVariable).boolValue = true;
                        if (serializedObject.FindProperty(currentFileVariable).intValue >= serializedObject.FindProperty(recordedFilesVariable).arraySize)
                        {
                            serializedObject.FindProperty(currentFileVariable).intValue = serializedObject.FindProperty(recordedFilesVariable).arraySize - 1;
                        }
                    }

                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }


            EditorGUILayout.BeginHorizontal();
            if (serializedObject.FindProperty(currentFileVariable).intValue != -1 && serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(currentFileVariable).intValue).objectReferenceValue != null)
            {
                EditorGUILayout.LabelField("Current file: (" + (serializedObject.FindProperty(currentFileVariable).intValue + 1) + ") " + ((TextAsset)serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(serializedObject.FindProperty(currentFileVariable).intValue).objectReferenceValue).name);
            }
            else
            {
                EditorGUILayout.LabelField("No files loaded");
            }

            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty(awaitingRefreshVariable).boolValue || Application.isPlaying);

            if (GUILayout.Button("Revert Changes", Styles.miniButton, GUILayout.Width(110)))
            {
                ((PlaybackManager)serializedObject.targetObject).RevertFileChanges();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(serializedObject.FindProperty(recordedFilesVariable).arraySize == 0 || Application.isPlaying);

            if (GUILayout.Button("Clear Files", Styles.miniButton, GUILayout.Width(90)))
            {
                serializedObject.FindProperty(recordedFilesVariable).ClearArray();
                serializedObject.FindProperty(awaitingRefreshVariable).boolValue = true;
                serializedObject.FindProperty(currentFileVariable).intValue = -1;
                serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        void Playlists()
        {
            EditorGUILayout.LabelField(new GUIContent("Playlist"), Styles.textBold);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(serializedObject.FindProperty(recordedFilesVariable).arraySize == 0 || serializedObject.FindProperty(awaitingRefreshVariable).boolValue);

            if (GUILayout.Button(new GUIContent("Save Playlist", serializedObject.FindProperty(awaitingRefreshVariable).boolValue ? "Please make sure to press Update Files before trying to save a playlist." : "Save the current set of files into a playlist.")))
            {
                var path = EditorUtility.SaveFilePanel("Save Playlist", "", "playlist.json", "json");
                if (path.Length != 0)
                {
                    List<PlaylistItem> playlist = new List<PlaylistItem>();
                    for (int i = 0; i < serializedObject.FindProperty(recordedFilesVariable).arraySize; i++)
                    {
                        TextAsset t = (TextAsset)serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue;
                        if(t == null)
                        {
                            continue;
                        }
                        string guid = "";
                        long local = 0;
                        if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(t, out guid, out local))
                        {
                            playlist.Add(new PlaylistItem(t.name, AssetDatabase.GUIDToAssetPath(guid), guid));
                        }
                    }
                    FileUtil.SavePlaylist(path, playlist);
                    EditorUtility.DisplayDialog("Saved", "Play list saved to " + path, "Ok");
                }
            }

            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button(new GUIContent("Load Playlist","Loading a playlist will overwrite your current set of loaded files. Playlist order is determined on how files are found, not the order they are present within the file.")))
            {
                var path = EditorUtility.OpenFilePanel("Load Playlist", "", "json");
                if(path.Length != 0)
                {
                    try
                    {
                        List<PlaylistItem> playlist = FileUtil.LoadPlaylist(System.IO.File.ReadAllBytes(path));
                        List<TextAsset> textAssets = new List<TextAsset>();

                        string temp;
                        for (int i = 0; i < playlist.Count; i++)
                        {
                            temp = AssetDatabase.GUIDToAssetPath(playlist[i].guid.ToString());
                            if(temp.Contains(playlist[i].name + ".bytes"))
                            {
                                textAssets.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(temp));
                                playlist.RemoveAt(i);
                                i--;
                            }
                        }
                        if (playlist.Count > 0)
                        {
                            string[] assets = AssetDatabase.FindAssets("t:TextAsset");
                            for (int i = 0; i < assets.Length; i++)
                            {
                                temp = AssetDatabase.GUIDToAssetPath(assets[i]);
                                if(temp.EndsWith("bytes"))
                                {
                                    for (int j = 0; j < playlist.Count; j++)
                                    {
                                        if(temp.Contains(playlist[j].name))
                                        {
                                            textAssets.Add(AssetDatabase.LoadAssetAtPath<TextAsset>(temp));
                                            playlist.RemoveAt(j);
                                            break;
                                        }
                                    }
                                }
                                if(playlist.Count == 0)
                                {
                                    break;
                                }
                            }
                        }

                        serializedObject.FindProperty(recordedFilesVariable).ClearArray();

                        for (int i = 0; i < textAssets.Count; i++)
                        {
                            serializedObject.FindProperty(recordedFilesVariable).InsertArrayElementAtIndex(i);
                            serializedObject.FindProperty(recordedFilesVariable).GetArrayElementAtIndex(i).objectReferenceValue = textAssets[i];
                        }

                        serializedObject.FindProperty(currentFileVariable).intValue = 0;
                        serializedObject.ApplyModifiedProperties();

                        EditorUtility.DisplayDialog("Loaded", "Playlist loaded. " + textAssets.Count + " file(s) set.", "Ok");
                        ((PlaybackManager)serializedObject.targetObject).ChangeFiles();
                        serializedObject.FindProperty(awaitingRefreshVariable).boolValue = false;

                    }
                    catch
                    {
                        EditorUtility.DisplayDialog("Error", "Invalid playlist file loaded.", "Ok");
                    }

                }
            }

            EditorGUILayout.EndHorizontal();
        }

        void RecordComponents()
        {
            int gC = 0, rC = 0;

            for (int i = 0; i < serializedObject.FindProperty(bindersVariable).arraySize; i++)
            {
                if (serializedObject.FindProperty(bindersVariable).GetArrayElementAtIndex(i).FindPropertyRelative("recordComponent").objectReferenceValue == null)
                {
                    rC++;
                }
                else
                {
                    gC++;
                }
            }

            GUIContent recordComponents = new GUIContent("Recorded Components - " + serializedObject.FindProperty(bindersVariable).arraySize + " / <size=20><color=#00B200>■</color></size> " + gC + " / <size=20><color=#D90000>■</color></size> " + rC, "The green cube signifies how many components are correctly assigned, while red is the number that are not currently assigned.");

            serializedObject.FindProperty(bindersVariable).isExpanded = EditorGUILayout.Foldout(serializedObject.FindProperty(bindersVariable).isExpanded, recordComponents, true, Styles.foldoutBold);

            if (serializedObject.FindProperty(bindersVariable).isExpanded)
            {
                EditorGUILayout.BeginHorizontal();
                componentFilter = EditorGUILayout.TextField(filterComponentsGUI, componentFilter);
                if (GUILayout.Button("Clear Filter", Styles.miniButton, GUILayout.Width(90)))
                {
                    componentFilter = "";
                }
                EditorGUILayout.EndHorizontal();
                compopentScrollPos = EditorGUILayout.BeginScrollView(compopentScrollPos, GUILayout.Height(400));
                int c = 0;
                for (int i = 0; i < serializedObject.FindProperty(bindersVariable).arraySize; i++)
                {
                    if (componentFilter == "" || serializedObject.FindProperty(bindersVariable).GetArrayElementAtIndex(i).FindPropertyRelative("descriptor").stringValue.Contains(componentFilter, StringComparison.InvariantCultureIgnoreCase) || serializedObject.FindProperty(bindersVariable).GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Contains(componentFilter, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (c > 0)
                        {
                            EditorUtils.DrawUILine(Color.grey, 1, 4);
                        }
                        EditorGUILayout.PropertyField(serializedObject.FindProperty(bindersVariable).GetArrayElementAtIndex(i));
                        c++;
                    }
                }
                if (c == 0 && serializedObject.FindProperty(bindersVariable).arraySize != 0)
                {
                    EditorGUILayout.LabelField("No component descriptors match your filter term.");
                }
                EditorGUILayout.EndScrollView();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Expand All", Styles.miniButton))
                {
                    ToggleExpanded(true);
                }
                if (GUILayout.Button("Collapse All", Styles.miniButton))
                {
                    ToggleExpanded(false);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        void PlaybackControls()
        {
            EditorGUILayout.LabelField("Playback Parameters", EditorStyles.boldLabel);

            serializedObject.FindProperty("_playbackRate").floatValue = EditorGUILayout.Slider(playbackRateGUI, serializedObject.FindProperty("_playbackRate").floatValue, 0, 3.0f);
            serializedObject.FindProperty("_scrubWaitTime").floatValue = EditorGUILayout.Slider(scrubWaitGUI, serializedObject.FindProperty("_scrubWaitTime").floatValue, 0, 1.0f);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || serializedObject.FindProperty(recordedFilesVariable).arraySize == 0);

            EditorGUILayout.LabelField("Current Playback Information", EditorStyles.boldLabel);

            GUIContent playpause = new GUIContent();
            if (serializedObject.FindProperty("_paused").boolValue)
            {
                playpause.text = "Play";
            }
            else
            {
                playpause.text = "Pause";
            }
            if (GUILayout.Button(playpause))
            {
                ((PlaybackManager)serializedObject.targetObject).TogglePlaying();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!serializedObject.FindProperty("_playing").boolValue);
            int value = (int)EditorGUILayout.Slider(new GUIContent("Frame", "The current frame being played back from the current file."), Mathf.Clamp(serializedObject.FindProperty("_currentTickVal").intValue, 0, serializedObject.FindProperty("_maxTickVal").intValue), 0, serializedObject.FindProperty("_maxTickVal").intValue);

            if (value != Mathf.Clamp(serializedObject.FindProperty("_currentTickVal").intValue, 0, serializedObject.FindProperty("_maxTickVal").intValue))
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
        }

        
    }

}