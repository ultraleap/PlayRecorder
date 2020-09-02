using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    [System.Serializable]
    public class RecordItem
    {
        [HideInInspector]
        public string id;
        public string descriptor;
        public string type;
        public List<RecordPart> parts = new List<RecordPart>();
        public List<RecordMessage> messages = new List<RecordMessage>();
        public List<RecordStatus> status = new List<RecordStatus>();
        
        public RecordItem(bool active)
        {
            AddStatus(active, 0);
        }

        public void AddStatus(bool active, int tick)
        {
            status.Add(new RecordStatus(active, tick));
        }

    }

    [System.Serializable]
    public class RecordPart
    {
        /// <summary>
        /// Do not directly add to this unless you are planning to manually add the frame ticks.
        /// </summary>
        /// 
        public List<Frame> frames = new List<Frame>();

        public void AddFrame(Frame frame)
        {
            if(frames.Count > 0)
            {
                frames[frames.Count - 1].nextTick = frame.tick;
                frame.previousTick = frames[frames.Count - 1].tick;
            }
            frames.Add(frame);
        }

        /// <summary>
        /// Used for playback purposes only
        /// </summary>
        [System.NonSerialized, HideInInspector]
        public int currentFrameIndex = -1;
        private int increasingTicks = 1;
        public Frame currentFrame { get { if(currentFrameIndex != -1 && frames.Count > 0) return frames[currentFrameIndex]; return null; } }

        public int SetCurrentFrame(int tick)
        {
            if (frames.Count == 0)
                return -1;

            if (currentFrameIndex == -1 || tick == 0)
            {
                currentFrameIndex = 0;
            }

            if(frames[currentFrameIndex].tick <= tick)
            {
                // increase
                increasingTicks = 1;
            }
            else
            {
                // decrease or stay
                increasingTicks = -1;
            }

            while(tick > frames[currentFrameIndex].tick && frames[currentFrameIndex].nextTick != -1)
            {
                currentFrameIndex+=increasingTicks;
            }
            
            return currentFrameIndex;
        }
    }

    public class RecordStatus
    {
        public bool status = true;
        public int frame = -1;

        public RecordStatus(bool status, int frame)
        {
            this.status = status;
            this.frame = frame;
        }
    }

    [System.Serializable]
    public class RecordMessage
    {
        public string message = "";
        public List<int> frames = new List<int>();
    }

}
