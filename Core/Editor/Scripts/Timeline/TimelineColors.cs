using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder.Timeline {

    [CreateAssetMenu(fileName = "TimelineColors", menuName = "PlayRecorder/Timeline Color Asset")]
    public class TimelineColors : ScriptableObject
    {
        public bool updateTimeline = true;

        public bool overrideSelected = false, overridePassive = false, overrideBackground = false, overrideTimeIndicator = false, overrideTimeIndicatorWidth = false, overrideMessageIndicatorWidth = false;
        public Color selectedColour, passiveColour, backgroundColour,timeIndicatorColour = Color.green,timeIndicatorPausedColour = Color.red;
        public int timeIndicatorWidth = 2,messageIndicatorWidth = 2;
        public List<TimelineColor> colours = new List<TimelineColor>();
    }

}