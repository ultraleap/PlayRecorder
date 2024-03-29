﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;
using System.Reflection;

namespace PlayRecorder.Statistics
{

    public class StatisticWindow : EditorWindow
    {
        public PlaybackManager playbackManager = null;

        [SerializeReference, HideInInspector]
        private List<DataCache> _dataCache = new List<DataCache>();
        private string[] _fileNames = null;

        [System.Serializable]
        public class StatCache
        {
            public int fileIndex = -1, messageIndex = -1, maxFrame = -1, statCount = -1;
            public string fileName = "";
            public bool validStat = false;
            public string statName = "";
            public string current = "", final = "";
            public object[] values;
            public System.Type type;
            public FieldInfo[] statFields;
            public float[] statTimes;
            public float maxTime;
            public RecordMessage recordMessage;
            public bool expanded = false;
            public int frameIndex = 0;
            public float positive, negative;
            public Texture2D graph;
        }
        [SerializeField]
        private List<StatCache> _statCache = new List<StatCache>();

        [SerializeField]
        private bool _emptyOnLoad = false;
        [SerializeField]
        private bool _followPlayback = false, _followFile = true, _allFiles = false;

        private bool _playStarted = false;

        [SerializeField]
        private int _fileIndex = -1;

        private Vector2 _messageScroll;
        private float _scrollbarWidth;
        private bool _scrollBarActive = false;

        private int _statCount = 0;
        private Rect _windowRect, _csvCurrentRect;
        private double _oldTime, _newTime, _deltaTime, _regenerateCounter;
        private int _globalFrame = 0, _globalMaxFrame = 0;

        private Texture2D _indicator;
        private List<Color> _graphColors;
        private Color _indicatorColor = Color.white;
        private Color _backgroundColor = Color.black;

        private GUIContent _copyButton;
        private GUIContent _skipToEndButton;
        private GUIStyle _wrappedLabel = null;

        [MenuItem("Ultraleap/PlayRecorder/Statistics")]
        public static void Init()
        {
            StatisticWindow window = GetWindow<StatisticWindow>();

            window.titleContent = new GUIContent("Statistics", Resources.Load<Texture>("Images/playrecorder"));

            window.playbackManager = FindObjectOfType<PlaybackManager>();

            if (window.playbackManager != null)
            {
                window.Show();
            }
            else
            {
                Debug.LogError(EditorMessages.noPlaybackManager);
            }
        }

        private void OnEnable()
        {
            Startup();
        }

        private void OnDestroy()
        {
            if (playbackManager != null)
            {
                playbackManager.OnDataCacheChange -= OnDataCacheChange;
                playbackManager.OnUpdateTick -= OnUpdateTick;
            }
        }

        private void Startup()
        {
            playbackManager = FindObjectOfType<PlaybackManager>();
            if (playbackManager == null)
            {
                return;
            }
            _dataCache = new List<DataCache>(playbackManager.GetDataCache());
            _dataCache.RemoveAll(item => item == null);

            playbackManager.OnDataCacheChange -= OnDataCacheChange;
            playbackManager.OnDataCacheChange += OnDataCacheChange;

            playbackManager.OnUpdateTick -= OnUpdateTick;
            playbackManager.OnUpdateTick += OnUpdateTick;

            playbackManager.OnPlaybackStart -= OnPlaybackStart;
            playbackManager.OnPlaybackStart += OnPlaybackStart;

            _copyButton = new GUIContent(EditorGUIUtility.IconContent("Clipboard"));
            _copyButton.tooltip = "Copy the field to the system clipboard.";

            _skipToEndButton = new GUIContent(EditorGUIUtility.IconContent("Animation.LastKey"));
            _skipToEndButton.tooltip = "Set the current frame to the last possible frame.";

            _backgroundColor = EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255);

            if (_dataCache.Count > 0)
            {
                _fileIndex = playbackManager.currentFileIndex;
            }

            _graphColors = new List<Color>();
            int maxCol = 4;
            for (int i = 0; i < maxCol; i++)
            {
                _graphColors.Add(Color.HSVToRGB(((float)i) / (maxCol - 1), .75f, 1f));
            }

            if (_dataCache.Count == 0)
            {
                _emptyOnLoad = true;
            }
            else
            {
                DataSet();
                if (_fileIndex != -1)
                {
                    SetUILists();
                }
            }

            _wrappedLabel = new GUIStyle(EditorStyles.label);
            _wrappedLabel.wordWrap = true;
        }

        private void OnPlaybackStart()
        {
            _playStarted = true;
        }

        private void Update()
        {
            _newTime = EditorApplication.timeSinceStartup;
            _deltaTime = _newTime - _oldTime;
            _oldTime = _newTime;
            if (_regenerateCounter > 0)
            {
                _regenerateCounter -= _deltaTime;
                if (_regenerateCounter <= 0)
                {
                    Graphs();
                }
            }
        }

        private void OnUpdateTick()
        {
            if (_followPlayback)
            {
                _globalFrame = playbackManager.currentTick;
                for (int i = 0; i < _statCache.Count; i++)
                {
                    _statCache[i].frameIndex = _globalFrame;
                }
            }
        }

        private void OnDataCacheChange(List<DataCache> cache)
        {
            _dataCache = new List<DataCache>(cache);
            _dataCache.RemoveAll(item => item == null);
            if (_fileIndex > _dataCache.Count)
            {
                _fileIndex = _dataCache.Count - 1;
            }
            if (_followFile)
            {
                _fileIndex = playbackManager.currentFileIndex;
            }
            DataSet();
            SetUILists();
        }

        private void DataSet()
        {
            _fileNames = _dataCache.Select(x => x.fileName).ToArray();
        }

        private bool CacheCheck()
        {
            if (_dataCache.Count == 0 && _emptyOnLoad)
            {
                _dataCache = playbackManager.GetDataCache();
                _dataCache.RemoveAll(item => item == null);
                if (_dataCache.Count > 0)
                {
                    _emptyOnLoad = false;
                    DataSet();
                    if (_fileIndex != -1)
                    {
                        SetUILists();
                    }
                }
            }
            if (_dataCache.Count == 0 || playbackManager.currentFileIndex == -1)
            {
                EditorGUILayout.LabelField(EditorMessages.noFilesInPlayback, _wrappedLabel);
                if (GUILayout.Button("Open Playback Manager"))
                {
                    Selection.activeObject = playbackManager.gameObject;
                }
                return false;
            }
            return true;
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying && playbackManager != null && !playbackManager.isPaused && _followPlayback)
                Repaint();
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                if (_windowRect != position)
                {
                    _windowRect = position;
                    _regenerateCounter = 0.2f;
                    _scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth + 1;
                }
            }

            if (playbackManager == null)
            {
                Startup();
                EditorGUILayout.LabelField(EditorMessages.noPlaybackManager);
            }
            else
            {
                EditorGUI.BeginDisabledGroup(playbackManager.changingFiles);
                if (CacheCheck())
                {
                    EditorGUILayout.BeginHorizontal();
                    FileDropdown();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    GlobalFrame();
                    EditorGUILayout.EndHorizontal();
                    EditorUtil.DrawDividerLine();
                    EditorGUILayout.BeginHorizontal();
                    ExportCSVButtons();
                    EditorGUILayout.LabelField("|", Styles.textCentered, GUILayout.Width(Sizes.widthIcon));
                    ExpandCollapseButtons();
                    EditorGUILayout.EndHorizontal();
                    EditorUtil.DrawDividerLine();
                    if (_fileIndex != -1)
                    {
                        Messages();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void FileDropdown()
        {
            EditorGUI.BeginDisabledGroup(_allFiles || _followFile);
            GUIContent label = new GUIContent("Current File");
            EditorGUILayout.LabelField(label, Styles.textBold, GUILayout.Width(Styles.textBold.CalcSize(label).x));
            int oldInd = _fileIndex;
            _fileIndex = EditorGUILayout.Popup(_fileIndex, _allFiles ? new string[] { "All Files" } : _fileNames);
            if (oldInd != _fileIndex)
            {
                SetUILists();
            }
            if (_allFiles)
            {
                EditorGUILayout.LabelField("");
            }
            else
            {
                EditorGUILayout.LabelField(_fileIndex != -1 ? _dataCache[_fileIndex].name : "");
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(_allFiles);
            GUIContent matchPlayFile = new GUIContent("Follow File", "Matches the current statistics set to the currently selected playback file.");
            EditorGUILayout.LabelField(matchPlayFile, GUILayout.Width(EditorStyles.label.CalcSize(matchPlayFile).x));
            bool oldMatchFiles = _followFile;
            _followFile = EditorGUILayout.Toggle(_followFile, GUILayout.Width(Sizes.widthIcon));
            if (_followFile != oldMatchFiles && CheckFollowUpdate())
            {
                SetUILists();
            }
            EditorGUI.EndDisabledGroup();

            GUIContent matchlabel = new GUIContent("All Files", "Shows all statistics for the currently loaded playback files in this window in one big list.");
            EditorGUILayout.LabelField(matchlabel, GUILayout.Width(EditorStyles.label.CalcSize(matchlabel).x));
            bool oldAllFiles = _allFiles;
            _allFiles = EditorGUILayout.Toggle(_allFiles, GUILayout.Width(Sizes.widthIcon));
            if (_allFiles != oldAllFiles)
            {
                SetUILists();
            }
        }

        private void GlobalFrame()
        {
            if (_fileIndex == -1)
                return;

            GUIContent matchlabel = new GUIContent("Match Playback");
            EditorGUILayout.LabelField(matchlabel, GUILayout.Width(EditorStyles.label.CalcSize(matchlabel).x));
            _followPlayback = EditorGUILayout.Toggle(_followPlayback, GUILayout.Width(Sizes.widthIcon));

            EditorGUI.BeginDisabledGroup(Application.isPlaying && _playStarted && _followPlayback);
            GUIContent label = new GUIContent("Global Frame");
            EditorGUILayout.LabelField(label, EditorStyles.label, GUILayout.Width(EditorStyles.label.CalcSize(label).x));
            int oldInd = _globalFrame;
            _globalFrame = EditorGUILayout.IntSlider(_globalFrame, 0, _globalMaxFrame);

            if (GUILayout.Button(_skipToEndButton, GUILayout.Width(Sizes.Timeline.widthFileButton)))
            {
                _globalFrame = _globalMaxFrame;
            }

            if (oldInd != _globalFrame)
            {
                for (int i = 0; i < _statCache.Count; i++)
                {
                    if (!_allFiles && _statCache[i].fileIndex != _fileIndex)
                        continue;

                    _statCache[i].frameIndex = _globalFrame;
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        private bool CheckFollowUpdate()
        {
            return _followFile && _fileIndex != playbackManager.currentFileIndex;
        }

        private void SetUILists()
        {
            if (_followFile)
            {
                _fileIndex = playbackManager.currentFileIndex;

            }
            int size = 0;
            int frame = 0;
            for (int i = 0; i < _dataCache.Count; i++)
            {
                size += _dataCache[i].messages.Count;
                if (_dataCache[i].frameCount > frame)
                {
                    frame = _dataCache[i].frameCount;
                }
            }
            _statCache.Resize(size);
            if (_allFiles)
            {
                _globalFrame = frame;
                _globalMaxFrame = frame;
            }
            else
            {
                if (_fileIndex >= _dataCache.Count)
                {
                    _fileIndex = _dataCache.Count - 1;
                }
                if (_fileIndex != -1)
                {
                    _globalFrame = _dataCache[_fileIndex].frameCount;
                    _globalMaxFrame = _globalFrame;
                }
            }
            SetStatCache();
            Graphs();
        }

        private void Graphs()
        {
            _indicator = new Texture2D(1, 1);
            _indicator.SetPixel(0, 0, _indicatorColor);
            _indicator.Apply();

            _backgroundColor = EditorGUIUtility.isProSkin ? new Color32(56, 56, 56, 255) : new Color32(194, 194, 194, 255);

            for (int i = 0; i < _statCache.Count; i++)
            {
                if (!_allFiles && _statCache[i].fileIndex != _fileIndex || _dataCache[_statCache[i].fileIndex].frameCount == 0)
                {
                    continue;
                }
                _statCache[i].graph = StatisticGraph.GenerateGraph(_statCache[i], (int)(_windowRect.width - 16 - (_scrollBarActive ? (_scrollbarWidth - 1) : 0)), 80, _globalMaxFrame, _graphColors, _backgroundColor);
            }
        }

        private void SetStatCache()
        {
            int c = 0;
            for (int i = 0; i < _dataCache.Count; i++)
            {
                SetStatCacheIndividual(c, i, _dataCache[i]);
                c += _dataCache[i].messages.Count;
            }
        }

        private void SetStatCacheIndividual(int indexOffset, int file, DataCache cache)
        {
            int f = file;
            int ind;
            for (int i = 0; i < cache.messages.Count; i++)
            {
                ind = indexOffset + i;
                int mi = i;
                FieldInfo[] fi = cache.messages[mi].GetType().GetFields().Where(x => x.Name != "message" && x.Name != "frames" && x.DeclaringType.BaseType == typeof(RecordMessage)).ToArray();
                if (fi.Length == 0)
                {
                    continue;
                }
                List<object> objectList = new List<object>();
                FieldInfo[] fields = new FieldInfo[] { };
                object obj = fi[0].GetValue(cache.messages[mi]);
                System.Type t = null;
                if (obj is IList && ((IList)obj).Count > 0)
                {
                    IList list = (IList)obj;
                    t = list[0].GetType();
                    bool simple = t.IsPrimitive || t == typeof(decimal) || t == typeof(string);
                    if (simple)
                    {
                        fields = new FieldInfo[] { null };
                    }
                    else
                    {
                        fields = list[0].GetType().GetFields().Where(x => x.IsStatic == false).ToArray();
                    }
                    if (fields.Length != 0)
                    {
                        foreach (var item in list)
                        {
                            objectList.Add(item);
                        }
                    }
                }

                if (fields.Length == 0)
                {
                    Debug.Log("Some recorded stats are not currently supported. (" + fi[0].FieldType.GetType().ToString() + ")");
                    continue;
                }
                if (_statCache[ind] == null)
                {
                    _statCache[ind] = new StatCache();
                    _statCache[ind].frameIndex = cache.frameCount;
                }
                _statCache[ind].values = objectList.ToArray();
                _statCache[ind].type = t;
                _statCache[ind].statFields = fields;
                _statCache[ind].fileIndex = f;
                _statCache[ind].messageIndex = mi;
                _statCache[ind].maxFrame = cache.frameCount;

                _statCache[ind].fileName = cache.name;
                _statCache[ind].statName = cache.messages[mi].message;
                _statCache[ind].recordMessage = cache.messages[mi];
                List<float> times = new List<float>();
                _statCache[ind].maxTime = (float)cache.frameCount / cache.frameRate;
                for (int j = 0; j < _statCache[ind].recordMessage.frames.Count; j++)
                {
                    times.Add((float)(InverseLerpDouble(0, cache.frameCount, _statCache[ind].recordMessage.frames[j]) * _statCache[ind].maxTime));
                }
                _statCache[ind].statTimes = times.ToArray();
                _statCache[ind].statCount = cache.messages[mi].frames.Count;
                _statCache[ind].current = "";
                _statCache[ind].final = "";
            }
        }

        private static double InverseLerpDouble(double a, double b, double v)
        {
            return (v - a) / (b - a);
        }

        private void ExportCSVButtons()
        {
            bool current = false, final = false;
            GUIContent currentValues = new GUIContent("Export Statistics to CSV", "Exports the visible list of statistics current values. If all files is ticked, all files will be saved to the CSV as separate rows.");
            current = GUILayout.Button(currentValues);
            if (Event.current.type == EventType.Repaint)
            {
                _csvCurrentRect = GUILayoutUtility.GetLastRect();
            }

            if (current || final)
            {
                Rect b = new Rect(_csvCurrentRect.x, _csvCurrentRect.y, 0, 0);
                List<StatCache> cache;
                if (_allFiles)
                {
                    cache = _statCache.ToList();
                }
                else
                {
                    cache = _statCache.Where(x => x.fileIndex == _fileIndex).ToList();
                }
                if (cache.Count > 0)
                {
                    PopupWindow.Show(b, new StatisticCSVPopup(cache, _windowRect.width));
                }
                else
                {
                    EditorUtility.DisplayDialog("No Statistics", "You have no available statistics to export.", "Ok");
                }
            }
        }

        private void ExpandCollapseButtons()
        {
            if (GUILayout.Button("Expand All"))
            {
                for (int i = 0; i < _statCache.Count; i++)
                {
                    _statCache[i].expanded = true;
                }
            }
            if (GUILayout.Button("Collapse All"))
            {
                for (int i = 0; i < _statCache.Count; i++)
                {
                    _statCache[i].expanded = false;
                }
            }
        }

        private void Messages()
        {
            _messageScroll = EditorGUILayout.BeginScrollView(_messageScroll);
            _statCount = 0;

            for (int i = 0; i < _statCache.Count; i++)
            {
                if (!_allFiles && _statCache[i].fileIndex != _fileIndex)
                {
                    continue;
                }
                SingleMessage(_statCache[i], i);
            }

            if (_statCount == 0)
            {
                if (_allFiles)
                {
                    EditorGUILayout.LabelField("Current loaded files do not include any statistics.");
                }
                else
                {
                    EditorGUILayout.LabelField("Current file does not include any statistics.");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void SingleMessage(StatCache cache, int index)
        {
            _statCount++;
            GUILayout.BeginVertical(GUI.skin.box);

            // Convert the time to readable formats
            string timeLabel = TimeUtil.ConvertToTime(InverseLerpDouble(0, cache.maxFrame, cache.frameIndex) * cache.maxTime);
            string endTimeLabel = TimeUtil.ConvertToTime(cache.maxTime);

            GUIContent label = new GUIContent($"{(_allFiles ? (cache.fileIndex + 1).ToString() + ". " : "")} {cache.statName} - ({cache.frameIndex}/{cache.maxFrame}) - ({timeLabel}/{endTimeLabel})");
            cache.expanded = EditorGUILayout.BeginFoldoutHeaderGroup(cache.expanded, label);
            cache.validStat = false;
            Rect scrollCheck = GUILayoutUtility.GetLastRect();
            if (scrollCheck.width + _scrollbarWidth < _windowRect.width)
            {
                _scrollBarActive = true;
            }
            else
            {
                _scrollBarActive = false;
            }
            EditorGUILayout.BeginHorizontal();

            GUIContent statCount = new GUIContent($"({cache.values.Length})", "The number of recorded instances of the statistic.");
            EditorGUILayout.LabelField(statCount, Styles.textBold, GUILayout.Width(Styles.textBold.CalcSize(statCount).x));

            GUIContent currentValLab = new GUIContent("Current Value", "The value of the statistic based on the current frame.");
            EditorGUILayout.LabelField(currentValLab, GUILayout.Width(EditorStyles.label.CalcSize(currentValLab).x));
            _statCache[index].current = SingleStatBox(cache.values[GetStatIndex(cache)], cache.statFields);

            GUIContent lastValLab = new GUIContent("Final Value", "The final value of the statistic, regardless of the current frame.");
            EditorGUILayout.LabelField(lastValLab, GUILayout.Width(EditorStyles.label.CalcSize(lastValLab).x));
            _statCache[index].final = SingleStatBox(cache.values[cache.values.Length - 1], cache.statFields);
            _statCache[index].validStat = true;

            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndHorizontal();

            if (cache.expanded)
            {
                EditorGUILayout.BeginHorizontal();

                cache.frameIndex = EditorGUILayout.IntSlider("Frame", cache.frameIndex, 0, _dataCache[cache.fileIndex].frameCount);

                if (GUILayout.Button(_skipToEndButton, GUILayout.Width(Sizes.Timeline.widthFileButton)))
                {
                    cache.frameIndex = cache.maxFrame;
                }

                EditorGUILayout.EndHorizontal();

                if (cache.graph != null)
                {
                    GUILayout.Label("", GUILayout.Height(cache.graph.height + Sizes.padding));
                    Rect r = GUILayoutUtility.GetLastRect();
                    r.width = cache.graph.width;
                    r.height = cache.graph.height;
                    GUI.DrawTexture(r, cache.graph);
                    if ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDrag) && r.Contains(Event.current.mousePosition))
                    {
                        cache.frameIndex = (int)((Event.current.mousePosition.x - r.x) / (r.width - 2) * _dataCache[cache.fileIndex].frameCount);
                        Repaint();
                    }
                    Rect r2 = new Rect(r);
                    r2.x = r.x + (((float)cache.frameIndex / (float)_dataCache[cache.fileIndex].frameCount) * (r.width));
                    r2.width = 2;
                    GUI.DrawTexture(r2, _indicator);
                    GUI.Label(r, cache.positive.ToString(), Styles.textTopLeft);
                    GUI.Label(r, cache.negative.ToString(), Styles.textBottomLeft);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private int GetStatIndex(StatCache cache)
        {
            int v = 0;
            for (int i = 0; i < cache.statCount; i++)
            {
                if (cache.recordMessage.frames[i] <= cache.frameIndex)
                {
                    v = i;
                }
            }
            return v;
        }

        private string SingleStatBox(object item, FieldInfo[] info)
        {
            string output = "";
            if (info.Length == 1)
            {
                output = EditorGUILayout.TextField(item.ToString());
                CopyButton(output);
            }
            else
            {
                output = "(";
                for (int i = 0; i < info.Length; i++)
                {
                    output += info[i].GetValue(item).ToString();
                    if (i != info.Length - 1)
                    {
                        output += ", ";
                    }
                }
                output += ")";
                EditorGUILayout.TextField(output);
                CopyButton(output);
            }
            return output;
        }

        private void CopyButton(string toCopy)
        {
            if (GUILayout.Button(_copyButton, GUILayout.Width(Sizes.widthCharButton)))
            {
                GUIUtility.systemCopyBuffer = toCopy;
            }
        }
    }

}