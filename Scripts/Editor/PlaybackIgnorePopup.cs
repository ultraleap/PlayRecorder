using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;

namespace PlayRecorder
{
    public class PlaybackIgnorePopup : PopupWindowContent
    {
        private PlaybackIgnoreComponentsEditor _editor;
        private int _index;
        private List<string> _currentItems = new List<string>();
        private List<int> _availableItems = new List<int>();
        private List<string> _cleanItems = new List<string>();
        private float _width = 0;

        private Behaviour _sceneObject;

        private Behaviour[] _currentBehaviours;

        public PlaybackIgnorePopup(PlaybackIgnoreComponentsEditor editor,int index, List<string> currentItems, float width)
        {
            _editor = editor;
            _index = index;
            _currentItems = currentItems;
            _width = width;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_width,((Sizes.heightLine + (Sizes.padding * 2)) * 1) + (_availableItems.Count > 0 ? ((_availableItems.Count + 1) * (Sizes.heightLine+Sizes.padding)) : 0));
        }

        public override void OnGUI(Rect rect)
        {
            Behaviour oldScene = _sceneObject;

            _sceneObject = (Behaviour)EditorGUILayout.ObjectField(new GUIContent("Game Object"), _sceneObject, typeof(Behaviour),true);

            if(_sceneObject != oldScene)
            {
                if(_sceneObject == null)
                {
                    _currentBehaviours = new Behaviour[] { };
                }
                else
                {
                    _currentBehaviours = _sceneObject.GetComponents<Behaviour>();
                }
                _availableItems.Clear();
                _cleanItems.Clear();
                int lastIndex = -1;
                for (int i = 0; i < _currentBehaviours.Length; i++)
                {
                    if (!_currentItems.Contains(_currentBehaviours[i].GetType().ToString()))
                    {
                        int j = i;
                        _availableItems.Add(j);
                        string t = _currentBehaviours[i].GetType().ToString().ToString();
                        lastIndex = t.LastIndexOf('.');
                        if (lastIndex == -1)
                        {
                            _cleanItems.Add(t);
                        }
                        else
                        {
                            _cleanItems.Add(t.Substring(lastIndex + 1));
                        }
                    }
                }
            }

            if (_currentBehaviours != null && _availableItems.Count > 0)
            {
                EditorGUILayout.LabelField("Available Scripts", EditorStyles.boldLabel);
                for (int i = 0; i < _availableItems.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    if(GUILayout.Button("+",Styles.miniButton,GUILayout.Width(Sizes.widthCharButton)))
                    {
                        _editor.AddIgnoreItem(_index, _currentBehaviours[_availableItems[i]].GetType().ToString());
                        _availableItems.RemoveAt(i);
                        _cleanItems.RemoveAt(i);
                        GUIUtility.ExitGUI();
                    }
                    GUIContent g = EditorGUIUtility.ObjectContent(_currentBehaviours[_availableItems[i]], null);
                    g.text = _cleanItems[i];
                    g.tooltip = _currentBehaviours[_availableItems[i]].GetType().ToString();
                    EditorGUILayout.LabelField(g);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
}