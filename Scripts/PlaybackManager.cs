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

    public class PlaybackManager : MonoBehaviour
    {

        public class PlaybackCache
        {
            public string name;
            public byte[] bytes;
        }

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
        List<TextAsset> _recordedFiles = new List<TextAsset>();
        
        // DO NOT USE WILL BE REMOVED
        [SerializeField]
        List<Data> _data = new List<Data>();

        // TO BE ADDED
        // CURRENT MULTI-FILE SUPPORT IS LAGGY AS HELL PAST LIKE 5 FILES
        [SerializeField]
        List<PlaybackCache> _dataCache = new List<PlaybackCache>();

        Data _currentData = null;

        [SerializeField]
        bool _awaitingFileRefresh = false;

        [SerializeField]
        int _currentFile = -1;

        public Data currentData {
            get {
                if (_currentFile != -1 && _data.Count > 0)
                    return _data[_currentFile];
                else
                    return null;
            } }

        public Data GetData(int i)
        {
            if(i >= _data.Count)
            {
                return null;
            }
            return _data[i];
        }

        // Please use the custom inspector.
        [SerializeField]
        List<PlaybackBinder> _binders = new List<PlaybackBinder>();

        List<ComponentCache> _componentCache = new List<ComponentCache>();

        float _mainThreadTime = -1;

        // Playing is to say whether anything has started playing (e.g. the thread has been started)
        // Paused is to change whether time is progressing
        [SerializeField]
        bool _playing = false, _paused = false;
        bool _firstLoad = true;

        [SerializeField]
        float _timeCounter = 0f;

        double _tickRate = 0, tickCounter = 0, tickDelta = 0;
        float ticks = 0;
        bool _ticked = false, _scrubbed = false;

        [SerializeField]
        int _currentFrameRate = 0, _currentTickVal = 0, _maxTickVal = 0;

        [SerializeField]
        float _playbackRate = 1.0f;

        // Scrubbing Playback
        int _desiredScrubTick = 0;
        bool _countingScrub = false;
        [SerializeField]
        float _scrubWaitTime = 0.2f;
        float _scrubWaitCounter = 0f;

        Thread _playbackThread = null;


        [SerializeField]
        bool _changingFiles = false;
        bool _removeErrorFile = false;
        Thread _loadFilesThread = null, _changeFilesThread = null;

        #region Actions

        public Action<RecordComponent, List<string>> OnPlayMessages;
        public Action<Data> OnDataFileChange;

        #endregion

        public int currentTick { get { return _currentTickVal; } private set { _currentTickVal = value; _timeCounter = _currentTickVal / _data[_currentFile].frameRate; } }
        public float currentTime { get { return _timeCounter; } }

        public void ChangeFiles()
        {
            if(_changingFiles)
            {
                Debug.LogError("Unable to change files. Currently working.");
            }
            _data.Clear();

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
                Debug.LogError("Null files were removed.");
            }


            if (_recordedFiles.Count == 0)
            {
                Debug.LogError("No files chosen. Aborting.");
                _binders.Clear();
                return;
            }

            RemoveDuplicateFiles();

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
                        _data.Add(d);
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
            }
            _changeFilesThread = new Thread(ChangeFilesThread);
            _changeFilesThread.Start();
            while (_changeFilesThread.IsAlive)
            {
                yield return waitForSeconds;
            }
            _changingFiles = false;
            ChangeCurrentFile(_currentFile);
        }

        private void ChangeFilesThread()
        {

            for (int i = 0; i < _binders.Count; i++)
            {
                _binders[i].count = 0;
            }
            int rCacheInd = -1, binderInd = -1;
            for (int i = 0; i < _data.Count; i++)
            {
                for (int j = 0; j < _data[i].objects.Count; j++)
                {
                    binderInd = _binders.FindIndex(x => (x.descriptor == _data[i].objects[j].descriptor) && (x.type == _data[i].objects[j].type));
                    if (binderInd != -1)
                    {
                        _binders[binderInd].count++;
                        if(_binders[binderInd].recordComponent == null && _componentCache.Count > 0)
                        {
                            rCacheInd = _componentCache.FindIndex(x => x.descriptor == _binders[binderInd].descriptor);
                            if(rCacheInd != -1)
                            {
                                _binders[binderInd].recordComponent = _componentCache[rCacheInd].recordComponent;
                            }
                        }
                    }
                    else
                    {
                        RecordComponent rc = null;
                        if(_componentCache.Count > 0)
                        {
                            rCacheInd = _componentCache.FindIndex(x => x.descriptor == _data[i].objects[j].descriptor);
                            if(rCacheInd != -1)
                            {
                                rc = _componentCache[rCacheInd].recordComponent;
                            }
                        }
                        _binders.Add(new PlaybackBinder
                        {
                            descriptor = _data[i].objects[j].descriptor,
                            count = 1,
                            type = _data[i].objects[j].type,
                            recordComponent = rc,
                        });
                    }
                }
            }
            bool mismatch = false;
            for (int i = 0; i < _binders.Count; i++)
            {
                if (_binders[i].count == 0)
                {
                    _binders.RemoveAt(i);
                    i--;
                    continue;
                }
                if (_binders[i].count != _data.Count)
                {
                    mismatch = true;
                }
            }
            if (mismatch)
            {
                Debug.LogWarning("You have a mismatch between your recorded item count and your data files. This may mean certain objects do not have unique playback data.");
            }
        }

        public void ChangeCurrentFile(int fileIndex)
        {
            // implement coroutine and thread to change files
            _changingFiles = true;
            if (fileIndex == -1)
            {
                fileIndex = 0;
            }
            if (fileIndex >= _data.Count)
            {
                _currentFile = _data.Count - 1;
                Debug.LogWarning("Currently selected data file exceeds maximum file count. Your current file choice has been changed.");
            }
            else
            {
                _currentFile = fileIndex;
            }
            for (int i = 0; i < _binders.Count; i++)
            {
                int ind = _data[fileIndex].objects.FindIndex(x => (x.descriptor == _binders[i].descriptor) && (x.type == _binders[i].type));
                if (ind != -1)
                {
                    _binders[i].recordItem = _data[fileIndex].objects[ind];
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
            _currentFrameRate = _data[fileIndex].frameRate;
            _maxTickVal = _data[fileIndex].frameCount;
            _tickRate = 1.0 / _data[fileIndex].frameRate;
            OnDataFileChange?.Invoke(_data[fileIndex]);
            if(_firstLoad && Application.isPlaying)
            {
                StartPlayingAfterLoad();
            }
            _changingFiles = false;
            _scrubbed = true;
            _ticked = true;
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
                _ticked = false;
                _scrubbed = false;
            }
        }

        void UpdateComponentStatus()
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
                        _binders[i].recordComponent.gameObject.SetActive(_binders[i].recordItem.status[_binders[i].statusIndex[_binders[i].statusIndex.Count - 1]].status);
                    }
                }
                _binders[i].statusIndex.Clear();
            }
        }

        public void StartPlaying()
        {
            if(_data.Count == 0)
            {
                Debug.LogError("No files to play.");
                return;
            }
            if (_data.Count > 0)
            {
                ChangeFiles();
                _playing = true;
            }
        }

        void StartPlayingAfterLoad()
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
            for (int i = 0; i < _binders.Count; i++)
            {
                if (_binders[i].recordComponent != null)
                {
                    _binders[i].recordComponent.StartPlaying();
                }
            }
            _playbackThread.Start();
        }

        // 

        public bool TogglePlaying()
        {
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
                _desiredScrubTick = tick;
                _countingScrub = true;
                _scrubWaitCounter = _scrubWaitTime;
            }
        }

        protected void PlaybackThread()
        {
            ticks = _mainThreadTime;
            tickDelta = 0.0;
            List<string> tempMessages = new List<string>();
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
                        currentTick = (_desiredScrubTick - 1);
                        tickCounter =  _tickRate - (tickDelta * _playbackRate);
                        _scrubbed = true;
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
                    //Debug.Log("TC: " + tickCounter + " TR: " + _tickRate + " TD: " + tickDelta);
                    tickCounter -= _tickRate;
                    currentTick++;
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
                        tempMessages = _binders[i].recordComponent.PlayMessages(currentTick);
                        if(tempMessages != null && tempMessages.Count > 0)
                        {
                            OnPlayMessages?.Invoke(_binders[i].recordComponent,tempMessages);
                        }
                    }
                }
                // Woh there cowboy not so fast
                Thread.Sleep(1);
            }
        }

    }

}