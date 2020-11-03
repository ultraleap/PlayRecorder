using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PlayRecorder.Timeline
{

    [CustomPropertyDrawer(typeof(TimelineColor))]
    public class TimelineColourPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            property.FindPropertyRelative("message").stringValue = EditorGUI.TextField(new Rect(position.x, position.y, (position.width / 3*2)+2, position.height), property.FindPropertyRelative("message").stringValue);

            property.FindPropertyRelative("color").colorValue = EditorGUI.ColorField(new Rect((position.width / 3*2)+21, position.y, (position.width / 3)-2, position.height), property.FindPropertyRelative("color").colorValue);

            property.serializedObject.ApplyModifiedProperties();

            EditorGUI.EndProperty();
        }
    }

}