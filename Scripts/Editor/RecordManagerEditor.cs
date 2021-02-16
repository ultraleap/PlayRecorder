using System.Collections;
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

        static string componentString = "_components",recordingNameString = "recordingName", recordingFolderString = "recordingFolderName",frameRateString = "_frameRateVal";

        Vector2 scrollPos;

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

            serializedObject.FindProperty(recordingNameString).stringValue = EditorGUILayout.TextField("Recording Name", serializedObject.FindProperty(recordingNameString).stringValue);

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Recording Folder: <b>" + serializedObject.FindProperty(recordingFolderString).stringValue+"</b>","Recording folders are relative to the project Assets folder in the Editor or the "+Application.productName+"_Data folder in builds."),Styles.textRich);
            
            GUIContent chooseFolder = new GUIContent(EditorGUIUtility.IconContent("FolderEmpty Icon"));
            chooseFolder.text = " Choose Folder";
            chooseFolder.tooltip = "Select a folder to store recordings.";

            if (GUILayout.Button(chooseFolder, Styles.buttonIcon,GUILayout.Height(20),GUILayout.Width(112)))
            {
                var path = EditorUtility.OpenFolderPanel("Select Recording Storage Folder", "","");
                if(path.Length != 0)
                {
                    path = path + "\\";
                    string relPath = FileUtil.MakeRelativePath(Application.dataPath + "\\", path).Replace(@"\", "/");
                    relPath = relPath.Remove(relPath.Length - 1);
                    serializedObject.FindProperty(recordingFolderString).stringValue = relPath;
                    serializedObject.ApplyModifiedProperties();
                }
                GUIUtility.ExitGUI();
            }

            GUIContent showFolder = new GUIContent(EditorGUIUtility.IconContent("FolderFavorite Icon", "| Ping the current recording folder for recordings. If the folder is outside the project, then that folder will be opened (and created if it does not exist)."));

            if (GUILayout.Button(showFolder, Styles.buttonIcon,GUILayout.Height(20),GUILayout.Width(20)))
            {
                if(serializedObject.FindProperty(recordingFolderString).stringValue.StartsWith("../"))
                {
                    string path = (Application.dataPath + "/" +serializedObject.FindProperty(recordingFolderString).stringValue).Replace("/", @"\");
                    if(!System.IO.Directory.Exists(path))
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }
                    EditorUtility.RevealInFinder(path);
                }
                else
                {
                    Object obj = AssetDatabase.LoadAssetAtPath("Assets/" + serializedObject.FindProperty(recordingFolderString).stringValue, typeof(Object));

                    Selection.activeObject = obj;

                    EditorGUIUtility.PingObject(obj);
                }
                
            }

            EditorGUILayout.EndHorizontal();
#else
            serializedObject.FindProperty(recordingFolderString).stringValue = EditorGUILayout.TextField(new GUIContent("Recording Folder","Recording folders are relative to the project Assets folder in the Editor or the "+Application.productName+"_Data folder in builds."), serializedObject.FindProperty(recordingFolderString).stringValue);
#endif

            EditorGUILayout.BeginHorizontal();

            serializedObject.FindProperty(frameRateString).intValue = EditorGUILayout.IntSlider("Recording Frame Rate",serializedObject.FindProperty(frameRateString).intValue, 0, 120);

            if(GUILayout.Button("30",Styles.miniButton,GUILayout.Width(32)))
            {
                serializedObject.FindProperty(frameRateString).intValue = 30;
            }

            if (GUILayout.Button("60", Styles.miniButton, GUILayout.Width(32)))
            {
                serializedObject.FindProperty(frameRateString).intValue = 60;
            }

            EditorGUILayout.EndHorizontal();

            EditorUtil.DrawDividerLine();

            EditorGUILayout.BeginHorizontal();

            if(serializedObject.FindProperty(componentString).isExpanded = EditorGUILayout.Foldout(serializedObject.FindProperty(componentString).isExpanded,new GUIContent("Recording Components ("+ serializedObject.FindProperty(componentString).arraySize+")", "All recording components currently found within the scene."),true,Styles.foldoutBold))
            {
                RefreshComponentsButton();
                EditorGUILayout.EndHorizontal();

                GUIContent hierarchyButton = new GUIContent(EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow"));
                hierarchyButton.tooltip = "Ping the object within the scene hierarchy.";

                GUIContent recordLabel = new GUIContent("Record", "Decides whether this component will be used during the next recording. Does not affect playback.");

                GUIStyle s = new GUIStyle(EditorStyles.boldLabel);

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
                for (int i = 0; i < serializedObject.FindProperty(componentString).arraySize; i++)
                {
                    RecordComponent r = (RecordComponent)serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue;

                    if (serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        serializedObject.FindProperty(componentString).DeleteArrayElementAtIndex(i);
                        i--;
                        continue;
                    }

                    if(i > 0)
                    {
                        EditorUtil.DrawDividerLine();
                    }

                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button(hierarchyButton, Styles.buttonIcon, GUILayout.Width(20)))
                    {
                        EditorGUIUtility.PingObject(serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue);
                    }
                    if(GUILayout.Button(((RecordComponent)serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue).descriptor + " ("+ serializedObject.FindProperty(componentString).GetArrayElementAtIndex(i).objectReferenceValue.name+")", r.required ? Styles.textBold : Styles.textDisabledBold))
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