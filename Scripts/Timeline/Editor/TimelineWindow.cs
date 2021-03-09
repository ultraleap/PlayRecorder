using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using PlayRecorder.Tools;
using System.Text.RegularExpressions;
using System;

namespace PlayRecorder.Timeline
{

    public class TimelineWindow : EditorWindow
    {

        public PlaybackManager playbackManager = null;

        [SerializeField]
        private List<DataCache> _dataCache = new List<DataCache>();
        private List<string> _currentMessages = new List<string>();
        public List<string> currentMessages { get { return _currentMessages; } }

        [SerializeField]
        private bool _emptyOnLoad = false;

        [SerializeField]
        private double _maximumTime = -1;

        [SerializeField]
        private int _maximumTick = -1;

        [SerializeField]
        private Rect _currentTimelineWrapperRect, _currentTimelineRect, _timelineTickerRect, _timelineScrubRect;

        private Rect _windowRect;
        private float _oldWidth = -1;

        [SerializeField]
        private Vector2 _scrollPos;

        [SerializeField]
        private Color _normalBackground, _darkerBackground, _lighterBackground, _backgroundDefault;

        private double _oldTime, _newTime, _deltaTime, _regenerateCounter;

        private float _scrollbarWidth = 13;

        [SerializeField]
        private List<Texture2D> _messageTextures = new List<Texture2D>();

        private Texture2D _timelineBG;
        private Texture2D _timelineColourBG;
        private Texture2D _timelineIndicator, _timelineIndicatorPaused;

        private List<TimelineColors> _timelineColourObjects = new List<TimelineColors>();
        private string[] _timelineColourNames;
        private int _timelineColourIndex = 0;

        private bool _timelineValid { get { return _timelineColourObjects != null && _timelineColourObjects.Count > 0 && _timelineColourObjects.Count > _timelineColourIndex; } }

        private const string _timelinePrefName = "PlayRecorder_Timeline_Colours";

        [MenuItem("Tools/PlayRecorder/Timeline")]
        static public void Init()
        {
            TimelineWindow window = GetWindow<TimelineWindow>();
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

        private void Startup()
        {
            playbackManager = FindObjectOfType<PlaybackManager>();
            if(playbackManager == null)
            {
                return;
            }
            _timelineBG = Resources.Load<Texture2D>("Images/timelinebg");
            _windowRect = position;
            _dataCache = new List<DataCache>(playbackManager.GetDataCache());
            playbackManager.OnDataCacheChange -= OnDataCacheChange;
            playbackManager.OnDataCacheChange += OnDataCacheChange;
            _normalBackground = GUI.backgroundColor;
            _normalBackground.a = 1;
            float h, s, v;
            Color.RGBToHSV(_normalBackground, out h, out s, out v);
            _darkerBackground = Color.HSVToRGB(h, s, v * 0.75f);
            _backgroundDefault = Color.HSVToRGB(h, s, v * 0.15f);
            _lighterBackground = Color.HSVToRGB(h, s, v * 1f);
            
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

        private void DataSet()
        {
            ChangeMaximumFrame();
            ColourRefresh();
            GenerateTextures();
        }

        private void OnGUI()
        {
            if(!_timelineValid)
            {
                ColourRefresh();
            }

            if(_timelineValid && _timelineColourObjects[_timelineColourIndex].updateTimeline)
            {
                GenerateTextures();
            }

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
                    ColourDropdown();
                    EditorGUILayout.EndHorizontal();
                    EditorUtil.DrawDividerLine();
                    TimelineHeader();
                    Timeline();
                }
                EditorGUI.EndDisabledGroup();
            }
        }

        private void OnDestroy()
        {
            if(playbackManager != null)
            {
                playbackManager.OnDataCacheChange -= OnDataCacheChange;
            }
        }

        private void OnDataCacheChange(List<DataCache> cache)
        {
            _dataCache = new List<DataCache>(cache);
            DataSet();
        }

        private void ChangeMaximumFrame()
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

        public void ColourRefresh()
        {
            string[] assets = AssetDatabase.FindAssets("t:TimelineColors");
            _timelineColourObjects.Clear();
            string path = "";
            for (int i = 0; i < assets.Length; i++)
            {
                path = AssetDatabase.GUIDToAssetPath(assets[i]);
                TimelineColors tc = AssetDatabase.LoadAssetAtPath<TimelineColors>(path);
                if(tc != null)
                {
                    _timelineColourObjects.Add(tc);
                }
            }

            _timelineColourIndex = 0;
            if (_timelineColourObjects.Count == 0)
                return;

            _timelineColourNames = _timelineColourObjects.Select(x => x.name).ToArray();

            if (EditorPrefs.HasKey(_timelinePrefName))
            {
                string editorKey = EditorPrefs.GetString(_timelinePrefName);
                for (int i = 0; i < _timelineColourObjects.Count; i++)
                {
                    if(editorKey == _timelineColourObjects[i].name)
                    {
                        _timelineColourIndex = i;
                        break;
                    }
                }
            }
            else
            {
                EditorPrefs.SetString(_timelinePrefName, _timelineColourObjects[0].name);                
            }
        }

        private bool CacheCheck()
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
            if(_dataCache.Count == 0 || playbackManager.currentFileIndex == -1)
            {
                EditorGUILayout.LabelField("No files currently loaded. Please add files to the PlaybackManager and press the Update Files button");
                return false;
            }
            return true;
        }

        private void RecordingInfo()
        {
            EditorGUILayout.LabelField("Current File: (" + (playbackManager.currentFileIndex+1) + ") " + _dataCache[playbackManager.currentFileIndex].name, Styles.textBold);
        }

        private void TimelineHeader()
        {
            EditorGUILayout.BeginHorizontal();

            GUI.backgroundColor = playbackManager.isPaused ? Colors.red : Colors.green;
            GUI.Box(new Rect(_timelineTickerRect.x, _timelineTickerRect.y+18, _timelineTickerRect.width * (playbackManager.currentTick / _maximumTick), 1),"");

            GUI.backgroundColor = _normalBackground;
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            GUIContent pp = playbackManager.isPaused ? new GUIContent(EditorGUIUtility.IconContent("PlayButton")) : new GUIContent(EditorGUIUtility.IconContent("PauseButton"));
            pp.tooltip = Application.isPlaying ? "Play/pause the current file." : "Please enter play mode to play recording.";
            if (GUILayout.Button(pp, Styles.buttonIcon, GUILayout.Width(Sizes.Timeline.widthFileButton)))
            {
                playbackManager.TogglePlaying();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!Application.isPlaying || !playbackManager.hasStarted);

            GUIContent rsb = new GUIContent(EditorGUIUtility.IconContent("Animation.FirstKey"));
            rsb.tooltip = "Jump back to the start of the file.";
            if(GUILayout.Button(rsb,Styles.buttonIcon,GUILayout.Width(Sizes.Timeline.widthFileButton)))
            {
                playbackManager.ScrubTick(0);
            }

            EditorGUI.EndDisabledGroup();

            playbackManager.playbackRate = EditorGUILayout.Slider(playbackManager.playbackRate, 0, 3.0f, GUILayout.MaxWidth(150));
            
            if(Event.current.type == EventType.Repaint)
            {
                _timelineTickerRect = GUILayoutUtility.GetLastRect();
            }

            EditorGUILayout.LabelField(new GUIContent(playbackManager.currentTick + " - " + TimeUtil.ConvertToTime((double)playbackManager.currentTime) + " / " + TimeUtil.ConvertToTime(_maximumTime),"The end time value shown here is the end time of the longest file loaded."), Styles.textCentered,GUILayout.ExpandWidth(true));

            GUIContent maxTickLabel = new GUIContent(_maximumTick.ToString(),"The maximum tick of all currently loaded files.");

            EditorGUILayout.LabelField(maxTickLabel, GUILayout.Width(GUI.skin.label.CalcSize(maxTickLabel).x));

            if (Event.current.type == EventType.Repaint)
            {
                Rect t = GUILayoutUtility.GetLastRect();

                _timelineTickerRect.width = ((t.x + t.width) - _timelineTickerRect.x)-10;
                _timelineTickerRect.x -= 1;
            }

            EditorGUILayout.EndHorizontal();

        }

        private void ColourDropdown()
        {
            if (_timelineColourObjects.Count == 0)
            {
                GUIContent errGc = new GUIContent(EditorGUIUtility.IconContent("console.erroricon.sml"));
                errGc.tooltip = "Create a Timeline Color Asset in your project to edit colours. In your project assets, right click -> Create -> PlayRecorder -> Timeline Color Asset.";
                EditorGUILayout.LabelField(errGc, GUILayout.Width(Sizes.widthIcon));
                return;
            }

            int oldInd = _timelineColourIndex;
            EditorGUILayout.LabelField(new GUIContent("Color File", "Change the currently selected set of colours used for the timeline messages."),GUILayout.Width(60));
            _timelineColourIndex = EditorGUILayout.Popup(_timelineColourIndex, _timelineColourNames,GUILayout.Width(200));
            if (oldInd != _timelineColourIndex)
            {
                EditorPrefs.SetString(_timelinePrefName, _timelineColourNames[_timelineColourIndex]);
                GenerateTextures();
            }
            GUIContent gc = new GUIContent(EditorGUIUtility.IconContent("ScriptableObject Icon"));
            gc.tooltip = "Select the chosen asset in the Inspector window";
            if(GUILayout.Button(gc,Styles.buttonIcon,GUILayout.Width(Sizes.widthCharButton)))
            {
                Selection.activeObject = _timelineColourObjects[_timelineColourIndex];
            }
        }

        private void Timeline()
        {
            var tempRect = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            _scrollPos = GUI.BeginScrollView(tempRect, _scrollPos, _currentTimelineRect,false,true);
            if (Event.current.type == EventType.Repaint)
            {
                _currentTimelineWrapperRect = tempRect;
                _currentTimelineRect = _currentTimelineWrapperRect;
                _currentTimelineRect.height = _dataCache.Count * (Sizes.Timeline.heightItem + Sizes.padding);
                _currentTimelineRect.width -= _scrollbarWidth + (Sizes.padding*2);
                _timelineScrubRect = _currentTimelineRect;
                _timelineScrubRect.width -= _scrollbarWidth + (Sizes.padding * 2);
                _timelineScrubRect.x += Sizes.Timeline.widthFileButton + (Sizes.padding * 2);
            }
            if(playbackManager.hasStarted && Event.current.type == EventType.MouseUp && _timelineScrubRect.Contains(Event.current.mousePosition) && (Event.current.mousePosition.x - 36 <= (_timelineScrubRect.width - (_scrollbarWidth + 4))))
            {
                playbackManager.ScrubTick((int)(_maximumTick * (float)(Event.current.mousePosition.x - 36) / (_timelineScrubRect.width - (_scrollbarWidth + 4))));
            }

            bool overrideColours = false;
            if(_timelineValid)
            {
                overrideColours = true;
            }

            bool customWidth = overrideColours && _timelineColourObjects[_timelineColourIndex].overrideMessageIndicatorWidth;

            if(overrideColours && _timelineColourObjects[_timelineColourIndex].overrideBackground)
            {
                GUI.DrawTexture(_timelineScrubRect, _timelineColourBG);
            }

            Rect timelineBGSize = new Rect(0, 0, _timelineScrubRect.width / _timelineBG.width, _timelineScrubRect.height / _timelineBG.height);
            GUI.DrawTextureWithTexCoords(_timelineScrubRect, _timelineBG, timelineBGSize);

            float timelineButtonWidth = _currentTimelineRect.width - ((Sizes.padding*2) + Sizes.Timeline.widthFileButton);
            float timelineButtonHeight = 0;

            for (int i = 0; i < _dataCache.Count; i++)
            {
                timelineButtonHeight = i * (Sizes.Timeline.heightItem + Sizes.padding);
                GUI.backgroundColor = _normalBackground;
                Rect currentRect = new Rect(
                    (Sizes.padding*2) + Sizes.Timeline.widthFileButton,
                    _currentTimelineWrapperRect.y + timelineButtonHeight,
                    (timelineButtonWidth * ((float)_dataCache[i].frameCount / _maximumTick)),
                    Sizes.Timeline.heightItem);

                if (GUI.Button(new Rect(
                    Sizes.padding,
                    _currentTimelineRect.y + timelineButtonHeight,
                    Sizes.Timeline.widthFileButton,
                    Sizes.Timeline.heightItem),
                    new GUIContent(playbackManager.currentFileIndex == i ? ">" + (i + 1).ToString() : (i + 1).ToString(), "Change the currently selected file to this file."),
                    playbackManager.currentFileIndex == i ? Styles.miniButtonBold : Styles.miniButton
                    ))
                {
                    playbackManager.ChangeCurrentFile(i);
                }
                if(i == playbackManager.currentFileIndex)
                {
                    GUI.backgroundColor = overrideColours && _timelineColourObjects[_timelineColourIndex].overrideSelected ? _timelineColourObjects[_timelineColourIndex].selectedColour : _lighterBackground;
                }
                else
                {
                    GUI.backgroundColor = overrideColours && _timelineColourObjects[_timelineColourIndex].overridePassive ? _timelineColourObjects[_timelineColourIndex].passiveColour : _darkerBackground;
                }
                if(GUI.Button(currentRect, "") && playbackManager.currentFileIndex != i)
                {
                    playbackManager.ChangeCurrentFile(i);
                }
                GUI.Label(new Rect(
                    36 + (timelineButtonWidth * ((float)_dataCache[i].frameCount / _maximumTick)) + (customWidth ? _timelineColourObjects[_timelineColourIndex].messageIndicatorWidth : 2),
                    _currentTimelineRect.y + timelineButtonHeight,
                    100,
                    Sizes.Timeline.heightItem),
                    TimeUtil.ConvertToTime((float)_dataCache[i].frameCount / _dataCache[i].frameRate));
                currentRect.width += customWidth ? _timelineColourObjects[_timelineColourIndex].messageIndicatorWidth : 2;
                currentRect.width = (int)currentRect.width - Sizes.padding;
                if (_messageTextures[i] != null)
                {
                    GUI.DrawTexture(currentRect, _messageTextures[i]);
                }
            }

            if(Application.isPlaying)
            {
                GUI.DrawTexture(new Rect(
                    (Sizes.padding * 2) + Sizes.Timeline.widthFileButton + (timelineButtonWidth * ((float)playbackManager.currentTick / _maximumTick)),
                    _currentTimelineRect.y,
                    (overrideColours && _timelineColourObjects[_timelineColourIndex].overrideTimeIndicatorWidth) ? _timelineColourObjects[_timelineColourIndex].timeIndicatorWidth : 2,
                    _currentTimelineRect.height),
                    playbackManager.isPaused ? _timelineIndicatorPaused : _timelineIndicator);
            }
            GUI.backgroundColor = _normalBackground;
            GUI.EndScrollView();
        }

        private class TextureMessageCache
        {
            public int px;
            public List<string> messages = new List<string>();
        }

        private void GenerateTextures()
        {
            if(_oldWidth == _windowRect.width)
            {
                return;
            }

            for (int i = 0; i < _messageTextures.Count; i++)
            {
                if(_messageTextures[i] != null)
                    DestroyImmediate(_messageTextures[i]);
            }
            _messageTextures.Clear();
            _currentMessages.Clear();

            int fInd = -1;

            bool useColours = _timelineColourObjects != null && _timelineColourObjects.Count > 0 && _timelineColourObjects.Count > _timelineColourIndex;
            bool customWidth = useColours && _timelineColourObjects[_timelineColourIndex].overrideMessageIndicatorWidth;
            int messageWidth = customWidth ? _timelineColourObjects[_timelineColourIndex].messageIndicatorWidth : 2;

            int actualWidth = 0, texWidth = 0;

            _timelineColourBG = Generate1pxTexture((useColours && _timelineColourObjects[_timelineColourIndex].overrideBackground) ? _timelineColourObjects[_timelineColourIndex].backgroundColour : _backgroundDefault);

            _timelineIndicator = Generate1pxTexture((useColours && _timelineColourObjects[_timelineColourIndex].overrideTimeIndicator) ? _timelineColourObjects[_timelineColourIndex].timeIndicatorColour : Colors.green);

            _timelineIndicatorPaused = Generate1pxTexture((useColours && _timelineColourObjects[_timelineColourIndex].overrideTimeIndicator) ? _timelineColourObjects[_timelineColourIndex].timeIndicatorPausedColour : Colors.red);

            for (int i = 0; i < _dataCache.Count; i++)
            {
                if (_dataCache[i].messages.Count == 0)
                {
                    _messageTextures.Add(null);
                    continue;
                }

                actualWidth = (int)((_windowRect.width - (_scrollbarWidth + (Sizes.Timeline.widthFileButton + (Sizes.padding * 6)))) * ((float)_dataCache[i].frameCount / _maximumTick));
                texWidth = actualWidth + messageWidth;

                Texture2D t2d = new Texture2D(texWidth, (int)Sizes.Timeline.heightItem,TextureFormat.ARGB32,false);
                FillTextureWithTransparency(t2d);
                List<TextureMessageCache> tMCache = new List<TextureMessageCache>();

                float frame = 0;
                for (int j = 0; j < _dataCache[i].messages.Count; j++)
                {
                    for (int k = 0; k < _dataCache[i].messages[j].frames.Count; k++)
                    {
                        frame = _dataCache[i].messages[j].frames[k];
                        fInd = tMCache.FindIndex(x => x.px >= (int)(actualWidth * (frame/_dataCache[i].frameCount))-messageWidth && x.px <= ((int)(actualWidth * (frame / _dataCache[i].frameCount)))+messageWidth);
                        if(fInd != -1)
                        {
                            tMCache[fInd].messages.Add(_dataCache[i].messages[j].message);
                        }
                        else
                        {
                            tMCache.Add(new TextureMessageCache() { px = (int)(actualWidth * (frame / _dataCache[i].frameCount)), messages = new List<string>() { _dataCache[i].messages[j].message } });
                        }
                    }
                }

                tMCache.Sort((x, y) => x.px.CompareTo(y.px));

                for (int j = 0; j < tMCache.Count; j++)
                {
                    for (int k = 0; k < tMCache[j].messages.Count; k++)
                    {
                        if(!_currentMessages.Contains(tMCache[j].messages[k]))
                        {
                            _currentMessages.Add(tMCache[j].messages[k]);
                        }
                    }
                }

                int currentHeight = 0;
                for (int j = 0; j < tMCache.Count; j++)
                {
                    currentHeight = 0;
                    List<int> heights = DistributeInteger((int)Sizes.Timeline.heightItem - 2, tMCache[j].messages.Count).ToList();
                    Color c = Color.black;
                    for (int k = 0; k < tMCache[j].messages.Count; k++)
                    {
                        if(useColours)
                        {
                            
                            fInd = _timelineColourObjects[_timelineColourIndex].colours.FindIndex(x => x.message == tMCache[j].messages[k]);
                            if(fInd == -1)
                            {
                                fInd = _timelineColourObjects[_timelineColourIndex].colours.FindIndex(x => x.message.Contains("*") && Regex.IsMatch(tMCache[j].messages[k], WildCardToRegular(x.message)));
                            }
                            if(fInd == -1)
                            {
                                c = Color.black;
                            }
                            else
                            {
                                c = _timelineColourObjects[_timelineColourIndex].colours[fInd].color;
                            }
                        }

                        if (c.a == 0)
                            continue;

                        for (int l = 0; l < heights[k]; l++)
                        {
                            for (int m = 0; m < messageWidth; m++)
                            {
                                t2d.SetPixel(tMCache[j].px + m, 1 + currentHeight + l, c);
                            }
                        }
                        currentHeight += heights[k];
                    }
                }

                t2d.Apply();
                _messageTextures.Add(t2d);
            }
            if(_timelineValid)
            {
                _timelineColourObjects[_timelineColourIndex].updateTimeline = false;
            }
        }

        private Texture2D Generate1pxTexture(Color color)
        {
            Texture2D t2d = new Texture2D(1, 1);
            t2d.SetPixel(0, 0, color);
            t2d.Apply();
            return t2d;
        }

        private static void FillTextureWithTransparency(Texture2D texture)
        {
            Color[] colors = new Color[texture.width * texture.height];
            texture.SetPixels(colors);
            texture.Apply();
        }

        public static IEnumerable<int> DistributeInteger(int total, int divider)
        {
            if (divider == 0)
            {
                yield return 0;
            }
            else
            {
                int rest = total % divider;
                double result = total / (double)divider;

                for (int i = 0; i < divider; i++)
                {
                    if (rest-- > 0)
                        yield return (int)Math.Ceiling(result);
                    else
                        yield return (int)Math.Floor(result);
                }
            }
        }

        private static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

    }

}