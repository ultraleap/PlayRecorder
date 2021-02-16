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

        List<string> timelineMessages = new List<string>();
        List<string> colourMessages = new List<string>();
        List<string> nonAddedMessages = new List<string>();

        int listSize = 0;

        public override void OnInspectorGUI()
        {
            if(listSize != serializedObject.FindProperty("colours").arraySize)
            {
                RefreshMessageDifferences();
                listSize = serializedObject.FindProperty("colours").arraySize;
            }

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

            EditorGUILayout.BeginHorizontal();

            serializedObject.FindProperty("overrideMessageIndicatorWidth").boolValue = EditorGUILayout.Toggle("Override Message Indicator Width", serializedObject.FindProperty("overrideMessageIndicatorWidth").boolValue);

            if (serializedObject.FindProperty("overrideMessageIndicatorWidth").boolValue)
            {
                serializedObject.FindProperty("messageIndicatorWidth").intValue = (int)EditorGUILayout.Slider(serializedObject.FindProperty("messageIndicatorWidth").intValue, 1, 10);
                //serializedObject.FindProperty("messageIndicatorWidth").intValue = EditorGUILayout.IntField(serializedObject.FindProperty("messageIndicatorWidth").intValue);
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

            if (nonAddedMessages.Count > 0)
            {
                EditorUtil.DrawDividerLine();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Missing Messages", "These missing messages are based upon the currently loaded files present in the timeline window."), Styles.textBold);
                if(GUILayout.Button(new GUIContent("Add All Missing","Adds all missing message to the colour list."), Styles.miniButton))
                {
                    while(nonAddedMessages.Count > 0)
                    {
                        AddMissingMessage(0);
                    }
                }
                EditorGUILayout.EndHorizontal();

                int selected = -1;
                selected = GUILayout.SelectionGrid(selected, nonAddedMessages.ToArray(), 4, Styles.buttonIcon);

                if (selected != -1)
                {
                    AddMissingMessage(selected);
                }
            }

            if (EditorGUI.EndChangeCheck())
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
                timelineMessages.Clear();
                serializedObject.FindProperty("updateTimeline").boolValue = true;
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < windows.Length; i++)
                {
                    windows[i].ColourRefresh();
                    timelineMessages.AddRange(windows[i].currentMessages);
                }
                RefreshMessageDifferences();
                listSize = serializedObject.FindProperty("colours").arraySize;
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

        void RefreshMessageDifferences()
        {
            colourMessages.Clear();
            nonAddedMessages.Clear();
            for (int i = 0; i < serializedObject.FindProperty("colours").arraySize; i++)
            {
                colourMessages.Add(serializedObject.FindProperty("colours").GetArrayElementAtIndex(i).FindPropertyRelative("message").stringValue);
            }
            for (int i = 0; i < timelineMessages.Count; i++)
            {
                int ind = colourMessages.IndexOf(timelineMessages[i]);
                if(ind == -1)
                {
                    nonAddedMessages.Add(timelineMessages[i]);
                }
            }
        }

        void AddMissingMessage(int selected)
        {
            int ind = 0;
            if (serializedObject.FindProperty("colours").arraySize > 0)
            {
                ind = serializedObject.FindProperty("colours").arraySize - 1;
            }
            serializedObject.FindProperty("colours").InsertArrayElementAtIndex(ind);
            serializedObject.FindProperty("colours").GetArrayElementAtIndex(serializedObject.FindProperty("colours").arraySize - 1).FindPropertyRelative("message").stringValue = nonAddedMessages[selected];

            nonAddedMessages.RemoveAt(selected);
        }
    }

}