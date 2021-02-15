using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using PlayRecorder.Tools;

namespace PlayRecorder
{

    [CustomPropertyDrawer(typeof(PlaybackBinder))]
    public class PlaybackBinderDrawer : PropertyDrawer
    {

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight * 4;
            }
            return EditorGUIUtility.singleLineHeight;
        }

        private RecordComponent currentComponent = null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            EditorGUI.BeginProperty(position, label, property);

            GUIContent statusIcon;
            GUIContent foldoutlabel = new GUIContent("Descriptor: " + property.FindPropertyRelative("descriptor").stringValue);


            if (property.FindPropertyRelative("recordComponent").objectReferenceValue == null)
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

            //EditorGUI.DrawRect(foldoutRect, Color.red);
            EditorGUI.LabelField(iconRect, statusIcon);

            //EditorGUI.indentLevel++;


            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, foldoutlabel, true, Styles.foldoutIconBold);

            if (property.isExpanded)
            {
                var typeRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, 18);
                var countRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight * 2), position.width, 18);
                var recordRect = new Rect(position.x, position.y + (EditorGUIUtility.singleLineHeight * 3), position.width, 18);

                string type = property.FindPropertyRelative("type").stringValue;

                //EditorGUI.LabelField(descriptorRect, , s);
                EditorGUI.LabelField(typeRect, "Type: " + type);

                EditorGUI.LabelField(countRect,new GUIContent("Instances: " + property.FindPropertyRelative("count").intValue.ToString(),"The number of times this component appears in all files. If this number is different to the file count then you may have non-unique components in your save data."));

                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                EditorGUI.PropertyField(recordRect, property.FindPropertyRelative("recordComponent"), new GUIContent("Component","This will only accept components that have the same type as listed above."));
                currentComponent = (RecordComponent)property.FindPropertyRelative("recordComponent").objectReferenceValue;
                EditorGUI.EndDisabledGroup();
                if (currentComponent != null && currentComponent.GetType().ToString() != type)
                {
                    Debug.LogError("RecordComponent type mismatch. Please assign the correct RecordComponent type.");
                    currentComponent = null;
                    property.FindPropertyRelative("recordComponent").objectReferenceValue = null;
                }
                else
                {
                    property.FindPropertyRelative("recordComponent").objectReferenceValue = currentComponent;
                }
            }

            //EditorGUI.indentLevel--;

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }

}