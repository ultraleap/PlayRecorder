using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;

namespace PlayRecorder
{

    [System.Serializable]
    public class LeapFrame : Frame
    {
        public Hand hand;
        public LeapFrame(int tick, Hand hand) : base(tick)
        {
            this.hand = hand;
        }
    }


    public class LeapHandRecordComponent : RecordComponent
    {
        HandModelBase _handModel;

        Hand _hand;

        bool _handUpdated = false;

        public override void StartRecording()
        {
            _handModel = GetComponent<HandModelBase>();
            
            if (_handModel == null)
            {
                Debug.LogError("Leap Hand recorder has no Leap Hand model on object.");
                return;
            }

            _handModel.OnUpdate += HandModelUpdate;
            _handModel.OnBegin += HandModelBegin;
            _handModel.OnFinish += HandModelFinish;

            base.StartRecording();
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
            _hand = _handModel.GetLeapHand();
            _handUpdated = true;
        }

        protected override void RecordTickLogic()
        {
            if(_handUpdated)
            {
                _handUpdated = false;
                _recordItem.parts[0].AddFrame(new LeapFrame(_currentTick, _hand));
            }
        }

        public override void StartPlaying()
        {
            base.StartPlaying();
            _handModel = GetComponent<HandModelBase>();
        }

        protected override void PlayTickLogic(int index)
        {
            _handModel.SetLeapHand(((LeapFrame)_recordItem.parts[index].frames[_recordItem.parts[index].currentFrameIndex]).hand);
            Debug.Log("set hand!");
        }
    }

}