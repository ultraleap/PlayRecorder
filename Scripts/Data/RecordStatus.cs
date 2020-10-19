using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
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
}