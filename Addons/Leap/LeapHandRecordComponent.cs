// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_LEAP;
// This records a HandModel object (e.g. RiggedHand), listening to the transform information. It does not record raw Leap frames (Leap frames are ~1k data points).
#if PR_LEAP
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using PlayRecorder.Hands;

namespace PlayRecorder.Leap
{

    [System.Serializable]
    public class LeapByteFrame : RecordFrame
    {
        public byte[] hand;

        public LeapByteFrame(int tick, byte[] hand) : base(tick)
        {
            this.hand = (byte[])hand.Clone();
        }
    }

    [System.Serializable]
    public class LeapStatFrame : RecordFrame
    {
        public float stat;
        public LeapStatFrame(int tick, float stat) : base(tick)
        {
            this.stat = stat;
        }
    }

    [System.Serializable]
    public class LeapVectorStatFrame : RecordFrame
    {
        public Vector stat;
        public LeapVectorStatFrame(int tick, Vector stat) : base(tick)
        {
            this.stat = stat;
        }
    }

    [AddComponentMenu("PlayRecorder/RecordComponents/Leap Hand Record Component")]
    public class LeapHandRecordComponent : RecordComponent
    {

        [SerializeField,Tooltip("The distance difference between previous joint positions before a frame is recorded. Whole hand will be recorded.")]
        private float _distanceThreshold = 0.00075f;

        private HandModelBase _handModel;

        private VectorHand _vectorHand;

        private bool _pinchStrengthUpdated = false, _pinchDistanceUpdated = false, _palmWidthUpdated, _grabStrengthUpdated, _grabAngleUpdated, _palmVelocityUpdated;
        private float _pinchStrength, _pinchDistance, _palmWidth, _grabStrength, _grabAngle;
        private Vector _palmVelocity;

        private byte[] _handArray;
        private Vector3[] _jointCache;
        private bool _handUpdate;

        private const int _statCount = 6;

        #region Unity Events

        #endregion

        #region Recording

        public override bool StartRecording()
        {
            _handModel = GetComponent<HandModelBase>();

            if (_handModel == null)
            {
                Debug.LogError("Leap Hand recorder has no Leap Hand on object.");
                return false;
            }

            base.StartRecording();

            _vectorHand = new VectorHand();
            
            HandItem.HandID lhi = HandItem.HandID.Left;

            if (_handModel.Handedness == Chirality.Right)
            {
                lhi = HandItem.HandID.Right;
            }

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new HandItem(_descriptor, this.GetType().ToString(), _handModel.IsTracked, lhi);

            SetCaches();

            _handModel.OnUpdate += HandModelUpdate;
            _handModel.OnBegin += HandModelBegin;
            _handModel.OnFinish += HandModelFinish;

            return true;
        }

        private void SetCaches()
        {
            // Hand Stats
            for (int i = 0; i < _statCount; i++)
            {
                _recordItem.parts.Add(new RecordPart());
            }

            // Joints
            _jointCache = new Vector3[VectorHand.NUM_JOINT_POSITIONS];
            _handArray = new byte[VectorHand.NUM_BYTES];
            _recordItem.parts.Add(new RecordPart());
        }

        public override RecordItem StopRecording()
        {
            _handModel.OnUpdate -= HandModelUpdate;
            _handModel.OnBegin -= HandModelBegin;
            _handModel.OnFinish -= HandModelFinish;
            return base.StopRecording();
        }

        private void HandModelBegin()
        {
            _recordItem.AddStatus(true, _currentTick);
        }

        private void HandModelFinish()
        {
            _recordItem.AddStatus(false, _currentTick);
        }

        private void HandModelUpdate()
        {
            Hand hand = _handModel.GetLeapHand();
            _vectorHand.Encode(hand);

            UpdateStats(hand);
            UpdateJoints(_vectorHand);
        }

        private void UpdateStats(Hand hand)
        {
            if(_pinchStrength != hand.PinchStrength)
            {
                _pinchStrength = hand.PinchStrength;
                _pinchStrengthUpdated = true;
            }

            if(_pinchDistance != hand.PinchDistance)
            {
                _pinchDistance = hand.PinchDistance;
                _pinchDistanceUpdated = true;
            }

            if (_palmWidth != hand.PalmWidth)
            {
                _palmWidth = hand.PalmWidth;
                _palmWidthUpdated = true;
            }

            if (_grabStrength != hand.GrabStrength)
            {
                _grabStrength = hand.GrabStrength;
                _grabStrengthUpdated = true;
            }

            if (_grabAngle != hand.GrabStrength)
            {
                _grabAngle = hand.GrabAngle;
                _grabAngleUpdated = true;
            }

            if(_palmVelocity != hand.PalmVelocity)
            {
                _palmVelocity = hand.PalmVelocity;
                _palmVelocityUpdated = true;
            }
        }

        private void UpdateJoints(VectorHand hand)
        {
            for (int i = 0; i < _jointCache.Length; i++)
            {
                if(Vector3.Distance(hand.jointPositions[i],_jointCache[i]) > _distanceThreshold)
                {
                    _jointCache[i] = hand.jointPositions[i];
                    _handUpdate = true;
                }
            }
            if(_handUpdate)
            {
                _vectorHand.FillBytes(_handArray);
            }
        }

        protected override void RecordTickLogic()
        {
            RecordStatFrames();
            RecordJointFrame();
        }

        private void RecordStatFrames()
        {
            if (_pinchStrengthUpdated)
            {
                _recordItem.parts[0].AddFrame(new LeapStatFrame(_currentTick, _pinchStrength));
                _pinchStrengthUpdated = false;
            }

            if (_pinchDistanceUpdated)
            {
                _recordItem.parts[1].AddFrame(new LeapStatFrame(_currentTick, _pinchDistance));
                _pinchDistanceUpdated = false;
            }

            if (_palmWidthUpdated)
            {
                _recordItem.parts[2].AddFrame(new LeapStatFrame(_currentTick, _palmWidth));
                _palmWidthUpdated = false;
            }

            if (_grabStrengthUpdated)
            {
                _recordItem.parts[3].AddFrame(new LeapStatFrame(_currentTick, _grabStrength));
                _grabStrengthUpdated = false;
            }

            if (_grabAngleUpdated)
            {
                _recordItem.parts[4].AddFrame(new LeapStatFrame(_currentTick, _grabAngle));
                _grabAngleUpdated = false;
            }

            if (_palmVelocityUpdated)
            {
                _recordItem.parts[5].AddFrame(new LeapVectorStatFrame(_currentTick, _palmVelocity));
                _palmVelocityUpdated = false;
            }
        }

        private void RecordJointFrame()
        {
            if(_handUpdate)
            {
                _recordItem.parts[_statCount].AddFrame(new LeapByteFrame(_currentTick, _handArray));
                _handUpdate = false;
            }
        }

        #endregion

        #region Playback

        protected override void SetPlaybackIgnoreTransforms()
        {
            base.SetPlaybackIgnoreTransforms();
            LeapProvider leap = FindObjectOfType<LeapProvider>();
            if(leap != null)
            {
                _playbackIgnoreTransforms.Add(leap.transform);
            }
        }

        public override void StartPlaying()
        {
            _handModel = GetComponent<HandModelBase>();
            if (_handModel != null)
            {
                _vectorHand = new VectorHand();
                base.StartPlaying();
            }
        }

        protected override void PlayUpdateLogic()
        {
            bool handUpdated = false;
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                if(_playUpdatedParts[i] < _statCount)
                {
                    if(PlayStat(_playUpdatedParts[i]))
                    {
                        // Only care if changed TO true, not false
                        handUpdated = true;
                    }
                }
                else
                {
                    _handArray = ((LeapByteFrame)_recordItem.parts[_playUpdatedParts[i]].currentFrame).hand;
                    handUpdated = true;
                }
            }
            if(handUpdated)
            {
                Hand h  = new Hand();
                int ind = 0;
                _vectorHand.ReadBytes(_handArray, ref ind, h);
                h.PinchStrength = _pinchStrength;
                h.PinchDistance = _pinchDistance;
                h.PalmWidth = _palmWidth;
                h.GrabStrength = _grabStrength;
                h.GrabAngle = _grabAngle;
                h.PalmVelocity = _palmVelocity;
                _handModel.SetLeapHand(h);
                _handModel.UpdateHand();
            }
        }

        private bool PlayStat(int stat)
        {
            switch (stat)
            {
                case 0:
                    _pinchStrength = ((LeapStatFrame)_recordItem.parts[stat].currentFrame).stat;
                    return true;
                case 1:
                    _pinchDistance = ((LeapStatFrame)_recordItem.parts[stat].currentFrame).stat;
                    return true;
                case 2:
                    _palmWidth = ((LeapStatFrame)_recordItem.parts[stat].currentFrame).stat;
                    return true;
                case 3:
                    _grabStrength = ((LeapStatFrame)_recordItem.parts[stat].currentFrame).stat;
                    return true;
                case 4:
                    _grabAngle = ((LeapStatFrame)_recordItem.parts[stat].currentFrame).stat;
                    return true;
                case 5:
                    _palmVelocity = ((LeapVectorStatFrame)_recordItem.parts[stat].currentFrame).stat;
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}
#endif