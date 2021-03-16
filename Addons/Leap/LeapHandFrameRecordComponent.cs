// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_LEAP;
// This records a HandModel object (e.g. RiggedHand), listening to the raw frame information.
#if PR_LEAP
using UnityEngine;
using Leap;
using Leap.Unity;
using Leap.Unity.Encoding;
using PlayRecorder.Hands;

namespace PlayRecorder.Leap
{
    [AddComponentMenu("PlayRecorder/RecordComponents/Leap Hand Frame Record Component")]
    public class LeapHandFrameRecordComponent : RecordComponent
    {

        [SerializeField,Tooltip("The distance difference between previous joint positions before a frame is recorded. Whole hand will be recorded.")]
        private float _distanceThreshold = 0.00075f;

        private HandModelBase _handModel;

        private LeapHandCache _handCache;

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

            _handCache = new LeapHandCache();
            
            HandItem.HandID lhi = HandItem.HandID.Left;

            if (_handModel.Handedness == Chirality.Right)
            {
                lhi = HandItem.HandID.Right;
            }

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new HandItem(_descriptor, this.GetType().ToString(), _handModel.IsTracked, lhi);

            _handCache.CreateHandParts(_recordItem);

            _handModel.OnUpdate += HandModelUpdate;
            _handModel.OnBegin += HandModelBegin;
            _handModel.OnFinish += HandModelFinish;

            return true;
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
            _handCache.UpdateHand(_handModel.GetLeapHand(),_distanceThreshold);
        }

        protected override void RecordTickLogic()
        {
            _handCache.RecordStatFrames(_recordItem, _currentTick, 0);
            _handCache.RecordJointFrame(_recordItem, _currentTick, LeapHandCache.handStatCount);
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
                _handCache = new LeapHandCache();
                base.StartPlaying();
            }
        }

        protected override void PlayUpdateLogic()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                if(_playUpdatedParts[i] < LeapHandCache.handStatCount)
                {
                    _handCache.PlayHandStat(_recordItem.parts[_playUpdatedParts[i]].currentFrame, _playUpdatedParts[i]);
                }
                else
                {
                    _handCache.PlayHand(((LeapByteFrame)_recordItem.parts[_playUpdatedParts[i]].currentFrame).hand);
                }
            }
            if(_handCache.handUpdated)
            {
                _handCache.SetHand();
                _handModel.SetLeapHand(_handCache.hand);
                _handModel.UpdateHand();
            }
        }
        #endregion
    }
}
#endif