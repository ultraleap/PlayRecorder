using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    [System.Serializable]
    public class TransformFrame : Frame
    {
        // Used in recording thread
        public Vector3 position;
        public Vector3 localPosition;
        public Quaternion rotation;
        public Quaternion localRotation;
        public Vector3 localScale;

        public TransformFrame(int tick, TransformCache tc) : base(tick)
        {
            position = tc.position;
            localPosition = tc.localPosition;
            rotation = tc.rotation;
            localRotation = tc.localRotation;
            localScale = tc.localScale;
        }
    }

    public class TransformCache
    {
        Transform transform;

        // Used in main thread
        public Vector3 position;
        public Vector3 localPosition;
        public Quaternion rotation;
        public Quaternion localRotation;
        public Vector3 localScale;

        public bool hasChanged = false;

        public TransformCache(Transform transform)
        {
            this.transform = transform;
            position = transform.position;
            localPosition = transform.localPosition;
            rotation = transform.rotation;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
        }

        public void Update()
        {
            hasChanged = false;
            if (transform.position != position)
            {
                position = transform.position;
                hasChanged = true;
            }
            if (transform.localPosition != localPosition)
            {
                localPosition = transform.localPosition;
                hasChanged = true;
            }
            if (transform.rotation != rotation)
            {
                rotation = transform.rotation;
                hasChanged = true;
            }
            if (transform.localRotation != localRotation)
            {
                localRotation = transform.localRotation;
                hasChanged = true;
            }
            if (transform.localScale != localScale)
            {
                localScale = transform.localScale;
                hasChanged = true;
            }
        }

    }

    public class TransformRecordComponent : RecordComponent
    {

        [SerializeField]
        Transform baseTransform = null;

        [SerializeField]
        List<Transform> _extraTransforms = new List<Transform>();

        List<TransformCache> _transformCache = new List<TransformCache>();

        public override void StartRecording()
        {
            base.StartRecording();

            baseTransform = gameObject.transform;

            _transformCache.Clear();
            _transformCache.Add(new TransformCache(baseTransform));
            for (int i = 0; i < _extraTransforms.Count; i++)
            {
                TransformCache tc = new TransformCache(_extraTransforms[i]);
                _transformCache.Add(tc);
                
            }

            for (int i = 0; i < _transformCache.Count; i++)
            {
                RecordPart rp = new RecordPart();
                rp.AddFrame(new TransformFrame(_currentTick, _transformCache[i]));
                _recordItem.parts.Add(rp);
            }
        }

        protected override void RecordUpdate()
        {
            for (int i = 0; i < _transformCache.Count; i++)
            {
                _transformCache[i].Update();
            }
        }

        protected override void RecordTickLogic()
        {
            for (int i = 0; i < _transformCache.Count; i++)
            {
                if(_transformCache[i].hasChanged)
                {
                    _transformCache[i].hasChanged = false;
                    _recordItem.parts[i].AddFrame(new TransformFrame(_currentTick, _transformCache[i]));
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            baseTransform = gameObject.transform;
        }
#endif

        protected override void PlayUpdate()
        {
            if (_recordItem.parts.Count == 0 || _playUpdatedParts.Count == 0)
                return;

            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                switch (_playUpdatedParts[i])
                {
                    case 0:
                        if(baseTransform != null)
                            ApplyTransform((TransformFrame)_recordItem.parts[0].currentFrame, baseTransform);
                        break;
                    default:
                        if (_extraTransforms[_playUpdatedParts[i] - 1] != null)
                            ApplyTransform((TransformFrame)_recordItem.parts[_playUpdatedParts[i]].currentFrame, _extraTransforms[_playUpdatedParts[i] - 1]);
                        break;
                }
            }
            
        }

        void ApplyTransform(TransformFrame frame, Transform transform)
        {
            transform.position = frame.position;
            transform.localPosition = frame.localPosition;
            transform.rotation = frame.rotation;
            transform.localRotation = frame.localRotation;
            transform.localScale = frame.localScale;
        }

    }

}