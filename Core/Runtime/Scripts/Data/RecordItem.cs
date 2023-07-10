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
        
        public RecordItem(string descriptor, bool activeInHierarchy)
        {
            this.descriptor = descriptor;
            this.type = this.GetType().ToString();
            AddStatus(activeInHierarchy, 0);
        }

        public void AddStatus(bool activeInHierarchy, int tick)
        {
            int ind = status.FindIndex(x => x.frame == tick);
            if(ind == -1)
            {
                status.Add(new RecordStatus(activeInHierarchy, tick));
            }
            else
            {
                status[ind].status = activeInHierarchy;
            }
        }
    }
}
