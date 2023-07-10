// Several basic level frame types for faster development
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class BoolFrame : RecordFrame
    {
        public bool value;
        public BoolFrame(int tick, bool value) : base(tick)
        {
            this.value = value;
        }
    }

    [System.Serializable]
    public class IntFrame : RecordFrame
    {
        public int value;
        public IntFrame(int tick, int value) : base(tick)
        {
            this.value = value;
        }
    }

    [System.Serializable]
    public class FloatFrame : RecordFrame
    {
        public double value;
        public float convertedValue { get { return (float)value; } }

        public FloatFrame(int tick, float value) : base(tick)
        {
            this.value = value;
        }
    }

    [System.Serializable]
    public class Vector2Frame : RecordFrame
    {
        public Vector2 value;
        public Vector2Frame(int tick, Vector2 value) : base(tick)
        {
            this.value = value;
        }
    }

    [System.Serializable]
    public class Vector3Frame : RecordFrame
    {
        public Vector3 value;
        public Vector3Frame(int tick, Vector3 value) : base(tick)
        {
            this.value = value;
        }
    }

    [System.Serializable]
    public class QuaternionFrame : RecordFrame
    {
        public Quaternion value;
        public QuaternionFrame(int tick, Quaternion value) : base(tick)
        {
            this.value = value;
        }
    }
}