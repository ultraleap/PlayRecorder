using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;

namespace PlayRecorder.Timeline
{

    [CustomEditor(typeof(TimelineColors))]
    public class TimelineColorsEditor : Editor
    {
        public override void OnInspectorGUI()
        {

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("This file acts as the key to the different message colours within the Timeline window.", Styles.boxBorderText);
            EditorGUILayout.LabelField("You can use a <b>*</b> operator as a wildcard at any point during your message. (E.g. my_*_* would find both my_cool_message and my_bad_event)", Styles.boxBorderText);
            EditorGUILayout.LabelField("Setting a colour to fully transparent will hide it from the timeline.", Styles.boxBorderText);
            if(GUILayout.Button("Open Timeline"))
            {
                TimelineWindow.Init();
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Messages",Styles.textBold);

            if(GUILayout.Button("+",Styles.miniButtonBold,GUILayout.Width(22)))
            {
                serializedObject.FindProperty("colours").InsertArrayElementAtIndex(serializedObject.FindProperty("colours").arraySize);
            }

            EditorGUILayout.EndHorizontal();

            for (int i = 0; i < serializedObject.FindProperty("colours").arraySize; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("colours").GetArrayElementAtIndex(i));
                if (GUILayout.Button("-", Styles.miniButtonBold, GUILayout.Width(22)))
                {
                    serializedObject.FindProperty("colours").DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndHorizontal();
            }

            if(EditorGUI.EndChangeCheck())
            {
                serializedObject.FindProperty("updateTimeline").boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }

        }
    }

}