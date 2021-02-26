using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class PlaybackIgnoreItem
    {
        public string recordComponent;
        public bool makeKinematic = true, disableCollisions = false, disableRenderer = false, disableCamera = false, disableVRCamera = true;
        public List<string> enabledComponents = new List<string>();
#if UNITY_EDITOR
        /// <summary>
        /// Editor usage only.
        /// </summary>
        public bool open = true, coreLogicOpen = false, componentsOpen = false;
#endif

        public PlaybackIgnoreItem(string prType)
        {
            recordComponent = prType;
        }
    }

}