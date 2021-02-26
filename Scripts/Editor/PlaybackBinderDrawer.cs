using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using PlayRecorder.Tools;

namespace PlayRecorder
{

    [CustomPropertyDrawer(typeof(PlaybackBinder))]
    public class PlaybackBinderDrawer : PropertyDrawer
    {

        private SerializedProperty _descriptor;
        private SerializedProperty _recordComponent;
        private SerializedProperty _count;
        private SerializedProperty _type;

        private RecordComponent _currentComponent = null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight * 4;
            }
            return EditorGUIUtility.singleLineHeight;
        }

        private void SetProperties(SerializedProperty baseProperty)
        {
            _descriptor = baseProperty.FindPropertyRelative("descriptor");
            _recordComponent = baseProperty.FindPropertyRelative("recordComponent");
            _count = baseProperty.FindPropertyRelative("count");
            _type = baseProperty.FindPropertyRelative("type");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SetProperties(property);

            EditorGUI.BeginProperty(position, label, property);

            GUIContent statusIcon;
            GUIContent foldoutlabel = new GUIContent("Descriptor: " + _descriptor.stringValue);


            if (_recordComponent.objectReferenceValue == null)
            {
                statusIcon = EditorGUIUtility.IconContent("redLight");
                foldoutlabel.tooltip = "Component does not have a valid playback object assigned.";
            }
            else
            {
                statusIcon = EditorGUIUtility.IconContent("greenLight");
            }
            

            Rect iconRect = new Rect(position.x, position.y, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight);
            Rect foldoutRect = new Rect(position.x + (EditorGUIUtility.singleLineHeight*1.75f), position.y, position.width - (EditorGUIUtility.singleLineHeight*1.75f), EditorGUIUtility.singleLineHeight);

            EditorGUI.LabelField(iconRect, statusIcon);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutlabel, true, Styles.foldoutIconBold);

            if (property.isExpanded)
            {
                var typeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, 18);
                var countRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight * 2), position.width, 18);
                var recordRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight * 3), position.width, 18);

                string type = _type.stringValue;

                EditorGUI.LabelField(typeRect, new GUIContent("Type: " + type.FormatType(), type));

                EditorGUI.LabelField(countRect,new GUIContent("Instances: " + _count.intValue.ToString(),"The number of times this component appears in all files. If this number is different to the file count then you may have non-unique components in your save data."));

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                EditorGUI.PropertyField(recordRect, _recordComponent, new GUIContent("Component","This will only accept components that have the same type as listed above."));
                _currentComponent = (RecordComponent)_recordComponent.objectReferenceValue;
                EditorGUI.EndDisabledGroup();
                if (_currentComponent != null && _currentComponent.GetType().ToString() != type)
                {
                    Debug.LogError("RecordComponent type mismatch. Please assign the correct RecordComponent type.");
                    _currentComponent = null;
                    _recordComponent.objectReferenceValue = null;
                }
                else
                {
                    _recordComponent.objectReferenceValue = _currentComponent;
                }
            }

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }

}