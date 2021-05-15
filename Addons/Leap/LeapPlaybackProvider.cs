// To enable this addon go to Edit -> Project Settings -> Player -> Other Settings -> Scripting Define Symbols and add
// PR_LEAP;
#if PR_LEAP
using UnityEngine;
using System;
using Leap;
using Leap.Unity;

namespace PlayRecorder.Leap
{
    [AddComponentMenu("PlayRecorder/Ultraleap/Leap Playback Provider")]
    public class LeapPlaybackProvider : LeapXRServiceProvider
    {
        protected bool _isVR { get { return editTimePose == TestHandFactory.TestHandPose.HeadMountedA || editTimePose == TestHandFactory.TestHandPose.HeadMountedB; } }

        protected bool _playbackStarted = false;

        protected LeapServiceProviderRecordComponent _recordComponent = null;

        protected Frame _localUpdateFrame = null, _localFixedFrame = null;

        public event Action<Frame> OnLocalUpdateFrame;
        public event Action<Frame> OnLocalPostUpdateFrame;
        public event Action<Frame> OnLocalFixedFrame;

        protected Transform _worldOrigin;

        protected Frame _playbackUntransformedUpdateFrame = null, _playbackUntransformedFixedFrame = null;
        protected Frame _playbackUpdateFrame = null, _playbackFixedFrame = null;
        protected Frame _currentPlaybackFrame = null;
        public Frame CurrentPlaybackFrame
        {
            set { _currentPlaybackFrame = value; }
        }

        public Frame CurrentUntransformedFrame
        {
            get
            {
                if (_frameOptimization == FrameOptimizationMode.ReusePhysicsForUpdate)
                {
                    return _untransformedFixedFrame;
                }
                else
                {
                    return _untransformedUpdateFrame;
                }
            }
        }

        public Frame CurrentUntransformedFixedFrame
        {
            get
            {
                if (_frameOptimization == FrameOptimizationMode.ReuseUpdateForPhysics)
                {
                    return _untransformedUpdateFrame;
                }
                else
                {
                    return _untransformedFixedFrame;
                }
            }
        }

        protected override void OnEnable()
        {
            if (_playbackStarted)
            {

            }
            else if (_isVR)
            {
                base.OnEnable();
            }
        }

        protected override void OnDisable()
        {
            if (_playbackStarted)
            {

            }
            else if (_isVR)
            {
                base.OnDisable();
            }
        }

        protected override void Start()
        {
            _recordComponent = GetComponent<LeapServiceProviderRecordComponent>();
            GameObject go = GameObject.Find("Leap World Origin Cache");
            if(go == null)
            {
                _worldOrigin = new GameObject("Leap World Origin Cache").transform;
                _worldOrigin.gameObject.isStatic = true;
            }
            else
            {
                _worldOrigin = go.transform;
            }
            if (_recordComponent == null)
            {
                Debug.LogWarning("No LeapServiceProviderRecordComponent on LeapPlaybackProvider.");
            }
            else
            {
                _recordComponent.OnStartPlayback -= OnStartPlayback;
                _recordComponent.OnStartPlayback += OnStartPlayback;
            }
            if (_isVR)
            {
                base.Start();
            }
            else
            {
                createController();
                _transformedUpdateFrame = new Frame();
                _transformedFixedFrame = new Frame();
                _untransformedUpdateFrame = new Frame();
                _untransformedFixedFrame = new Frame();
            }
            _localFixedFrame = new Frame();
            _localUpdateFrame = new Frame();
        }

        protected override void Reset()
        {
            base.Reset();
            editTimePose = TestHandFactory.TestHandPose.DesktopModeA;
        }

        protected void OnStartPlayback()
        {
            _playbackStarted = true;
            _playbackUntransformedUpdateFrame = new Frame();
            _playbackUntransformedFixedFrame = new Frame();
            _playbackUpdateFrame = new Frame();
            _playbackFixedFrame = new Frame();
        }

        protected override void Update()
        {
            if (_playbackStarted)
            {
                PlaybackUpdate();
            }
            else
            {
                ControllerUpdate();
            }
        }

        protected void PlaybackUpdate()
        {
            if (_currentPlaybackFrame == null || _playbackUntransformedUpdateFrame == _currentPlaybackFrame)
                return;

            try
            {
                _playbackUntransformedUpdateFrame.CopyFrom(_currentPlaybackFrame);
            }
            catch
            {
                // Very minor issue with copying frames at times.
            }
            if (_playbackUntransformedUpdateFrame != null)
            {
                ApplyPlaybackTransform(_playbackUntransformedUpdateFrame, _playbackUpdateFrame);
                DispatchUpdateFrameEvent(_playbackUpdateFrame);
                //DispatchLocalUpdateFrameEvent(_playbackUntransformedUpdateFrame);
            }
        }

        protected void ControllerUpdate()
        {
            base.Update();
            if (_untransformedUpdateFrame != null)
            {
                TransformToOrigin(_untransformedUpdateFrame, _localUpdateFrame);
                DispatchLocalUpdateFrameEvent(_localUpdateFrame);
            }
        }

        protected override void FixedUpdate()
        {
            if (_playbackStarted)
            {
                PlaybackFixedUpdate();
            }
            else
            {
                ControllerFixedUpdate();
            }
        }

        protected void ControllerFixedUpdate()
        {
            base.FixedUpdate();
            if (_untransformedFixedFrame != null)
            {
                TransformToOrigin(_untransformedFixedFrame, _localFixedFrame);
                DispatchLocalFixedFrameEvent(_localFixedFrame);
            }
        }

        protected void PlaybackFixedUpdate()
        {
            if (_currentPlaybackFrame == null || _playbackFixedFrame == _currentPlaybackFrame)
                return;

            try
            {
                _playbackUntransformedFixedFrame.CopyFrom(_currentPlaybackFrame);
            }
            catch
            {
                // Very minor issue with copying frames at times.
            }
            if (_playbackUntransformedFixedFrame != null)
            {
                ApplyPlaybackTransform(_playbackUntransformedFixedFrame, _playbackFixedFrame);
                DispatchUpdateFrameEvent(_playbackFixedFrame);
                //DispatchLocalUpdateFrameEvent(_localFixedFrame);
            }
        }

        protected void DispatchLocalUpdateFrameEvent(Frame frame)
        {
            if (OnLocalUpdateFrame != null)
            {
                OnLocalUpdateFrame(frame);
            }
            if (OnLocalPostUpdateFrame != null)
            {
                OnLocalPostUpdateFrame(frame);
            }
        }

        protected void DispatchLocalFixedFrameEvent(Frame frame)
        {
            if (OnLocalFixedFrame != null)
            {
                OnLocalFixedFrame(frame);
            }
        }

        protected override long CalculateInterpolationTime(bool endOfFrame = false)
        {
            if (_isVR)
            {
                return base.CalculateInterpolationTime(endOfFrame);
            }
            else
            {
#if UNITY_ANDROID
      return _leapController.Now() - 16000;
#else
                if (_leapController != null)
                {
                    return _leapController.Now()
                           - (long)_smoothedTrackingLatency.value
                           + ((updateHandInPrecull && !endOfFrame) ?
                                (long)(Time.smoothDeltaTime * S_TO_NS / Time.timeScale)
                              : 0);
                }
                else
                {
                    return 0;
                }
#endif
            }
        }

        protected override void initializeFlags()
        {
            if (_leapController == null)
            {
                return;
            }

            if (editTimePose == TestHandFactory.TestHandPose.DesktopModeA)
            {
                _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_DEFAULT);
                _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
                _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
            }
            else if (editTimePose == TestHandFactory.TestHandPose.ScreenTop)
            {
                _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_DEFAULT);
                _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
                _leapController.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
            }
            else if (_isVR)
            {
                _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_DEFAULT);
                _leapController.ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
                _leapController.SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
            }
        }

        protected void TransformToOrigin(Frame source, Frame dest)
        {
            dest.CopyFrom(source).Transform(_worldOrigin.GetLeapMatrix());
        }

        protected void ApplyPlaybackTransform(Frame source, Frame dest)
        {
            dest.CopyFrom(source).ApplyUnityTransform(transform);
        }

        protected override void transformFrame(Frame source, Frame dest)
        {
            if (_isVR)
            {
                LeapTransform leapTransform = GetWarpedMatrix(source.Timestamp);
                dest.CopyFrom(source).Transform(leapTransform);
            }
            else
            {
                dest.CopyFrom(source).Transform(transform.GetLeapMatrix());
            }
        }
    }
}
#endif