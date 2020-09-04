using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OdinSerializer;
using System.Threading;

namespace PlayRecorder {

    [System.Serializable]
    public class PlaybackBinder
    {
        public string descriptor;
        public int count = 0;
        public string type;
        public RecordComponent recordComponent;
        public RecordItem recordItem;
    }

    public class PlaybackManager : MonoBehaviour
    {

        [SerializeField]
        List<TextAsset> _recordedFiles = new List<TextAsset>();

        [SerializeField]
        List<Data> _data = new List<Data>();
        string _dataBytes = "";
        [SerializeField]
        int _currentFile = -1;

        public Data currentData {
            get {
                if (_currentFile != -1 && _data.Count > 0)
                    return _data[_currentFile];
                else
                    return null;
            } }

        // Please use the custom inspector.
        [SerializeField, HideInInspector]
        List<PlaybackBinder> _binders = new List<PlaybackBinder>();

        float _mainThreadTime = -1;

        // Playing is to say whether anything has started playing (e.g. the thread has been started)
        // Paused is to change whether time is progressing
        bool _playing = false, _paused = false;

        float _timeCounter = 0f;

        double _tickRate = 0;
        bool _ticked = false;
        int _statusIndex = -1;

        int _currentFrameRate = 0, _currentTickVal = 0;

        [SerializeField, HideInInspector]
        float _playbackRate = 1.0f;

        // Scrubbing Playback
        int _desiredScrubTick = 0;
        bool _countingScrub = false;
        [SerializeField, HideInInspector]
        float _scrubWaitTime = 0.2f;
        float _scrubWaitCounter = 0f;

        Thread _playbackThread = null;


        #region Actions

        public Action<RecordComponent, List<string>> OnPlayMessages;
        public Action<Data> OnDataFileChange;

        #endregion


        public int currentTick { get { return _currentTickVal; } private set { _currentTickVal = value; _timeCounter = _currentTickVal / _data[_currentFile].frameRate; } }
        public float currentTime { get { return _timeCounter; } }

        public void ChangeFiles()
        {
            _data.Clear();
            if (_recordedFiles.Count == 0)
            {
                Debug.LogWarning("No files chosen. Aborting.");
                _binders.Clear();
                return;
            }
            for (int i = 0; i < _recordedFiles.Count; i++)
            {
                if (_recordedFiles[i] == null)
                {
                    _recordedFiles.RemoveAt(i);
                    Debug.LogError("Null file removed.");
                    i--;
                    continue;
                }
                try
                {
                    Data d = SerializationUtility.DeserializeValue<Data>(_recordedFiles[i].bytes, DataFormat.JSON);
                    if (d.objects != null)
                    {
                        _data.Add(d);
                        for (int j = 0; j < d.objects.Count; j++)
                        {
                            if (d.objects[j].parts.Count > 0)
                            {
                                Debug.Log(d.objects[j].type.ToString());
                                if (d.objects[j].parts[0].frames.Count > 0)
                                    Debug.Log(d.objects[j].parts[0].frames[0].GetType().ToString());
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError(_recordedFiles[i].name + " is an invalid recording file and has been ignored and removed.");
                        _recordedFiles.RemoveAt(i);
                        i--;
                    }
                }
                catch
                {
                    Debug.LogError(_recordedFiles[i].name + " is an invalid recording file and has been ignored and removed.");
                    _recordedFiles.RemoveAt(i);
                    i--;
                }
            }
            for (int i = 0; i < _binders.Count; i++)
            {
                _binders[i].count = 0;
            }
            for (int i = 0; i < _data.Count; i++)
            {
                for (int j = 0; j < _data[i].objects.Count; j++)
                {
                    int ind = _binders.FindIndex(x => (x.descriptor == _data[i].objects[j].descriptor) && (x.type == _data[i].objects[j].type));
                    if (ind != -1)
                    {
                        _binders[ind].count++;
                    }
                    else
                    {
                        _binders.Add(new PlaybackBinder
                        {
                            descriptor = _data[i].objects[j].descriptor,
                            count = 1,
                            type = _data[i].objects[j].type,
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
            ChangeCurrentFile(_currentFile);
        }

        public void ChangeCurrentFile(int fileIndex)
        {
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
                    for (int j = 0; j < _binders[i].recordItem.parts.Count; j++)
                    {
                        Debug.Log(_binders[i].recordItem.type.ToString());
                        if (_binders[i].recordItem.parts[j].frames.Count > 0)
                            Debug.Log("CCF: "+ _binders[i].recordItem.parts[j].frames[0].GetType().ToString());
                    }
                    if (_binders[i].recordComponent != null)
                    {
                        _binders[i].recordComponent.SetPlaybackData(_binders[i].recordItem);
                    }
                }
                else
                {
                    _binders[i].recordItem = null;
                }
            }
            _currentFrameRate = _data[fileIndex].frameRate;
            _tickRate = 1.0 / _data[fileIndex].frameRate;
            OnDataFileChange?.Invoke(_data[fileIndex]);
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
                _ticked = false;
                for (int i = 0; i < _binders.Count; i++)
                {
                    if (_binders[i].recordItem != null && _binders[i].recordItem.status != null)
                    {
                        _statusIndex = _binders[i].recordItem.status.FindIndex(x => x.frame == currentTick);
                        if (_statusIndex != -1)
                        {
                            // Can't modify gameObject status during a thread
                            _binders[i].recordComponent.gameObject.SetActive(_binders[i].recordItem.status[_statusIndex].status);
                        }
                    }
                }
            }
        }

        public void StartPlaying()
        {
            if (_data.Count > 0)
            {
                ChangeFiles();
            }
            _playbackThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                PlaybackThread();
            });
            for (int i = 0; i < _binders.Count; i++)
            {
                if (_binders[i].recordComponent != null)
                {
                    _binders[i].recordComponent.StartPlaying();
                }
            }
            _playing = true;
            _playbackThread.Start();
        }

        // 

        public bool TogglePlaying()
        {
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
            float ticks = _mainThreadTime;
            double tickDelta = 0.0, tickCounter = 0.0;
            List<string> tempMessages = new List<string>();
            int statusIndex = -1;
            while (_playing)
            {
                tickDelta = (_mainThreadTime - ticks);
                ticks = _mainThreadTime;

                if (_countingScrub)
                {
                    _scrubWaitCounter -= (float)tickDelta;
                    if (_scrubWaitCounter <= 0)
                    {
                        _countingScrub = false;
                        currentTick = _desiredScrubTick;
                        tickCounter = 0;
                        for (int i = 0; i < _binders.Count; i++)
                        {
                            _binders[i]?.recordComponent.PlayTick(currentTick);
                        }
                    }
                }
                if(_paused)
                {
                    Thread.Sleep(1);
                    continue;
                }
                _timeCounter += (float)tickDelta;
                tickCounter += tickDelta;
                if (tickCounter >= _tickRate)
                {
                    tickCounter -= _tickRate;
                    currentTick++;
                    _ticked = true;

                    for (int i = 0; i < _binders.Count; i++)
                    {
                        if (_binders[i].recordComponent == null)
                            continue;

                        _binders[i].recordComponent.PlayTick(currentTick);
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