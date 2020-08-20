using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    // information about recording as a whole
    // user id, task being performed,
    // length of recording, frame count, frame rate

    // dictionary of all objects
    // dictionary of items
    // list of frames - each frame as previous and next

    // playback
    // for each object
    // each item
    // loop through the frames until the frame uint is less than or equal to current frame


    // store previous index at which item was moving

    [System.Serializable]
    public class Frame
    {
        public int tick, previousTick = -1, nextTick = -1;

        public Frame(int tick)
        {
            this.tick = tick;
        }
    }

}
