#if PR_LEAP
using UnityEngine;
using Leap;
using Leap.Unity.Encoding;

namespace PlayRecorder.Leap
{
    public class LeapHandCache
    {
        public VectorHand vectorHand;
        public Hand hand;
        public bool handUpdated = false;
        public bool isTracked = false;
        public byte[] handArray;
        public Vector3[] jointCache;

        public bool handIDUpdated = false;
        public int handID;
        public bool confidenceUpdated = false, pinchStrengthUpdated = false, pinchDistanceUpdated = false, palmWidthUpdated = false, grabStrengthUpdated = false, palmVelocityUpdated = false;
        public float confidence, pinchStrength, pinchDistance, palmWidth, grabStrength;
        public Vector3 palmVelocity;

        public const int framePosition = 0, handStatCount = 7, leftHandOffset = 1, rightHandOffset = leftHandOffset + handStatCount + 1;

        public LeapHandCache()
        {
            hand = new Hand();
            vectorHand = new VectorHand();
            handArray = new byte[VectorHand.NUM_BYTES];
            jointCache = new Vector3[VectorHand.NUM_JOINT_POSITIONS];
        }

        public void CreateHandParts(RecordItem recordItem)
        {
            // Hand Stats
            for (int i = 0; i < handStatCount; i++)
            {
                recordItem.parts.Add(new RecordPart());
            }

            // Joints
            recordItem.parts.Add(new RecordPart());
        }

        public void UpdateHand(Hand hand, float distanceThreshold)
        {
            this.hand = hand;
            UpdateStats();
            UpdateJoints(distanceThreshold);
        }

        public void UpdateStats()
        {
            if (handID != hand.Id)
            {
                handID = hand.Id;
                handIDUpdated = true;
            }

            if (confidence != hand.Confidence)
            {
                confidence = hand.Confidence;
                confidenceUpdated = true;
            }

            if (pinchStrength != hand.PinchStrength)
            {
                pinchStrength = hand.PinchStrength;
                pinchStrengthUpdated = true;
            }

            if (pinchDistance != hand.PinchDistance)
            {
                pinchDistance = hand.PinchDistance;
                pinchDistanceUpdated = true;
            }

            if (palmWidth != hand.PalmWidth)
            {
                palmWidth = hand.PalmWidth;
                palmWidthUpdated = true;
            }

            if (grabStrength != hand.GrabStrength)
            {
                grabStrength = hand.GrabStrength;
                grabStrengthUpdated = true;
            }

            if (palmVelocity != hand.PalmVelocity)
            {
                palmVelocity = hand.PalmVelocity;
                palmVelocityUpdated = true;
            }
        }

        public void UpdateJoints()
        {
            UpdateJoints(0);
        }

        public void UpdateJoints(float distanceThreshold)
        {
            vectorHand.Encode(hand);
            for (int i = 0; i < jointCache.Length; i++)
            {
                if (Vector3.Distance(vectorHand.jointPositions[i], jointCache[i]) > distanceThreshold)
                {
                    jointCache[i] = vectorHand.jointPositions[i];
                    handUpdated = true;
                }
            }
            if (handUpdated)
            {
                vectorHand.FillBytes(handArray);
            }
        }

        public void RecordStatFrames(RecordItem recordItem, int currentTick, int offset)
        {
            if (handIDUpdated)
            {
                recordItem.parts[offset].AddFrame(new LeapIntStatFrame(currentTick, handID));
                handIDUpdated = false;
            }

            if (confidenceUpdated)
            {
                recordItem.parts[offset + 1].AddFrame(new LeapStatFrame(currentTick, confidence));
                confidenceUpdated = false;
            }

            if (pinchStrengthUpdated)
            {
                recordItem.parts[offset + 2].AddFrame(new LeapStatFrame(currentTick, pinchStrength));
                pinchStrengthUpdated = false;
            }

            if (pinchDistanceUpdated)
            {
                recordItem.parts[offset + 3].AddFrame(new LeapStatFrame(currentTick, pinchDistance));
                pinchDistanceUpdated = false;
            }

            if (palmWidthUpdated)
            {
                recordItem.parts[offset + 4].AddFrame(new LeapStatFrame(currentTick, palmWidth));
                palmWidthUpdated = false;
            }

            if (grabStrengthUpdated)
            {
                recordItem.parts[offset + 5].AddFrame(new LeapStatFrame(currentTick, grabStrength));
                grabStrengthUpdated = false;
            }

            if (palmVelocityUpdated)
            {
                recordItem.parts[offset + 6].AddFrame(new LeapVectorStatFrame(currentTick, palmVelocity));
                palmVelocityUpdated = false;
            }
        }

        public void RecordJointFrame(RecordItem recordItem, int currentTick, int jointIndex)
        {
            if (handUpdated)
            {
                recordItem.parts[jointIndex].AddFrame(new LeapByteFrame(currentTick, handArray));
                handUpdated = false;
            }
        }

        public bool PlayHandStat(RecordFrame frame, int stat)
        {
            if(stat < handStatCount)
            {
                handUpdated = true;
            }
            switch (stat)
            {
                case 0:
                    handID = ((LeapIntStatFrame)frame).stat;
                    return true;
                case 1:
                    confidence = ((LeapStatFrame)frame).stat;
                    return true;
                case 2:
                    pinchStrength = ((LeapStatFrame)frame).stat;
                    return true;
                case 3:
                    pinchDistance = ((LeapStatFrame)frame).stat;
                    return true;
                case 4:
                    palmWidth = ((LeapStatFrame)frame).stat;
                    return true;
                case 5:
                    grabStrength = ((LeapStatFrame)frame).stat;
                    return true;
                case 6:
                    palmVelocity = ((LeapVectorStatFrame)frame).stat;
                    return true;
            }
            return false;
        }

        public void PlayHand(byte[] bytes)
        {
            handArray = bytes;
            handUpdated = true;
            int ind = 0;
            vectorHand.ReadBytes(handArray, ref ind, hand);
        }

        public void SetHand()
        {
            hand.Id = handID;
            hand.Confidence = confidence;
            hand.PinchStrength = pinchStrength;
            hand.PinchDistance = pinchDistance;
            hand.PalmWidth = palmWidth;
            hand.GrabStrength = grabStrength;
            hand.PalmVelocity = palmVelocity;
        }
    }
}
#endif