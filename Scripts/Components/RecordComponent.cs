using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    public class RecordComponent : MonoBehaviour
    {

        protected RecordingManager _manager;

        protected RecordItem _recordItem;

        protected int _currentTick = 0;

        protected string _id;

        [SerializeField, HideInInspector]
        protected string _descriptor = "";
        public string descriptor { get { return _descriptor; } }

        protected bool _recording = false;
        protected bool _playing = false;

        private void Awake()
        {
            _manager = FindObjectOfType<RecordingManager>();
            _manager?.AddComponent(this);
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

        protected virtual void OnEnable()
        {
            if (_recording)
            {
                _currentTick = _manager.currentTick;
                _recordItem.AddStatus(true, _currentTick);
            }
        }

        protected virtual void OnDisable()
        {
            if(_recording)
            {
                _recordItem.AddStatus(false, _currentTick);
            }
        }

        public virtual void StartRecording()
        {
            if(_id == null || _id == "")
            {
                _id = System.Guid.NewGuid().ToString();
            }
            _currentTick = 0;
            _recording = true;
            _recordItem = new RecordItem(gameObject.activeInHierarchy)
            {
                id = _id,
                descriptor = _descriptor,
                type = this.GetType().ToString()
            };
        }

        public virtual RecordItem StopRecording()
        {
            _recording = false;
            return _recordItem;
        }

        public void AddEmptyMessage(string message)
        {
            int ind = _recordItem.messages.FindIndex(x => x.message == message);
            if (ind != -1)
            {
                _recordItem.messages.Add(new RecordMessage { message = message });
            }
        }

        public void AddMessage(string message)
        {
            int ind = _recordItem.messages.FindIndex(x => x.message == message);
            if(ind != -1)
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
                _recordItem.parts[i].AddFrame(new Frame(_currentTick));
            }
        }

        public void SetPlaybackData(RecordItem data)
        {
            _recordItem = data;
        }

        public virtual void StartPlaying()
        {
            _playing = true;
        }

        public void PlayTick(int tick)
        {
            if(_playing)
            {
                _currentTick = tick;
                Debug.Log("Tick rec!");
                if (_recordItem.parts != null)
                {
                    for (int i = 0; i < _recordItem.parts.Count; i++)
                    {
                        int oF = _recordItem.parts[i].currentFrameIndex;
                        int nF = _recordItem.parts[i].SetCurrentFrame(tick);
                        Debug.Log("uh oh! " + oF + " " + nF);
                        if (oF != nF)
                        {
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
            if(_playing)
            {
                for (int i = 0; i < _recordItem.messages.Count; i++)
                {
                    if (_recordItem.messages[i].frames.Contains(tick))
                        messages.Add(_recordItem.messages[i].message);
                }
            }
            return messages;
        }

    }

}
