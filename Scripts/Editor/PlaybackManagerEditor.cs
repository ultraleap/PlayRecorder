using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using PlayRecorder.Tools;

namespace PlayRecorder
{

    [CustomEditor(typeof(PlaybackManager), true)]
    public class PlaybackManagerEditor : Editor
    {

        private Vector2 _fileScrollPos, _componentScrollPos;

        private SerializedProperty _recordedFiles;
        private SerializedProperty _dataCache;
        private SerializedProperty _binders;
        private SerializedProperty _currentFile;
        private SerializedProperty _awaitingRefresh;
        private SerializedProperty _oldFileIndex;
        private SerializedProperty _playing;
        private SerializedProperty _changingFiles;
        private SerializedProperty _playbackRate;
        private SerializedProperty _scrubWait;
        private SerializedProperty _paused;
        private SerializedProperty _currentTick;
        private SerializedProperty _maxTick;
        private SerializedProperty _currentFrameRate;
        private SerializedProperty _timeCounter;

        private string _componentFilter = "";

        GUIContent recordFoldoutGUI = new GUIContent("", "You can drag files onto this header to add them to the files list.");
        GUIContent loadingButtonGUI = new GUIContent("Loading...", _updateFilesDescription);
        GUIContent updateFilesGUI = new GUIContent("Update Files", _updateFilesDescription);
        GUIContent filterComponentsGUI = new GUIContent("Filter Components", "Filter to specific components based upon their descriptor and component type.");
        GUIContent playbackRateGUI = new GUIContent("Playback Rate", "The rate/speed at which the recordings should play.");
        GUIContent scrubWaitGUI = new GUIContent("Scrubbing Wait Time", "The amount of time to wait before jumping to a specific point on the timeline.");

        private const string _updateFilesDescription = "This can take a while to process depending on your system, the number of files, and the recording complexity. You may find certain features or options inaccessible until this button is pressed.";
        private const string _recordComponentsInformation = "The green signifies how many components are correctly assigned, while red is the number that are not currently assigned.";

        private void Awake()
        {
            SetProperties();
            for (int i = 0; i < _recordedFiles.arraySize; i++)
            {
                if (_recordedFiles.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    _recordedFiles.DeleteArrayElementAtIndex(i);
                }
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return false;
        }

        private void SetProperties()
        {
            _recordedFiles = serializedObject.FindProperty("_recordedFiles");
            _dataCache = serializedObject.FindProperty("_dataCacheNames");
            _binders = serializedObject.FindProperty("_binders");
            _currentFile = serializedObject.FindProperty("_currentFile");
            _awaitingRefresh = serializedObject.FindProperty("_awaitingFileRefresh");
            _oldFileIndex = serializedObject.FindProperty("_oldFileIndex");
            _playing = serializedObject.FindProperty("_playing");
            _changingFiles = serializedObject.FindProperty("_changingFiles");
            _playbackRate = serializedObject.FindProperty("_playbackRate");
            _scrubWait = serializedObject.FindProperty("_scrubWaitTime");
            _paused = serializedObject.FindProperty("_paused");
            _currentTick = serializedObject.FindProperty("_currentTickVal");
            _maxTick = serializedObject.FindProperty("_maxTickVal");
            _currentFrameRate = serializedObject.FindProperty("_currentFrameRate");
            _timeCounter = serializedObject.FindProperty("_timeCounter");
        }

        public override void OnInspectorGUI()
        {
            SetProperties();

            EditorGUI.BeginChangeCheck();

            PlaybackLocker.SetPlayModeEnabled(!_changingFiles.boolValue && !_awaitingRefresh.boolValue);

            EditorGUI.BeginDisabledGroup(_changingFiles.boolValue);

            RecordedFiles();

            EditorUtil.DrawDividerLine();

            Playlists();

            EditorUtil.DrawDividerLine();

            RecordComponents();

            EditorUtil.DrawDividerLine();

            PlaybackControls();

            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void ToggleExpanded(bool expand)
        {
            for (int i = 0; i < _binders.arraySize; i++)
            {
                _binders.GetArrayElementAtIndex(i).isExpanded = expand;
            }
        }

        private void RecordedFiles()
        {
            EditorGUILayout.BeginHorizontal();

            RecordedDragDrops();

            if (_changingFiles.boolValue)
            {
                Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
                EditorGUI.ProgressBar(progressRect, (float)((PlaybackManager)serializedObject.targetObject).dataCacheCount / _recordedFiles.arraySize, "Parsing files...");
            }

            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            if (GUILayout.Button("+", Styles.miniButton, GUILayout.Width(Sizes.widthCharButton)))
            {
                _recordedFiles.InsertArrayElementAtIndex(_recordedFiles.arraySize);
                _recordedFiles.GetArrayElementAtIndex(_recordedFiles.arraySize - 1).objectReferenceValue = null;
                _recordedFiles.isExpanded = true;
                _awaitingRefresh.boolValue = true;
            }

            if (_changingFiles.boolValue)
            {
                GUILayout.Button(loadingButtonGUI, Styles.miniButtonGrey, GUILayout.Width(90));
            }
            else
            {
                if (GUILayout.Button(updateFilesGUI, _awaitingRefresh.boolValue ? Styles.miniButtonBoldRed : Styles.miniButton, GUILayout.Width(90)))
                {
                    _awaitingRefresh.boolValue = false;
                    serializedObject.ApplyModifiedProperties();
                    ((PlaybackManager)serializedObject.targetObject).ChangeFiles();
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            RecordedFilesList();

            EditorGUILayout.BeginHorizontal();
            if (_currentFile.intValue != -1 && _recordedFiles.arraySize > 0 && _recordedFiles.GetArrayElementAtIndex(_currentFile.intValue).objectReferenceValue != null)
            {
                EditorGUILayout.LabelField("Current file: (" + (_currentFile.intValue + 1) + ") " + ((TextAsset)_recordedFiles.GetArrayElementAtIndex(_currentFile.intValue).objectReferenceValue).name);
            }
            else
            {
                EditorGUILayout.LabelField("No files loaded");
            }

            EditorGUI.BeginDisabledGroup(!_awaitingRefresh.boolValue || Application.isPlaying);

            if (GUILayout.Button("Revert Changes", Styles.miniButton, GUILayout.Width(110)))
            {
                ((PlaybackManager)serializedObject.targetObject).RevertFileChanges();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_recordedFiles.arraySize == 0 || Application.isPlaying);

            if (GUILayout.Button("Clear Files", Styles.miniButton, GUILayout.Width(90)))
            {
                _recordedFiles.ClearArray();
                _awaitingRefresh.boolValue = true;
                _oldFileIndex.intValue = _currentFile.intValue;
                _currentFile.intValue = -1;
                serializedObject.ApplyModifiedProperties();
                GUIUtility.ExitGUI();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void RecordedDragDrops()
        {
            Rect dragRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));

            recordFoldoutGUI.text = "Recorded Files (" + _recordedFiles.arraySize + ")";

            _recordedFiles.isExpanded = EditorGUI.Foldout(dragRect, _recordedFiles.isExpanded, recordFoldoutGUI, true, Styles.foldoutBold);

            if (dragRect.Contains(Event.current.mousePosition) && !_changingFiles.boolValue)
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
                            _recordedFiles.InsertArrayElementAtIndex(_recordedFiles.arraySize);
                            _recordedFiles.GetArrayElementAtIndex(_recordedFiles.arraySize - 1).objectReferenceValue = DragAndDrop.objectReferences[i];
                            dragged++;
                        }
                    }

                    Event.current.Use();

                    if (dragged > 0)
                    {
                        _recordedFiles.isExpanded = true;
                        _awaitingRefresh.boolValue = true;
                        serializedObject.ApplyModifiedProperties();
                        ((PlaybackManager)serializedObject.targetObject).RemoveDuplicateFiles();
                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private void RecordedFilesList()
        {
            if (_recordedFiles.isExpanded)
            {
                _fileScrollPos = EditorGUILayout.BeginScrollView(_fileScrollPos, GUILayout.Height(Sizes.Playback.heightFileScroll));
                for (int i = 0; i < _recordedFiles.arraySize; i++)
                {
                    if (!(_fileScrollPos.y - ((Sizes.heightLine + Sizes.padding)*2) <= ((Sizes.heightLine+Sizes.padding) * (i - 1)) && _fileScrollPos.y + Sizes.Playback.heightFileScroll > ((Sizes.heightLine + Sizes.padding) * i)))
                    {
                        EditorGUILayout.LabelField("");
                        continue;
                    }

                    if (_recordedFiles.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        _awaitingRefresh.boolValue = true;
                    }

                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.BeginDisabledGroup(_awaitingRefresh.boolValue);

                    RecordedFileListArrowButton(i);

                    EditorGUI.EndDisabledGroup();

                    string recName = "";
                    if (_dataCache.arraySize > i)
                    {
                        recName = _dataCache.GetArrayElementAtIndex(i).stringValue;
                    }

                    _recordedFiles.GetArrayElementAtIndex(i).objectReferenceValue = EditorGUILayout.ObjectField(recName.Length > 0 ? recName : (i + 1).ToString(), _recordedFiles.GetArrayElementAtIndex(i).objectReferenceValue, typeof(TextAsset), false);

                    EditorGUI.BeginDisabledGroup(Application.isPlaying);

                    if (GUILayout.Button("-", Styles.miniButton, GUILayout.Width(Sizes.widthCharButton)))
                    {
                        _recordedFiles.GetArrayElementAtIndex(i).objectReferenceValue = null;
                        _recordedFiles.DeleteArrayElementAtIndex(i);
                        _awaitingRefresh.boolValue = true;
                        if (_currentFile.intValue >= _recordedFiles.arraySize)
                        {
                            _currentFile.intValue = _recordedFiles.arraySize - 1;
                        }
                    }

                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void RecordedFileListArrowButton(int i)
        {
            if (GUILayout.Button(new GUIContent(_currentFile.intValue == i ? ">" + (i + 1).ToString() : (i + 1).ToString(), "Change the currently selected file to this file."), GUILayout.Width(32)))
            {
                ((PlaybackManager)serializedObject.targetObject).ChangeCurrentFile(i);
                GUIUtility.ExitGUI();
            }
        }

        private void Playlists()
        {
            EditorGUILayout.LabelField(new GUIContent("Playlist"), Styles.textBold);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(_recordedFiles.arraySize == 0 || _awaitingRefresh.boolValue);

            if (GUILayout.Button(new GUIContent("Save Playlist", _awaitingRefresh.boolValue ? "Please make sure to press Update Files before trying to save a playlist." : "Save the current set of files into a playlist.")))
            {
                var path = EditorUtility.SaveFilePanel("Save Playlist", "", "playlist.json", "json");
                if (path.Length != 0)
                {
                    List<PlaylistItem> playlist = new List<PlaylistItem>();
                    for (int i = 0; i < _recordedFiles.arraySize; i++)
                    {
                        TextAsset t = (TextAsset)_recordedFiles.GetArrayElementAtIndex(i).objectReferenceValue;
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
                GUIUtility.ExitGUI();
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

                        _recordedFiles.ClearArray();

                        for (int i = 0; i < textAssets.Count; i++)
                        {
                            _recordedFiles.InsertArrayElementAtIndex(i);
                            _recordedFiles.GetArrayElementAtIndex(i).objectReferenceValue = textAssets[i];
                        }

                        _currentFile.intValue = 0;
                        serializedObject.ApplyModifiedProperties();

                        EditorUtility.DisplayDialog("Loaded", "Playlist loaded. " + textAssets.Count + " file(s) set.", "Ok");
                        ((PlaybackManager)serializedObject.targetObject).ChangeFiles();
                        _awaitingRefresh.boolValue = false;

                    }
                    catch
                    {
                        EditorUtility.DisplayDialog("Error", "Invalid playlist file loaded.", "Ok");
                    }

                }
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RecordComponents()
        {
            int assignedCount = 0, unassignedCount = 0;

            for (int i = 0; i < _binders.arraySize; i++)
            {
                if (_binders.GetArrayElementAtIndex(i).FindPropertyRelative("recordComponent").objectReferenceValue == null)
                {
                    unassignedCount++;
                }
                else
                {
                    assignedCount++;
                }
            }

            GUIContent recordComponents = new GUIContent("Recorded Components (" + _binders.arraySize+")", _recordComponentsInformation);

            EditorGUILayout.BeginHorizontal();

            Rect foldoutRect = GUILayoutUtility.GetRect(0, Sizes.widthIcon + Sizes.padding, GUILayout.ExpandWidth(true));

            GUIContent greenLabel = new GUIContent(EditorGUIUtility.IconContent("greenLight"));
            greenLabel.tooltip = _recordComponentsInformation;
            GUIContent redLabel = new GUIContent(EditorGUIUtility.IconContent("redLight"));
            redLabel.tooltip = _recordComponentsInformation;

            _binders.isExpanded = EditorGUI.Foldout(foldoutRect,_binders.isExpanded, recordComponents, true, Styles.foldoutBold);

            EditorGUILayout.LabelField(greenLabel,Styles.textIconBold,GUILayout.Width(Sizes.widthIcon));

            GUIContent gcLabel = new GUIContent(assignedCount.ToString(), _recordComponentsInformation);

            EditorGUILayout.LabelField(gcLabel,Styles.textBold, GUILayout.Width(Styles.textBold.CalcSize(gcLabel).x));

            EditorGUILayout.LabelField(redLabel,Styles.textIconBold, GUILayout.Width(Sizes.widthIcon));

            GUIContent rcLabel = new GUIContent(unassignedCount.ToString(), _recordComponentsInformation);

            EditorGUILayout.LabelField(rcLabel, Styles.textBold,GUILayout.Width(Styles.textBold.CalcSize(rcLabel).x));

            EditorGUILayout.EndHorizontal();

            if (_binders.isExpanded)
            {
                EditorGUILayout.BeginHorizontal();
                _componentFilter = EditorGUILayout.TextField(filterComponentsGUI, _componentFilter);
                if (GUILayout.Button("Clear Filter", Styles.miniButton, GUILayout.Width(90)))
                {
                    _componentFilter = "";
                }
                EditorGUILayout.EndHorizontal();
                _componentScrollPos = EditorGUILayout.BeginScrollView(_componentScrollPos, GUILayout.Height(Sizes.Playback.heightComponentScroll));
                int c = 0;
                for (int i = 0; i < _binders.arraySize; i++)
                {
                    if (_componentFilter == "" || _binders.GetArrayElementAtIndex(i).FindPropertyRelative("descriptor").stringValue.Contains(_componentFilter, StringComparison.InvariantCultureIgnoreCase) || _binders.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue.Contains(_componentFilter, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (c > 0)
                        {
                            EditorUtil.DrawDividerLine();
                        }
                        EditorGUILayout.PropertyField(_binders.GetArrayElementAtIndex(i));
                        c++;
                    }
                }
                if (c == 0 && _binders.arraySize != 0)
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

        private void PlaybackControls()
        {
            EditorGUILayout.LabelField("Playback Parameters", EditorStyles.boldLabel);

            _playbackRate.floatValue = EditorGUILayout.Slider(playbackRateGUI, _playbackRate.floatValue, 0, 3.0f);
            _scrubWait.floatValue = EditorGUILayout.Slider(scrubWaitGUI, _scrubWait.floatValue, 0, 1.0f);

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || _recordedFiles.arraySize == 0);

            EditorGUILayout.LabelField("Current Playback Information", EditorStyles.boldLabel);

            GUIContent playpause = new GUIContent();
            if (_paused.boolValue)
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

            EditorGUI.BeginDisabledGroup(!_playing.boolValue);

            int value = (int)EditorGUILayout.Slider(new GUIContent("Frame", "The current frame being played back from the current file."),
                Mathf.Clamp(_currentTick.intValue, 0, _maxTick.intValue),
                0,
                _maxTick.intValue);

            if (value != Mathf.Clamp(_currentTick.intValue, 0, _maxTick.intValue))
            {
                ((PlaybackManager)serializedObject.targetObject).ScrubTick(value);
            }

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Frame Rate: " + _currentFrameRate.intValue.ToString());

            if (_currentFrameRate.intValue > 0)
            {
                EditorGUILayout.LabelField("Time: " + TimeUtil.ConvertToTime((double)_timeCounter.floatValue) + " / " + TimeUtil.ConvertToTime((double)_maxTick.intValue / (double)_currentFrameRate.intValue));
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