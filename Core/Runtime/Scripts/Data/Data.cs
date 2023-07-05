using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PlayRecorder
{
    
    [System.Serializable]
    public class Data
    {

        public string recordingName;
        public DateTime dateTime;
        public int frameCount = 0, frameRate = 0;
        
        public List<RecordItem> objects = new List<RecordItem>();

    }

}
