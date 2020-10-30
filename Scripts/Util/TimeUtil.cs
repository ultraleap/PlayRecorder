using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder.Tools
{

    public static class TimeUtil
    {

        public static string ConvertToTime(double seconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            return time.ToString(@"mm\:ss\:ff");
        }

    }

}