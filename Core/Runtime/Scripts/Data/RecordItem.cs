using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class RecordItem
    {
        public string descriptor;
        /// <summary>
        /// The type of RecordComponent being used
        /// </summary>
        public string componentType;
        /// <summary>
        /// The type of RecordItem being used
        /// </summary>
        public string type;
        public List<RecordPart> parts = new List<RecordPart>();
        public List<RecordMessage> messages = new List<RecordMessage>();
        public List<RecordStatus> status = new List<RecordStatus>();
        
        public RecordItem(string descriptor, string componentType, bool activeInHierarchy)
        {
            this.descriptor = descriptor;
            this.componentType = componentType;
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
