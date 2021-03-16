// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_LEAP;
// This records information from a LeapProvider object (e.g. LeapServiceProvider/LeapXRServiceProvider), listening to all frame and hand information.
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
    // The information regarding the frames themselves
    [System.Serializable]
    public class LeapIDFrame : RecordFrame
    {
        public long id;
        public long timestamp;
        public float fps;
        public bool left, right;
        public LeapIDFrame(int tick, long id, long timestamp, float fps, bool left, bool right) : base(tick)
        {
            this.id = id;
            this.timestamp = timestamp;
            this.fps = fps;
            this.left = left;
            this.right = right;
        }
    }

    // The raw joint data in bytes
    [System.Serializable]
    public class LeapByteFrame : RecordFrame
    {
        public byte[] hand;
        public LeapByteFrame(int tick, byte[] hand) : base(tick)
        {
            this.hand = (byte[])hand.Clone();
        }
    }

    // The data for individual hand properties (palm width, pinch strength, etc)
    [System.Serializable]
    public class LeapStatFrame : RecordFrame
    {
        public float stat;
        public LeapStatFrame(int tick, float stat) : base(tick)
        {
            this.stat = stat;
        }
    }

    // The data for recording the ID of the hand.
    [System.Serializable]
    public class LeapIntStatFrame : RecordFrame
    {
        public int stat;
        public LeapIntStatFrame(int tick, int stat) : base(tick)
        {
            this.stat = stat;
        }
    }

    // The data for an individual hand property (in this case only used for hand velocity)
    [System.Serializable]
    public class LeapVectorStatFrame : RecordFrame
    {
        public Vector stat;
        public LeapVectorStatFrame(int tick, Vector stat) : base(tick)
        {
            this.stat = stat;
        }
    }

    [AddComponentMenu("PlayRecorder/RecordComponents/Leap Service Provider Record Component")]
    public class LeapServiceProviderRecordComponent : RecordComponent
    {
        private LeapProvider _leapProvider;
        private LeapPlaybackProvider _playbackProvider;

        private bool _frameUpdated = false;
        private Frame _currentFrame;

        private const int _framePosition = 0, _handStatCount = 8, _leftHandOffset = 1, _rightHandOffset = _leftHandOffset + _handStatCount + 1;

        [SerializeField]
        private float _handDistanceThreshold = 0.00075f;

        private class LeapHandCache
        {
            public VectorHand vectorHand;
            public Hand hand;
            public bool handUpdated = false;
            public bool isTracked = false;
            public byte[] handArray;
            public Vector3[] jointCache;

            public bool handIDUpdated = false;
            public int handID;
            public bool confidenceUpdated = false, pinchStrengthUpdated = false, pinchDistanceUpdated = false, palmWidthUpdated = false, grabStrengthUpdated = false, grabAngleUpdated = false, palmVelocityUpdated = false;
            public float confidence, pinchStrength, pinchDistance, palmWidth, grabStrength, grabAngle;
            public Vector palmVelocity;

            public LeapHandCache()
            {
                hand = new Hand();
                vectorHand = new VectorHand();
                handArray = new byte[VectorHand.NUM_BYTES];
                jointCache = new Vector3[VectorHand.NUM_JOINT_POSITIONS];
            }
        }

        private LeapHandCache _leftCache, _rightCache;

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

            // Left Hand Stats
            for (int i = 0; i < _handStatCount; i++)
            {
                _recordItem.parts.Add(new RecordPart());
            }

            // Left Hand
            _recordItem.parts.Add(new RecordPart());

            // Right Hand Stats
            for (int i = 0; i < _handStatCount; i++)
            {
                _recordItem.parts.Add(new RecordPart());
            }

            // Right Hand
            _recordItem.parts.Add(new RecordPart());
        }

        private void OnUpdateFrame(Frame frame)
        {
            _frameUpdated = true;
            _currentFrame = frame;

            for (int i = 0; i < _currentFrame.Hands.Count; i++)
            {
                if(_currentFrame.Hands[i].IsLeft)
                {
                    UpdateStats(_currentFrame.Hands[i], _leftCache);
                    UpdateJoints(_currentFrame.Hands[i], _leftCache);
                }
                else
                {
                    UpdateStats(_currentFrame.Hands[i], _rightCache);
                    UpdateJoints(_currentFrame.Hands[i], _rightCache);
                }
            }

        }

        private void UpdateStats(Hand hand, LeapHandCache cache)
        {
            if(cache.handID != hand.Id)
            {
                cache.handID = hand.Id;
                cache.handIDUpdated = true;
            }

            if(cache.confidence != hand.Confidence)
            {
                cache.confidence = hand.Confidence;
                cache.confidenceUpdated = true;
            }

            if (cache.pinchStrength != hand.PinchStrength)
            {
                cache.pinchStrength = hand.PinchStrength;
                cache.pinchStrengthUpdated = true;
            }

            if (cache.pinchDistance != hand.PinchDistance)
            {
                cache.pinchDistance = hand.PinchDistance;
                cache.pinchDistanceUpdated = true;
            }

            if (cache.palmWidth != hand.PalmWidth)
            {
                cache.palmWidth = hand.PalmWidth;
                cache.palmWidthUpdated = true;
            }

            if (cache.grabStrength != hand.GrabStrength)
            {
                cache.grabStrength = hand.GrabStrength;
                cache.grabStrengthUpdated = true;
            }

            if (cache.grabAngle != hand.GrabStrength)
            {
                cache.grabAngle = hand.GrabAngle;
                cache.grabAngleUpdated = true;
            }

            if (cache.palmVelocity != hand.PalmVelocity)
            {
                cache.palmVelocity = hand.PalmVelocity;
                cache.palmVelocityUpdated = true;
            }
        }

        private void UpdateJoints(Hand hand, LeapHandCache cache)
        {
            cache.vectorHand.Encode(hand);

            for (int i = 0; i < cache.jointCache.Length; i++)
            {
                if (Vector3.Distance(cache.vectorHand.jointPositions[i], cache.jointCache[i]) > _handDistanceThreshold)
                {
                    cache.jointCache[i] = cache.vectorHand.jointPositions[i];
                    cache.handUpdated = true;
                }
            }
            if (cache.handUpdated)
            {
                cache.vectorHand.FillBytes(cache.handArray);
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
            _recordItem.parts[_framePosition].AddFrame(new LeapIDFrame(_currentTick, _currentFrame.Id, _currentFrame.Timestamp, _currentFrame.CurrentFramesPerSecond, left, right));
            
            RecordStatFrames(_leftCache, _leftHandOffset);
            RecordJointFrame(_leftCache, _leftHandOffset + _handStatCount);

            RecordStatFrames(_rightCache, _rightHandOffset);
            RecordJointFrame(_rightCache, _rightHandOffset + _handStatCount);
        }

        private void RecordStatFrames(LeapHandCache cache, int offset)
        {
            if(cache.handIDUpdated)
            {
                _recordItem.parts[offset].AddFrame(new LeapIntStatFrame(_currentTick, cache.handID));
                cache.handIDUpdated = false;
            }

            if(cache.confidenceUpdated)
            {
                _recordItem.parts[offset + 1].AddFrame(new LeapStatFrame(_currentTick, cache.confidence));
                cache.confidenceUpdated = false;
            }

            if (cache.pinchStrengthUpdated)
            {
                _recordItem.parts[offset + 2].AddFrame(new LeapStatFrame(_currentTick, cache.pinchStrength));
                cache.pinchStrengthUpdated = false;
            }

            if (cache.pinchDistanceUpdated)
            {
                _recordItem.parts[offset + 3].AddFrame(new LeapStatFrame(_currentTick, cache.pinchDistance));
                cache.pinchDistanceUpdated = false;
            }

            if (cache.palmWidthUpdated)
            {
                _recordItem.parts[offset + 4].AddFrame(new LeapStatFrame(_currentTick, cache.palmWidth));
                cache.palmWidthUpdated = false;
            }

            if (cache.grabStrengthUpdated)
            {
                _recordItem.parts[offset + 5].AddFrame(new LeapStatFrame(_currentTick, cache.grabStrength));
                cache.grabStrengthUpdated = false;
            }

            if (cache.grabAngleUpdated)
            {
                _recordItem.parts[offset + 6].AddFrame(new LeapStatFrame(_currentTick, cache.grabAngle));
                cache.grabAngleUpdated = false;
            }

            if (cache.palmVelocityUpdated)
            {
                _recordItem.parts[offset + 7].AddFrame(new LeapVectorStatFrame(_currentTick, cache.palmVelocity));
                cache.palmVelocityUpdated = false;
            }
        }

        private void RecordJointFrame(LeapHandCache cache, int index)
        {
            if (cache.handUpdated)
            {
                _recordItem.parts[index].AddFrame(new LeapByteFrame(_currentTick, cache.handArray));
                cache.handUpdated = false;
            }
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
                case _leftHandOffset + _handStatCount:
                    ReadLeapJoints(_leftCache, (LeapByteFrame)_recordItem.parts[index].currentFrame);
                    return;
                case _rightHandOffset + _handStatCount:
                    ReadLeapJoints(_rightCache, (LeapByteFrame)_recordItem.parts[index].currentFrame);
                    return;
            }
            if(index >= _leftHandOffset && index < _leftHandOffset + _handStatCount)
            {
                ReadLeapStats(_leftCache, _recordItem.parts[index].currentFrame, index - _leftHandOffset);
            }
            if (index >= _rightHandOffset && index < _rightHandOffset + _handStatCount)
            {
                ReadLeapStats(_rightCache, _recordItem.parts[index].currentFrame, index - _rightHandOffset);
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
        
        private void ReadLeapStats(LeapHandCache cache, RecordFrame frame, int stat)
        {
            switch (stat)
            {
                case 0:
                    cache.handID = ((LeapIntStatFrame)frame).stat;
                    break;
                case 1:
                    cache.confidence = ((LeapStatFrame)frame).stat;
                    break;
                case 2:
                    cache.pinchStrength = ((LeapStatFrame)frame).stat;
                    break;
                case 3:
                    cache.pinchDistance = ((LeapStatFrame)frame).stat;
                    break;
                case 4:
                    cache.palmWidth = ((LeapStatFrame)frame).stat;
                    break;
                case 5:
                    cache.grabStrength = ((LeapStatFrame)frame).stat;
                    break;
                case 6:
                    cache.grabAngle = ((LeapStatFrame)frame).stat;
                    break;
                case 7:
                    cache.palmVelocity = ((LeapVectorStatFrame)frame).stat;
                    break;
            }
            _frameUpdated = true;
        }

        private void ReadLeapJoints(LeapHandCache cache, LeapByteFrame frame)
        {
            cache.handArray = frame.hand;

            int ind = 0;
            cache.vectorHand.ReadBytes(cache.handArray, ref ind, cache.hand);

            _frameUpdated = true;
        }

        private void SetLeapStatsToHand(LeapHandCache cache)
        {
            if(_currentFrame != null)
            {
                cache.hand.FrameId = _currentFrame.Id;
            }
            cache.hand.PinchStrength = cache.pinchStrength;
            cache.hand.PinchDistance = cache.pinchDistance;
            cache.hand.PalmWidth = cache.palmWidth;
            cache.hand.GrabStrength = cache.grabStrength;
            cache.hand.GrabAngle = cache.grabAngle;
            cache.hand.PalmVelocity = cache.palmVelocity;
        }
    }
}
#endif