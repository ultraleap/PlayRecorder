using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    [System.Serializable]
    public class TransformFrame : RecordFrame
    {
        // Used in recording thread
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public TransformFrame(int tick, TransformRecordComponent.TransformCache tc) : base(tick)
        {
            localPosition = tc.localPosition;
            localRotation = tc.localRotation;
            localScale = tc.localScale;
        }
    }

    [System.Serializable]
    public enum TransformSpace
    {
        Local = 0,
        World = 1
    }

    [System.Serializable]
    public class TransformItem : RecordItem
    {
        public TransformSpace space;

        public TransformItem(string descriptor, string type, bool active, TransformSpace space) : base(descriptor, type, active)
        {
            this.space = space;
        }
    }

    [AddComponentMenu("PlayRecorder/RecordComponents/Transform Record Component")]
    public class TransformRecordComponent : RecordComponent
    {
        [SerializeField, Tooltip("Controls whether the recording will be done in local or world space. Playback will use the recorded space.")]
        protected TransformSpace _transformSpace = TransformSpace.Local;
        private TransformSpace _playbackSpace = TransformSpace.Local;

        [SerializeField, Tooltip("Automatically assigned to the current object transform, changes will be ignored and reset once recording starts.")]
        protected Transform _baseTransform = null;

        [SerializeField]
        protected List<Transform> _extraTransforms = new List<Transform>();

        protected List<TransformCache> _transformCache = new List<TransformCache>();

        public class TransformCache
        {
            private Transform _transform;
            private TransformSpace _space;
            // Used in main thread
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;

            public bool hasChanged = false;

            public TransformCache(Transform transform, TransformSpace space)
            {
                this._transform = transform;
                this._space = space;
                switch (this._space)
                {
                    case TransformSpace.Local:
                        localPosition = transform.localPosition;
                        localRotation = transform.localRotation;
                        localScale = transform.localScale;
                        break;
                    case TransformSpace.World:
                        localPosition = transform.position;
                        localRotation = transform.rotation;
                        localScale = transform.localScale;
                        break;
                }
            }

            public void Update()
            {
                if (_transform.localPosition != localPosition)
                {
                    localPosition = _transform.localPosition;
                    hasChanged = true;
                }
                if (_transform.localRotation != localRotation)
                {
                    localRotation = _transform.localRotation;
                    hasChanged = true;
                }
                if (_transform.localScale != localScale)
                {
                    localScale = _transform.localScale;
                    hasChanged = true;
                }
            }
        }

        #region Unity Events

#if UNITY_EDITOR
        private void OnValidate()
        {
            _baseTransform = gameObject.transform;
        }

        protected override void Reset()
        {
            base.Reset();
            _baseTransform = gameObject.transform;
        }
#endif

        #endregion

        #region Recording

        public override bool StartRecording()
        {
            base.StartRecording();

            _baseTransform = gameObject.transform;

            SetTransformParts();
            return true;
        }

        protected void SetTransformParts()
        {
            _transformCache.Clear();
            _transformCache.Add(new TransformCache(_baseTransform,_transformSpace));
            for (int i = 0; i < _extraTransforms.Count; i++)
            {
                if (_extraTransforms[i] == null)
                    continue;

                TransformCache tc = new TransformCache(_extraTransforms[i],_transformSpace);
                _transformCache.Add(tc);

            }

            for (int i = 0; i < _transformCache.Count; i++)
            {
                RecordPart rp = new RecordPart();
                rp.AddFrame(new TransformFrame(_currentTick, _transformCache[i]));
                _recordItem.parts.Add(rp);
            }
        }

        protected override void RecordUpdateLogic()
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
                if (_transformCache[i].hasChanged)
                {
                    _transformCache[i].hasChanged = false;
                    _recordItem.parts[i].AddFrame(new TransformFrame(_currentTick, _transformCache[i]));
                }
            }
        }

        protected override void OnRecordingEnable()
        {
            for (int i = 0; i < _transformCache.Count; i++)
            {
                _transformCache[i].Update();
                _transformCache[i].hasChanged = true;
            }
        }

        #endregion

        #region Playback

        protected override void OnSetPlaybackData()
        {
            if(_recordItem.type == typeof(TransformItem).ToString())
            {
                // Updated transform recording
                _playbackSpace = ((TransformItem)_recordItem).space;
            }
            else
            {
                // Previous data type
                _playbackSpace = TransformSpace.Local;
            }
        }

        protected override void SetPlaybackIgnoreTransforms()
        {
            _extraTransforms.Clear();
            if(_baseTransform != null)
            {
                _playbackIgnoreTransforms.Add(_baseTransform);
            }
            for (int i = 0; i < _extraTransforms.Count; i++)
            {
                _playbackIgnoreTransforms.Add(_extraTransforms[i]);
            }
        }

        protected override PlaybackIgnoreItem SetDefaultPlaybackIgnores(string type)
        {
            PlaybackIgnoreItem pbi = new PlaybackIgnoreItem(type);
            pbi.enabledBehaviours.Add("UnityEngine.UI.");
            return pbi;
        }

        protected override void PlayUpdateLogic()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                switch (_playUpdatedParts[i])
                {
                    case 0:
                        if (_baseTransform != null && _recordItem.parts[0].currentFrame != null)
                            ApplyTransform((TransformFrame)_recordItem.parts[0].currentFrame, _baseTransform);
                        break;
                    default:
                        if (_extraTransforms[_playUpdatedParts[i] - 1] != null && _recordItem.parts[_playUpdatedParts[i]].currentFrame != null)
                            ApplyTransform((TransformFrame)_recordItem.parts[_playUpdatedParts[i]].currentFrame, _extraTransforms[_playUpdatedParts[i] - 1]);
                        break;
                }
            }
        }

        #endregion

        protected void ApplyTransform(TransformFrame frame, Transform transform)
        {
            try
            {
                switch (_playbackSpace)
                {
                    case TransformSpace.Local:
                        transform.localPosition = frame.localPosition;
                        transform.localRotation = frame.localRotation;
                        transform.localScale = frame.localScale;
                        break;
                    case TransformSpace.World:
                        transform.position = frame.localPosition;
                        transform.rotation = frame.localRotation;
                        transform.localScale = frame.localScale;
                        break;
                }
            }
            catch
            {
                Debug.LogWarning("Transform unable to be updated on " + name + " at tick " + _currentTick.ToString());
            }
        }
    }
}