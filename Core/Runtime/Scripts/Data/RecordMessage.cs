using System.Collections.Generic;

namespace PlayRecorder
{
    [System.Serializable]
    public class RecordMessage
    {
        public string message = "";
        public List<int> frames = new List<int>();
    }
}