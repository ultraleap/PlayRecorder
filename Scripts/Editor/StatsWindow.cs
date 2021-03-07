using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;
using System.Reflection;

namespace PlayRecorder.Stats
{

    public class StatsWindow : EditorWindow
    {
        public PlaybackManager playbackManager = null;

        [SerializeField, HideInInspector]
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

        private int _statCount = 0;
        private Rect _windowRect, _csvCurrentRect;
        private double _oldTime, _newTime, _deltaTime, _regenerateCounter;
        private int _globalFrame = 0, _globalMaxFrame = 0;

        private Texture2D _indicator;
        private List<Color> _graphColors;
        private Color _indiciatorColor = Color.white;

        private GUIContent _copyButton;

        [MenuItem("Tools/PlayRecorder/Message Stats")]
        static public void Init()
        {
            StatsWindow window = GetWindow<StatsWindow>();

            window.titleContent = new GUIContent("Message Stats", Resources.Load<Texture>("Images/playrecorder"));

            window.playbackManager = FindObjectOfType<PlaybackManager>();

            if(window.playbackManager != null)
            {
                window.Show();
            }
            else
            {
                Debug.LogError("Please add a PlaybackManager to your scene before trying to open the stats window.");
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

            playbackManager.OnDataCacheChange -= OnDataCacheChange;
            playbackManager.OnDataCacheChange += OnDataCacheChange;

            playbackManager.OnUpdateTick -= OnUpdateTick;
            playbackManager.OnUpdateTick += OnUpdateTick;

            playbackManager.OnPlaybackStart -= OnPlaybackStart;
            playbackManager.OnPlaybackStart += OnPlaybackStart;

            _copyButton = new GUIContent(EditorGUIUtility.IconContent("Clipboard"));
            _copyButton.tooltip = "Copy the field to the system clipboard.";

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
            if(_fileIndex > _dataCache.Count)
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
                if (_dataCache.Count > 0)
                {
                    _emptyOnLoad = false;
                    DataSet();
                }
            }
            if (_dataCache.Count == 0 || playbackManager.currentFileIndex == -1)
            {
                EditorGUILayout.LabelField("No files currently loaded. Please add files to the PlaybackManager and press the Update Files button");
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
                }
            }

            if (playbackManager == null)
            {
                Startup();
                EditorGUILayout.LabelField("Please add a PlaybackManager to your scene before trying to use the stats window.");
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
            EditorGUILayout.LabelField(label,Styles.textBold,GUILayout.Width(Styles.textBold.CalcSize(label).x));
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
            GUIContent matchPlayFile = new GUIContent("Follow File","Matches the current stat file to the currently selected playback file.");
            EditorGUILayout.LabelField(matchPlayFile, GUILayout.Width(EditorStyles.label.CalcSize(matchPlayFile).x));
            bool oldMatchFiles = _followFile;
            _followFile = EditorGUILayout.Toggle(_followFile, GUILayout.Width(Sizes.widthIcon));
            if (_followFile != oldMatchFiles && CheckFollowUpdate())
            {
                SetUILists();
            }
            EditorGUI.EndDisabledGroup();

            GUIContent matchlabel = new GUIContent("All Files","Shows all stats for the currently loaded playback files in this window in one big list.");
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
            if (_followFile && _fileIndex != playbackManager.currentFileIndex)
                return true;

            return false;
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
            _indicator.SetPixel(0, 0, _indiciatorColor);
            _indicator.Apply();

            for (int i = 0; i < _statCache.Count; i++)
            {
                if (!_allFiles && _statCache[i].fileIndex != _fileIndex)
                {
                    continue;
                }
                GenerateGraphs(_statCache[i]);
            }
        }

        private void GenerateGraphs(StatCache cache)
        {
            int endFrame = _dataCache[cache.fileIndex].frameCount;
            if (endFrame == 0)
            {
                cache.graph = null;
                return;
            }
            RecordMessage rm = _dataCache[cache.fileIndex].messages[cache.messageIndex];
            FieldInfo[] fields = rm.GetType().GetFields();
            for (int j = 0; j < fields.Length; j++)
            {
                if (fields[j].Name == "message" || fields[j].Name == "frames")
                    continue;

                object obj = fields[j].GetValue(rm);
                if (obj is IList)
                {
                    System.Type t = obj.GetType().GetGenericArguments().Single();
                    if(t == typeof(string))
                    {
                        cache.graph = null;
                        return;
                    }
                    else
                    {
                        cache.graph = new Texture2D((int)(Mathf.Abs(_windowRect.width - 16)*(cache.maxFrame/(float)_globalMaxFrame)), 80);
                        GraphLines(cache.graph, cache, rm.frames, endFrame, obj);
                        cache.graph.Apply();
                        return;
                    }
                }
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
            for (int i = 0; i < cache.messages.Count; i++)
            {
                if(_statCache[indexOffset + i] == null)
                {
                    _statCache[indexOffset + i] = new StatCache();
                    _statCache[indexOffset + i].frameIndex = cache.frameCount;
                }
                int mi = i;
                _statCache[indexOffset + i].fileIndex = f;
                _statCache[indexOffset + i].messageIndex = mi;
                _statCache[indexOffset + i].maxFrame = cache.frameCount;
                _statCache[indexOffset + i].fileName = cache.fileName;
                _statCache[indexOffset + i].statName = cache.messages[i].message;
                _statCache[indexOffset + i].current = "";
                _statCache[indexOffset + i].final = "";
            }
        }

        private void GraphLines(Texture2D texture, StatCache cache, List<int> frames, int endFrame, object obj)
        {
            IList list = (IList)obj;
            float positive = 0, negative = 0, range;
            object item;
            for (int i = 0; i < list.Count; i++)
            {
                item = list[i];
                if (item.GetType() == typeof(int) ||
                    item.GetType() == typeof(float) ||
                    item.GetType() == typeof(double))
                {
                    //float val = (float)item;
                    float val = 0f;
                    float.TryParse(item.ToString(), out val);
                    if(positive < val)
                    {
                        positive = val;
                    }
                    if(negative > val)
                    {
                        negative = val;
                    }
                }
                else if (item.GetType() == typeof(Vector2))
                {
                    if(positive < ((Vector2)item).MaxAxis())
                    {
                        positive = ((Vector2)item).MaxAxis();
                    }
                    if(negative > ((Vector2)item).MinAxis())
                    {
                        negative = ((Vector2)item).MinAxis();
                    }
                }
                else if (item.GetType() == typeof(Vector3))
                {
                    if (positive < ((Vector3)item).MaxAxis())
                    {
                        positive = ((Vector3)item).MaxAxis();
                    }
                    if (negative > ((Vector3)item).MinAxis())
                    {
                        negative = ((Vector3)item).MinAxis();
                    }
                }
                else if (item.GetType() == typeof(Vector4))
                {
                    if (positive < ((Vector4)item).MaxAxis())
                    {
                        positive = ((Vector4)item).MaxAxis();
                    }
                    if (negative > ((Vector4)item).MinAxis())
                    {
                        negative = ((Vector4)item).MinAxis();
                    }
                }
            }

            range = positive + Mathf.Abs(negative);
            cache.negative = negative;
            cache.positive = positive;
           
            if(frames.Count == 1)
            {
                item = list[0];
                DrawLinesFromObject(item, item, texture, frames[0], endFrame, endFrame, negative, range);
            }
            else
            {
                object previous = null;
                for (int i = 0; i < list.Count; i++)
                {
                    item = list[i];
                    if(i == 0)
                    {
                        previous = item;
                        continue;
                    }
                    DrawLinesFromObject(item, previous, texture, frames[i - 1], frames[i], endFrame, negative, range);
                    previous = item;
                }
                DrawLinesFromObject(previous, previous, texture, frames[frames.Count - 1], endFrame, endFrame, negative, range);
            }
        }

        private void DrawLinesFromObject(object item, object previousItem, Texture2D texture, float previousFrame, float currentFrame, float endFrame, float negative, float range)
        {
            if (item.GetType() == typeof(int) ||
                item.GetType() == typeof(float) ||
                item.GetType() == typeof(double))
            {
                float val = 0f, valPrev = 0;
                // Complaining about not being able to convert a double to a float, this solves it for some reason.
                float.TryParse(item.ToString(), out val);
                float.TryParse(previousItem.ToString(), out valPrev);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, valPrev, val, negative, range, _graphColors[0 % _graphColors.Count]);
            }
            else if (item.GetType() == typeof(Vector2))
            {
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector2)previousItem).x, ((Vector2)item).x, negative, range, _graphColors[0 % _graphColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector2)previousItem).y, ((Vector2)item).y, negative, range, _graphColors[1 % _graphColors.Count]);
            }
            else if (item.GetType() == typeof(Vector3))
            {
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector3)previousItem).x, ((Vector3)item).x, negative, range, _graphColors[0 % _graphColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector3)previousItem).y, ((Vector3)item).y, negative, range, _graphColors[1 % _graphColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector3)previousItem).z, ((Vector3)item).z, negative, range, _graphColors[2 % _graphColors.Count]);
            }
            else if (item.GetType() == typeof(Vector4))
            {
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector4)previousItem).x, ((Vector4)item).x, negative, range, _graphColors[0 % _graphColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector4)previousItem).y, ((Vector4)item).y, negative, range, _graphColors[1 % _graphColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector4)previousItem).z, ((Vector4)item).z, negative, range, _graphColors[2 % _graphColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, ((Vector4)previousItem).w, ((Vector4)item).w, negative, range, _graphColors[3 % _graphColors.Count]);
            }
        }

        private void DrawSingleLine(Texture2D texture, float previousFrame, float currentFrame, float endFrame, float previousValue, float currentValue, float negative, float range, Color color)
        {
            texture.DrawLine(new Vector2((previousFrame / endFrame) * texture.width, ((previousValue + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2((currentFrame / endFrame) * texture.width, ((currentValue + Mathf.Abs(negative)) / range) * texture.height),
                        color);
        }

        private void ExportCSVButtons()
        {
            bool current = false, final = false;
            GUIContent currentValues = new GUIContent("Export Current Values", "Exports the visible list of stats current values. If all files is ticked, all files will be saved to the CSV as separate rows.");
            current = GUILayout.Button(currentValues);
            if(Event.current.type == EventType.Repaint)
            {
                _csvCurrentRect = GUILayoutUtility.GetLastRect();
            }
            GUIContent finalValues = new GUIContent("Export Final Values", "Exports the visible list of stats final values. If all files is ticked, all files will be saved to the CSV as separate rows.");
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
                PopupWindow.Show(b, new StatsCSVPopup(cache, final, _windowRect.width));
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
                    EditorGUILayout.LabelField("Current loaded file do not include any stats.");
                }
                else
                {
                    EditorGUILayout.LabelField("Current file does not include any stats.");
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

            GUIContent label = new GUIContent((_allFiles ? (fileIndex+1).ToString() + ". " : "") + message.message + " - " + message.GetType().ToString().FormatType() + " (" + _statCache[index].frameIndex + "/" + _statCache[index].maxFrame + ")");

            _statCache[index].expanded = EditorGUILayout.BeginFoldoutHeaderGroup(_statCache[index].expanded, label);
            _statCache[index].validStat = false;
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
                    GUIContent statCount = new GUIContent("("+list.Count.ToString()+")");
                    EditorGUILayout.LabelField(statCount,Styles.textBold,GUILayout.Width(Styles.textBold.CalcSize(statCount).x));

                    GUIContent currentValLab = new GUIContent("Current Value", "The value of the stat based on the current frame.");
                    EditorGUILayout.LabelField(currentValLab, GUILayout.Width(EditorStyles.label.CalcSize(currentValLab).x));
                    _statCache[index].current = SingleStatBox(list[GetStatIndex(message, _statCache[index].frameIndex)]);

                    GUIContent lastValLab = new GUIContent("Final Value", "The final value of the stat, regardless of the current frame.");
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
                _statCache[index].frameIndex = EditorGUILayout.IntSlider("Frame", _statCache[index].frameIndex, 0, _dataCache[fileIndex].frameCount);
                
                if(_statCache[index].graph != null)
                {
                    GUILayout.Label("", GUILayout.Height(_statCache[index].graph.height + Sizes.padding));
                    Rect r = GUILayoutUtility.GetLastRect();
                    r.width = _statCache[index].graph.width;
                    r.height = _statCache[index].graph.height;
                    if ((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDrag) && r.Contains(Event.current.mousePosition))
                    {
                        _statCache[index].frameIndex = (int)((Event.current.mousePosition.x - r.x) / r.width * _dataCache[fileIndex].frameCount);
                        Repaint();
                    }
                    GUI.DrawTexture(r, _statCache[index].graph);
                    Rect r2 = new Rect(r);
                    r2.x = r.x + (((float)_statCache[index].frameIndex / (float)_dataCache[fileIndex].frameCount) * r.width);
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