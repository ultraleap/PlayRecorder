// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_LEAP;
// This records information from a LeapProvider object (e.g. LeapServiceProvider/LeapXRServiceProvider), listening to all frame and hand information. Use this when you want to reproduce all the logic that both hands may have done.
// You need to add a LeapPlaybackProvider to your object have frames played back, in place of your original LeapProvider.
#if PR_LEAP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;

namespace PlayRecorder.Leap
{
    [AddComponentMenu("PlayRecorder/Leap/Leap Service Provider Record Component")]
    public class LeapServiceProviderRecordComponent : RecordComponent
    {
        private LeapProvider _leapProvider;
        private LeapPlaybackProvider _playbackProvider;

        private bool _frameUpdated = false;
        private Frame _currentFrame;

        [SerializeField, Tooltip("The distance difference between previous joint positions before a frame is recorded. Whole hand will be recorded.")]
        private float _handDistanceThreshold = 0.00075f;

        private LeapHandCache _leftCache, _rightCache;

        public override string editorHelpbox => "You will need to add a LeapPlaybackProvider component and remove your default LeapServiceProvider to enable playback.";

        public override bool StartRecording()
        {
            _leapProvider = GetComponent<LeapProvider>();

            if (_leapProvider == null)
            {
                Debug.LogError("Leap recorder has no Leap Provider on object.");
                return false;
            }

            base.StartRecording();

            _leftCache = new LeapHandCache();
            _rightCache = new LeapHandCache();

            if(_leapProvider.CurrentFrame != null)
            {
                OnUpdateFrame(_leapProvider.CurrentFrame);
            }

            _leapProvider.OnUpdateFrame -= OnUpdateFrame;
            _leapProvider.OnUpdateFrame += OnUpdateFrame;

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new RecordItem(_descriptor, this.GetType().ToString(), true);

            CreateParts();

            return true;
        }

        public override RecordItem StopRecording()
        {
            if(_recording)
            {
                _leapProvider.OnUpdateFrame -= OnUpdateFrame;
                return base.StopRecording();
            }
            return null;
        }

        private void CreateParts()
        {
            // Leap IDs
            _recordItem.parts.Add(new RecordPart());

            _leftCache.CreateHandParts(_recordItem);
            _rightCache.CreateHandParts(_recordItem);
        }

        private void OnUpdateFrame(Frame frame)
        {
            _frameUpdated = true;
            _currentFrame = frame;

            for (int i = 0; i < _currentFrame.Hands.Count; i++)
            {
                if(_currentFrame.Hands[i].IsLeft)
                {
                    _leftCache.UpdateHand(_currentFrame.Hands[i], _handDistanceThreshold);
                }
                else
                {
                    _rightCache.UpdateHand(_currentFrame.Hands[i], _handDistanceThreshold);
                }
            }
        }

        protected override void RecordTickLogic()
        {
            bool left = false, right = false;
            if(_currentFrame.Hands.Count > 0)
            {
                left = _currentFrame.Hands.FindIndex(x => x.IsLeft) != -1;
                right = _currentFrame.Hands.FindIndex(x => !x.IsLeft) != -1;
            }
            _recordItem.parts[LeapHandCache.framePosition].AddFrame(new LeapIDFrame(_currentTick, _currentFrame.Id, _currentFrame.Timestamp, _currentFrame.CurrentFramesPerSecond, left, right));
            
            _leftCache.RecordStatFrames(_recordItem, _currentTick, LeapHandCache.leftHandOffset);
            _leftCache.RecordJointFrame(_recordItem, _currentTick, LeapHandCache.leftHandOffset + LeapHandCache.handStatCount);

            _rightCache.RecordStatFrames(_recordItem, _currentTick, LeapHandCache.rightHandOffset);
            _rightCache.RecordJointFrame(_recordItem, _currentTick, LeapHandCache.rightHandOffset + LeapHandCache.handStatCount);
        }

        public override void StartPlaying()
        {
            _playbackProvider = GetComponent<LeapPlaybackProvider>();
            if(_playbackProvider == null)
            {
                Debug.LogError("Leap playback requires a LeapPlaybackProvider. Please add one to this object.", this);
                return;
            }
            _playbackProvider.StartPlayback();
            _currentFrame = null;
            _playbackProvider.SetFrame(null);
            _leftCache = new LeapHandCache();
            _rightCache = new LeapHandCache();
            base.StartPlaying();
        }

        protected override PlaybackIgnoreItem SetDefaultPlaybackIgnores(string type)
        {
            PlaybackIgnoreItem pbi = new PlaybackIgnoreItem(type);
            pbi.enabledComponents.Add("Leap.");
            pbi.enabledComponents.Add(typeof(LeapPlaybackProvider).ToString());
            return pbi;
        }

        protected override void PlayTickLogic(int index)
        {
            switch (index)
            {
                case 0:
                    ReadLeapIDFrame((LeapIDFrame)_recordItem.parts[index].currentFrame);
                    return;
                case LeapHandCache.leftHandOffset + LeapHandCache.handStatCount:
                    _leftCache.PlayHand(((LeapByteFrame)_recordItem.parts[index].currentFrame).hand);
                    _frameUpdated = true;
                    return;
                case LeapHandCache.rightHandOffset + LeapHandCache.handStatCount:
                    _rightCache.PlayHand(((LeapByteFrame)_recordItem.parts[index].currentFrame).hand);
                    _frameUpdated = true;
                    return;
            }
            if(index >= LeapHandCache.leftHandOffset && index < LeapHandCache.leftHandOffset + LeapHandCache.handStatCount)
            {
                _leftCache.PlayHandStat(_recordItem.parts[index].currentFrame, index - LeapHandCache.leftHandOffset);
                _frameUpdated = true;
            }
            if (index >= LeapHandCache.rightHandOffset && index < LeapHandCache.rightHandOffset + LeapHandCache.handStatCount)
            {
                _rightCache.PlayHandStat(_recordItem.parts[index].currentFrame, index - LeapHandCache.rightHandOffset);
                _frameUpdated = true;
            }
        }

        protected override void PlayAfterTickLogic()
        {
            if(_frameUpdated && _currentFrame != null)
            {
                _currentFrame.Hands.Clear();
                if(_leftCache.isTracked)
                {
                    SetLeapStatsToHand(_leftCache);
                    _currentFrame.Hands.Add(_leftCache.hand);
                }
                if(_rightCache.isTracked)
                {
                    SetLeapStatsToHand(_rightCache);
                    _currentFrame.Hands.Add(_rightCache.hand);
                }
                _playbackProvider.SetFrame(_currentFrame);
                _frameUpdated = false;
            }
        }

        private void ReadLeapIDFrame(LeapIDFrame frame)
        {
            if(_currentFrame == null)
            {
                _currentFrame = new Frame();
            }
            _currentFrame.Id = frame.id;
            _currentFrame.Timestamp = frame.timestamp;
            _currentFrame.CurrentFramesPerSecond = frame.fps;
            _leftCache.isTracked = frame.left;
            _rightCache.isTracked = frame.right;
            _frameUpdated = true;
        }

        private void SetLeapStatsToHand(LeapHandCache cache)
        {
            cache.SetHand();
            if(_currentFrame != null)
            {
                cache.hand.FrameId = _currentFrame.Id;
            }
        }
    }
}
#endif