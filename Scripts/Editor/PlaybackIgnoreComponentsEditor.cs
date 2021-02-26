using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Reflection;
using System.Linq;
using PlayRecorder.Tools;

namespace PlayRecorder
{
    [CustomEditor(typeof(PlaybackIgnoreComponentsObject))]
    public class PlaybackIgnoreComponentsEditor : Editor
    {
        private PlaybackIgnoreComponentsObject _ignoreItems;

        private List<string> _allTypes = new List<string>();
        private List<string> _availableTypes = new List<string>();

        private List<Rect> _popupRect = new List<Rect>();

        private Vector2 _availableScroll;

        private void SetProperties()
        {
            _ignoreItems = (PlaybackIgnoreComponentsObject)serializedObject.targetObject;
        }

        public void OnEnable()
        {
            SetProperties();
            SetTypes();
        }

        private void SetTypes()
        {
            _allTypes.Clear();
            _allTypes.Add(typeof(RecordComponent).ToString());
            foreach (Type type in
                Assembly.GetAssembly(typeof(RecordComponent)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(RecordComponent))))
            {
                _allTypes.Add(type.ToString());
            }
        }

        private void CheckTypeDifferences()
        {
            _availableTypes.Clear();
            for (int i = 0; i < _allTypes.Count; i++)
            {
                if(_ignoreItems.ignoreItems.FindIndex(x => x.recordComponent == _allTypes[i]) == -1)
                {
                    _availableTypes.Add(_allTypes[i]);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            SetProperties();
            CheckTypeDifferences();

            EditorGUILayout.HelpBox("This file allows you to specify which particular components you want to modify when playback starts, usually to stop dynamic logic taking control.", MessageType.Info);
            EditorGUILayout.HelpBox("By default RecordComponents may try to disable specific components (not all RecordComponents will try to), by adding components to this file you can override their effects.", MessageType.Info);

            EditorGUILayout.LabelField("Available Record Components", Styles.textBold);
            _availableScroll = EditorGUILayout.BeginScrollView(_availableScroll,GUILayout.Height(100));
            if(_availableTypes.Count == 0)
            {
                EditorGUILayout.LabelField("All available Record Components have a filter defined.");
            }

            for (int i = 0; i < _availableTypes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+", Styles.miniButton, GUILayout.Width(Sizes.widthCharButton)))
                {
                    _ignoreItems.ignoreItems.Add(new PlaybackIgnoreItem(_availableTypes[i]));
                    serializedObject.ApplyModifiedProperties();
                    GUIUtility.ExitGUI();
                    return;
                }
                EditorGUILayout.LabelField(new GUIContent(_availableTypes[i].FormatType(),_availableTypes[i]));
                EditorGUILayout.EndHorizontal();
                EditorUtil.DrawDividerLine();
            }

            EditorGUILayout.EndScrollView();

            EditorUtil.DrawDividerLine();

            EditorGUILayout.LabelField("Current Playback Ignore Overrides", Styles.textBold);

            if(_ignoreItems.ignoreItems.Count == 0)
            {
                EditorGUILayout.LabelField("No currently specified ignore overrides.");
            }
            
            for (int i = 0; i < _ignoreItems.ignoreItems.Count; i++)
            {
                PlaybackIgnoreItem pbi = _ignoreItems.ignoreItems[i];
                _ignoreItems.ignoreItems[i].open = EditorGUILayout.Foldout(_ignoreItems.ignoreItems[i].open, new GUIContent(pbi.recordComponent.FormatType(), pbi.recordComponent),true,Styles.foldoutBold);
                if (_ignoreItems.ignoreItems[i].open)
                {
                    EditorGUI.indentLevel++;

                    _ignoreItems.ignoreItems[i].coreLogicOpen = EditorGUILayout.Foldout(_ignoreItems.ignoreItems[i].coreLogicOpen, new GUIContent("Disabled Unity Logic", "These control parameters and fundamental Unity logic items that often cannot be 'disabled', only modified."), true);
                    if(_ignoreItems.ignoreItems[i].coreLogicOpen)
                    {
                        pbi.makeKinematic = EditorGUILayout.Toggle(new GUIContent("Make Object Kinematic","Makes the object kinematic, preventing gravitational changes."), pbi.makeKinematic);
                        pbi.disableCollisions = EditorGUILayout.Toggle(new GUIContent("Disable Collisions", "Disables all object colliders."), pbi.disableCollisions);
                        pbi.disableRenderer = EditorGUILayout.Toggle(new GUIContent("Disable Renderers", "Turns off 2D and 3D renderers."), pbi.disableRenderer);
                        pbi.disableCamera = EditorGUILayout.Toggle(new GUIContent("Disable Camera", "Turns off attached cameras."), pbi.disableCamera);
                        pbi.disableVRCamera = EditorGUILayout.Toggle(new GUIContent("Disable VR Camera", "Forces camera to render to desktop, not headset."), pbi.disableVRCamera);
                    }

                    _ignoreItems.ignoreItems[i].componentsOpen = EditorGUILayout.Foldout(_ignoreItems.ignoreItems[i].componentsOpen, new GUIContent("Enabled Components (" + _ignoreItems.ignoreItems[i].enabledComponents.Count + ")", "The components that will remain enabled when playback begins."), true);
                    if (_ignoreItems.ignoreItems[i].componentsOpen)
                    {
                        for (int j = 0; j < pbi.enabledComponents.Count; j++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button(new GUIContent("-", "Remove this class from the ignore list."), Styles.miniButton, GUILayout.Width(Sizes.widthCharButton)))
                            {
                                pbi.enabledComponents.RemoveAt(j);
                                GUIUtility.ExitGUI();
                            }
                            pbi.enabledComponents[j] = EditorGUILayout.TextField(pbi.enabledComponents[j]);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                    if (_popupRect.Count <= i)
                    {
                        _popupRect.Add(new Rect());
                    }
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent("Add Components from GameObject","Allows you to select a GameObject in the scene and quickly select components you want to ignore.")))
                    {
                        PopupWindow.Show(_popupRect[i], new PlaybackIgnorePopup(this, i, _ignoreItems.ignoreItems[i].enabledComponents, _popupRect[i].width));
                    }
                    if (GUILayout.Button(new GUIContent("Add Empty","Adds an empty row allowing you to add whatever text you wish. Logic is tested on a string contains test so you could include a whole namespace as an example.")))
                    {
                        pbi.enabledComponents.Add("");
                    }
                    EditorGUILayout.EndHorizontal();
                    if (Event.current.type == EventType.Repaint)
                    {
                        _popupRect[i] = GUILayoutUtility.GetLastRect();
                    }
                    EditorGUI.indentLevel--;
                }
                EditorUtil.DrawDividerLine();
            }
        }

        public void AddIgnoreItem(int index, string item)
        {
            _ignoreItems.ignoreItems[index].enabledComponents.Add(item);
            _ignoreItems.ignoreItems[index].componentsOpen = true;
        }
    }
}