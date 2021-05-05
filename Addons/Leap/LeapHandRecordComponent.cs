// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_LEAP;
// This records a HandModel object (e.g. RiggedHand), listening to the transform information. It does not record raw Leap frames (Leap frames are ~1k data points).
// This method of recording is the older method and mostly only included to prevent issues with old recordings.
#if PR_LEAP
using UnityEngine;
using Leap;
using Leap.Unity;
using PlayRecorder.Hands;
using System;

namespace PlayRecorder.Leap
{
    [AddComponentMenu("PlayRecorder/Ultraleap/Leap Hand Record Component")]
    [Obsolete("DEPRECATED\nPlease try to make use of the Leap Hand Frame Record Component instead of this.")]
    public class LeapHandRecordComponent : RecordComponent
    {

        [SerializeField,Tooltip("This value is the square magnitude at which rotation changes will cause a frame to be stored.")]
        private float _rotationThreshold = 0.5f;

        private HandModel _handModel;

        private PalmCache _palmCache;
        private FingerCache _thumbCache, _indexCache, _middleCache, _ringCache, _pinkyCache;

        #region Unity Events

        #endregion

        #region Recording

        public override bool StartRecording()
        {
            _handModel = GetComponent<HandModel>();

            if (_handModel == null)
            {
                Debug.LogError("Leap Hand recorder has no Leap Hand model on object.");
                return false;
            }

            _handModel.OnUpdate += HandModelUpdate;
            _handModel.OnBegin += HandModelBegin;
            _handModel.OnFinish += HandModelFinish;


            base.StartRecording();

            HandItem.HandID lhi = HandItem.HandID.Left;

            if (_handModel.Handedness == Chirality.Right)
            {
                lhi = HandItem.HandID.Right;
            }

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new HandItem(_descriptor, this.GetType().ToString(), gameObject.activeInHierarchy, lhi);

            SetCaches();

            HandPart palm = new HandPart(HandPart.HandPartID.Palm);
            _recordItem.parts.Add(palm);

            HandPart thumb = new HandPart(HandPart.HandPartID.Thumb);
            _recordItem.parts.Add(thumb);

            HandPart index = new HandPart(HandPart.HandPartID.Index);
            _recordItem.parts.Add(index);

            HandPart middle = new HandPart(HandPart.HandPartID.Middle);
            _recordItem.parts.Add(middle);

            HandPart ring = new HandPart(HandPart.HandPartID.Ring);
            _recordItem.parts.Add(ring);

            HandPart pinky = new HandPart(HandPart.HandPartID.Pinky);
            _recordItem.parts.Add(pinky);

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
            _palmCache.Update(_rotationThreshold);
            _thumbCache.Update(_rotationThreshold);
            _indexCache.Update(_rotationThreshold);
            _middleCache.Update(_rotationThreshold);
            _ringCache.Update(_rotationThreshold);
            _pinkyCache.Update(_rotationThreshold);
        }

        protected override void RecordTickLogic()
        {
            if (_palmCache.updated)
            {
                _palmCache.updated = false;
                _recordItem.parts[0].AddFrame(new PalmFrame(_currentTick, _palmCache.localPosition, _palmCache.localRotation));
            }
            if (_thumbCache.updated)
            {
                AddFingerFrame(1, ref _thumbCache);
            }
            if (_indexCache.updated)
            {
                AddFingerFrame(2, ref _indexCache);
            }
            if (_middleCache.updated)
            {
                AddFingerFrame(3, ref _middleCache);
            }
            if (_ringCache.updated)
            {
                AddFingerFrame(4, ref _ringCache);
            }
            if (_pinkyCache.updated)
            {
                AddFingerFrame(5, ref _pinkyCache);
            }
        }

        private void AddFingerFrame(int partIndex, ref FingerCache cache)
        {
            cache.updated = false;
            _recordItem.parts[partIndex].AddFrame(new FingerFrame(_currentTick,
                cache.metaPos, cache.proxPos, cache.interPos, cache.distPos,
                cache.metaRot, cache.proxRot, cache.interRot, cache.distRot));
        }

        #endregion

        #region Playback

        public override void StartPlaying()
        {
            LeapServiceProvider lsp = FindObjectOfType<LeapServiceProvider>();
            if (lsp != null)
            {
                lsp.enabled = false;
            }
            HandEnableDisable hed = GetComponent<HandEnableDisable>();
            if (hed != null)
            {
                Destroy(hed);
            }
            _handModel = GetComponent<HandModel>();
            if (_handModel != null)
            {
                SetCaches();
                base.StartPlaying();
            }

        }

        protected override void PlayUpdateLogic()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                switch (_playUpdatedParts[i])
                {
                    case 0:
                        _palmCache.PlayUpdate((PalmFrame)_recordItem.parts[0].currentFrame);
                        break;
                    case 1:
                        _thumbCache.PlayUpdate((FingerFrame)_recordItem.parts[1].currentFrame);
                        break;
                    case 2:
                        _indexCache.PlayUpdate((FingerFrame)_recordItem.parts[2].currentFrame);
                        break;
                    case 3:
                        _middleCache.PlayUpdate((FingerFrame)_recordItem.parts[3].currentFrame);
                        break;
                    case 4:
                        _ringCache.PlayUpdate((FingerFrame)_recordItem.parts[4].currentFrame);
                        break;
                    case 5:
                        _pinkyCache.PlayUpdate((FingerFrame)_recordItem.parts[5].currentFrame);
                        break;
                }
            }
        }

        #endregion

        private void SetCaches()
        {
            _palmCache = new PalmCache(_handModel.palm);
            
            FingerModel[] fingers = _handModel.palm.GetComponentsInChildren<FingerModel>();

            for (int i = 0; i < fingers.Length; i++)
            {
                switch (fingers[i].fingerType)
                {
                    case Finger.FingerType.TYPE_THUMB:
                        _thumbCache = new FingerCache(null, fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_INDEX:
                        _indexCache = new FingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_MIDDLE:
                        _middleCache = new FingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_RING:
                        _ringCache = new FingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_PINKY:
                        _pinkyCache = new FingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                }
            }
        }
    }
}
#endif