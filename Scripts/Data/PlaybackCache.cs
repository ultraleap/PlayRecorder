using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class PlaybackCache
    {

        string name;

        List<RecordMessage> messages = new List<RecordMessage>();

        public PlaybackCache(Data data)
        {
            if (data.objects == null || data.objects.Count == 0)
                return;

            name = data.recordingName;

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

            for (int i = 0; i < messages.Count; i++)
            {
                messages[i].frames.Sort();
            }
        }
    }
}
