using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class DataCache
    {

        public string name;

        public int frameCount = 0, frameRate = 0;

        [HideInInspector]
        public List<RecordMessage> messages = new List<RecordMessage>();

        public DataCache(Data data)
        {
            if (data.objects == null || data.objects.Count == 0)
                return;

            name = data.recordingName;

            frameCount = data.frameCount;

            frameRate = data.frameRate;

            for (int i = 0; i < data.objects.Count; i++)
            {
                int mInd = -1;
                for (int j = 0; j < data.objects[i].messages.Count; j++)
                {
                    mInd = messages.FindIndex(x => x.message == data.objects[i].messages[j].message);
                    if(mInd == -1)
                    {
                        messages.Add(data.objects[i].messages[j]);
                    }
                    else
                    {
                        messages[mInd].frames.AddRange(data.objects[i].messages[j].frames);
                    }
                }
            }
            if(messages.Count > 0)
            {
                for (int i = 0; i < messages.Count; i++)
                {
                    messages[i].frames.Sort();
                }
            }
        }
    }
}
