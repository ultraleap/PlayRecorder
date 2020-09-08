using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap;
using Leap.Unity;

namespace PlayRecorder.Leap
{

    [System.Serializable]
    public class LeapPalmFrame : Frame
    {
        // Local Position
        public Vector3 position;
        // Local Rotation
        public Quaternion rotation;

        public LeapPalmFrame(int tick, Vector3 position, Quaternion rotation) : base(tick)
        {
            this.position = position;
            this.rotation = rotation;
        }

    }

    [System.Serializable]
    public class LeapFingerFrame : Frame
    {
        // Metacarpal -> Proximal -> Intermediate -> Distal
        // Thumb only uses last 3

        // Local Positions
        public Vector3 metaPos, proxPos, interPos, distPos;
        // Local Rotations
        public Quaternion metaRot, proxRot, interRot, distRot;

        public LeapFingerFrame(int tick, Vector3 metaPos, Vector3 proxPos, Vector3 interPos, Vector3 distPos,
            Quaternion metaRot, Quaternion proxRot, Quaternion interRot, Quaternion distRot) : base(tick)
        {
            this.metaPos = metaPos;
            this.metaRot = metaRot;

            this.proxPos = proxPos;
            this.proxRot = proxRot;

            this.interPos = interPos;
            this.interRot = interRot;

            this.distPos = distPos;
            this.distRot = distRot;
        }

    }

    [System.Serializable]
    public enum LeapPartID
    {
        Palm = 0,
        Thumb = 1,
        Index = 2,
        Middle = 3,
        Ring = 4,
        Pinky = 5
    }

    [System.Serializable]
    public class LeapPart : RecordPart
    {
        public LeapPartID handPart;

        public LeapPart(LeapPartID handPart)
        {
            this.handPart = handPart;
        }
    }

    [System.Serializable]
    public enum LeapHandID
    {
        Left = 0,
        Right = 1
    }

    [System.Serializable]
    public class LeapRecordItem : RecordItem
    {

        public LeapHandID handID;

        public LeapRecordItem(string descriptor, string type, bool active, LeapHandID hand) : base(descriptor, type, active)
        {
            handID = hand;
        }
    }

    public class LeapPalmCache
    {
        public Transform transform;

        public bool updated;

        public Vector3 localPosition;
        public Quaternion localRotation;

        public LeapPalmCache(Transform transform)
        {
            this.transform = transform;
            updated = true;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
        }

        public void Update(float rotationThreshold)
        {
            localPosition = transform.localPosition;
            if((transform.localRotation.eulerAngles - localRotation.eulerAngles).sqrMagnitude > rotationThreshold)
            {
                localRotation = transform.localRotation;
                updated = true;
            }
        }

        public void PlayUpdate(LeapPalmFrame frame)
        {
            transform.localPosition = frame.position;
            transform.localRotation = frame.rotation;
        }
    }

    public class LeapFingerCache
    {
        public Transform meta, prox, inter, dist;

        public bool updated;

        public Vector3 metaPos, proxPos, interPos, distPos;
        public Quaternion metaRot, proxRot, interRot, distRot;

        public LeapFingerCache(Transform meta, Transform prox, Transform inter, Transform dist)
        {
            this.meta = meta;
            this.prox = prox;
            this.inter = inter;
            this.dist = dist;
            updated = true;
        }

        public void Update(float rotationThreshold)
        {
            if(meta != null)
            {
                metaPos = meta.localPosition;
                if ((meta.localRotation.eulerAngles - metaRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    metaRot = meta.localRotation;
                    updated = true;
                }
            }
            if(prox != null)
            {
                proxPos = prox.localPosition;
                if ((prox.localRotation.eulerAngles - proxRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    proxRot = prox.localRotation;
                    updated = true;
                }
            }
            if(inter != null)
            {
                interPos = inter.localPosition;
                if ((inter.localRotation.eulerAngles - interRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    interRot = inter.localRotation;
                    updated = true;
                }
            }
            if(dist != null)
            {
                distPos = dist.localPosition;
                if ((dist.localRotation.eulerAngles - distRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    distRot = dist.localRotation;
                    updated = true;
                }
            }
        }

        public void PlayUpdate(LeapFingerFrame frame)
        {
            if(meta != null)
            {
                meta.localPosition = frame.metaPos;
                meta.localRotation = frame.metaRot;
            }
            if(prox != null)
            {
                prox.localPosition = frame.proxPos;
                prox.localRotation = frame.proxRot;
            }
            if(inter != null)
            {
                inter.localPosition = frame.interPos;
                inter.localRotation = frame.interRot;
            }
            if(dist != null)
            {
                dist.localPosition = frame.distPos;
                dist.localRotation = frame.distRot;
            }
        }
    }



    public class LeapHandRecordComponent : RecordComponent
    {

        [SerializeField,Tooltip("This value is the square magnitude at which rotation changes will cause a frame to be stored.")]
        float _rotationThreshold = 0.5f;

        HandModel _handModel;

        LeapPalmCache _palmCache;
        LeapFingerCache _thumbCache, _indexCache, _middleCache, _ringCache, _pinkyCache;

        // Only used in playback
        LeapFingerCache _playbackCache;

        public override void StartRecording()
        {
            _handModel = GetComponent<HandModel>();
            
            if (_handModel == null)
            {
                Debug.LogError("Leap Hand recorder has no Leap Hand model on object.");
                return;
            }

            _handModel.OnUpdate += HandModelUpdate;
            _handModel.OnBegin += HandModelBegin;
            _handModel.OnFinish += HandModelFinish;


            base.StartRecording();

            LeapHandID lhi = LeapHandID.Left;

            if(_handModel.Handedness == Chirality.Right)
            {
                lhi = LeapHandID.Right;
            }

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new LeapRecordItem(_descriptor, this.GetType().ToString(), gameObject.activeInHierarchy, lhi);

            SetCaches(_handModel);

            LeapPart palm = new LeapPart(LeapPartID.Palm);
            _recordItem.parts.Add(palm);

            LeapPart thumb = new LeapPart(LeapPartID.Thumb);
            _recordItem.parts.Add(thumb);

            LeapPart index = new LeapPart(LeapPartID.Index);
            _recordItem.parts.Add(index);

            LeapPart middle = new LeapPart(LeapPartID.Middle);
            _recordItem.parts.Add(middle);

            LeapPart ring = new LeapPart(LeapPartID.Ring);
            _recordItem.parts.Add(ring);

            LeapPart pinky = new LeapPart(LeapPartID.Pinky);
            _recordItem.parts.Add(pinky);
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
            Debug.Log("hmb");
            _recordItem.AddStatus(true, _currentTick);
        }

        private void HandModelFinish()
        {
            Debug.Log("hmf");
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

        private void SetCaches(HandModel handModel)
        {
            _palmCache = new LeapPalmCache(_handModel.palm);
            
            FingerModel[] fingers = _handModel.palm.GetComponentsInChildren<FingerModel>();

            for (int i = 0; i < fingers.Length; i++)
            {
                switch (fingers[i].fingerType)
                {
                    case Finger.FingerType.TYPE_THUMB:
                        _thumbCache = new LeapFingerCache(null, fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_INDEX:
                        _indexCache = new LeapFingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_MIDDLE:
                        _middleCache = new LeapFingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_RING:
                        _ringCache = new LeapFingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                    case Finger.FingerType.TYPE_PINKY:
                        _pinkyCache = new LeapFingerCache(fingers[i].bones[0], fingers[i].bones[1], fingers[i].bones[2], fingers[i].bones[3]);
                        break;
                }
            }
        }

        protected override void RecordTickLogic()
        {
            if(_palmCache.updated)
            {
                _palmCache.updated = false;
                _recordItem.parts[0].AddFrame(new LeapPalmFrame(_currentTick, _palmCache.localPosition, _palmCache.localRotation));
            }
            if(_thumbCache.updated)
            {
                AddFingerFrame(1, ref _thumbCache);
            }
            if (_indexCache.updated)
            {
                AddFingerFrame(2, ref _indexCache);
            }
            if(_middleCache.updated)
            {
                AddFingerFrame(3, ref _middleCache);
            }
            if(_ringCache.updated)
            {
                AddFingerFrame(4, ref _ringCache);
            }
            if(_pinkyCache.updated)
            {
                AddFingerFrame(5, ref _pinkyCache);
            }
        }

        private void AddFingerFrame(int partIndex, ref LeapFingerCache cache)
        {
            cache.updated = false;
            _recordItem.parts[partIndex].AddFrame(new LeapFingerFrame(_currentTick,
                cache.metaPos, cache.proxPos, cache.interPos, cache.distPos,
                cache.metaRot, cache.proxRot, cache.interRot, cache.distRot));
        }

        public override void StartPlaying()
        {
            LeapServiceProvider lsp = FindObjectOfType<LeapServiceProvider>();
            if(lsp != null)
            {
                lsp.enabled = false;
            }
            HandEnableDisable hed = GetComponent<HandEnableDisable>();
            if(hed != null)
            {
                Destroy(hed);
            }
            _handModel = GetComponent<HandModel>();
            if(_handModel != null)
            {
                SetCaches(_handModel);
                base.StartPlaying();
            }

        }

        protected override void PlayUpdate()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                switch (_playUpdatedParts[i])
                {
                    case 0:
                        _palmCache.PlayUpdate((LeapPalmFrame)_recordItem.parts[0].currentFrame);
                        break;
                    case 1:
                        _thumbCache.PlayUpdate((LeapFingerFrame)_recordItem.parts[1].currentFrame);
                        break;
                    case 2:
                        _indexCache.PlayUpdate((LeapFingerFrame)_recordItem.parts[2].currentFrame);
                        break;
                    case 3:
                        _middleCache.PlayUpdate((LeapFingerFrame)_recordItem.parts[3].currentFrame);
                        break;
                    case 4:
                        _ringCache.PlayUpdate((LeapFingerFrame)_recordItem.parts[4].currentFrame);
                        break;
                    case 5:
                        _pinkyCache.PlayUpdate((LeapFingerFrame)_recordItem.parts[5].currentFrame);
                        break;
                }
            }
        }

    }

}