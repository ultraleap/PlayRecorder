using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PlayRecorder.Tools
{

    public class StopwatchWindow : EditorWindow
    {

        public PlaybackManager playbackManager;

        private float _startTime, _stopTime, _differenceTime;

        private List<float> _previousTimes = new List<float>();

        [SerializeField]
        private bool _expandedHistory = true;

        [MenuItem("Ultraleap/PlayRecorder/Stopwatch")]
        static public void Init()
        {
            StopwatchWindow window = GetWindow<StopwatchWindow>();
            window.titleContent = new GUIContent("Stopwatch", Resources.Load<Texture>("Images/playrecorder"));

            window.playbackManager = FindObjectOfType<PlaybackManager>();

            if (window.playbackManager != null)
            {
                window.Show();
            }
            else
            {
                Debug.LogError("Please add a PlaybackManager to your scene before trying to open the timeline window.");
            }
        }

        private void OnEnable()
        {
            Startup();
        }

        private void Startup()
        {
            playbackManager = FindObjectOfType<PlaybackManager>();
        }

        private void OnGUI()
        {

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Start"))
            {
                _startTime = playbackManager.currentTime;
            }
            if (GUILayout.Button("Stop"))
            {
                _stopTime = playbackManager.currentTime;
                _differenceTime = _stopTime - _startTime;
                float d = _differenceTime;
                _previousTimes.Add(d);
                if(_previousTimes.Count > 10)
                {
                    _previousTimes.RemoveAt(0);
                }
            }
            if (GUILayout.Button("Reset"))
            {
                _startTime = 0;
                _stopTime = 0;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Start", GUILayout.Width(70));
            EditorGUILayout.FloatField(_startTime);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Stop", GUILayout.Width(70));
            EditorGUILayout.FloatField(_stopTime);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Difference",GUILayout.Width(70));
            EditorGUILayout.FloatField(_differenceTime);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            _expandedHistory = EditorGUILayout.Foldout(_expandedHistory, "History (" + _previousTimes.Count + ")",true);

            if (GUILayout.Button("Clear History",GUILayout.Width(90)))
            {
                _previousTimes.Clear();
            }

            EditorGUILayout.EndHorizontal();

            if (_expandedHistory)
            {
                for (int i = _previousTimes.Count - 1; i > -1; i--)
                {
                    EditorGUILayout.FloatField(_previousTimes[i]);
                }
            }
        }
    }

}
