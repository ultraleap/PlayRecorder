using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PlayRecorder {

    public class RecordingManager : MonoBehaviour
    {

        protected Data _data;

        [SerializeField, HideInInspector]
        bool _duplicateItems = false;

        [SerializeField, HideInInspector]
        bool _recording = false, _recordingPaused = false;

        [SerializeField] [Range(1, 120)]
        int _frameRateVal = 60;
        public int frameRate { get { return _frameRateVal; } }

        float _timeCounter = 0f;

        float _mainThreadTime = 0f;

        bool _ticked = false;
        int _currentTickVal = 0;
        public int currentTick { get { return _currentTickVal; } }

        [SerializeField]
        bool _recordOnStartup = false;
        [SerializeField]
        float _recordOnStartupDelay = 0f;
        [SerializeField]
        bool _recordStartupInProgress = false;

        public string recordingFolderName = "Recordings";
        public string recordingName = "";
        string _recordingTimeDate = "";
        string _unityDataPath;

        [SerializeField, HideInInspector]
        protected List<RecordComponent> _components = new List<RecordComponent>();
        // The components used for the current file
        protected List<RecordComponent> _currentComponents = new List<RecordComponent>();

        protected Thread _recordingThread, _savingThread;

        /// <summary>
        /// Occurs on the recording thread.
        /// </summary>
        public Action<int> OnTick;
        /// <summary>
        /// Occurs on the Unity update thread after the most recent thread tick.
        /// </summary>
        public Action OnUpdateTick;

        private void Start()
        {
            if(_recordOnStartup)
            {
                if(_recordOnStartupDelay > 0)
                {
                    StartCoroutine(RecordingStartupDelay());
                }
                else
                {
                    StartRecording();
                }
            }
        }

        IEnumerator RecordingStartupDelay()
        {
            _recordStartupInProgress = true;
            float time = _recordOnStartupDelay;
            yield return null;
            while(time > 0)
            {
                time -= Time.unscaledDeltaTime;
                yield return null;
            }
            _recordStartupInProgress = false;
            StartRecording();
        }

        public void StartRecording()
        {
            if(_duplicateItems)
            {
                Debug.LogError("There are duplicate descriptors in your current setup. Please fix before trying to record.");
            }
            _unityDataPath = Application.dataPath;
            _data = new Data()
            {
                recordingName = recordingName,
                dateTime = DateTime.Now,
                frameRate = _frameRateVal
            };
            _recordingTimeDate = DateTime.Now.ToString("yyyyMMddHHmmss");
            _timeCounter = 0f;
            _currentTickVal = 0;
            _recording = true;
            _recordingPaused = false;
            _currentComponents.Clear();
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] == null)
                    continue;

                if(_components[i].required)
                {
                    _components[i].StartRecording();
                    _currentComponents.Add(_components[i]);
                }
            }
            _mainThreadTime = Time.time;
            _recordingThread = new Thread(() => 
            {
                Thread.CurrentThread.IsBackground = true;
                RecordingThread(_frameRateVal);
            });
            Debug.Log("Starting recording: " + _recordingTimeDate + " " + recordingName);
            _recordingThread.Start();
        }

        public void StopRecording()
        {
            _recording = false;
            _recordingPaused = false;
            _data.frameCount = _currentTickVal;
            for (int i = 0; i < _currentComponents.Count; i++)
            {
                _data.objects.Add(_currentComponents[i].StopRecording());
            }

            _savingThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                FileUtil.SaveDataToFile(_unityDataPath + "/" + recordingFolderName + "/", _recordingTimeDate + " " + recordingName, _data);
            });
            Debug.Log("Saving recording: " + _recordingTimeDate + " " + recordingName);
            _savingThread.Start();
        }

        // Temporarily pause recording, without stopping it.
        public void PauseRecording()
        {
            _recordingPaused = true;
        }

        // Resume recording, if already recording then nothing will change.
        public void ResumeRecording()
        {
            _recordingPaused = false;
        }

        void OnDestroy()
        {
            if(_recording)
            {
                StopRecording();
            }
        }

        private void Update()
        {
            if(_recording)
            {
                _mainThreadTime = Time.time;
                if(_ticked)
                {
                    RecordingUpdate();
                    _ticked = false;
                }
            }
        }

        void RecordingUpdate()
        {
            for (int i = 0; i < _currentComponents.Count; i++)
            {
                _currentComponents[i].RecordUpdate();
            }
            OnUpdateTick?.Invoke();
        }

        public void AddComponent(RecordComponent component)
        {
            if(!_components.Contains(component))
            {
                _components.Add(component);
            }
        }

        public void RemoveComponent(RecordComponent component)
        {
            _components.Remove(component);
        }

        protected void RecordingThread(int frameRate)
        {
            float ticks = _mainThreadTime;
            double tickTime = 1.0 / frameRate, tickDelta = 0.0, tickCounter = 0.0;
            while (_recording)
            {
                tickDelta = (_mainThreadTime - ticks);
                ticks = _mainThreadTime;
                if (!_recordingPaused)
                {
                    _timeCounter += (float)tickDelta;
                    tickCounter += tickDelta;
                }
                if (tickCounter >= tickTime)
                {
                    tickCounter -= tickTime;
                    _currentTickVal++;

                    for (int i = 0; i < _currentComponents.Count; i++)
                    {
                        _currentComponents[i].RecordTick(_currentTickVal);
                    }
                    _ticked = true;
                    OnTick?.Invoke(_currentTickVal);
                }
                // Woh there cowboy not so fast
                Thread.Sleep(1);
            }
        }

        public bool CheckUniqueDescriptor(RecordComponent component)
        {
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] == null)
                {
                    _components.RemoveAt(i); 
                    i--;
                }

                if (component == _components[i])
                    continue;


                if (component.descriptor == _components[i].descriptor)
                    return false;
            }
            return true;
        }

        public bool CheckUniqueDescriptor(string descriptor)
        {
            for (int i = 0; i < _components.Count; i++)
            {
                if (descriptor == _components[i].descriptor)
                    return false;
            }
            return true;
        }

        public void RefreshComponents()
        {
            RecordComponent[] rc = Resources.FindObjectsOfTypeAll<RecordComponent>();
            for (int i = 0; i < rc.Length; i++)
            {
                AddComponent(rc[i]);
            }
            for (int i = 0; i < _components.Count; i++)
            {
                if(_components[i].gameObject.scene.name == null)
                {
                    RemoveComponent(_components[i]);
                    i--;
                }
            }
        }
    }

}