#if PR_LEAP
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap;

namespace PlayRecorder.Leap
{
    public class LeapPlaybackProvider : LeapProvider
    {
        private Frame _currentFrame = null;
        private bool _playing = false;
        
        public override Frame CurrentFrame
        {
            get { return _currentFrame; }
        }

        public override Frame CurrentFixedFrame
        {
            get { return _currentFrame; }
        }

        public void SetFrame(Frame frame)
        {
            _currentFrame = frame;
        }

        public void StartPlayback()
        {
            _playing = true;
        }

        protected virtual void Update()
        {
            if(_playing && _currentFrame != null)
            {
                DispatchUpdateFrameEvent(_currentFrame);
            }
        }

        protected virtual void FixedUpdate()
        {
            if(_playing && _currentFrame != null)
            {
                DispatchFixedFrameEvent(_currentFrame);
            }
        }

    }
}
#endif