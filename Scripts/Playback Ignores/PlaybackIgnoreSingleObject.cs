using UnityEngine;
using System.Collections.Generic;

namespace PlayRecorder
{
    [CreateAssetMenu(fileName = "SinglePlaybackIgnore", menuName = "PlayRecorder/Single Playback Ignore Asset")]
    public class PlaybackIgnoreSingleObject : ScriptableObject
    {
        public PlaybackIgnoreItem item;
    }
}