using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PlayRecorder
{

    public static class TypeUtil
    {
        public static bool IsSameOrSubclass(this Type potentialBase, Type potentialDescendant)
        {
            return potentialDescendant.IsSubclassOf(potentialBase)
                   || potentialDescendant == potentialBase;
        }

    }
}
