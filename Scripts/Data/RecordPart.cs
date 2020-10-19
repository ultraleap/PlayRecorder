using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class RecordPart
    {
        /// <summary>
        /// Do not directly add to this unless you are planning to manually add the frame ticks.
        /// </summary>
        /// 
        public List<RecordFrame> frames = new List<RecordFrame>();

        public void AddFrame(RecordFrame frame)
        {
            frames.Add(frame);
        }

        /// <summary>
        /// Used for playback purposes only
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public int currentFrameIndex = -1;
        public RecordFrame currentFrame { get { if (currentFrameIndex != -1 && frames.Count > 0) return frames[Mathf.Clamp(currentFrameIndex,0,frames.Count-1)]; return null; } }

        public int SetCurrentFrame(int tick)
        {
            if (frames.Count == 0)
                return -1;

            if (currentFrameIndex == -1 || tick == 0)
            {
                currentFrameIndex = 0;
            }

            if(currentFrameIndex >= frames.Count - 1)
            {
                currentFrameIndex = frames.Count - 1;
            }
            else
            {
                if (frames[currentFrameIndex].tick <= tick)
                {
                    // increase
                    while (tick > frames[currentFrameIndex].tick && currentFrameIndex < frames.Count - 1 && tick >= frames[currentFrameIndex + 1].tick)
                    {
                        currentFrameIndex++;
                    }
                }
                else
                {
                    // decrease or stay
                    while (tick < frames[currentFrameIndex].tick && currentFrameIndex > 0 && tick < frames[currentFrameIndex - 1].tick)
                    {
                        currentFrameIndex--;
                    }
                }
            }

            return currentFrameIndex;
        }
    }
}