using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;
using System.Text.RegularExpressions;

namespace PlayRecorder
{

    [CustomEditor(typeof(RecordComponent), true)]
    public class RecordComponentEditor : Editor
    {

        private bool descriptorCheck = true;
        private string currentDescriptor = "";

        private RecordingManager manager = null;
        private PlaybackManager playbackManager = null;

        private SerializedProperty _descriptor;
        private SerializedProperty _uniqueDescriptor;
        private SerializedProperty _required;
        private SerializedProperty _customIgnoreFile;
        private string _tooltip;

        private Rect _popupRect;

        private void SetProperties()
        {
            _descriptor = serializedObject.FindProperty("_descriptor");
            _uniqueDescriptor = serializedObject.FindProperty("_uniqueDescriptor");
            _required = serializedObject.FindProperty("_required");
            _customIgnoreFile = serializedObject.FindProperty("_customPlaybackIgnoreItem");
            _tooltip = ((RecordComponent)serializedObject.targetObject).editorHelpbox;
        }

        public override void OnInspectorGUI()
        {
            SetProperties();

            if (((RecordComponent)serializedObject.targetObject).gameObject.scene.name == null)
            {
                EditorGUILayout.LabelField("Please add object to scene to enable this inspector.");
                if(manager != null)
                {
                    manager.RemoveComponent((RecordComponent)serializedObject.targetObject);
                    manager = null;
                }
            }
            else
            {
                currentDescriptor = _descriptor.stringValue;

                if (manager == null)
                {
                    manager = FindObjectOfType<RecordingManager>();
                    if(manager != null)
                    {
                        manager.AddComponent((RecordComponent)serializedObject.targetObject);
                    }
                }

                if (descriptorCheck && manager != null)
                {
                    descriptorCheck = false;
                    _uniqueDescriptor.boolValue = manager.CheckUniqueDescriptor((RecordComponent)serializedObject.targetObject);
                }

                RecordingInfo();
                PlaybackInfo();

                serializedObject.ApplyModifiedProperties();

                if (_tooltip != null && _tooltip != "")
                {
                    EditorGUILayout.HelpBox(_tooltip, MessageType.Info);
                }

                DrawDefaultInspector();
                if (currentDescriptor != _descriptor.stringValue)
                {
                    descriptorCheck = true;
                }
            }
        }

        private void RecordingInfo()
        {
            EditorGUILayout.BeginVertical(Styles.boxBorder);

            EditorGUILayout.LabelField("Recording Info", Styles.textBold);

            if (manager == null)
            {
                EditorGUILayout.LabelField("No recording manager in scene. Please add one.", Styles.textBoldRed);
            }
            else
            {
                EditorGUILayout.PropertyField(_descriptor, new GUIContent("Descriptor", "This needs to be unique."));

                if (!_uniqueDescriptor.boolValue || Regex.Replace(_descriptor.stringValue, @"\s+", "") == "")
                {
                    EditorGUILayout.LabelField("Current descriptor is not unique. Please change this to make it unique.", Styles.textBoldRed);
                }

                _required.boolValue = EditorGUILayout.Toggle(new GUIContent("Required for Recording", "Decides whether this component will be used during the next recording. Does not affect playback."), _required.boolValue);
            }

            EditorGUILayout.EndVertical();
        }

        private void PlaybackInfo()
        {
            if(playbackManager == null)
            {
                playbackManager = FindObjectOfType<PlaybackManager>();
            }

            EditorGUILayout.BeginVertical(Styles.boxBorder);

            EditorGUILayout.LabelField("Playback Info", Styles.textBold);

            if(playbackManager != null && playbackManager.hasStarted)
            {
                EditorGUILayout.HelpBox("Playback has already started. No changes to the ignore settings will be respected.", MessageType.Info);
            }

            GUIContent gc = new GUIContent("Custom Playback Ignore Settings", "Allows you to manually override this particular record components ignore settings for playback. These settings will take precedence over the file set in the Playback Manager. By default, most non-PlayRecorder components will be disabled by default.");

            EditorGUILayout.BeginHorizontal();

            PlaybackIgnoreSingleObject so = (PlaybackIgnoreSingleObject)_customIgnoreFile.objectReferenceValue;

            EditorGUILayout.LabelField(gc);

            if (_customIgnoreFile.objectReferenceValue == null)
            {
                if(GUILayout.Button("Create",Styles.miniButton,GUILayout.Width(60)))
                {
                    PlaybackIgnoreSingleObject soNew = CreateInstance<PlaybackIgnoreSingleObject>();
                    soNew.item = new PlaybackIgnoreItem(GetType().ToString());
                    soNew.item.coreLogicOpen = true;
                    soNew.item.componentsOpen = true;
                    _customIgnoreFile.objectReferenceValue = soNew;
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                if (GUILayout.Button("Delete", Styles.miniButton, GUILayout.Width(60)))
                {
                    DestroyImmediate(so);
                    _customIgnoreFile.objectReferenceValue = null;
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel++;
                so.item.recordComponent = serializedObject.targetObject.GetType().ToString();
                so.item.coreLogicOpen = EditorGUILayout.Foldout(so.item.coreLogicOpen, new GUIContent("Disabled Unity Logic", "These control parameters and fundamental Unity logic items that often cannot be 'disabled', only modified."), true);
                if (so.item.coreLogicOpen)
                {
                    so.item.makeKinematic = EditorGUILayout.Toggle(new GUIContent("Make Object Kinematic", "Makes the object kinematic, preventing gravitational changes."), so.item.makeKinematic);
                    so.item.disableCollisions = EditorGUILayout.Toggle(new GUIContent("Disable Collisions", "Disables all object colliders."), so.item.disableCollisions);
                    so.item.disableRenderer = EditorGUILayout.Toggle(new GUIContent("Disable Renderers", "Turns off 2D and 3D renderers."), so.item.disableRenderer);
                    so.item.disableCamera = EditorGUILayout.Toggle(new GUIContent("Disable Camera", "Turns off attached cameras."), so.item.disableCamera);
                    so.item.disableVRCamera = EditorGUILayout.Toggle(new GUIContent("Disable VR Camera", "Forces camera to render to desktop, not headset."), so.item.disableVRCamera);
                }

                so.item.componentsOpen = EditorGUILayout.Foldout(so.item.componentsOpen, new GUIContent("Enabled Components (" + so.item.enabledComponents.Count + ")", "The components that will remain enabled when playback begins."), true);
                if (so.item.componentsOpen)
                {
                    for (int j = 0; j < so.item.enabledComponents.Count; j++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent("-", "Remove this class from the ignore list."), Styles.miniButton, GUILayout.Width(Sizes.widthCharButton)))
                        {
                            so.item.enabledComponents.RemoveAt(j);
                            GUIUtility.ExitGUI();
                        }
                        so.item.enabledComponents[j] = EditorGUILayout.TextField(so.item.enabledComponents[j]);
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Add Components from GameObject", "Allows you to select a GameObject in the scene and quickly select components you want to ignore.")))
                {
                    PopupWindow.Show(_popupRect, new PlaybackIgnorePopup(this, so.item.enabledComponents, _popupRect.width));
                }
                if (GUILayout.Button(new GUIContent("Add Empty", "Adds an empty row allowing you to add whatever text you wish. Logic is tested on a string contains test so you could include a whole namespace as an example.")))
                {
                    so.item.enabledComponents.Add("");
                }
                EditorGUILayout.EndHorizontal();
                if (Event.current.type == EventType.Repaint)
                {
                    _popupRect = GUILayoutUtility.GetLastRect();
                }
                EditorGUI.indentLevel--;

            }
            EditorGUILayout.EndVertical();
        }

        public void AddIgnoreItem(string item)
        {
            PlaybackIgnoreSingleObject so = (PlaybackIgnoreSingleObject)_customIgnoreFile.objectReferenceValue;
            so.item.enabledComponents.Add(item);
            so.item.componentsOpen = true;
        }
    }
}
