using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder.Timeline {

    [CreateAssetMenu(fileName = "TimelineColors", menuName = "PlayRecorder/Timeline Color Asset")]
    public class TimelineColors : ScriptableObject
    {
        public bool updateTimeline = true;
        public List<TimelineColor> colours;
    }

}