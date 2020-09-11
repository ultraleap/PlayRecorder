using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayRecorder.Hands;
using Valve.VR;

namespace PlayRecorder.SteamVR
{

    [System.Serializable]
    public class SteamVRPalmFrame : PalmFrame
    {
        public Vector3 rootPosition, wristPosition;
        public Quaternion rootRotation, wristRotation;

        public SteamVRPalmFrame(int tick, Vector3 position, Vector3 rootPosition, Vector3 wristPosition, Quaternion rotation, Quaternion rootRotation, Quaternion wristRotation) : base(tick, position, rotation)
        {
            this.rootPosition = rootPosition;
            this.rootRotation = rootRotation;

            this.wristPosition = wristPosition;
            this.wristRotation = wristRotation;
        }
    }

    [System.Serializable]
    public class SteamVRFingerFrame : FingerFrame
    {
        public Vector3 auxPos;
        public Quaternion auxRot;

        public SteamVRFingerFrame(int tick, Vector3 metaPos, Vector3 proxPos, Vector3 interPos, Vector3 distPos, Vector3 auxPos,
            Quaternion metaRot, Quaternion proxRot, Quaternion interRot, Quaternion distRot, Quaternion auxRot) : base(tick,metaPos,proxPos,interPos,distPos,metaRot,proxRot,interRot,distRot)
        {
            this.auxPos = auxPos;
            this.auxRot = auxRot;
        }
    }

    public class SteamVRPalmCache : PalmCache
    {
        Transform rootTransform, wristTransform;

        public Vector3 rootPosition, wristPosition;
        public Quaternion rootRotation, wristRotation;

        public SteamVRPalmCache (Transform transform, Transform root, Transform wrist) : base(transform)
        {
            this.rootTransform = root;
            this.wristTransform = wrist;
        }

        public override void Update(float rotationThreshold)
        {
            base.Update(rotationThreshold);
            rootPosition = rootTransform.localPosition;
            if ((rootTransform.localRotation.eulerAngles - rootRotation.eulerAngles).sqrMagnitude > rotationThreshold)
            {
                rootRotation = rootTransform.localRotation;
                updated = true;
            }
            wristPosition = wristTransform.localPosition;
            if ((wristTransform.localRotation.eulerAngles - wristRotation.eulerAngles).sqrMagnitude > rotationThreshold)
            {
                wristRotation = wristTransform.localRotation;
                updated = true;
            }
        }

        public override void PlayUpdate(PalmFrame frame)
        {
            base.PlayUpdate(frame);
            rootTransform.localPosition = ((SteamVRPalmFrame)frame).rootPosition;
            rootTransform.localRotation = ((SteamVRPalmFrame)frame).rootRotation;

            wristTransform.localPosition = ((SteamVRPalmFrame)frame).wristPosition;
            wristTransform.localRotation = ((SteamVRPalmFrame)frame).wristRotation;
        }
    }

    public class SteamVRFingerCache : FingerCache
    {

        Transform aux;

        public Vector3 auxPos;
        public Quaternion auxRot;

        public SteamVRFingerCache(Transform meta, Transform prox, Transform inter, Transform dist, Transform aux) : base(meta,prox,inter,dist)
        {
            this.aux = aux;
        }

        public override void Update(float rotationThreshold)
        {
            base.Update(rotationThreshold);
            if(aux != null)
            {
                auxPos = aux.localPosition;
                if ((aux.localRotation.eulerAngles - auxRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    auxRot = aux.localRotation;
                    updated = true;
                }
            }
        }

        public override void PlayUpdate(FingerFrame frame)
        {
            base.PlayUpdate(frame);
            if(aux != null)
            {
                aux.localPosition = ((SteamVRFingerFrame)frame).auxPos;
                aux.localRotation = ((SteamVRFingerFrame)frame).auxRot;
            }
        }
    }

    public class SteamVRSkeletonRecordComponent : RecordComponent
    {
        [SerializeField, Tooltip("This value is the square magnitude at which rotation changes will cause a frame to be stored.")]
        float _rotationThreshold = 0.5f;

        [SerializeField]
        SteamVR_Behaviour_Pose _handPose;

        [SerializeField]
        SteamVR_Behaviour_Skeleton _handSkeleton;

        SteamVRPalmCache _palmCache;
        SteamVRFingerCache _thumbCache, _indexCache, _middleCache, _ringCache, _pinkyCache;

        // Only used in playback
        FingerCache _playbackCache;

        public override void StartRecording()
        {
            _handPose = GetComponent<SteamVR_Behaviour_Pose>();

            if(_handPose == null)
            {
                Debug.LogError("SteamVR hand recorder has no SteamVR Behaviour Pose on the current object.");
                return;
            }

            _handSkeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>(true);

            if (_handSkeleton == null)
            {
                Debug.LogError("SteamVR hand recorder has no SteamVR Behaviour Skeleton on object.");
                return;
            }

            HandItem.HandID shi = HandItem.HandID.Left;

            switch (_handSkeleton.inputSource)
            {
                case SteamVR_Input_Sources.LeftHand:
                    shi = HandItem.HandID.Left;
                    break;
                case SteamVR_Input_Sources.RightHand:
                    shi = HandItem.HandID.Right;
                    break;
                default:
                    // Only record hands!
                    Debug.LogError("SteamVR Behaviour Skeleton is not set to a hand and will be ignored.");
                    return;
            }

            base.StartRecording();

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new HandItem(_descriptor, this.GetType().ToString(), gameObject.activeInHierarchy, shi);

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
        }

        protected override void RecordUpdate()
        {
            _palmCache.Update(_rotationThreshold);
            _thumbCache.Update(_rotationThreshold);
            _indexCache.Update(_rotationThreshold);
            _middleCache.Update(_rotationThreshold);
            _ringCache.Update(_rotationThreshold);
            _pinkyCache.Update(_rotationThreshold);
        }

        private void SetCaches()
        {
            _palmCache = new SteamVRPalmCache(_handPose.transform,_handSkeleton.root,_handSkeleton.wrist);

            _thumbCache = new SteamVRFingerCache(null, _handSkeleton.thumbProximal, _handSkeleton.thumbMiddle, _handSkeleton.thumbDistal, _handSkeleton.thumbAux);

            _indexCache = new SteamVRFingerCache(_handSkeleton.indexMetacarpal, _handSkeleton.indexProximal, _handSkeleton.indexMiddle, _handSkeleton.indexDistal, _handSkeleton.indexAux);
            _middleCache = new SteamVRFingerCache(_handSkeleton.middleMetacarpal, _handSkeleton.middleProximal, _handSkeleton.middleMiddle, _handSkeleton.middleDistal, _handSkeleton.middleAux);
            _ringCache = new SteamVRFingerCache(_handSkeleton.ringMetacarpal, _handSkeleton.ringProximal, _handSkeleton.ringMiddle, _handSkeleton.ringDistal, _handSkeleton.ringAux);
            _pinkyCache = new SteamVRFingerCache(_handSkeleton.pinkyMetacarpal, _handSkeleton.pinkyProximal, _handSkeleton.pinkyMiddle, _handSkeleton.pinkyDistal, _handSkeleton.pinkyAux);
        }

        protected override void RecordTickLogic()
        {
            if (_palmCache.updated)
            {
                _palmCache.updated = false;
                _recordItem.parts[0].AddFrame(new SteamVRPalmFrame(_currentTick, _palmCache.localPosition, _palmCache.rootPosition, _palmCache.wristPosition, _palmCache.localRotation, _palmCache.rootRotation, _palmCache.wristRotation));
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

        private void AddFingerFrame(int partIndex, ref SteamVRFingerCache cache)
        {
            cache.updated = false;
            _recordItem.parts[partIndex].AddFrame(new SteamVRFingerFrame(_currentTick,
                cache.metaPos, cache.proxPos, cache.interPos, cache.distPos, cache.auxPos,
                cache.metaRot, cache.proxRot, cache.interRot, cache.distRot, cache.auxRot));
        }

        public override void StartPlaying()
        {
            _handPose = GetComponent<SteamVR_Behaviour_Pose>();
            _handSkeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>(true);
            if(_handPose == null || _handSkeleton == null)
            {
                Debug.LogError("SteamVR hand recorder does not included required components and will not be played back.");
                return;
            }
            SetCaches();
            _handPose.enabled = false;
            _handSkeleton.enabled = false;
            Animator anim = _handPose.GetComponentInChildren<Animator>(true);
            if(anim != null)
            {
                anim.enabled = false;
            }
            SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if(smr != null)
            {
                smr.gameObject.SetActive(true);
                smr.enabled = true;
            }
            base.StartPlaying();
        }

        protected override void PlayUpdate()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                switch (_playUpdatedParts[i])
                {
                    case 0:
                        _palmCache.PlayUpdate((SteamVRPalmFrame)_recordItem.parts[0].currentFrame);
                        break;
                    case 1:
                        _thumbCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[1].currentFrame);
                        break;
                    case 2:
                        _indexCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[2].currentFrame);
                        break;
                    case 3:
                        _middleCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[3].currentFrame);
                        break;
                    case 4:
                        _ringCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[4].currentFrame);
                        break;
                    case 5:
                        _pinkyCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[5].currentFrame);
                        break;
                }
            }
        }

        private void OnValidate()
        {
            if(_handPose == null)
            {
                _handPose = GetComponent<SteamVR_Behaviour_Pose>();
            }
            if(_handSkeleton == null)
            {
                _handSkeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>();
            }
        }
    }

}