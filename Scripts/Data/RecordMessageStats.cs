using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder
{
    [System.Serializable]
    public class RecordStatInt : RecordMessage
    {
        public List<int> values = new List<int>();
    }

    [System.Serializable]
    public class RecordStatDouble : RecordMessage
    {
        public List<double> values = new List<double>();
    }

    [System.Serializable]
    public class RecordStatBool : RecordMessage
    {
        public List<bool> values = new List<bool>();
    }

    [System.Serializable]
    public class RecordStatVector2 : RecordMessage
    {
        public List<Vector2> values = new List<Vector2>();
    }

    [System.Serializable]
    public class RecordStatVector3 : RecordMessage
    {
        public List<Vector3> values = new List<Vector3>();
    }

    [System.Serializable]
    public class RecordStatVector4 : RecordMessage
    {
        public List<Vector4> values = new List<Vector4>();
    }

}