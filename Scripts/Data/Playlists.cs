using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{

    [System.Serializable]
    public class PlaylistItem
    {
        public string name;
        public int guid;

        public PlaylistItem(string name, int guid)
        {
            this.name = name;
            this.guid = guid;
        }
    }

}
