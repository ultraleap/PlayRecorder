using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    [System.Serializable]
    public class AnimatorItem : TransformItem
    {
        public bool applyRootMotion = false, fireEvents = false, keepAnimatorControllerState = false;
        public int updateMode = 1, cullingMode = 1;

        public AnimatorItem(string descriptor, string type, bool active, TransformSpace space, bool applyRootMotion, int updateMode, int cullingMode, bool fireEvents, bool keepAnimatorControllerState) : base(descriptor, type, active, space)
        {
            this.applyRootMotion = applyRootMotion;
            this.updateMode = updateMode;
            this.cullingMode = cullingMode;
            this.fireEvents = fireEvents;
            this.keepAnimatorControllerState = keepAnimatorControllerState;
        }
    }

    [System.Serializable]
    public class AnimatorPart : RecordPart
    {
        public string animatorParamName = "";
        public int animatorParamType = 1;
        public AnimatorPart(string name, int type)
        {
            this.animatorParamName = name;
            this.animatorParamType = type;
        }
    }

    public class AnimatorCache
    {
        private Animator _animator;

        public float speed, feetPivotActive;
        public bool speedChanged = true, feetPivotActiveChanged = true;
        public bool stabilizeFeet;
        public bool stabilizeFeetChanged = true;

        public AnimatorCache(Animator animator)
        {
            this._animator = animator;
            this.speed = animator.speed;
            this.feetPivotActive = animator.feetPivotActive;
            this.stabilizeFeet = animator.stabilizeFeet;
        }

        public void Update()
        {
            if(this.speed != _animator.speed)
            {
                this.speed = _animator.speed;
                this.speedChanged = true;
            }
            if(this.feetPivotActive != _animator.feetPivotActive)
            {
                this.feetPivotActive = _animator.feetPivotActive;
                this.feetPivotActiveChanged = true;
            }
            if(this.stabilizeFeet != _animator.stabilizeFeet)
            {
                this.stabilizeFeet = _animator.stabilizeFeet;
                this.stabilizeFeetChanged = true;
            }
        }

        public void ForceUpdate()
        {
            this.speedChanged = true;
            this.feetPivotActiveChanged = true;
            this.stabilizeFeetChanged = true;
        }
    }

    public class AnimatorParameterCache
    {
        private Animator _animator;
        public string name;
        public AnimatorControllerParameterType type;
        public float fValue;
        public int iValue;
        public bool bValue;

        public bool hasChanged = false;

        public AnimatorParameterCache(Animator animator, AnimatorControllerParameter parameter)
        {
            this._animator = animator;
            this.name = parameter.name;
            this.type = parameter.type;
        }

        public void Update()
        {
            switch (this.type)
            {
                case AnimatorControllerParameterType.Float:
                    if(this.fValue != _animator.GetFloat(name))
                    {
                        this.fValue = _animator.GetFloat(name);
                        hasChanged = true;
                    }
                    break;
                case AnimatorControllerParameterType.Int:
                    if(this.iValue != _animator.GetInteger(name))
                    {
                        this.iValue = _animator.GetInteger(name);
                        hasChanged = true;
                    }
                    break;
                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    if(this.bValue != _animator.GetBool(name))
                    {
                        this.bValue = _animator.GetBool(name);
                        hasChanged = true;
                    }
                    break;
            }
        }
    }

    [AddComponentMenu("PlayRecorder/RecordComponents/Animator Record Component")]
    public class AnimatorRecordComponent : RecordComponent
    {
        [SerializeField, Tooltip("Automatically assigned to the current object's Animator, changes will be ignored and reset once recording starts.")]
        protected Animator _animator = null;

        [SerializeField]
        protected TransformSpace _rootObjectTransformSpace = TransformSpace.Local;
        protected TransformSpace _playbackSpace = TransformSpace.Local;
        protected TransformCache _transformCache = null;
        protected AnimatorCache _animatorCache = null;

        protected List<AnimatorParameterCache> _parameterCache = new List<AnimatorParameterCache>();

        protected const int ANIMATOR_PARTS = 4;
        protected int _playbackFrameRate = 60;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _animator = GetComponent<Animator>();
        }

        protected override void Reset()
        {
            base.Reset();
            _animator = GetComponent<Animator>();
        }
#endif

        #region Recording

        public override bool StartRecording()
        {
            base.StartRecording();
            _recordItem = new AnimatorItem(descriptor, this.GetType().ToString(), gameObject.activeInHierarchy, _rootObjectTransformSpace, _animator.applyRootMotion, (int)_animator.updateMode, (int)_animator.cullingMode, _animator.fireEvents, _animator.keepAnimatorControllerStateOnDisable);

            _animator = GetComponent<Animator>();

            SetAnimatorParameters();

            return true;
        }

        protected void SetAnimatorParameters()
        {
            _transformCache = new TransformCache(transform, _rootObjectTransformSpace);
            _animatorCache = new AnimatorCache(_animator);
            for (int i = 0; i < ANIMATOR_PARTS; i++)
            {
                _recordItem.parts.Add(new RecordPart());
            }
            RecordAnimatorProperties();
            // speed f
            // feetpivot f
            // stabilizeFeet b

            _parameterCache.Clear();
            for (int i = 0; i < _animator.parameterCount; i++)
            {
                AnimatorControllerParameter acp = _animator.GetParameter(i);
                AnimatorPart ap = new AnimatorPart(acp.name,(int)acp.type);
                AnimatorParameterCache apc = new AnimatorParameterCache(_animator, acp);
                _parameterCache.Add(apc);
                RecordAnimatorPart(ap,apc);
                _recordItem.parts.Add(ap);
            }
        }

        protected void RecordAnimatorProperties()
        {
            for (int i = 0; i < ANIMATOR_PARTS; i++)
            {
                RecordFrame frame = null;
                switch (i)
                {
                    case 0:
                        if(_transformCache.hasChanged)
                        {
                            frame = new TransformFrame(_currentTick, _transformCache);
                            _transformCache.hasChanged = false;
                        }
                        break;
                    case 1:
                        if(_animatorCache.speedChanged)
                        {
                            frame = new FloatFrame(_currentTick, _animatorCache.speed);
                            _animatorCache.speedChanged = false;
                        }
                        break;
                    case 2:
                        if (_animatorCache.feetPivotActiveChanged)
                        {
                            frame = new FloatFrame(_currentTick, _animatorCache.feetPivotActive);
                            _animatorCache.feetPivotActiveChanged = false;
                        }
                        break;
                    case 3:
                        if (_animatorCache.stabilizeFeetChanged)
                        {
                            frame = new BoolFrame(_currentTick, _animatorCache.stabilizeFeet);
                            _animatorCache.stabilizeFeetChanged = false;
                        }
                        break;
                }
                if(frame != null)
                {
                    _recordItem.parts[i].AddFrame(frame);
                }
            }
        }

        protected void RecordAnimatorPart(AnimatorPart part, AnimatorParameterCache cache)
        {
            RecordFrame frame = null;
            switch (cache.type)
            {
                case AnimatorControllerParameterType.Float:
                    frame = new FloatFrame(_currentTick, cache.fValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    frame = new IntFrame(_currentTick, cache.iValue);
                    break;
                case AnimatorControllerParameterType.Bool:
                case AnimatorControllerParameterType.Trigger:
                    frame = new BoolFrame(_currentTick, cache.bValue);
                    break;
            }
            if(frame != null)
            {
                part.AddFrame(frame);
            }
        }

        protected override void OnRecordingEnable()
        {
            _transformCache.hasChanged = true;
            _animatorCache.ForceUpdate();
        }

        protected override void RecordUpdateLogic()
        {
            _transformCache.Update();
            _animatorCache.Update();
            for (int i = 0; i < _parameterCache.Count; i++)
            {
                _parameterCache[i].Update();
            }
        }

        protected override void RecordTickLogic()
        {
            RecordAnimatorProperties();
            for (int i = ANIMATOR_PARTS; i < _recordItem.parts.Count; i++)
            {
                if (_parameterCache[i - ANIMATOR_PARTS].hasChanged)
                {
                    _parameterCache[i - ANIMATOR_PARTS].hasChanged = false;
                    RecordAnimatorPart((AnimatorPart)_recordItem.parts[i],_parameterCache[i-ANIMATOR_PARTS]);
                }
            }
        }

        #endregion

        #region Playback

        protected override void OnSetPlaybackData()
        {
            _animator = GetComponent<Animator>();
            PlaybackManager pbm = FindObjectOfType<PlaybackManager>();
            if(pbm != null)
            {
                _playbackFrameRate = pbm.currentFrameRate;
                pbm.OnUpdateTick -= OnUpdateTick;
                pbm.OnUpdateTick += OnUpdateTick;
            }

            AnimatorItem ai = (AnimatorItem)_recordItem;
            _playbackSpace = ai.space;
            _animator.applyRootMotion = ai.applyRootMotion;
            _animator.fireEvents = ai.fireEvents;
            _animator.keepAnimatorControllerStateOnDisable = ai.keepAnimatorControllerState;
            _animator.updateMode = (AnimatorUpdateMode)Enum.ToObject(typeof(AnimatorUpdateMode), ai.updateMode);
            _animator.cullingMode = (AnimatorCullingMode)Enum.ToObject(typeof(AnimatorCullingMode), ai.cullingMode);
        }

        public override void StartPlaying()
        {
            base.StartPlaying();
        }

        protected override PlaybackIgnoreItem SetDefaultPlaybackIgnores(string type)
        {
            PlaybackIgnoreItem pbi = new PlaybackIgnoreItem(type);
            
            //pbi.AddComponent(typeof(Animator));
            return pbi;
        }

        protected override void PlayUpdateLogic()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                if(_playUpdatedParts[i] < ANIMATOR_PARTS)
                {
                    ApplyAnimatorProperties(_playUpdatedParts[i], _recordItem.parts[_playUpdatedParts[i]].currentFrame);
                }
                else
                {
                    ApplyAnimatorPart((AnimatorPart)_recordItem.parts[_playUpdatedParts[i]]);
                }
            }
        }

        private void OnUpdateTick()
        {
            _animator.Update(1f / _playbackFrameRate);
        }

        protected override void PlayAfterTickLogic()
        {
            base.PlayAfterTickLogic();
        }

        protected void ApplyAnimatorProperties(int ind, RecordFrame frame)
        {
            switch (ind)
            {
                case 0:
                    switch (_playbackSpace)
                    {
                        case TransformSpace.Local:
                            transform.localPosition = ((TransformFrame)frame).localPosition;
                            transform.localRotation = ((TransformFrame)frame).localRotation;
                            transform.localScale = ((TransformFrame)frame).localScale;
                            break;
                        case TransformSpace.World:
                            transform.position = ((TransformFrame)frame).localPosition;
                            transform.rotation = ((TransformFrame)frame).localRotation;
                            transform.localScale = ((TransformFrame)frame).localScale;
                            break;
                    }
                    break;
                case 1:
                    _animator.speed = ((FloatFrame)frame).convertedValue;
                    break;
                case 2:
                    _animator.feetPivotActive = ((FloatFrame)frame).convertedValue;
                    break;
                case 3:
                    _animator.stabilizeFeet = ((BoolFrame)frame).value;
                    break;
            }
        }

        protected void ApplyAnimatorPart(AnimatorPart part)
        {
            AnimatorControllerParameterType convertedType = (AnimatorControllerParameterType)Enum.ToObject(typeof(AnimatorControllerParameterType), part.animatorParamType);
            switch (convertedType)
            {
                case AnimatorControllerParameterType.Float:
                    _animator.SetFloat(part.animatorParamName, ((FloatFrame)part.currentFrame).convertedValue);
                    break;
                case AnimatorControllerParameterType.Int:
                    _animator.SetInteger(part.animatorParamName, ((IntFrame)part.currentFrame).value);
                    break;
                case AnimatorControllerParameterType.Bool:
                    _animator.SetBool(part.animatorParamName, ((BoolFrame)part.currentFrame).value);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    _animator.SetTrigger(part.animatorParamName);
                    break;
            }
        }

        #endregion
    }
}