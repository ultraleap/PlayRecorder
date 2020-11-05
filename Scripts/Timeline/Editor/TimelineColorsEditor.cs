using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;
using UnityEditorInternal;

namespace PlayRecorder.Timeline
{

    [CustomEditor(typeof(TimelineColors))]
    public class TimelineColorsEditor : Editor
    {
        ReorderableList colourList;

        public override void OnInspectorGUI()
        {

            EditorGUILayout.LabelField("This file acts as the key to the different message colours within the Timeline window.", Styles.boxBorderText);
            EditorGUILayout.LabelField("You can use a <b>*</b> operator as a wildcard at any point during your message. (E.g. my_*_* would find both my_cool_message and my_bad_event)", Styles.boxBorderText);
            EditorGUILayout.LabelField("Setting a colour to fully transparent will hide it from the timeline.", Styles.boxBorderText);
            if(GUILayout.Button("Open Timeline"))
            {
                TimelineWindow.Init();
            }

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginHorizontal();

            serializedObject.FindProperty("overrideSelected").boolValue = EditorGUILayout.Toggle("Override Selected Colour", serializedObject.FindProperty("overrideSelected").boolValue);

            if(serializedObject.FindProperty("overrideSelected").boolValue)
            {
                serializedObject.FindProperty("selectedColour").colorValue = EditorGUILayout.ColorField(serializedObject.FindProperty("selectedColour").colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            serializedObject.FindProperty("overridePassive").boolValue = EditorGUILayout.Toggle("Override Passive Colour", serializedObject.FindProperty("overridePassive").boolValue);

            if (serializedObject.FindProperty("overridePassive").boolValue)
            {
                serializedObject.FindProperty("passiveColour").colorValue = EditorGUILayout.ColorField(serializedObject.FindProperty("passiveColour").colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            serializedObject.FindProperty("overrideBackground").boolValue = EditorGUILayout.Toggle("Override Background Colour", serializedObject.FindProperty("overrideBackground").boolValue);

            if (serializedObject.FindProperty("overrideBackground").boolValue)
            {
                serializedObject.FindProperty("backgroundColour").colorValue = EditorGUILayout.ColorField(serializedObject.FindProperty("backgroundColour").colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            serializedObject.FindProperty("overrideTimeIndicator").boolValue = EditorGUILayout.Toggle("Override Time Indicator Colour", serializedObject.FindProperty("overrideTimeIndicator").boolValue);

            if (serializedObject.FindProperty("overrideTimeIndicator").boolValue)
            {
                serializedObject.FindProperty("timeIndicatorColour").colorValue = EditorGUILayout.ColorField(serializedObject.FindProperty("timeIndicatorColour").colorValue);
                serializedObject.FindProperty("timeIndicatorPausedColour").colorValue = EditorGUILayout.ColorField(serializedObject.FindProperty("timeIndicatorPausedColour").colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();


            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();
            colourList.DoLayoutList();
            if(EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
                serializedObject.FindProperty("updateTimeline").boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }

        }

        public void OnEnable()
        {
            TimelineWindow[] windows = Resources.FindObjectsOfTypeAll<TimelineWindow>();
            if (windows != null && windows.Length > 0)
            {
                serializedObject.FindProperty("updateTimeline").boolValue = true;
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < windows.Length; i++)
                {
                    windows[i].ColourRefresh();
                }
            }

            colourList = new ReorderableList(serializedObject, serializedObject.FindProperty("colours"), true, true, true, true);
            colourList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Messages",Styles.textBold);
            };
            colourList.drawElementCallback =
                (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = colourList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width - 121, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("message"), GUIContent.none);
                    EditorGUI.PropertyField(
                        new Rect(rect.x + rect.width - 120, rect.y, 120, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("color"), GUIContent.none);
                };
        }
    }

}