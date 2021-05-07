using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [CreateAssetMenu(fileName = "PlaybackIgnores", menuName = "PlayRecorder/Playback Ignore Asset")]
    public class PlaybackIgnoreComponentsObject : ScriptableObject
    {
        public List<PlaybackIgnoreItem> ignoreItems = new List<PlaybackIgnoreItem>();
    }
}