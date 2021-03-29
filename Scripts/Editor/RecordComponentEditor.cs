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

        private SerializedProperty _descriptor;
        private SerializedProperty _uniqueDescriptor;
        private SerializedProperty _required;
        private string _tooltip;

        private void SetProperties()
        {
            _descriptor = serializedObject.FindProperty("_descriptor");
            _uniqueDescriptor = serializedObject.FindProperty("_uniqueDescriptor");
            _required = serializedObject.FindProperty("_required");
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

                EditorGUILayout.BeginVertical(Styles.boxBorder);

                EditorGUILayout.LabelField("Recording Info", Styles.textBold);

                if (manager == null)
                {
                    EditorGUILayout.LabelField("No recording manager in scene. Please add one.",Styles.textBoldRed);
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
    }
}
