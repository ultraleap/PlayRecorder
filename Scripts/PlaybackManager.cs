using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
#if UNITY_EDITOR
using Unity.EditorCoroutines.Editor;
#endif

namespace PlayRecorder {

    [System.Serializable]
    public class PlaybackBinder
    {
        public string descriptor;
        public int count = 0;
        public string type;
        public List<int> statusIndex = new List<int>();
        public RecordComponent recordComponent;
        public RecordItem recordItem;
    }

    [System.Serializable]
    public class MessageCache
    {
        public RecordComponent component;
        public List<string> messages = new List<string>();

        public MessageCache(RecordComponent component, List<string> messages)
        {
            this.component = component;
            this.messages = messages;
        }
    }

    [AddComponentMenu("PlayRecorder/PlaybackManager",2)]
    public class PlaybackManager : MonoBehaviour
    { 

        public class ComponentCache
        {
            public string descriptor;
            public RecordComponent recordComponent;

            public ComponentCache(RecordComponent component)
            {
                recordComponent = component;
                descriptor = recordComponent.descriptor;
            }
        }

        [SerializeField]
        private List<TextAsset> _recordedFiles = new List<TextAsset>();

        [SerializeField]
        private List<TextAsset> _loadedRecordedFiles = new List<TextAsset>();

        [SerializeReference,HideInInspector]
        private List<DataCache> _dataCache = new List<DataCache>();

        [SerializeField]
        private List<string> _dataCacheNames = new List<string>();

        public int dataCacheCount { get { return _dataCache.Count; } }

        public List<DataCache> GetDataCache()
        {
            return _dataCache;
        }

        public DataCache GetDataCache(int i)
        {
            if(i >= _dataCache.Count)
            {
                return null;
            }
            return _dataCache[i];
        }

        Data _currentData = null;

        public Data currentData {
            get {
                return _currentData;
            } }

        public List<MessageCache> messageCache = new List<MessageCache>();

        [SerializeField]
        private bool _awaitingFileRefresh = false;

        [SerializeField]
        private int _currentFile = -1,_oldFileIndex = -1;

        public int currentFileIndex { get { return _currentFile; } }

        // Please use the custom inspector.
        [SerializeField]
        private List<PlaybackBinder> _binders = new List<PlaybackBinder>();

        private List<ComponentCache> _componentCache = new List<ComponentCache>();

        private float _mainThreadTime = -1;

        [SerializeField]
        private PlaybackIgnoreComponentsObject _ignoresObject = null;

        // Playing is to say whether anything has started playing (e.g. the thread has been started)
        // Paused is to change whether time is progressing
        [SerializeField]
        private bool _playing = false, _paused = false;
        
        public bool hasStarted {  get { return _playing; } }

        public bool isPaused { get { return !_playing || _paused; } }
        private bool _firstLoad = true;

        [SerializeField]
        private float _timeCounter = 0f;

        private double _tickRate = 0, tickCounter = 0, tickDelta = 0;
        private float ticks = 0;
        private bool _ticked = false, _scrubbed = false;

        [SerializeField]
        private int _currentFrameRate = 0, _currentTickVal = 0, _maxTickVal = 0;

        [SerializeField]
        private float _playbackRate = 1.0f;
        public float playbackRate { get { return _playbackRate; } set { _playbackRate = value; } }

        // Scrubbing Playback
        private int _desiredScrubTick = 0;
        private bool _countingScrub = false;
        [SerializeField]
        private float _scrubWaitTime = 0.2f;
        private float _scrubWaitCounter = 0f;

        private Thread _playbackThread = null;

        [SerializeField]
        private bool _changingFiles = false;
        public bool changingFiles { get { return _changingFiles; } }

        private bool _removeErrorFile = false;
        private Thread _loadFilesThread = null;

        #region Actions

        public Action<RecordComponent, List<string>> OnPlayMessages;
        public Action<Data> OnDataChange;
        public Action<List<DataCache>> OnDataCacheChange;
        public Action<int> OnTick, OnScrub;
        public Action OnUpdateTick;

        #endregion

        public int currentTick { get { return _currentTickVal; } private set { _currentTickVal = value; _timeCounter = (float)_currentTickVal / _currentData.frameRate; } }
        public float currentTime { get { return _timeCounter; } }
        public int currentFrameRate { get { if (_currentData != null) return _currentData.frameRate; return -1; } }

        public void ChangeFiles()
        {
            if(_changingFiles)
            {
                Debug.LogWarning("Unable to change files. Currently working.");
            }

            _loadedRecordedFiles.Clear();

            bool nulls = false;

            for (int i = 0; i < _recordedFiles.Count; i++)
            {
                if (_recordedFiles[i] == null)
                {
                    _recordedFiles.RemoveAt(i);
                    nulls = true;
                    i--;
                    continue;
                }
            }

            if(nulls)
            {
                Debug.LogWarning("Null files were removed.");
            }

            _dataCache.Clear();
            _dataCacheNames.Clear();

            if (_recordedFiles.Count == 0)
            {
                Debug.LogWarning("No files chosen. Aborting.");
                _binders.Clear();
                OnDataCacheChange?.Invoke(_dataCache);
                return;
            }

            RemoveDuplicateFiles();

            for (int i = 0; i < _binders.Count; i++)
            {
                _binders[i].count = 0;
            }


#if UNITY_EDITOR
            EditorCoroutineUtility.StartCoroutine(ChangeFilesCoroutine(), this);
#else
            StartCoroutine(ChangeFilesCoroutine());
#endif
        }

        public void RemoveDuplicateFiles()
        {
            int c = _recordedFiles.Count;
            _recordedFiles = _recordedFiles.Distinct().ToList();
            if(c != _recordedFiles.Count)
            {
                Debug.LogWarning("Duplicate files have been removed from playback.");
            }
        }

        private IEnumerator ChangeFilesCoroutine()
        {
            _changingFiles = true;
            _componentCache.Clear();
            RecordComponent[] rc = Resources.FindObjectsOfTypeAll<RecordComponent>();

            for (int i = 0; i < rc.Length; i++)
            {
                if (rc[i].gameObject.scene.name == null)
                    continue;

                _componentCache.Add(new ComponentCache(rc[i]));
            }

#if UNITY_EDITOR
            EditorWaitForSeconds waitForSeconds = new EditorWaitForSeconds(0.1f);
#else
            WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);
#endif
            byte[] tempBytes = null;
            string tempName = "";

            for (int i = 0; i < _recordedFiles.Count; i++)
            {                
                tempBytes = _recordedFiles[i].bytes;
                tempName = _recordedFiles[i].name;
                _loadFilesThread = new Thread(() => {
                    _removeErrorFile = false;
                    Data d =  FileUtil.LoadSingleFile(tempBytes);
                    if(d != null)
                    {
                        ChangeBinders(d);
                        _dataCache.Add(new DataCache(d,tempName));
                        _dataCacheNames.Add(d.recordingName);
                    }
                    else
                    {
                        Debug.LogError(tempName + " is an invalid recording file and has been ignored and removed.");
                        _removeErrorFile = true;
                    }
                });
                _loadFilesThread.Start();
                while(_loadFilesThread.IsAlive)
                {
                    yield return waitForSeconds;
                }
                if(_removeErrorFile)
                {
                    _recordedFiles.RemoveAt(i);
                }
                else
                {
                    _loadedRecordedFiles.Add(_recordedFiles[i]);
                }

            }
            bool mismatch = false;
            for (int i = 0; i < _binders.Count; i++)
            {
                if(_binders[i].count == 0)
                {
                    _binders.RemoveAt(i);
                    i--;
                    continue;
                }
                if(_binders[i].count != _recordedFiles.Count)
                {
                    mismatch = true;
                }
            }
            if(mismatch)
            {
                Debug.LogWarning("You have a mismatch between your recorded item count and your data files. This may mean certain objects do not have unique playback data.");
            }
            OnDataCacheChange?.Invoke(_dataCache);
            _changingFiles = false;
            ChangeCurrentFile(_currentFile);
        }

        private void ChangeBinders(Data data)
        {
            int rCacheInd = -1, binderInd = -1;
            for (int j = 0; j < data.objects.Count; j++)
            {
                binderInd = _binders.FindIndex(x => (x.descriptor == data.objects[j].descriptor) && (x.type == data.objects[j].type));
                if (binderInd != -1)
                {
                    _binders[binderInd].count++;
                    if (_binders[binderInd].recordComponent == null && _componentCache.Count > 0)
                    {
                        rCacheInd = _componentCache.FindIndex(x => x.descriptor == _binders[binderInd].descriptor);
                        if (rCacheInd != -1)
                        {
                            _binders[binderInd].recordComponent = _componentCache[rCacheInd].recordComponent;
                        }
                    }
                }
                else
                {
                    RecordComponent rc = null;
                    if (_componentCache.Count > 0)
                    {
                        rCacheInd = _componentCache.FindIndex(x => x.descriptor == data.objects[j].descriptor);
                        if (rCacheInd != -1)
                        {
                            rc = _componentCache[rCacheInd].recordComponent;
                        }
                    }
                    _binders.Add(new PlaybackBinder
                    {
                        descriptor = data.objects[j].descriptor,
                        count = 1,
                        type = data.objects[j].type,
                        recordComponent = rc,
                    });
                }
            }
        }

        public void ChangeCurrentFile(int fileIndex)
        {
            _changingFiles = true;
            if (fileIndex == -1)
            {
                fileIndex = 0;
            }
            if (fileIndex >= _dataCache.Count)
            {
                _currentFile = _dataCache.Count - 1;
                Debug.LogWarning("Currently selected data file exceeds maximum file count. Your current file choice has been changed.");
            }
            else
            {
                _currentFile = fileIndex;
            }

#if UNITY_EDITOR
            EditorCoroutineUtility.StartCoroutine(ChangeSingleFileCoroutine(), this);
#else
            StartCoroutine(ChangeSingleFileCoroutine());
#endif
        }

        IEnumerator ChangeSingleFileCoroutine()
        {
            _changingFiles = true;

#if UNITY_EDITOR
            EditorWaitForSeconds waitForSeconds = new EditorWaitForSeconds(0.1f);
#else
            WaitForSeconds waitForSeconds = new WaitForSeconds(0.1f);
#endif

            byte[] b = _recordedFiles[_currentFile].bytes;

            _loadFilesThread = new Thread(() =>
            {
                _removeErrorFile = false;
                Data d = FileUtil.LoadSingleFile(b);
                if (d != null)
                {
                    _currentData = d;
                }
                else
                {
                    Debug.LogError(_dataCache[_currentFile].name + " is an invalid recording file and has not been loaded.");
                    _removeErrorFile = true;
                }
            });
            _loadFilesThread.Start();
            while(_loadFilesThread.IsAlive)
            {
                yield return waitForSeconds;
            }

            for (int i = 0; i < _binders.Count; i++)
            {
                int ind = _currentData.objects.FindIndex(x => (x.descriptor == _binders[i].descriptor) && (x.type == _binders[i].type));
                if (ind != -1)
                {
                    _binders[i].recordItem = _currentData.objects[ind];
                    if (_binders[i].recordComponent != null)
                    {
                        _binders[i].recordComponent.SetPlaybackData(_binders[i].recordItem);
                    }
                }
                else
                {
                    _binders[i].recordItem = null;
                    if (_binders[i].recordComponent != null)
                    {
                        _binders[i].recordComponent.SetPlaybackData(null);
                    }
                }
            }

            _currentFrameRate = _currentData.frameRate;
            _maxTickVal = _currentData.frameCount;
            _tickRate = 1.0 / _currentData.frameRate;
            OnDataChange?.Invoke(_currentData);
            if (_firstLoad && _playing && Application.isPlaying)
            {
                StartPlayingAfterLoad();
            }
            _scrubbed = true;
            _ticked = true;
            _changingFiles = false;
        }

        public void RevertFileChanges()
        {
            _currentFile = _oldFileIndex;
            _awaitingFileRefresh = false;
            _recordedFiles = new List<TextAsset>(_loadedRecordedFiles);
        }

        public void Awake()
        {
            if (_currentFile == -1)
            {
                _currentFile = 0;
            }
        }

        public void Start()
        {
            _paused = true;
            _playing = false;
        }

        private void OnDestroy()
        {
            _playing = false;
        }

        private void Update()
        {
            if (_playing)
            {
                _mainThreadTime = Time.time;
            }
            if(_ticked)
            {
                UpdateComponentStatus();
                PlaybackUpdate();
                OnTick?.Invoke(currentTick);
                if(_scrubbed)
                {
                    OnScrub?.Invoke(currentTick);
                }
                _ticked = false;
                _scrubbed = false;
            }
            if(messageCache.Count > 0)
            {
                UpdateMessages();
            }
        }

        private void PlaybackUpdate()
        {
            for (int i = 0; i < _binders.Count; i++)
            {
                _binders[i].recordComponent?.PlayUpdate();
            }
            OnUpdateTick?.Invoke();
        }

        private void UpdateComponentStatus()
        {
            for (int i = 0; i < _binders.Count; i++)
            {
                if (_binders[i].recordItem != null && _binders[i].recordItem.status != null && _binders[i].recordComponent != null && _binders[i].recordItem.status.Count > 0)
                {
                    if (_scrubbed)
                    {
                        _binders[i].statusIndex.Add(_binders[i].recordItem.status.FindLastIndex(x => x.frame <= currentTick));
                    }
                    if (_binders[i].statusIndex.Count > 0)
                    {
                        // Can't modify gameObject status during a thread
                        _binders[i].recordComponent.PlaybackStatusChange(_binders[i].recordItem.status[_binders[i].statusIndex[_binders[i].statusIndex.Count - 1]].status);
                    }
                }
                _binders[i].statusIndex.Clear();
            }
        }

        private void UpdateMessages()
        {
            for (int i = 0; i < messageCache.Count; i++)
            {
                OnPlayMessages?.Invoke(messageCache[i].component, messageCache[i].messages);
            }
            messageCache.Clear();
        }

        public void StartPlaying()
        {
            if(_dataCache.Count == 0)
            {
                Debug.LogError("No files to play.");
                return;
            }
            if (_dataCache.Count > 0)
            {
                _playing = true;
                ChangeCurrentFile(_currentFile);
            }
        }

        private void StartPlayingAfterLoad()
        {
            _firstLoad = false;
            _playbackThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                PlaybackThread();
            });
            _currentTickVal = 0;
            _scrubbed = true;
            _ticked = true;
            UpdateComponentStatus();
            int ignoreIndex = -1;
            for (int i = 0; i < _binders.Count; i++)
            {
                if (_binders[i].recordComponent != null)
                {
                    PlaybackIgnoreItem pbi = null;
                    if(_ignoresObject != null)
                    {
                        ignoreIndex = _ignoresObject.ignoreItems.FindIndex(x => x.recordComponent == _binders[i].recordComponent.GetType().ToString());
                        if(ignoreIndex != -1)
                        {
                            pbi = _ignoresObject.ignoreItems[ignoreIndex];
                        }
                    }
                    _binders[i].recordComponent.SetPlaybackIgnores(pbi);
                    _binders[i].recordComponent.StartPlaying();
                }
            }
            _playbackThread.Start();
        }

        public bool SetPaused(bool paused)
        {
            _paused = !paused;
            return TogglePlaying();
        }

        public bool TogglePlaying()
        {
            if(_awaitingFileRefresh)
            {
                Debug.LogError("Awaiting file update. Playback has been prevented. Please refresh files in Edit mode before trying to play.");
                return false;
            }
            if(_changingFiles)
            {
                Debug.Log("Files currently changing.");
                return _paused;
            }
            if(!_playing)
            {
                StartPlaying();
            }
            return _paused = !_paused;
        }

        public void ScrubTick(int tick)
        {
            if(_playing)
            {
                _desiredScrubTick = Mathf.Max(tick,0);
                _countingScrub = true;
                _scrubWaitCounter = _scrubWaitTime;
            }
        }

        protected void PlaybackThread()
        {
            ticks = _mainThreadTime;
            tickDelta = 0.0;
            PlaybackThreadTick();
            while (_playing)
            {
                tickDelta = (_mainThreadTime - ticks);
                ticks = _mainThreadTime;

                if (_changingFiles)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if (_countingScrub)
                {
                    _scrubWaitCounter -= (float)tickDelta;
                    if (_scrubWaitCounter <= 0)
                    {
                        _countingScrub = false;
                        currentTick = Mathf.Max(_desiredScrubTick - 1,0);
                        tickCounter =  _tickRate - (tickDelta * _playbackRate);
                        _scrubbed = true;
                        PlaybackThreadTick();
                    }
                }

                if(_paused)
                {
                    Thread.Sleep(1);
                    continue;
                }
                _timeCounter += (float)tickDelta;
                tickCounter += (tickDelta * _playbackRate);
                if (tickCounter >= _tickRate)
                {
                    tickCounter -= _tickRate;
                    currentTick++;

                    PlaybackThreadTick();
                    
                }
                // Woh there cowboy not so fast
                Thread.Sleep(1);
            }
        }

        private void PlaybackThreadTick()
        {
            _ticked = true;
            for (int i = 0; i < _binders.Count; i++)
            {
                if (_binders[i].recordComponent == null || _binders[i].recordItem == null || _binders[i].recordItem.status == null)
                    continue;

                _binders[i].recordComponent.PlayTick(currentTick);
                int status = _binders[i].recordItem.status.FindIndex(x => x.frame == currentTick);
                if (status != -1)
                {
                    _binders[i].statusIndex.Add(status);
                }
                List<string> tempMessages = _binders[i].recordComponent.PlayMessages(currentTick);
                if (tempMessages != null && tempMessages.Count > 0)
                {
                    messageCache.Add(new MessageCache(_binders[i].recordComponent, tempMessages));
                }
            }
        }
    }

}