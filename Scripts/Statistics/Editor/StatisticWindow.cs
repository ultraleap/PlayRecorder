using System.Collections;
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
            public int fileIndex = -1, messageIndex = -1, maxFrame = -1;
            public string fileName = "";
            public bool validStat = false;
            public string statName = "";
            public string current = "", final = "";
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

        [MenuItem("Tools/PlayRecorder/Statistics")]
        public static void Init()
        {
            StatisticWindow window = GetWindow<StatisticWindow>();

            window.titleContent = new GUIContent("Statistics", Resources.Load<Texture>("Images/playrecorder"));

            window.playbackManager = FindObjectOfType<PlaybackManager>();

            if(window.playbackManager != null)
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
            if(playbackManager != null)
            {
                playbackManager.OnDataCacheChange -= OnDataCacheChange;
                playbackManager.OnUpdateTick -= OnUpdateTick;
            }
        }

        private void Startup()
        {
            playbackManager = FindObjectOfType<PlaybackManager>();
            if(playbackManager == null)
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
                _graphColors.Add(Color.HSVToRGB(((float)i)/(maxCol-1), .75f, 1f));
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
            if(_followPlayback)
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
            if(_followFile)
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
                EditorGUILayout.LabelField(EditorMessages.noFilesInPlayback);
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
                if(CacheCheck())
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
                    EditorGUILayout.LabelField("|",Styles.textCentered,GUILayout.Width(Sizes.widthIcon));
                    ExpandCollapseButtons();
                    EditorGUILayout.EndHorizontal();
                    EditorUtil.DrawDividerLine();
                    if(_fileIndex != -1)
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
            _fileIndex = EditorGUILayout.Popup(_fileIndex, _allFiles ? new string[]{ "All Files"} : _fileNames);
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
            GUIContent matchPlayFile = new GUIContent("Follow File", "Matches the current statistics file to the currently selected playback file.");
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
            if(_allFiles != oldAllFiles)
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

            if(GUILayout.Button(_skipToEndButton,GUILayout.Width(Sizes.Timeline.widthFileButton)))
            {
                _globalFrame = _globalMaxFrame;
            }

            if(oldInd != _globalFrame)
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
                _statCache[i].graph = StatisticGraph.GenerateGraph(_dataCache[_statCache[i].fileIndex].messages[_statCache[i].messageIndex], _statCache[i], (int)(_windowRect.width - 16 - (_scrollBarActive ? (_scrollbarWidth-1) : 0)), 80, _globalMaxFrame, _graphColors, _backgroundColor);
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
                if(_statCache[ind] == null)
                {
                    _statCache[ind] = new StatCache();
                    _statCache[ind].frameIndex = cache.frameCount;
                }
                int mi = i;
                _statCache[ind].fileIndex = f;
                _statCache[ind].messageIndex = mi;
                _statCache[ind].maxFrame = cache.frameCount;
                _statCache[ind].fileName = cache.fileName;
                _statCache[ind].statName = cache.messages[i].message;
                _statCache[ind].current = "";
                _statCache[ind].final = "";
            }
        }

        private void ExportCSVButtons()
        {
            bool current = false, final = false;
            GUIContent currentValues = new GUIContent("Export Current Values", "Exports the visible list of statistics current values. If all files is ticked, all files will be saved to the CSV as separate rows.");
            current = GUILayout.Button(currentValues);
            if(Event.current.type == EventType.Repaint)
            {
                _csvCurrentRect = GUILayoutUtility.GetLastRect();
            }
            GUIContent finalValues = new GUIContent("Export Final Values", "Exports the visible list of statistics final values. If all files is ticked, all files will be saved to the CSV as separate rows.");
            final = GUILayout.Button(finalValues);

            if (current || final)
            {
                Rect b = new Rect(_csvCurrentRect.x, _csvCurrentRect.y, 0, 0);
                List<StatCache> cache;
                if(_allFiles)
                {
                    cache = _statCache.Where(x => x.validStat).ToList();
                }
                else
                {
                    cache = _statCache.Where(x => x.fileIndex == _fileIndex && x.validStat).ToList();
                }
                if(cache.Count > 0)
                {
                    PopupWindow.Show(b, new StatisticCSVPopup(cache, final, _windowRect.width));
                }
                else
                {
                    EditorUtility.DisplayDialog("No Statistics", "You have no available statistics to export.", "Ok");
                }
            }
        }

        private void ExpandCollapseButtons()
        {
            if(GUILayout.Button("Expand All"))
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
                if(!_allFiles && _statCache[i].fileIndex != _fileIndex)
                {
                    continue;
                }
                SingleMessage(_dataCache[_statCache[i].fileIndex].messages[_statCache[i].messageIndex], _statCache[i].fileIndex, i);
            }

            if(_statCount == 0)
            {
                if(_allFiles)
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

        private void SingleMessage(RecordMessage message, int fileIndex, int index)
        {
            if (message.GetType() == typeof(RecordMessage))
            {
                _statCache[index].validStat = false;
                return;
            }
            _statCount++;
            FieldInfo[] fields = message.GetType().GetFields();
            GUILayout.BeginVertical(GUI.skin.box);
            GUIContent label = new GUIContent((_allFiles ? (fileIndex+1).ToString() + ". " : "") + message.message + " - " + message.GetType().FormatType() + " (" + _statCache[index].frameIndex + "/" + _statCache[index].maxFrame + ")");
            _statCache[index].expanded = EditorGUILayout.BeginFoldoutHeaderGroup(_statCache[index].expanded, label);
            _statCache[index].validStat = false;
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
            for (int i = 0; i < fields.Length; i++)
            {
                // Ignore the first two
                if (fields[i].Name == "message" || fields[i].Name == "frames")
                    continue;

                object obj = fields[i].GetValue(message);
                if(obj is IList)
                {
                    IList list = (IList)obj;
                    GUIContent statCount = new GUIContent("("+list.Count.ToString()+")","The number of recorded instances of the statistic.");
                    EditorGUILayout.LabelField(statCount,Styles.textBold,GUILayout.Width(Styles.textBold.CalcSize(statCount).x));

                    GUIContent currentValLab = new GUIContent("Current Value", "The value of the statistic based on the current frame.");
                    EditorGUILayout.LabelField(currentValLab, GUILayout.Width(EditorStyles.label.CalcSize(currentValLab).x));
                    _statCache[index].current = SingleStatBox(list[GetStatIndex(message, _statCache[index].frameIndex)]);

                    GUIContent lastValLab = new GUIContent("Final Value", "The final value of the statistic, regardless of the current frame.");
                    EditorGUILayout.LabelField(lastValLab, GUILayout.Width(EditorStyles.label.CalcSize(lastValLab).x));
                    _statCache[index].final = SingleStatBox(list[list.Count-1]);
                    _statCache[index].validStat = true;
                    break;
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            EditorGUILayout.EndHorizontal();

            if (_statCache[index].expanded)
            {
                EditorGUILayout.BeginHorizontal();

                _statCache[index].frameIndex = EditorGUILayout.IntSlider("Frame", _statCache[index].frameIndex, 0, _dataCache[fileIndex].frameCount);

                if (GUILayout.Button(_skipToEndButton, GUILayout.Width(Sizes.Timeline.widthFileButton)))
                {
                    _statCache[index].frameIndex = _statCache[index].maxFrame;
                }

                EditorGUILayout.EndHorizontal();

                if (_statCache[index].graph != null)
                {
                    GUILayout.Label("", GUILayout.Height(_statCache[index].graph.height + Sizes.padding));
                    Rect r = GUILayoutUtility.GetLastRect();
                    r.width = _statCache[index].graph.width;
                    r.height = _statCache[index].graph.height;
                    GUI.DrawTexture(r, _statCache[index].graph);
                    if ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDrag) && r.Contains(Event.current.mousePosition))
                    {
                        _statCache[index].frameIndex = (int)((Event.current.mousePosition.x - r.x) / r.width * _dataCache[fileIndex].frameCount);
                        Repaint();
                    }
                    Rect r2 = new Rect(r);
                    r2.x = r.x + (((float)_statCache[index].frameIndex / (float)_dataCache[fileIndex].frameCount) * (r.width));
                    r2.width = 2;
                    GUI.DrawTexture(r2, _indicator);
                    GUI.Label(r, _statCache[index].positive.ToString(), Styles.textTopLeft);
                    GUI.Label(r, _statCache[index].negative.ToString(), Styles.textBottomLeft);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private int GetStatIndex(RecordMessage message, int desiredIndex)
        {
            int v = 0;
            for (int i = 0; i < message.frames.Count; i++)
            {
                if(message.frames[i] <= desiredIndex)
                {
                    v = i;
                }
            }
            return v;
        }

        private string SingleStatBox(object item)
        {
            string output = "";
            if (item.GetType() == typeof(string) ||
                item.GetType() == typeof(int) ||
                item.GetType() == typeof(float) ||
                item.GetType() == typeof(double))
            {
                output = EditorGUILayout.TextField(item.ToString());
                CopyButton(output);
            }
            else if(item.GetType() == typeof(Vector2))
            {
                Vector2 item2 = (Vector2)item;
                output = EditorGUILayout.TextField("("+ item2.x.ToString()+", "+item2.y.ToString()+")");
                CopyButton(output);
            }
            else if(item.GetType() == typeof(Vector3))
            {
                Vector3 item3 = (Vector3)item;
                output = EditorGUILayout.TextField("(" + item3.x.ToString() + ", " + item3.y.ToString() + ", " + item3.z.ToString() + ")");
                CopyButton(output);
            }
            else if (item.GetType() == typeof(Vector4))
            {
                Vector4 item4 = (Vector4)item;
                output = EditorGUILayout.TextField("(" + item4.x.ToString() + ", " + item4.y.ToString() + ", " + item4.z.ToString() + ", " + item4.w.ToString() + ")");
                CopyButton(output);
            }
            return output;
        }

        private void CopyButton(string toCopy)
        {
            if(GUILayout.Button(_copyButton,GUILayout.Width(Sizes.widthCharButton)))
            {
                GUIUtility.systemCopyBuffer = toCopy;
            }
        }
    }

}