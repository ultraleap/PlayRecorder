#if PR_LEAP
using UnityEngine;
using System;
using Leap;
using Leap.Unity;

namespace PlayRecorder.Leap
{
    [AddComponentMenu("PlayRecorder/Ultraleap/Leap Playback Provider"),
        Tooltip("This provider will continue to show live hand tracking data until playback begins."),
        RequireComponent(typeof(LeapServiceProviderRecordComponent))]
    public class LeapPlaybackProvider : PostProcessProvider
    {
        [SerializeField, HideInInspector]
        private LeapServiceProviderRecordComponent _recordComponent = null;

        private Frame _liveFrame = null;

        private Frame _playbackFrame = null;

        private bool _isPlaying = false, _isRecording = false;

        private void Awake()
        {
            FindElements();   
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            BindEvents();
        }

        protected virtual void OnDisable()
        {
            UnbindEvents();
        }

        private void BindEvents()
        {
            if(_recordComponent != null )
            {
                _recordComponent.OnStartPlayback += OnStartPlayback;
                _recordComponent.OnStartRecording += OnStartRecording;
            }
        }

        private void UnbindEvents()
        {
            if (_recordComponent != null)
            {
                _recordComponent.OnStartPlayback -= OnStartPlayback;
                _recordComponent.OnStartRecording -= OnStartRecording;
            }
        }

        private void OnStartPlayback()
        {
            _isPlaying = true;
            _playbackFrame = new Frame();
        }

        private void OnStartRecording()
        {
            _isRecording = true;
            _liveFrame = new Frame();
        }

        internal void SetPlaybackFrame(Frame frame)
        {
            _playbackFrame = frame;
        }

        public override void ProcessFrame(ref Frame inputFrame)
        {
            if(_isPlaying)
            {
                inputFrame.CopyFrom(_playbackFrame);
                return;
            }

            if (_isRecording)
            {
                _liveFrame.CopyFrom(inputFrame);
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            // Make sure we're updating in both steps
            dataUpdateMode = DataUpdateMode.UpdateAndFixedUpdate;
            passthroughOnly = false;
            FindElements();
        }

        private void FindElements()
        {
            if(_recordComponent == null)
            {
                _recordComponent = GetComponent<LeapServiceProviderRecordComponent>();
            }
        }

    }
}
#endif