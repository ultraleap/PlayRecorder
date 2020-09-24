using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    [System.Serializable]
    public class RecordItem
    {
        public string descriptor;
        public string type;
        public List<RecordPart> parts = new List<RecordPart>();
        public List<RecordMessage> messages = new List<RecordMessage>();
        public List<RecordStatus> status = new List<RecordStatus>();
        
        public RecordItem(string descriptor, string type, bool active)
        {
            this.descriptor = descriptor;
            this.type = type;
            AddStatus(active, 0);
        }

        public void AddStatus(bool active, int tick)
        {
            int ind = status.FindIndex(x => x.frame == tick);
            if(ind == -1)
            {
                status.Add(new RecordStatus(active, tick));
            }
            else
            {
                status[ind].status = active;
            }
        }

    }

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
        public RecordFrame currentFrame { get { if(currentFrameIndex != -1 && frames.Count > 0) return frames[currentFrameIndex]; return null; } }

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
                while (tick > frames[currentFrameIndex].tick && currentFrameIndex < frames.Count - 1 && tick >= frames[currentFrameIndex + 1].tick)
                {
                    currentFrameIndex++;
                }

            }
            else
            {
                // decrease or stay
                while(tick < frames[currentFrameIndex].tick && currentFrameIndex > 0 && tick < frames[currentFrameIndex - 1].tick)
                {
                    currentFrameIndex--;
                }
            }

            return currentFrameIndex;
        }
    }

    [System.Serializable]
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
