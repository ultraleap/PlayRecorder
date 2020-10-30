using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;

namespace PlayRecorder.Timeline
{

    public class TimelineWindow : EditorWindow
    {

        public PlaybackManager playbackManager { get; set; } = null;

        List<DataCache> _dataCache = new List<DataCache>();

        Data _currentData;

        bool _emptyOnLoad = false;

        double _maximumTime = -1;

        int _maximumTick = -1;
        
        [MenuItem("Tools/PlayRecorder/Timeline")]
        static void Init()
        {
            TimelineWindow window = (TimelineWindow)GetWindow(typeof(TimelineWindow));
            window.titleContent = new GUIContent("Timeline", Resources.Load<Texture>("Images/playrecorder"));

            window.playbackManager = FindObjectOfType<PlaybackManager>();

            if(window.playbackManager != null)
            {
                window.Show();
            }
            else
            {
                Debug.LogError("Please add a PlaybackManager to your scene before trying to open the timeline window.");
            }

        }

        private void Awake()
        {
            playbackManager = FindObjectOfType<PlaybackManager>();
            _dataCache = playbackManager.GetDataCache();
            playbackManager.OnDataCacheChange += OnDataCacheChange;
            playbackManager.OnDataChange += OnDataChange;
            if(_dataCache.Count == 0)
            {
                _emptyOnLoad = true;
            }
            else
            {
                DataSet();
            }
        }

        void DataSet()
        {
            _currentData = playbackManager.currentData;
            ChangeMaximumFrame();
        }

        private void OnGUI()
        {
            if(playbackManager == null)
            {
                EditorGUILayout.LabelField("Please add a PlaybackManager to your scene before trying to use the timeline window.");
            }
            else
            {
                EditorGUI.BeginDisabledGroup(playbackManager.changingFiles);
                if(CacheCheck())
                {
                    RecordingInfo();
                    TimelineHeader();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void OnDestroy()
        {
            playbackManager.OnDataCacheChange -= OnDataCacheChange;
            playbackManager.OnDataChange -= OnDataChange;
        }

        void OnDataCacheChange(List<DataCache> cache)
        {
            _dataCache = cache;
            DataSet();
        }

        void OnDataChange(Data data)
        {
            _currentData = data;
        }

        void ChangeMaximumFrame()
        {
            _maximumTime = -1;
            for (int i = 0; i < _dataCache.Count; i++)
            {
                if(((float)_dataCache[i].frameCount / _dataCache[i].frameRate) > _maximumTime)
                {
                    _maximumTime = ((double)_dataCache[i].frameCount / _dataCache[i].frameRate);
                    _maximumTick = _dataCache[i].frameCount;
                }
            }
        }

        bool CacheCheck()
        {
            if(_dataCache.Count == 0 && _emptyOnLoad)
            {
                _dataCache = playbackManager.GetDataCache();
                if(_dataCache.Count > 0)
                {
                    _emptyOnLoad = false;
                    DataSet();
                }
            }
            if(_dataCache.Count == 0)
            {
                EditorGUILayout.LabelField("No files currently loaded. Please add files to the PlaybackManager and press the Update Files button");
                return false;
            }
            return true;
        }

        void RecordingInfo()
        {
            EditorGUILayout.LabelField("Current File: (" + (playbackManager.currentFileIndex+1) + ") " + _dataCache[playbackManager.currentFileIndex].name, Styles.textBold);
            
            EditorUtils.DrawUILine(Color.grey, 1, 4);
        }

        void TimelineHeader()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("0",GUILayout.Width(9));

            EditorGUILayout.LabelField(new GUIContent(playbackManager.currentTick + " - " + TimeUtil.ConvertToTime((double)playbackManager.currentTime) + " / " + TimeUtil.ConvertToTime(_maximumTime),"The end time value shown here is the end time of the longest file loaded."), Styles.textCentered,GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField(_maximumTick.ToString(), GUILayout.Width(9 + ((_maximumTick.ToString().Length-1) * 7)));

            EditorGUILayout.EndHorizontal();

        }

    }

}