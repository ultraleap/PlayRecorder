using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [AddComponentMenu("PlayRecorder/RecordComponents/RecordComponent")]
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

        protected PlaybackIgnoreItem _playbackIgnoreItem;
        protected List<Transform> _playbackIgnoreTransforms = new List<Transform>();

        // Playback temp variables
        private int _oldFrame, _newFrame;

        public Action OnStartRecording, OnStopRecording, OnStartPlayback;

        #region Unity Events
        private void Awake()
        {
            AddToManager();
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

#if UNITY_EDITOR

        protected virtual void Reset()
        {
            _descriptor = name +"_"+ this.GetType().ToString();
        }

#endif

        #endregion

        #region Recording

        public void AddToManager()
        {
            _manager = FindObjectOfType<RecordingManager>();
            _manager?.AddComponent(this);
        }

        protected virtual void OnRecordingEnable()
        {

        }

        protected virtual void OnRecordingDisable()
        {

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

        public void RecordUpdate()
        {
            RecordUpdateLogic();
        }

        /// <summary>
        /// This function fires during the Unity main thread. If you have logic that does not need Unity specific functionality, try to place it in RecordTickLogic.
        /// </summary>
        protected virtual void RecordUpdateLogic()
        {

        }

        public void RecordTick(int tick)
        {
            _currentTick = tick;
            RecordTickLogic();
        }

        /// <summary>
        /// This function fires during the recording thread. If you are wanting to access Unity engine functions, refer to RecordUpdateLogic.
        /// </summary>
        protected virtual void RecordTickLogic()
        {
            for (int i = 0; i < _recordItem.parts.Count; i++)
            {
                _recordItem.parts[i].AddFrame(new RecordFrame(_currentTick));
            }
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
            if (ind == -1)
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

        #endregion

        #region Playback

        public void PlaybackStatusChange(bool active)
        {
            gameObject.SetActive(active);
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

        public void SetPlaybackIgnores(PlaybackIgnoreItem playbackIgnore)
        {
            if(playbackIgnore != null)
            {
                _playbackIgnoreItem = playbackIgnore;
            }
            else
            {
                _playbackIgnoreItem = SetDefaultPlaybackIgnores(GetType().ToString());
            }
            // This allows for empty ignores to not affect other components
            if(_playbackIgnoreItem != null)
            {
                SetPlaybackIgnoreTransforms();
                PlaybackIgnore();
            }
        }

        protected virtual PlaybackIgnoreItem SetDefaultPlaybackIgnores(string type)
        {
            return null;
        }

        protected virtual void SetPlaybackIgnoreTransforms()
        {
            _playbackIgnoreTransforms.Add(transform);
        }

        protected void PlaybackIgnore()
        {
            for (int i = 0; i < _playbackIgnoreTransforms.Count; i++)
            {
                Component[] components = transform.GetComponents<Component>();
                for (int j = 0; j < components.Length; j++)
                {
                    // Can't switch a type
                    if (typeof(Rigidbody).IsSameOrSubclass(components[j].GetType()) && _playbackIgnoreItem.makeKinematic)
                    {
                        ((Rigidbody)components[j]).isKinematic = true;
                        continue;
                    }
                    if (typeof(Rigidbody2D).IsSameOrSubclass(components[j].GetType()) && _playbackIgnoreItem.makeKinematic)
                    {
                        ((Rigidbody2D)components[j]).isKinematic = true;
                        continue;
                    }

                    if(typeof(Collider).IsSameOrSubclass(components[j].GetType()) && _playbackIgnoreItem.disableCollisions)
                    {
                        ((Collider)components[j]).enabled = false;
                        continue;
                    }
                    if (typeof(Collider2D).IsSameOrSubclass(components[j].GetType()) && _playbackIgnoreItem.disableCollisions)
                    {
                        ((Collider2D)components[j]).enabled = false;
                        continue;
                    }

                    if (typeof(Renderer).IsSameOrSubclass(components[j].GetType()) && _playbackIgnoreItem.disableRenderer)
                    {
                        ((Renderer)components[j]).enabled = false;
                        continue;
                    }
                    
                }

                Behaviour[] behaviours = transform.GetComponents<Behaviour>();
                bool found = false;
                for (int j = 0; j < behaviours.Length; j++)
                {
                    // Don't disable ourselves
                    if (typeof(RecordComponent).IsSameOrSubclass(behaviours[j].GetType()))
                        continue;

                    // Enforce the settings for the specific defined items
                    if (behaviours[j].GetType() == typeof(Camera))
                    {
                        if(_playbackIgnoreItem.disableCamera)
                        {
                            behaviours[j].enabled = false;
                        }
                        if(_playbackIgnoreItem.disableVRCamera)
                        {
                            ((Camera)behaviours[j]).stereoTargetEye = StereoTargetEyeMask.None;
                        }
                        continue;
                    }

                    found = false;
                    for (int k = 0; k < _playbackIgnoreItem.enabledComponents.Count; k++)
                    {
                        if(behaviours[j].GetType().ToString().Contains(_playbackIgnoreItem.enabledComponents[k],StringComparison.InvariantCultureIgnoreCase))
                        {
                            found = true;
                        }
                    }

                    if(!found)
                    {
                        behaviours[j].enabled = false;
                    }
                }
            }
        }

        public virtual void StartPlaying()
        {
            _playing = true;
            
            OnStartPlayback?.Invoke();
        }

        public void PlayUpdate()
        {
            if (_recordItem != null && (_recordItem.parts.Count > 0 || _playUpdatedParts.Count > 0))
            {
                if (ValidPlayUpdate())
                {
                    PlayUpdateLogic();
                }
                _playUpdatedParts.Clear();
            }
        }

        /// <summary>
        /// This function fires during the Unity main thread. If you have logic that does not need Unity specific functionality, try to place it in PlayTickLogic.
        /// </summary>
        protected virtual void PlayUpdateLogic()
        {

        }

        private bool ValidPlayUpdate()
        {
            if (_playUpdatedParts.Count > _recordItem.parts.Count)
            {
                return false;
            }
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                if (_recordItem.parts[_playUpdatedParts[i]].currentFrame == null)
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
            if (_playing)
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
                            if (!_playUpdatedParts.Contains(j))
                                _playUpdatedParts.Add(j);
                            PlayTickLogic(i);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This function fires during the playback thread. If you are wanting to access Unity engine functions, refer to PlayUpdateLogic.
        /// </summary>
        protected virtual void PlayTickLogic(int index)
        {
            
        }

        public List<string> PlayMessages(int tick)
        {
            List<string> messages = new List<string>();
            if (_playing && _recordItem != null && _recordItem.messages != null)
            {
                for (int i = 0; i < _recordItem.messages.Count; i++)
                {
                    if (_recordItem.messages[i].frames.Contains(tick))
                        messages.Add(_recordItem.messages[i].message);
                }
            }
            return messages;
        }

        #endregion

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
