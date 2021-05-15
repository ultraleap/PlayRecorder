#if PR_LEAP
using Leap;
using Leap.Unity;
using UnityEngine;

namespace PlayRecorder.Leap
{ 
    public static class LeapTransformExtensions
    {

        public static Frame ApplyUnityTransform(this Frame frame, Transform transform)
        {
            for (int i = 0; i < frame.Hands.Count; i++)
            {
                frame.Hands[i].SetTransform(transform.position + (transform.rotation * Vector3.Scale(frame.Hands[i].PalmPosition.ToVector3(), transform.lossyScale)), transform.rotation * frame.Hands[i].GetPalmPose().rotation, transform.lossyScale);
            }
            return frame;
        }

        public static void SetTransform(this Hand hand, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            hand.Transform(Vector3.zero, Quaternion.Slerp((rotation * Quaternion.Inverse(hand.Rotation.ToQuaternion())), Quaternion.identity, 0f), scale);
            hand.Transform(position - hand.PalmPosition.ToVector3(), Quaternion.identity);
        }

        public static void Transform(this Hand hand, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            hand.Transform(new LeapTransform(position.ToVector(), rotation.ToLeapQuaternion(), scale.ToVector()));
        }
    }

}
#endif