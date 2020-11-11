using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    public class RecordComponent : MonoBehaviour
    {

        protected RecordingManager _manager;

        protected RecordItem _recordItem;

        protected int _currentTick = 0;

        [SerializeField, HideInInspector]
        protected string _descriptor = "";
        public string descriptor { get { return _descriptor; } }
        [SerializeField, HideInInspector]
        protected bool _uniqueDescriptor = true;

        [SerializeField, HideInInspector]
        protected bool _required = true;
        public bool required { get { return _required; } set { _required = value; } }

        protected bool _recording = false;
        public bool recording { get { return _recording; } }
        protected bool _playing = false;

        protected bool _recordUpdated = false;
        protected List<int> _playUpdatedParts = new List<int>();

        // Playback temp variables
        private int _oldFrame, _newFrame;

        public Action OnStartRecording, OnStopRecording, OnStartPlayback;

        private void Awake()
        {
            AddToManager();
        }

        public void AddToManager()
        {
            _manager = FindObjectOfType<RecordingManager>();
            _manager?.AddComponent(this);
        }

        private void Update()
        {
            if(_recording)
            {
                RecordUpdate();
            }
            if(_playing && _recordItem != null && (_recordItem.parts.Count > 0 || _playUpdatedParts.Count > 0))
            {
                if(ValidPlayUpdate())
                {
                    PlayUpdate();
                }
                _playUpdatedParts.Clear();
            }
        }

        [ExecuteInEditMode]
        private void OnDestroy()
        {
            if (_manager == null)
            {
                _manager = FindObjectOfType<RecordingManager>();
            }
            _manager?.RemoveComponent(this);
        }

        protected void OnEnable()
        {
            if (_recording)
            {
                _currentTick = _manager.currentTick;
                _recordItem.AddStatus(true, _currentTick);
                OnRecordingEnable();
            }
        }

        protected void OnDisable()
        {
            if(_recording)
            {
                _recordItem.AddStatus(false, _currentTick);
                OnRecordingDisable();
            }
        }

        protected virtual void OnRecordingEnable()
        {

        }

        protected virtual void OnRecordingDisable()
        {

        }

        public void PlaybackStatusChange(bool active)
        {
            gameObject.SetActive(active);

            if (_playing && _recordItem != null && (_recordItem.parts.Count > 0 || _playUpdatedParts.Count > 0))
            {
                if (ValidPlayUpdate())
                {
                    PlayUpdate();
                }
                _playUpdatedParts.Clear();
            }
        }

        public virtual void StartRecording()
        {
            _currentTick = 0;
            _recording = true;
            _recordItem = new RecordItem(_descriptor, this.GetType().ToString(), gameObject.activeInHierarchy);
            OnStartRecording?.Invoke();
        }

        public virtual RecordItem StopRecording()
        {
            _recording = false;
            OnStopRecording?.Invoke();
            return _recordItem;
        }

        public void AddEmptyMessage(string message)
        {
            if (_recordItem == null || _recordItem.messages == null)
                return;

            int ind = _recordItem.messages.FindIndex(x => x.message == message);
            if (ind == -1)
            {
                _recordItem.messages.Add(new RecordMessage { message = message });
            }
        }

        public void AddMessage(string message)
        {
            if (_recordItem == null || _recordItem.messages == null)
                return;

            int ind = _recordItem.messages.FindIndex(x => x.message == message);
            if(ind == -1)
            {
                RecordMessage rm = new RecordMessage { message = message };
                rm.frames.Add(_currentTick);
                _recordItem.messages.Add(rm);
            }
            else
            {
                _recordItem.messages[ind].frames.Add(_manager.currentTick);
            }
        }

        protected virtual void RecordUpdate()
        {

        }
      
        public void RecordTick(int tick)
        {
            if(_recording)
            {
                _currentTick = tick;
                RecordTickLogic();
            }
        }
    
        protected virtual void RecordTickLogic()
        {
            for (int i = 0; i < _recordItem.parts.Count; i++)
            {
                _recordItem.parts[i].AddFrame(new RecordFrame(_currentTick));
            }
        }

        public void SetPlaybackData(RecordItem data)
        {
            _recordItem = data;
            if (_recordItem != null && _recordItem.parts != null)
            {
                for (int i = 0; i < _recordItem.parts.Count; i++)
                {
                    _recordItem.parts[i].currentFrameIndex = -1;
                }
                OnSetPlaybackData();
            }
        }

        protected virtual void OnSetPlaybackData()
        {

        }

        public virtual void StartPlaying()
        {
            _playing = true;

            OnStartPlayback?.Invoke();
        }

        protected virtual void PlayUpdate()
        {
            _playUpdatedParts.Clear();
        }

        private bool ValidPlayUpdate()
        {
            if(_playUpdatedParts.Count > _recordItem.parts.Count)
            {
                return false;
            }
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                if(_recordItem.parts[_playUpdatedParts[i]].currentFrame == null)
                    return false;
            }
            return true;
        }

        public virtual void OnChangeFile()
        {
            _playUpdatedParts.Clear();
        }

        public void PlayTick(int tick)
        {
            if(_playing)
            {
                _currentTick = tick;
                if (_recordItem != null && _recordItem.parts != null)
                {
                    for (int i = 0; i < _recordItem.parts.Count; i++)
                    {
                        _oldFrame = _recordItem.parts[i].currentFrameIndex;
                        _newFrame = _recordItem.parts[i].SetCurrentFrame(tick);
                        if (_oldFrame != _newFrame)
                        {
                            int j = i;
                            if(!_playUpdatedParts.Contains(j))
                                _playUpdatedParts.Add(j);
                            PlayTickLogic(i);
                        }
                    }
                }
            }
        }

        protected virtual void PlayTickLogic(int index)
        {
            //_recordItem.parts[index]
        }

        public List<string> PlayMessages(int tick)
        {
            List<string> messages = new List<string>();
            if(_playing && _recordItem != null && _recordItem.messages != null)
            {
                for (int i = 0; i < _recordItem.messages.Count; i++)
                {
                    if (_recordItem.messages[i].frames.Contains(tick))
                        messages.Add(_recordItem.messages[i].message);
                }
            }
            return messages;
        }

        public void SetDescriptor(string descriptor)
        {
            _descriptor = descriptor;
            if(_manager != null)
            {
                _uniqueDescriptor = _manager.CheckUniqueDescriptor(this);
            }
        }

    }

}
