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
        private List<bool> _expanded = new List<bool>();
        private List<int> _frameIndex = new List<int>();

        private List<string> _currentValues = new List<string>();
        private List<string> _finalValues = new List<string>();

        [SerializeField]
        private bool _emptyOnLoad = false;

        [SerializeField]
        private int _fileIndex = -1;

        private Vector2 _messageScroll;

        private int _statCount = 0;
        private Rect _windowRect;
        private float _scrollbarWidth;
        private double _oldTime, _newTime, _deltaTime, _regenerateCounter;
        private int _globalFrame = 0;

        private List<Texture2D> _graphs = new List<Texture2D>();
        private Texture2D _indicator;
        private List<Color> _graphColors;
        private Color _indiciatorColor = Color.white;

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

            _graphColors = new List<Color>();
            for (int i = 0; i < 5; i++)
            {
                _graphColors.Add(Color.HSVToRGB(((float)i)/4, 1, 1));
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
                    SetGraphs();
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
            DataSet();
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

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {

                if (_windowRect != position)
                {
                    _windowRect = position;
                    _scrollbarWidth = GUI.skin.verticalScrollbar.fixedWidth;
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
            GUIContent label = new GUIContent("Current File");
            EditorGUILayout.LabelField(label,Styles.textBold,GUILayout.Width(Styles.textBold.CalcSize(label).x));
            int oldInd = _fileIndex;
            _fileIndex = EditorGUILayout.Popup(_fileIndex, _fileNames);
            if (oldInd != _fileIndex)
            {
                SetUILists();
            }
            EditorGUILayout.LabelField(_fileIndex != -1 ? _dataCache[_fileIndex].name : "");
        }

        private void GlobalFrame()
        {
            if (_fileIndex == -1)
                return;

            GUIContent label = new GUIContent("Global Frame");
            EditorGUILayout.LabelField(label, EditorStyles.label, GUILayout.Width(EditorStyles.label.CalcSize(label).x));
            int oldInd = _globalFrame;
            _globalFrame = EditorGUILayout.IntSlider(_globalFrame, 0, _dataCache[_fileIndex].frameCount);
            if(oldInd != _globalFrame)
            {
                _frameIndex = _frameIndex.Select(x => x = _globalFrame).ToList();
            }
        }

        private void SetUILists()
        {
            _expanded.Resize(_dataCache[_fileIndex].messages.Count);
            _frameIndex.Resize(_dataCache[_fileIndex].messages.Count);
            _globalFrame = _dataCache[_fileIndex].frameCount;
            _frameIndex = _frameIndex.Select(x => x = _dataCache[_fileIndex].frameCount).ToList();
            
            if(_fileIndex >= _dataCache.Count)
            {
                _fileIndex = _dataCache.Count - 1;
            }
            SetGraphs();
        }

        private void SetGraphs()
        {
            _indicator = new Texture2D(1, 1);
            _indicator.SetPixel(0,0,_indiciatorColor);
            _indicator.Apply();
            _graphs.Resize(_dataCache[_fileIndex].messages.Count);
            int endFrame = _dataCache[_fileIndex].frameCount;
            for (int i = 0; i < _dataCache[_fileIndex].messages.Count; i++)
            {
                if (endFrame == 0)
                {
                    _graphs[i] = null;
                    continue;
                }
                RecordMessage rm = _dataCache[_fileIndex].messages[i];
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
                            _graphs[i] = null;
                        }
                        else
                        {
                            _graphs[i] = new Texture2D((int)(_windowRect.width - 20), 80);
                            GraphLines(_graphs[i], rm.frames, endFrame, obj);
                            _graphs[i].Apply();
                        }
                    }
                }
            }
        }

        private void GraphLines(Texture2D texture, List<int> frames, int endFrame, object obj)
        {
            float positive=0, negative=0, range;
            foreach (var item in obj as IEnumerable)
            {
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

            range = (int)(positive+1) + Mathf.Abs((int)negative);

            int c = 0;
            object previous = null;
            foreach (var item in obj as IEnumerable)
            {
                if(c == 0)
                {
                    previous = item;
                    c++;
                    continue;
                }
                if (item.GetType() == typeof(int) ||
                    item.GetType() == typeof(float) ||
                    item.GetType() == typeof(double))
                {
                    float val = 0f,valPrev = 0;
                    float.TryParse(item.ToString(), out val);
                    float.TryParse(previous.ToString(), out valPrev);
                    texture.DrawLine(new Vector2(((float)frames[c-1]/(float)endFrame)*texture.width,((valPrev + Mathf.Abs(negative))/range)*texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((val + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[0]);
                }
                else if (item.GetType() == typeof(Vector2))
                {
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector2)previous).x + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector2)item).x + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[0]);
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector2)previous).y + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector2)item).y + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[1]);
                }
                else if (item.GetType() == typeof(Vector3))
                {
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector3)previous).x + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector3)item).x + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[0]);
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector3)previous).y + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector3)item).y + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[1]);
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector3)previous).z + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector3)item).z + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[2]);
                }
                else if (item.GetType() == typeof(Vector4))
                {
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector4)previous).x + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector4)item).x + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[0]);
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector4)previous).y + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector4)item).y + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[1]);
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector4)previous).z + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector4)item).z + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[2]);
                    texture.DrawLine(new Vector2(((float)frames[c - 1] / (float)endFrame) * texture.width, ((((Vector4)previous).w + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2(((float)frames[c] / (float)endFrame) * texture.width, ((((Vector4)item).w + Mathf.Abs(negative)) / range) * texture.height),
                        _graphColors[3]);
                }
                previous = item;
                c++;
            }
        }

        private void ExportCSVButtons()
        {
            GUILayout.Button("Export Current Values");
            GUILayout.Button("Export Final Values");
        }

        private void ExpandCollapseButtons()
        {
            if(GUILayout.Button("Expand All"))
            {
                _expanded = _expanded.Select(x => x = true).ToList();
            }
            if (GUILayout.Button("Collapse All"))
            {
                _expanded = _expanded.Select(x => x = false).ToList();
            }
        }

        private void Messages()
        {
            _messageScroll = EditorGUILayout.BeginScrollView(_messageScroll);

            _statCount = 0;
            for (int i = 0; i < _dataCache[_fileIndex].messages.Count; i++)
            {
                SingleMessage(_dataCache[_fileIndex].messages[i], i);
            }

            if(_statCount == 0)
            {
                EditorGUILayout.LabelField("Current file does not include any stats.");
            }

            EditorGUILayout.EndScrollView();
        }

        private void SingleMessage(RecordMessage message, int index)
        {
            if (message.GetType() == typeof(RecordMessage))
                return;

            _statCount++;

            FieldInfo[] fields = message.GetType().GetFields();

            GUILayout.BeginVertical(GUI.skin.box);

            EditorGUILayout.BeginHorizontal();

            GUIContent label = new GUIContent(message.message + " - " + message.GetType().ToString().FormatType());

            _expanded[index] = EditorGUILayout.BeginFoldoutHeaderGroup(_expanded[index], label);

            for (int i = 0; i < fields.Length; i++)
            {
                // Ignore the first two
                if (fields[i].Name == "message" || fields[i].Name == "frames")
                    continue;

                object obj = fields[i].GetValue(message);
                if(obj is IList)
                {
                    GUIContent statCount = new GUIContent("("+StatCount(obj).ToString()+")");
                    EditorGUILayout.LabelField(statCount,Styles.textBold,GUILayout.Width(Styles.textBold.CalcSize(statCount).x));

                    GUIContent currentValLab = new GUIContent("Current Value");
                    EditorGUILayout.LabelField(currentValLab, GUILayout.Width(EditorStyles.label.CalcSize(currentValLab).x));
                    SingleStatBox(fields[i].GetValue(message), GetStatIndex(message,_frameIndex[index]));

                    GUIContent lastValLab = new GUIContent("Last Value");
                    EditorGUILayout.LabelField(lastValLab, GUILayout.Width(EditorStyles.label.CalcSize(lastValLab).x));
                    SingleStatBox(fields[i].GetValue(message), message.frames.Count - 1);
                    break;
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.EndHorizontal();

            if (_expanded[index])
            {
                _frameIndex[index] = EditorGUILayout.IntSlider("Frame",_frameIndex[index], 0, _dataCache[_fileIndex].frameCount);
                
                if(_graphs[index] != null)
                {
                    GUILayout.Label("", GUILayout.Height(_graphs[index].height));
                    Rect r = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.MouseUp && r.Contains(Event.current.mousePosition))
                    {
                        _frameIndex[index] = (int)((Event.current.mousePosition.x - r.x) / r.width * _dataCache[_fileIndex].frameCount);
                        Repaint();
                    }
                    GUI.DrawTexture(r, _graphs[index]);
                    r.x = r.x + (((float)_frameIndex[index] / (float)_dataCache[_fileIndex].frameCount) * r.width);
                    r.width = 2;
                    GUI.DrawTexture(r, _indicator);
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

        private void SingleStatBox(object field, int index)
        {
            int c = 0;
            foreach (var item in field as IEnumerable)
            {
                if(c == index)
                {
                    if (item.GetType() == typeof(string) ||
                        item.GetType() == typeof(int) ||
                        item.GetType() == typeof(float) ||
                        item.GetType() == typeof(double))
                    {
                        EditorGUILayout.TextField(item.ToString());
                    }
                    else if(item.GetType() == typeof(Vector2))
                    {
                        Vector2 item2 = (Vector2)item;
                        EditorGUILayout.TextField("("+ item2.x.ToString()+", "+item2.y.ToString()+")");
                    }
                    else if(item.GetType() == typeof(Vector3))
                    {
                        Vector3 item3 = (Vector3)item;
                        EditorGUILayout.TextField("(" + item3.x.ToString() + ", " + item3.y.ToString() + ", " + item3.z.ToString() + ")");
                    }
                    else if (item.GetType() == typeof(Vector4))
                    {
                        Vector4 item4 = (Vector4)item;
                        EditorGUILayout.TextField("(" + item4.x.ToString() + ", " + item4.y.ToString() + ", " + item4.z.ToString() + ", " + item4.w.ToString() + ")");
                    }
                    break;
                }
                c++;
            }
        }

        private int StatCount(object list)
        {
            int c = 0;
            foreach (var item in list as IEnumerable)
            {
                c++;
            }
            return c;
        }
    }

}