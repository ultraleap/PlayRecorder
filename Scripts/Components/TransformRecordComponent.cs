using System.Collections;
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

        public TransformFrame(int tick, TransformCache tc) : base(tick)
        {
            localPosition = tc.localPosition;
            localRotation = tc.localRotation;
            localScale = tc.localScale;
        }
    }

    public class TransformCache
    {
        Transform transform;

        // Used in main thread
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;

        public bool hasChanged = false;

        public TransformCache(Transform transform)
        {
            this.transform = transform;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
            localScale = transform.localScale;
        }

        public void Update()
        {
            hasChanged = false;
            if (transform.localPosition != localPosition)
            {
                localPosition = transform.localPosition;
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

        [SerializeField, Tooltip("Automatically assigned to the current object transform, changes will be ignored and reset once recording starts.")]
        protected Transform baseTransform = null;

        [SerializeField]
        protected List<Transform> _extraTransforms = new List<Transform>();

        protected List<TransformCache> _transformCache = new List<TransformCache>();

        public override void StartRecording()
        {
            base.StartRecording();

            baseTransform = gameObject.transform;

            _transformCache.Clear();
            _transformCache.Add(new TransformCache(baseTransform));
            for (int i = 0; i < _extraTransforms.Count; i++)
            {
                if (_extraTransforms[i] == null)
                    continue;

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
                if (_transformCache[i].hasChanged)
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

        public override void StartPlaying()
        {
            base.StartPlaying();
            if (baseTransform != null)
            {
                DisableAllComponents(baseTransform);
            }


            for (int i = 0; i < _extraTransforms.Count; i++)
            {
                if (_extraTransforms[i] != null)
                {
                    DisableAllComponents(_extraTransforms[i]);
                }
            }
        }

        protected override void PlayUpdate()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                switch (_playUpdatedParts[i])
                {
                    case 0:
                        if (baseTransform != null && _recordItem.parts[0].currentFrame != null)
                            ApplyTransform((TransformFrame)_recordItem.parts[0].currentFrame, baseTransform);
                        break;
                    default:
                        if (_extraTransforms[_playUpdatedParts[i] - 1] != null && _recordItem.parts[_playUpdatedParts[i]].currentFrame != null)
                            ApplyTransform((TransformFrame)_recordItem.parts[_playUpdatedParts[i]].currentFrame, _extraTransforms[_playUpdatedParts[i] - 1]);
                        break;
                }
            }
        }

        void ApplyTransform(TransformFrame frame, Transform transform)
        {
            try
            {
                transform.localPosition = frame.localPosition;
                transform.localRotation = frame.localRotation;
                transform.localScale = frame.localScale;
            }
            catch
            {
                Debug.Log("ah");
            }
        }

        void DisableAllComponents(Transform transform)
        {
            Behaviour[] mb = transform.GetComponents<Behaviour>();
            for (int i = 0; i < mb.Length; i++)
            {
                // This may need more items to be added
                if (!(typeof(RecordComponent).IsSameOrSubclass(mb[i].GetType()) ||
                   mb[i].GetType() == typeof(Renderer) ||
                   mb[i].GetType() == typeof(MeshFilter) ||
                   mb[i].GetType() == typeof(Camera)))
                {
                    mb[i].enabled = false;
                }
                if (mb[i].GetType() == typeof(Camera))
                {
                    ((Camera)mb[i]).stereoTargetEye = StereoTargetEyeMask.None;
                }
            }
        }

    }

}