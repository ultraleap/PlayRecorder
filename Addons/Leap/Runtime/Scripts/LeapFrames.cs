#if PR_LEAP
using UnityEngine;

namespace PlayRecorder.Leap
{
    // The information regarding the frames themselves
    [System.Serializable]
    public class LeapIDFrame : RecordFrame
    {
        public long id;
        public long timestamp;
        public float fps;
        public bool left, right;
        public LeapIDFrame(int tick, long id, long timestamp, float fps, bool left, bool right) : base(tick)
        {
            this.id = id;
            this.timestamp = timestamp;
            this.fps = fps;
            this.left = left;
            this.right = right;
        }
    }

    // The raw joint data in bytes
    [System.Serializable]
    public class LeapByteFrame : RecordFrame
    {
        public byte[] hand;
        public LeapByteFrame(int tick, byte[] hand) : base(tick)
        {
            this.hand = (byte[])hand.Clone();
        }
    }

    // The data for individual hand properties (palm width, pinch strength, etc)
    [System.Serializable]
    public class LeapStatFrame : RecordFrame
    {
        public float stat;
        public LeapStatFrame(int tick, float stat) : base(tick)
        {
            this.stat = stat;
        }
    }

    // The data for recording the ID of the hand.
    [System.Serializable]
    public class LeapIntStatFrame : RecordFrame
    {
        public int stat;
        public LeapIntStatFrame(int tick, int stat) : base(tick)
        {
            this.stat = stat;
        }
    }

    // The data for an individual hand property (in this case only used for hand velocity)
    [System.Serializable]
    public class LeapVectorStatFrame : RecordFrame
    {
        public Vector3 stat;
        public LeapVectorStatFrame(int tick, Vector3 stat) : base(tick)
        {
            this.stat = stat;
        }
    }
}
#endif