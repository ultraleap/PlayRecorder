using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class AnimationItem : RecordItem
    {
        public bool playAutomatically = false, animatePhysics = false;
        public int cullingType = 0;
        
        public AnimationItem(string descriptor, string componentType, bool active, bool playAutomatically, bool animatePhysics, int cullingType) : base(descriptor, componentType, active)
        {
            this.playAutomatically = playAutomatically;
            this.animatePhysics = animatePhysics;
            this.cullingType = cullingType;
        }
    }

    [System.Serializable]
    public class AnimationPart : RecordPart
    {
        public string clipName = "";
        public AnimationPart(string clipName)
        {
            this.clipName = clipName;
        }
    }

    [AddComponentMenu("PlayRecorder/RecordComponents/Animation Record Component")]
    public class AnimationRecordComponent : RecordComponent
    {
        [SerializeField, Tooltip("Automatically assigned to the current object's Animation, changes will be ignored and reset once recording starts.")]
        protected Animation _animation = null;

        protected List<AnimationCache> _animationCache = new List<AnimationCache>();

#if UNITY_EDITOR
        public override string editorHelpbox => "Due to the nature of how the Unity Animation component works, this is not recommended for usage if you are needing to pause or scrub during playback. Please make use of the Animator version instead.";
#endif 

        public class AnimationCache
        {
            private Animation _animation;
            private bool _previousPlaying = false, _currentPlaying = false;
            public string clipName;
            public bool startedPlaying = false, finishedPlaying = true;

            public AnimationCache(Animation animation, string clipName)
            {
                this._animation = animation;
                this.clipName = clipName;
            }

            public void Update()
            {
                _currentPlaying = _animation.IsPlaying(clipName);
                if(_currentPlaying != _previousPlaying)
                {
                    if(_currentPlaying)
                    {
                        startedPlaying = true;
                    }
                    else
                    {
                        finishedPlaying = true;
                    }
                }
                _previousPlaying = _currentPlaying;
            }
        }

#if UNITY_EDITOR
        protected override void Reset()
        {
            base.Reset();
            _animation = GetComponent<Animation>();
        }

        private void OnValidate()
        {
            _animation = GetComponent<Animation>();
        }
#endif

        #region Recording

        public override bool StartRecording()
        {
            _animation = GetComponent<Animation>();
            if(_animation == null)
            {
                return false;
            }

            base.StartRecording();
            _recordItem = new AnimationItem(descriptor, this.GetType().ToString(), gameObject.activeInHierarchy, _animation.playAutomatically, _animation.animatePhysics, (int)_animation.cullingType);

            SetAnimationClips();

            return true;
        }

        protected void SetAnimationClips()
        {
            _animationCache.Clear();
            foreach (AnimationState item in _animation)
            {
                _recordItem.parts.Add(new AnimationPart(item.clip.name));
                _animationCache.Add(new AnimationCache(_animation, item.clip.name));
            }
        }

        protected override void RecordUpdateLogic()
        {
            for (int i = 0; i < _animationCache.Count; i++)
            {
                _animationCache[i].Update();
            }
        }

        protected override void RecordTickLogic()
        {
            for (int i = 0; i < _animationCache.Count; i++)
            {
                if(_animationCache[i].startedPlaying)
                {
                    _animationCache[i].startedPlaying = false;
                    _recordItem.parts[i].AddFrame(new BoolFrame(_currentTick, true));
                }
                if(_animationCache[i].finishedPlaying)
                {
                    _animationCache[i].finishedPlaying = false;
                    _recordItem.parts[i].AddFrame(new BoolFrame(_currentTick, false));
                }
            }
        }

        #endregion

        #region Playback

        protected override PlaybackIgnoreItem SetDefaultPlaybackIgnores(string type)
        {
            PlaybackIgnoreItem pbi = new PlaybackIgnoreItem(type);
            pbi.AddComponent(typeof(Animation));
            return pbi;
        }

        protected override void OnSetPlaybackData()
        {
            _animation = GetComponent<Animation>();
            if(Application.isPlaying)
            {
                AnimationItem ai = (AnimationItem)_recordItem;
                _animation.animatePhysics = ai.animatePhysics;
                _animation.playAutomatically = ai.playAutomatically;
                _animation.cullingType = (AnimationCullingType)Enum.ToObject(typeof(AnimationCullingType), ai.cullingType);
            }
        }

        protected override void PlayUpdateLogic()
        {
            for (int i = 0; i < _playUpdatedParts.Count; i++)
            {
                if(((BoolFrame)_recordItem.parts[_playUpdatedParts[i]].currentFrame).value)
                {
                    _animation.Play(((AnimationPart)_recordItem.parts[_playUpdatedParts[i]]).clipName);
                }
            }
        }

        #endregion
    }
}