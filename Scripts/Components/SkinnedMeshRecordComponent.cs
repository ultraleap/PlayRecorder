using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class SkinnedMeshItem : RecordItem
    {
        public bool updateOffscreen = false;

        public SkinnedMeshItem(string descriptor, string type, bool active, bool updateOffscreen) : base(descriptor, type, active)
        {
            this.updateOffscreen = updateOffscreen;
        }
    }

    [AddComponentMenu("PlayRecorder/RecordComponents/Skinned Mesh Record Component")]
    public class SkinnedMeshRecordComponent : TransformRecordComponent
    {

        [SerializeField]
        protected SkinnedMeshRenderer _skinnedMeshRenderer = null;

#if UNITY_EDITOR

        public override string editorHelpbox { get { return "All baseTransform and extraTransform variables will be overriden by the values captured from the SkinnedMeshRenderer."; } }

        private void OnValidate()
        {
            _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            if(_skinnedMeshRenderer != null)
            {
                _baseTransform = _skinnedMeshRenderer.rootBone;
                _extraTransforms = _skinnedMeshRenderer.bones.ToList();
            }
        }

        protected override void Reset()
        {
            base.Reset();
            if (_skinnedMeshRenderer != null)
            {
                _baseTransform = _skinnedMeshRenderer.rootBone;
                _extraTransforms = _skinnedMeshRenderer.bones.ToList();
            }
        }

#endif

        public override bool StartRecording()
        {
            if (_skinnedMeshRenderer == null)
            {
                _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            }

            if (_skinnedMeshRenderer == null)
            {
                return false;
            }

            BasicStartRecording();

            _recordItem = new SkinnedMeshItem(_descriptor, this.GetType().ToString(), gameObject.activeInHierarchy, _skinnedMeshRenderer.updateWhenOffscreen);

            _baseTransform = _skinnedMeshRenderer.rootBone;
            _extraTransforms = _skinnedMeshRenderer.bones.ToList();

            SetTransformParts();

            return true;
        }

        public override void StartPlaying()
        {
            if(_skinnedMeshRenderer == null)
            {
                _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            }

            if (_skinnedMeshRenderer == null)
            {
                return;
            }

            _baseTransform = _skinnedMeshRenderer.rootBone;
            _extraTransforms = _skinnedMeshRenderer.bones.ToList();

            _skinnedMeshRenderer.updateWhenOffscreen = ((SkinnedMeshItem)_recordItem).updateOffscreen;

            base.StartPlaying();
        }


    }
}