using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class RecordFrame
    {
        public int tick;

        public RecordFrame(int tick)
        {
            this.tick = tick;
        }
    }
}
