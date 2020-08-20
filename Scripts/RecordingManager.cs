using System;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using OdinSerializer;
using Debug = UnityEngine.Debug;

namespace PlayRecorder {

    public class RecordingManager : MonoBehaviour
    {

        protected Data _data;

        [SerializeField, HideInInspector]
        bool _duplicateItems = false;

        [SerializeField, HideInInspector]
        bool _recording = false;

        [SerializeField] [Range(1, 100)]
        int _frameRateVal = 60;
        public int frameRate { get { return _frameRateVal; } }

        float _timeCounter = 0f;

        float _mainThreadTime = 0f;

        int _currentTickVal = 0;
        public int currentTick { get { return _currentTickVal; } }

        public string recordingFolderName = "Recordings";
        public string recordingName = "";
        string _recordingTimeDate = "";

        [SerializeField,HideInInspector]
        protected List<RecordComponent> components = new List<RecordComponent>();

        protected Thread _recordingThread;

        public Action OnTick;

        byte[] b = null;

        string s = "";

        [ContextMenu("Start")]
        public void StartRecording()
        {
            if(_duplicateItems)
            {
                Debug.LogError("There are duplicate descriptors in your current setup. Please fix before trying to record.");
            }
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
            for (int i = 0; i < components.Count; i++)
            {
                components[i].StartRecording();
            }
            _mainThreadTime = Time.time;
            _recordingThread = new Thread(() => 
            {
                Thread.CurrentThread.IsBackground = true;
                RecordingThread(_frameRateVal);
            });
            _recordingThread.Start();
        }

        [ContextMenu("Stop")]
        public void StopRecording()
        {
            _recording = false;
            _data.frameCount = _currentTickVal;
            for (int i = 0; i < components.Count; i++)
            {
                _data.objects.Add(components[i].StopRecording());
            }
            b = SerializationUtility.SerializeValue(_data, DataFormat.JSON);
            System.IO.Directory.CreateDirectory(Application.dataPath + "/../" + recordingFolderName + "/");
            System.IO.File.WriteAllBytes(Application.dataPath + "/../" + recordingFolderName + "/" + _recordingTimeDate + " " + recordingName + ".json",b);
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
            }
        }

        public void AddComponent(RecordComponent component)
        {
            if(!components.Contains(component))
            {
                components.Add(component);
            }
        }

        public void RemoveComponent(RecordComponent component)
        {
            components.Remove(component);
        }

        protected void RecordingThread(int frameRate)
        {
            float ticks = _mainThreadTime;
            double tickTime = 1.0 / frameRate, tickDelta = 0.0, tickCounter = 0.0;
            while (_recording)
            {
                tickDelta = (_mainThreadTime - ticks);
                ticks = _mainThreadTime;
                _timeCounter += (float)tickDelta;
                tickCounter += tickDelta;
                if (tickCounter >= tickTime)
                {
                    tickCounter -= tickTime;
                    _currentTickVal++;

                    for (int i = 0; i < components.Count; i++)
                    {
                        components[i].RecordTick(_currentTickVal);
                    }
                }
                // Woh there cowboy not so fast
                Thread.Sleep(1);
            }
        }

        [ContextMenu("Reserialize")]
        void Reser()
        {
            _data = null;
            
            _data = SerializationUtility.DeserializeValue<Data>(b, DataFormat.JSON);
            for (int i = 0; i < _data.objects.Count; i++)
            {
                if (_data.objects[i].parts.Count > 0)
                {
                    Debug.Log(_data.objects[i].type.ToString());
                    //Debug.Log(_data.objects[i].parts[0].frames[0].GetType().ToString());
                }
            }
        }

        public bool CheckUniqueDescriptor(RecordComponent component)
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (component == components[i])
                    continue;

                if (component.descriptor == components[i].descriptor)
                    return false;
            }
            return true;
        }

    }

}