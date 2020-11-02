using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;

namespace PlayRecorder.Timeline
{

    public class TimelineWindow : EditorWindow
    {

        public PlaybackManager playbackManager = null;

        [SerializeField]
        List<DataCache> _dataCache = new List<DataCache>();

        [SerializeField]
        Data _currentData;

        [SerializeField]
        bool _emptyOnLoad = false;

        [SerializeField]
        double _maximumTime = -1;

        [SerializeField]
        int _maximumTick = -1;

        [SerializeField]
        Rect _currentTimelineWrapperRect, _currentTimelineRect, _timelineTickerRect, _timelineScrubRect;

        Rect _windowRect;
        float _oldWidth = -1;

        [SerializeField]
        Vector2 _scrollPos;

        [SerializeField]
        Color _normalBackground, _darkerBackground, _lighterBackground;

        double _oldTime, _newTime, _deltaTime, _regenerateCounter;

        bool _awaitingTextures = false;

        float _scrollbarWidth = 13;

        [SerializeField]
        List<Texture2D> _messageTextures = new List<Texture2D>();

        [MenuItem("Tools/PlayRecorder/Timeline")]
        static void Init()
        {
            TimelineWindow window = (TimelineWindow)GetWindow(typeof(TimelineWindow));
            window.titleContent = new GUIContent("Timeline", Resources.Load<Texture>("Images/playrecorder"));

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

        private void OnInspectorUpdate()
        {
            if(Application.isPlaying && playbackManager != null && !playbackManager.isPaused)
                Repaint();
        }

        private void Update()
        {
            _newTime = EditorApplication.timeSinceStartup;
            _deltaTime = _newTime - _oldTime;
            _oldTime = _newTime;
            if(_regenerateCounter > 0)
            {
                _regenerateCounter -= _deltaTime;
                if(_regenerateCounter <= 0)
                {
                    GenerateTextures();
                }
            }
        }

        void Startup()
        {
            playbackManager = FindObjectOfType<PlaybackManager>();
            if(playbackManager == null)
            {
                return;
            }
            _windowRect = position;
            _dataCache = playbackManager.GetDataCache();
            playbackManager.OnDataCacheChange -= OnDataCacheChange;
            playbackManager.OnDataCacheChange += OnDataCacheChange;
            playbackManager.OnDataChange -= OnDataChange;
            playbackManager.OnDataChange += OnDataChange;
            _normalBackground = GUI.backgroundColor;
            float h, s, v;
            Color.RGBToHSV(GUI.backgroundColor, out h, out s, out v);
            _darkerBackground = Color.HSVToRGB(h, s, v * 0.5f);
            _lighterBackground = Color.HSVToRGB(h, s, v * 1.5f);
            if (_dataCache.Count == 0)
            {
                _emptyOnLoad = true;
            }
            else
            {
                DataSet();
            }
            _regenerateCounter = 0.2f;
        }

        void DataSet()
        {
            _currentData = playbackManager.currentData;
            ChangeMaximumFrame();
            GenerateTextures();
        }

        private void OnGUI()
        {
            if(Event.current.type == EventType.Repaint)
            {
                if(_windowRect != position)
                {
                    _windowRect = position;
                    _regenerateCounter = 0.3f;
                    _scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth;
                }

            }

            if(playbackManager == null)
            {
                Startup();
                EditorGUILayout.LabelField("Please add a PlaybackManager to your scene before trying to use the timeline window.");
            }
            else
            {
                EditorGUI.BeginDisabledGroup(playbackManager.changingFiles);
                if(CacheCheck())
                {
                    EditorGUILayout.BeginHorizontal();
                    RecordingInfo();
                    EditorGUILayout.EndHorizontal();
                    EditorUtils.DrawUILine(Color.grey, 1, 4);
                    TimelineHeader();
                    Timeline();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        // we shall see...
        private void OnSceneGUI()
        {
            Handles.BeginGUI();

            Handles.EndGUI();
        }

        private void OnDestroy()
        {
            playbackManager.OnDataCacheChange -= OnDataCacheChange;
            playbackManager.OnDataChange -= OnDataChange;
        }

        void OnDataCacheChange(List<DataCache> cache)
        {
            _dataCache = new List<DataCache>(cache);
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
        }

        void TimelineHeader()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = playbackManager.isPaused ? Styles.red : Styles.green;
            GUI.Box(new Rect(_timelineTickerRect.x, _timelineTickerRect.y+18, _timelineTickerRect.width * (playbackManager.currentTick / _maximumTick), 1),"");

            GUI.backgroundColor = _normalBackground;
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            GUIContent pp = playbackManager.isPaused ? EditorGUIUtility.IconContent("PlayButton") : EditorGUIUtility.IconContent("PauseButton");
            pp.tooltip = Application.isPlaying ? "Play/pause the current file." : "Please enter play mode to play recording.";
            if (GUILayout.Button(pp, Styles.buttonIcon, GUILayout.Width(32)))
            {
                playbackManager.TogglePlaying();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.LabelField("0",GUILayout.Width(9));
            
            if(Event.current.type == EventType.Repaint)
            {
                _timelineTickerRect = GUILayoutUtility.GetLastRect();
            }

            EditorGUILayout.LabelField(new GUIContent(playbackManager.currentTick + " - " + TimeUtil.ConvertToTime((double)playbackManager.currentTime) + " / " + TimeUtil.ConvertToTime(_maximumTime),"The end time value shown here is the end time of the longest file loaded."), Styles.textCentered,GUILayout.ExpandWidth(true));

            EditorGUILayout.LabelField(_maximumTick.ToString(), GUILayout.Width(9 + ((_maximumTick.ToString().Length-1) * 7)));

            if (Event.current.type == EventType.Repaint)
            {
                Rect t = GUILayoutUtility.GetLastRect();

                _timelineTickerRect.width = ((t.x + t.width) - _timelineTickerRect.x)-10;
                _timelineTickerRect.x -= 1;
            }

            EditorGUILayout.EndHorizontal();

        }

        void Timeline()
        {
            //GUILayout.EndArea();
            var tempRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            _scrollPos = GUI.BeginScrollView(tempRect, _scrollPos, _currentTimelineRect,false,true);
            if (Event.current.type == EventType.Repaint)
            {
                _currentTimelineWrapperRect = tempRect;
                _currentTimelineRect = _currentTimelineWrapperRect;
                _currentTimelineRect.height = _dataCache.Count * 22;
                _currentTimelineRect.width -= GUI.skin.verticalScrollbar.fixedWidth + 4;
                _timelineScrubRect = _currentTimelineRect;
                _timelineScrubRect.width -= GUI.skin.verticalScrollbar.fixedWidth + 4;
                _timelineScrubRect.x += 36;
                //_timelineScrubRect.y += 50;
                //_currentTimelineWrapperRect.y = 0;
            }
            if(playbackManager.hasStarted && Event.current.type == EventType.MouseUp && _timelineScrubRect.Contains(Event.current.mousePosition) && (Event.current.mousePosition.x - 36 <= (_timelineScrubRect.width - (GUI.skin.verticalScrollbar.fixedWidth + 4))))
            {
                playbackManager.ScrubTick((int)(_maximumTick * (float)(Event.current.mousePosition.x - 36) / (_timelineScrubRect.width - (GUI.skin.verticalScrollbar.fixedWidth + 4))));
            }
            for (int i = 0; i < _dataCache.Count; i++)
            {
                GUI.backgroundColor = _normalBackground;
                if(GUI.Button(new Rect(2,_currentTimelineRect.y + (i*22),32,20),new GUIContent(playbackManager.currentFileIndex == i ? "▶" + (i + 1).ToString() : (i + 1).ToString(), "Change the currently selected file to this file.")))
                {
                    playbackManager.ChangeCurrentFile(i);
                }
                if(i == playbackManager.currentFileIndex)
                {
                    GUI.backgroundColor = _lighterBackground;
                }
                else
                {
                    GUI.backgroundColor = _darkerBackground;
                }
                if(GUI.Button(new Rect(36, _currentTimelineWrapperRect.y + (i * 22), ((float)(_currentTimelineRect.width-34) * ((float)_dataCache[i].frameCount / _maximumTick)), 20), "", Styles.boxBorder) && playbackManager.currentFileIndex != i)
                {
                    playbackManager.ChangeCurrentFile(i);
                }
                GUI.Label(new Rect(36 + ((float)(_currentTimelineRect.width - 34) * ((float)_dataCache[i].frameCount / _maximumTick)), _currentTimelineRect.y + (i * 22), 100, 20), TimeUtil.ConvertToTime((float)_dataCache[i].frameCount / _dataCache[i].frameRate));
            }

            if(Application.isPlaying)
            {
                GUI.backgroundColor = playbackManager.isPaused ? Styles.red : Styles.green;
                GUI.Box(new Rect(36 + ((float)(_currentTimelineRect.width - 34) * ((float)playbackManager.currentTick / _maximumTick)),_currentTimelineRect.y,1,_currentTimelineRect.height),"");
            }
            GUI.backgroundColor = _normalBackground;
            GUI.EndScrollView();
        }

        void GenerateTextures()
        {
            if(_oldWidth == _windowRect.width)
            {
                return;
            }

            Debug.Log(_windowRect.width - (_scrollbarWidth + 38));

            for (int i = 0; i < _dataCache.Count; i++)
            {
                //tba
            }
        }

    }

}