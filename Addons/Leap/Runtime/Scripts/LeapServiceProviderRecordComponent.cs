// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_LEAP;
// This records information from a LeapProvider object (e.g. LeapServiceProvider/LeapXRServiceProvider), listening to all frame and hand information. Use this when you want to reproduce all the logic that both hands may have done.
// You need to add a LeapPlaybackProvider to your object have frames played back, in place of your original LeapProvider.
#if PR_LEAP
using UnityEngine;
using Leap;
using Leap.Unity;

namespace PlayRecorder.Leap
{
    [AddComponentMenu("PlayRecorder/Ultraleap/Leap Service Provider Record Component"),
        RequireComponent(typeof(LeapPlaybackProvider))]
    public class LeapServiceProviderRecordComponent : RecordComponent
    {
        [SerializeField, Tooltip("Note this needs to be attached to the same object as your provider, and will be overwritten at recording and playback start.")]
        private LeapPlaybackProvider _playbackProvider;

        private bool _frameUpdated = false;
        private Frame _currentFrame;

        [SerializeField, Tooltip("The distance difference between previous joint positions before a frame is recorded. Whole hand will be recorded.")]
        private float _handDistanceThreshold = 0.00075f;

        private LeapHandCache _leftCache, _rightCache;

        [SerializeField, Tooltip("Records a statistic when the hand tracking state changes.")]
        private bool _recordHandTrackingState = true;
        [SerializeField, Tooltip("Records a statistic when the hand pinch state changes. This will automatically record an unpinch event when the hand loses tracking.")]
        private bool _recordHandPinchState = true;

        private bool _oldLeftTracking = false, _oldRightTracking = false;
        private bool _oldLeftPinch = false, _oldRightPinch = false;

#if UNITY_EDITOR
        public override string editorHelpbox => "You will need to ensure any hand models or providers you want to record/playback consume the Leap Playback Provider in their Input Provider slot.";

        public void OnValidate()
        {
            _playbackProvider = GetComponent<LeapPlaybackProvider>();
        }

        protected override void Reset()
        {
            base.Reset();
            _playbackProvider = GetComponent<LeapPlaybackProvider>();
        }

#endif

        public override bool StartRecording()
        {
            _playbackProvider = GetComponent<LeapPlaybackProvider>();

            if (_playbackProvider == null)
            {
                Debug.LogError("Leap recorder has no Leap Provider on object.");
                return false;
            }

            base.StartRecording();

            _leftCache = new LeapHandCache();
            _rightCache = new LeapHandCache();

            if(_playbackProvider.CurrentFrame != null)
            {
                OnUpdateFrame(_playbackProvider.CurrentFrame);
            }

            _playbackProvider.OnUpdateFrame -= OnUpdateFrame;
            _playbackProvider.OnUpdateFrame += OnUpdateFrame;

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new RecordItem(_descriptor, true);

            CreateParts();

            return true;
        }

        public override RecordItem StopRecording()
        {
            if(_recording)
            {
                _playbackProvider.OnUpdateFrame -= OnUpdateFrame;
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
            bool leftPinch = false, rightPinch = false;
            if(_currentFrame.Hands.Count > 0)
            {
                int ind = _currentFrame.Hands.FindIndex(x => x.IsLeft);
                left = ind != -1;
                leftPinch = ind != -1 && _currentFrame.Hands[ind].IsPinching();

                ind = _currentFrame.Hands.FindIndex(x => !x.IsLeft);
                right = ind != -1;
                rightPinch = ind != -1 && _currentFrame.Hands[ind].IsPinching();
            }

            if (_recordHandTrackingState)
            {
                if (left != _oldLeftTracking)
                {
                    AddStatistic("Left Hand Tracked", left);
                    _oldLeftTracking = left;
                }

                if (right != _oldRightTracking)
                {
                    AddStatistic("Right Hand Tracked", right);
                    _oldRightTracking = right;
                }
            }

            if (_recordHandPinchState)
            {
                if (left)
                {
                    if(leftPinch != _oldLeftPinch)
                    {
                        AddStatistic("Left Hand Pinch", leftPinch);
                        _oldLeftPinch = leftPinch;
                    }
                }
                else
                {
                    if (_oldLeftPinch)
                    {
                        AddStatistic("Left Hand Pinch", false);
                        _oldLeftPinch = false;
                    }
                }

                if (right)
                {
                    if(rightPinch != _oldRightPinch)
                    {
                        AddStatistic("Right Hand Pinch", rightPinch);
                        _oldRightPinch = rightPinch;
                    }
                }
                else
                {
                    if (_oldRightPinch)
                    {
                        AddStatistic("Right Hand Pinch", false);
                        _oldRightPinch = false;
                    }
                }
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
            _currentFrame = null;
            _leftCache = new LeapHandCache();
            _rightCache = new LeapHandCache();
            base.StartPlaying();
        }

        protected override PlaybackIgnoreItem SetDefaultPlaybackIgnores(string type)
        {
            PlaybackIgnoreItem pbi = new PlaybackIgnoreItem(type);
            pbi.enabledBehaviours.Add("Leap.");
            pbi.enabledBehaviours.Add(typeof(LeapPlaybackProvider).ToString());
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
                _playbackProvider.SetPlaybackFrame(_currentFrame);
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