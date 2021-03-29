using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using PlayRecorder.Tools;

namespace PlayRecorder
{

    [CustomEditor(typeof(RecordingManager), true)]
    public class RecordManagerEditor : Editor
    {
        private Vector2 scrollPos;

        private SerializedProperty _recording;
        private SerializedProperty _recordingOnStartup;
        private SerializedProperty _recordingOnStartupInProgress;
        private SerializedProperty _recordingOnStartupDelay;
        private SerializedProperty _components;
        private SerializedProperty _recordingName;
        private SerializedProperty _recordingFolder;
        private SerializedProperty _frameRate;
        private SerializedProperty _duplicateItems;

        public class RecordManagerDuplicates
        {
            public string descriptor;
            public List<RecordComponent> components = new List<RecordComponent>();
        }

        List<RecordManagerDuplicates> componentNames = new List<RecordManagerDuplicates>();

        private void SetProperties()
        {
            _recording = serializedObject.FindProperty("_recording");
            _recordingOnStartup = serializedObject.FindProperty("_recordOnStartup");
            _recordingOnStartupInProgress = serializedObject.FindProperty("_recordStartupInProgress");
            _recordingOnStartupDelay = serializedObject.FindProperty("_recordOnStartupDelay");
            _components = serializedObject.FindProperty("_components");
            _recordingName = serializedObject.FindProperty("recordingName");
            _recordingFolder = serializedObject.FindProperty("recordingFolderName");
            _duplicateItems = serializedObject.FindProperty("_duplicateItems");
            _frameRate = serializedObject.FindProperty("_frameRateVal");
        }

        public override void OnInspectorGUI()
        {
            SetProperties();

            RecordingStartStopButtons();

            ComponentDuplicateChecks();

            RecordingName();

            RecordingFolder();

            RecordingFrameRate();

            RecordingOnStartup();

            EditorUtil.DrawDividerLine();

            RecordingComponents();

            serializedObject.ApplyModifiedProperties();
        }

        private void RecordingStartStopButtons()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || _recording.boolValue || _recordingOnStartupInProgress.boolValue);

            if (GUILayout.Button("Start Recording"))
            {
                ((RecordingManager)serializedObject.targetObject).StartRecording();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !_recording.boolValue || _recordingOnStartupInProgress.boolValue);

            if (GUILayout.Button("Stop Recording"))
            {
                ((RecordingManager)serializedObject.targetObject).StopRecording();
            }

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void ComponentDuplicateChecks()
        {
            for (int i = 0; i < _components.arraySize; i++)
            {
                if (_components.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    _components.DeleteArrayElementAtIndex(i);
                    serializedObject.ApplyModifiedProperties();
                    i--;
                }
            }

            componentNames.Clear();

            for (int i = 0; i < _components.arraySize; i++)
            {
                string d = ((RecordComponent)_components.GetArrayElementAtIndex(i).objectReferenceValue).descriptor;

                int ind = componentNames.FindIndex(x => x.descriptor == d);

                if (ind != -1)
                {
                    componentNames[ind].components.Add((RecordComponent)_components.GetArrayElementAtIndex(i).objectReferenceValue);
                }
                else
                {
                    componentNames.Add(new RecordManagerDuplicates()
                    {
                        descriptor = d,
                        components = new List<RecordComponent>() { (RecordComponent)_components.GetArrayElementAtIndex(i).objectReferenceValue }
                    });
                }
            }

            bool duplicates = false;
            for (int i = 0; i < componentNames.Count; i++)
            {
                if (componentNames[i].components.Count > 1 || Regex.Replace(componentNames[i].descriptor, @"\s+", "") == "")
                {
                    duplicates = true;
                    _duplicateItems.boolValue = duplicates;
                }
            }

            if (duplicates)
            {
                EditorGUILayout.HelpBox("You have duplicate or invalid descriptors for some components. Please fix, you will not be able to start recording until all issues are rectified.", MessageType.Error);
                for (int i = 0; i < componentNames.Count; i++)
                {
                    if (componentNames[i].components.Count > 1 || Regex.Replace(componentNames[i].descriptor, @"\s+", "") == "")
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(componentNames[i].descriptor, EditorStyles.helpBox);

                        EditorGUILayout.LabelField("(" + componentNames[i].components.Count + ")", EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();
                        for (int j = 0; j < componentNames[i].components.Count; j++)
                        {
                            EditorGUILayout.ObjectField(componentNames[i].components[j], typeof(RecordComponent), true);
                        }
                    }
                }
            }
        }

        private void RecordingName()
        {
            _recordingName.stringValue = EditorGUILayout.TextField(new GUIContent("Recording Name", "The name of the recording. All recordings are prefixed with a unix style timestamp."), _recordingName.stringValue);
        }

        private void RecordingFolder()
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Recording Folder: <b>" + _recordingFolder.stringValue + "</b>", "Recording folders are relative to the project Assets folder in the Editor or the " + Application.productName + "_Data folder in builds."), Styles.textRich);

            GUIContent chooseFolder = new GUIContent(EditorGUIUtility.IconContent("FolderEmpty Icon"));
            chooseFolder.text = " Choose Folder";
            chooseFolder.tooltip = "Select a folder to store recordings.";

            if (GUILayout.Button(chooseFolder, Styles.buttonIcon, GUILayout.Height(20), GUILayout.Width(112)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Recording Storage Folder", "", "");
                if (path.Length != 0)
                {
                    path = path + "\\";
                    string relPath = FileUtil.MakeRelativePath(Application.dataPath + "\\", path).Replace(@"\", "/");
                    relPath = relPath.Remove(relPath.Length - 1);
                    _recordingFolder.stringValue = relPath;
                    serializedObject.ApplyModifiedProperties();
                }
                GUIUtility.ExitGUI();
            }

            GUIContent showFolder = new GUIContent(EditorGUIUtility.IconContent("FolderFavorite Icon", "| Ping the current recording folder for recordings. If the folder is outside the project, then that folder will be opened (and created if it does not exist)."));

            if (GUILayout.Button(showFolder, Styles.buttonIcon, GUILayout.Height(20), GUILayout.Width(20)))
            {
                if (_recordingFolder.stringValue.StartsWith("../"))
                {
                    string path = (Application.dataPath + "/" + _recordingFolder.stringValue).Replace("/", @"\");
                    if (!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    Object obj = AssetDatabase.LoadAssetAtPath("Assets/" + _recordingFolder.stringValue, typeof(Object));

                    Selection.activeObject = obj;

                    EditorGUIUtility.PingObject(obj);
                }

            }

            EditorGUILayout.EndHorizontal();
#else
            _recordingFolder.stringValue = EditorGUILayout.TextField(new GUIContent("Recording Folder","Recording folders are relative to the project Assets folder in the Editor or the "+Application.productName+"_Data folder in builds."), _recordingFolder.stringValue);
#endif
        }

        private void RecordingFrameRate()
        {
            EditorGUILayout.BeginHorizontal();

            _frameRate.intValue = EditorGUILayout.IntSlider(new GUIContent("Recording Frame Rate", "How many times per second (FPS) in which the recording will try to record information"), _frameRate.intValue, 0, 120);

            if (GUILayout.Button("30", Styles.miniButton, GUILayout.Width(32)))
            {
                _frameRate.intValue = 30;
            }

            if (GUILayout.Button("60", Styles.miniButton, GUILayout.Width(32)))
            {
                _frameRate.intValue = 60;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void RecordingOnStartup()
        {
            _recordingOnStartup.boolValue = EditorGUILayout.Toggle(new GUIContent("Record On Play", "Starts recording as soon as play mode is entered."), _recordingOnStartup.boolValue);

            if (_recordingOnStartup.boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                _recordingOnStartupDelay.floatValue = EditorGUILayout.FloatField(new GUIContent("Record On Play Delay", "How many seconds the recording manager will wait before starting recording once in play mode."), _recordingOnStartupDelay.floatValue);

                if (GUILayout.Button("-1s", GUILayout.Width(40)))
                {
                    _recordingOnStartupDelay.floatValue = Mathf.Clamp(_recordingOnStartupDelay.floatValue - 1, 0, float.MaxValue); ;
                }

                if (GUILayout.Button("+1s", GUILayout.Width(40)))
                {
                    _recordingOnStartupDelay.floatValue++;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void RecordingComponents()
        {
            EditorGUILayout.BeginHorizontal();

            if (_components.isExpanded = EditorGUILayout.Foldout(_components.isExpanded, new GUIContent("Recording Components (" + _components.arraySize + ")", "All recording components currently found within the scene."), true, Styles.foldoutBold))
            {
                RefreshComponentsButton();
                EditorGUILayout.EndHorizontal();

                GUIContent hierarchyButton = new GUIContent(EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow"));
                hierarchyButton.tooltip = "Ping the object within the scene hierarchy.";

                GUIContent recordLabel = new GUIContent("Record", "Decides whether this component will be used during the next recording. Does not affect playback.");

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
                for (int i = 0; i < _components.arraySize; i++)
                {
                    RecordComponent recordComponent = (RecordComponent)_components.GetArrayElementAtIndex(i).objectReferenceValue;

                    if (_components.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        _components.DeleteArrayElementAtIndex(i);
                        i--;
                        continue;
                    }

                    if (i > 0)
                    {
                        EditorUtil.DrawDividerLine();
                    }

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(hierarchyButton, Styles.buttonIcon, GUILayout.Width(20)))
                    {
                        EditorGUIUtility.PingObject(_components.GetArrayElementAtIndex(i).objectReferenceValue);
                    }
                    if (GUILayout.Button(((RecordComponent)_components.GetArrayElementAtIndex(i).objectReferenceValue).descriptor + " (" + _components.GetArrayElementAtIndex(i).objectReferenceValue.name + ")", recordComponent.required ? Styles.textBold : Styles.textDisabledBold))
                    {
                        EditorGUIUtility.PingObject(_components.GetArrayElementAtIndex(i).objectReferenceValue);
                    }
                    EditorGUILayout.EndHorizontal();

                    string type = _components.GetArrayElementAtIndex(i).objectReferenceValue.GetType().ToString();
                    EditorGUILayout.LabelField(new GUIContent("Type: " + type.FormatType(), type));

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(recordLabel, EditorStyles.label, GUILayout.Width(46)))
                    {
                        recordComponent.required = !recordComponent.required;
                    }
                    recordComponent.required = EditorGUILayout.Toggle(recordComponent.required, GUILayout.Width(14));
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                RefreshComponentsButton();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RefreshComponentsButton()
        {
            GUIContent rf = new GUIContent(EditorGUIUtility.IconContent("Refresh"));
            rf.text = " Refresh";
            rf.tooltip = "Finds all the current components within the scene in case automatic additions fail.";
            if (GUILayout.Button(rf,Styles.buttonIcon,GUILayout.Width(72)))
            {
                ((RecordingManager)serializedObject.targetObject).RefreshComponents();
            }
        }
    }
}