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
        private ReorderableList colourList;

        private List<string> timelineMessages = new List<string>();
        private List<string> colourMessages = new List<string>();
        private List<string> nonAddedMessages = new List<string>();

        private int listSize = 0;

        private SerializedProperty _colours;
        private SerializedProperty _overrideSelected;
        private SerializedProperty _selectedColour;
        private SerializedProperty _overridePassive;
        private SerializedProperty _passiveColour;
        private SerializedProperty _overrideBackground;
        private SerializedProperty _backgroundColour;
        private SerializedProperty _overrideTimeIndicator;
        private SerializedProperty _timeIndicatorColour;
        private SerializedProperty _timeIndicatorPausedColour;
        private SerializedProperty _overrideTimeIndicatorWidth;
        private SerializedProperty _timeIndicatorWidth;
        private SerializedProperty _overrideMessageIndicatorWidth;
        private SerializedProperty _messageIndicatorWidth;
        private SerializedProperty _updateTimeline;

        private void SetProperties()
        {
            _colours = serializedObject.FindProperty("colours");
            _overrideSelected = serializedObject.FindProperty("overrideSelected");
            _selectedColour = serializedObject.FindProperty("selectedColour");
            _overridePassive = serializedObject.FindProperty("overridePassive");
            _passiveColour = serializedObject.FindProperty("passiveColour");
            _overrideBackground = serializedObject.FindProperty("overrideBackground");
            _backgroundColour = serializedObject.FindProperty("backgroundColour");
            _overrideTimeIndicator = serializedObject.FindProperty("overrideTimeIndicator");
            _timeIndicatorColour = serializedObject.FindProperty("timeIndicatorColour");
            _timeIndicatorPausedColour = serializedObject.FindProperty("timeIndicatorPausedColour");
            _overrideTimeIndicatorWidth = serializedObject.FindProperty("overrideTimeIndicatorWidth");
            _timeIndicatorWidth = serializedObject.FindProperty("timeIndicatorWidth");
            _overrideMessageIndicatorWidth = serializedObject.FindProperty("overrideMessageIndicatorWidth");
            _messageIndicatorWidth = serializedObject.FindProperty("messageIndicatorWidth");
            _updateTimeline = serializedObject.FindProperty("updateTimeline");
    }

        public override void OnInspectorGUI()
        {
            SetProperties();

            if(listSize != _colours.arraySize)
            {
                RefreshMessageDifferences();
                listSize = _colours.arraySize;
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

            _overrideSelected.boolValue = EditorGUILayout.Toggle("Override Selected Colour", _overrideSelected.boolValue);

            if(_overrideSelected.boolValue)
            {
                _selectedColour.colorValue = EditorGUILayout.ColorField(_selectedColour.colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _overridePassive.boolValue = EditorGUILayout.Toggle("Override Passive Colour", _overridePassive.boolValue);

            if (_overridePassive.boolValue)
            {
                _passiveColour.colorValue = EditorGUILayout.ColorField(_passiveColour.colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _overrideBackground.boolValue = EditorGUILayout.Toggle("Override Background Colour", _overrideBackground.boolValue);

            if (_overrideBackground.boolValue)
            {
                _backgroundColour.colorValue = EditorGUILayout.ColorField(_backgroundColour.colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _overrideTimeIndicator.boolValue = EditorGUILayout.Toggle("Override Time Indicator Colour", _overrideTimeIndicator.boolValue);

            if (_overrideTimeIndicator.boolValue)
            {
                _timeIndicatorColour.colorValue = EditorGUILayout.ColorField(_timeIndicatorColour.colorValue);
                _timeIndicatorPausedColour.colorValue = EditorGUILayout.ColorField(_timeIndicatorPausedColour.colorValue);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _overrideTimeIndicatorWidth.boolValue = EditorGUILayout.Toggle("Override Time Indicator Width", _overrideTimeIndicatorWidth.boolValue);

            if (_overrideTimeIndicatorWidth.boolValue)
            {
                _timeIndicatorWidth.intValue = (int)EditorGUILayout.Slider(_timeIndicatorWidth.intValue, 1, 10);
            }
            else
            {
                EditorGUILayout.LabelField("");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _overrideMessageIndicatorWidth.boolValue = EditorGUILayout.Toggle("Override Message Indicator Width", _overrideMessageIndicatorWidth.boolValue);

            if (_overrideMessageIndicatorWidth.boolValue)
            {
                _messageIndicatorWidth.intValue = (int)EditorGUILayout.Slider(_messageIndicatorWidth.intValue, 1, 10);
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
                _updateTimeline.boolValue = true;
                serializedObject.ApplyModifiedProperties();
            }

        }

        public void OnEnable()
        {
            SetProperties();
            TimelineWindow[] windows = Resources.FindObjectsOfTypeAll<TimelineWindow>();

            if (windows != null && windows.Length > 0)
            {
                timelineMessages.Clear();
                _updateTimeline.boolValue = true;
                serializedObject.ApplyModifiedProperties();
                for (int i = 0; i < windows.Length; i++)
                {
                    windows[i].ColourRefresh();
                    timelineMessages.AddRange(windows[i].currentMessages);
                }
                RefreshMessageDifferences();
                listSize = _colours.arraySize;
            }

            colourList = new ReorderableList(serializedObject, _colours, true, true, true, true);
            colourList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Messages",Styles.textBold);
            };
            colourList.drawElementCallback =
                (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var element = colourList.serializedProperty.GetArrayElementAtIndex(index);
                    rect.y += Sizes.padding;
                    EditorGUI.PropertyField(
                        new Rect(rect.x, rect.y, rect.width - (Sizes.Timeline.widthColorBox + Sizes.padding), EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("message"), GUIContent.none);
                    EditorGUI.PropertyField(
                        new Rect(rect.x + rect.width - Sizes.Timeline.widthColorBox, rect.y, Sizes.Timeline.widthColorBox, EditorGUIUtility.singleLineHeight),
                        element.FindPropertyRelative("color"), GUIContent.none);
                };
        }

        private void RefreshMessageDifferences()
        {
            colourMessages.Clear();
            nonAddedMessages.Clear();
            for (int i = 0; i < _colours.arraySize; i++)
            {
                colourMessages.Add(_colours.GetArrayElementAtIndex(i).FindPropertyRelative("message").stringValue);
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

        private void AddMissingMessage(int selected)
        {
            int ind = 0;
            if (_colours.arraySize > 0)
            {
                ind = _colours.arraySize - 1;
            }
            _colours.InsertArrayElementAtIndex(ind);
            _colours.GetArrayElementAtIndex(_colours.arraySize - 1).FindPropertyRelative("message").stringValue = nonAddedMessages[selected];

            nonAddedMessages.RemoveAt(selected);
        }
    }

}