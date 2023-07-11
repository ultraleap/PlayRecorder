// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_STEAMVR;
// This should be attached to a SteamVR_Behaviour_Pose and will require a SteamVR_Behaviour_Skeleton
#if PR_STEAMVR
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
        private Transform _rootTransform, _wristTransform;

        public Vector3 rootPosition, wristPosition;
        public Quaternion rootRotation, wristRotation;

        public SteamVRPalmCache (Transform transform, Transform root, Transform wrist) : base(transform)
        {
            this._rootTransform = root;
            this._wristTransform = wrist;
        }

        public override void Update(float rotationThreshold)
        {
            base.Update(rotationThreshold);
            rootPosition = _rootTransform.localPosition;
            if ((_rootTransform.localRotation.eulerAngles - rootRotation.eulerAngles).sqrMagnitude > rotationThreshold)
            {
                rootRotation = _rootTransform.localRotation;
                updated = true;
            }
            wristPosition = _wristTransform.localPosition;
            if ((_wristTransform.localRotation.eulerAngles - wristRotation.eulerAngles).sqrMagnitude > rotationThreshold)
            {
                wristRotation = _wristTransform.localRotation;
                updated = true;
            }
        }

        public override void PlayUpdate(PalmFrame frame)
        {
            base.PlayUpdate(frame);
            _rootTransform.localPosition = ((SteamVRPalmFrame)frame).rootPosition;
            _rootTransform.localRotation = ((SteamVRPalmFrame)frame).rootRotation;

            _wristTransform.localPosition = ((SteamVRPalmFrame)frame).wristPosition;
            _wristTransform.localRotation = ((SteamVRPalmFrame)frame).wristRotation;
        }
    }

    public class SteamVRFingerCache : FingerCache
    {

        private Transform _aux;

        public Vector3 auxPos;
        public Quaternion auxRot;

        public SteamVRFingerCache(Transform meta, Transform prox, Transform inter, Transform dist, Transform aux) : base(meta,prox,inter,dist)
        {
            this._aux = aux;
        }

        public override void Update(float rotationThreshold)
        {
            base.Update(rotationThreshold);
            if(_aux != null)
            {
                auxPos = _aux.localPosition;
                if ((_aux.localRotation.eulerAngles - auxRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    auxRot = _aux.localRotation;
                    updated = true;
                }
            }
        }

        public override void PlayUpdate(FingerFrame frame)
        {
            base.PlayUpdate(frame);
            if(_aux != null)
            {
                _aux.localPosition = ((SteamVRFingerFrame)frame).auxPos;
                _aux.localRotation = ((SteamVRFingerFrame)frame).auxRot;
            }
        }
    }

    [AddComponentMenu("PlayRecorder/SteamVR/SteamVR Skeleton Record Component")]
    public class SteamVRSkeletonRecordComponent : RecordComponent
    {
        [SerializeField, Tooltip("This value is the square magnitude at which rotation changes will cause a frame to be stored.")]
        private float _rotationThreshold = 0.5f;

        [SerializeField]
        private SteamVR_Behaviour_Pose _handPose;

        [SerializeField]
        private SteamVR_Behaviour_Skeleton _handSkeleton;

        private SteamVRPalmCache _palmCache;
        private SteamVRFingerCache _thumbCache, _indexCache, _middleCache, _ringCache, _pinkyCache;

        private const int _palmIndex = 0, _thumbIndex = 1, _indexIndex = 2, _middleIndex = 3, _ringIndex = 4, _pinkyIndex = 5;

        #region Unity Events

        private void OnValidate()
        {
            if (_handPose == null)
            {
                _handPose = GetComponent<SteamVR_Behaviour_Pose>();
            }
            if (_handSkeleton == null)
            {
                _handSkeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>();
            }
        }

        #endregion

        #region Recording

        public override bool StartRecording()
        {
            _handPose = GetComponent<SteamVR_Behaviour_Pose>();

            if (_handPose == null)
            {
                Debug.LogError("SteamVR hand recorder has no SteamVR Behaviour Pose on the current object.");
                return false;
            }

            _handSkeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>(true);

            if (_handSkeleton == null)
            {
                Debug.LogError("SteamVR hand recorder has no SteamVR Behaviour Skeleton on object.");
                return false;
            }

            if(!(_handSkeleton.inputSource == SteamVR_Input_Sources.LeftHand || _handSkeleton.inputSource == SteamVR_Input_Sources.RightHand))
            {
                // Only record hands!
                Debug.LogError("SteamVR Behaviour Skeleton is not set to a hand and will be ignored.");
                return false;
            }


            HandItem.HandID skeletonHandID = HandItem.HandID.Left;

            if(_handSkeleton.inputSource == SteamVR_Input_Sources.RightHand)
            {
                skeletonHandID = HandItem.HandID.Right;
            }

            base.StartRecording();

            // Could just not use the base.StartRecording() but we don't know what's going to change there
            _recordItem = new HandItem(_descriptor, this.GetType().ToString(), gameObject.activeInHierarchy, skeletonHandID);

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

        protected override void RecordUpdateLogic()
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
                _recordItem.parts[_palmIndex].AddFrame(new SteamVRPalmFrame(_currentTick, _palmCache.localPosition, _palmCache.rootPosition, _palmCache.wristPosition, _palmCache.localRotation, _palmCache.rootRotation, _palmCache.wristRotation));
            }
            if (_thumbCache.updated)
            {
                AddFingerFrame(_thumbIndex, ref _thumbCache);
            }
            if (_indexCache.updated)
            {
                AddFingerFrame(_indexIndex, ref _indexCache);
            }
            if (_middleCache.updated)
            {
                AddFingerFrame(_middleIndex, ref _middleCache);
            }
            if (_ringCache.updated)
            {
                AddFingerFrame(_ringIndex, ref _ringCache);
            }
            if (_pinkyCache.updated)
            {
                AddFingerFrame(_pinkyIndex, ref _pinkyCache);
            }
        }

        private void AddFingerFrame(int partIndex, ref SteamVRFingerCache cache)
        {
            cache.updated = false;
            _recordItem.parts[partIndex].AddFrame(new SteamVRFingerFrame(_currentTick,
                cache.metaPos, cache.proxPos, cache.interPos, cache.distPos, cache.auxPos,
                cache.metaRot, cache.proxRot, cache.interRot, cache.distRot, cache.auxRot));
        }

        #endregion

        #region Playback

        public override void StartPlaying()
        {
            _handPose = GetComponent<SteamVR_Behaviour_Pose>();
            _handSkeleton = GetComponentInChildren<SteamVR_Behaviour_Skeleton>(true);
            if (_handPose == null || _handSkeleton == null)
            {
                Debug.LogError("SteamVR hand recorder does not included required components and will not be played back.");
                return;
            }
            SetCaches();
            _handPose.enabled = false;
            _handSkeleton.enabled = false;
            Animator anim = _handPose.GetComponentInChildren<Animator>(true);
            if (anim != null)
            {
                anim.enabled = false;
            }
            SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (smr != null)
            {
                smr.gameObject.SetActive(true);
                smr.enabled = true;
            }
            base.StartPlaying();
        }

        protected override void PlayUpdateLogic()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                switch (_playUpdatedParts[i])
                {
                    case _palmIndex:
                        _palmCache.PlayUpdate((SteamVRPalmFrame)_recordItem.parts[_palmIndex].currentFrame);
                        break;
                    case _thumbIndex:
                        _thumbCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[_thumbIndex].currentFrame);
                        break;
                    case _indexIndex:
                        _indexCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[_indexIndex].currentFrame);
                        break;
                    case _middleIndex:
                        _middleCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[_middleIndex].currentFrame);
                        break;
                    case _ringIndex:
                        _ringCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[_ringIndex].currentFrame);
                        break;
                    case _pinkyIndex:
                        _pinkyCache.PlayUpdate((SteamVRFingerFrame)_recordItem.parts[_pinkyIndex].currentFrame);
                        break;
                }
            }
        }

        #endregion

        private void SetCaches()
        {
            _palmCache = new SteamVRPalmCache(_handPose.transform,_handSkeleton.root,_handSkeleton.wrist);

            _thumbCache = new SteamVRFingerCache(null, _handSkeleton.thumbProximal, _handSkeleton.thumbMiddle, _handSkeleton.thumbDistal, _handSkeleton.thumbAux);

            _indexCache = new SteamVRFingerCache(_handSkeleton.indexMetacarpal, _handSkeleton.indexProximal, _handSkeleton.indexMiddle, _handSkeleton.indexDistal, _handSkeleton.indexAux);
            _middleCache = new SteamVRFingerCache(_handSkeleton.middleMetacarpal, _handSkeleton.middleProximal, _handSkeleton.middleMiddle, _handSkeleton.middleDistal, _handSkeleton.middleAux);
            _ringCache = new SteamVRFingerCache(_handSkeleton.ringMetacarpal, _handSkeleton.ringProximal, _handSkeleton.ringMiddle, _handSkeleton.ringDistal, _handSkeleton.ringAux);
            _pinkyCache = new SteamVRFingerCache(_handSkeleton.pinkyMetacarpal, _handSkeleton.pinkyProximal, _handSkeleton.pinkyMiddle, _handSkeleton.pinkyDistal, _handSkeleton.pinkyAux);
        }
    }
}
#endif