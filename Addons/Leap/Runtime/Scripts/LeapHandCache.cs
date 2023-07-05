#if PR_LEAP
using UnityEngine;
using Leap;
using Leap.Unity.Encoding;

namespace PlayRecorder.Leap
{
    public class LeapHandCache
    {
        private VectorHand vectorHand;
        internal Hand hand;
        private bool handUpdated = false;
        internal bool isTracked = false;
        internal byte[] handArray;
        private Vector3[] jointCache;

        // Hand
        internal bool handIDUpdated = false;
        internal int handID;
        internal bool confidenceUpdated = false, pinchStrengthUpdated = false, pinchDistanceUpdated = false, palmWidthUpdated = false, grabStrengthUpdated = false, palmVelocityUpdated = false;
        internal float confidence, pinchStrength, pinchDistance, palmWidth, grabStrength;
        internal Vector3 palmVelocity;

        // Arm
        internal bool armWidthUpdated = false, wristPositionUpdated = false, elbowPositionUpdated = false, armRotationUpdated = false;
        internal float armWidth;
        internal Vector3 wristPosition, elbowPosition;
        internal Quaternion armRotation;

        internal const int framePosition = 0, handStatCount = 11, leftHandOffset = 1, rightHandOffset = leftHandOffset + handStatCount + 1;

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

            if (armWidth != hand.Arm.Width)
            {
                armWidth = hand.Arm.Width;
                armWidthUpdated = true;
            }

            if (wristPosition != hand.WristPosition)
            {
                wristPosition = hand.WristPosition;
                wristPositionUpdated = true;
            }

            if (elbowPosition != hand.Arm.ElbowPosition)
            {
                elbowPosition = hand.Arm.ElbowPosition;
                elbowPositionUpdated = true;
            }

            if (armRotation != hand.Arm.Rotation)
            {
                armRotation = hand.Arm.Rotation;
                armRotationUpdated = true;
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

            if (armWidthUpdated)
            {
                recordItem.parts[offset + 7].AddFrame(new LeapStatFrame(currentTick, armWidth));
                armWidthUpdated = false;
            }

            if (wristPositionUpdated)
            {
                recordItem.parts[offset + 8].AddFrame(new LeapVectorStatFrame (currentTick, wristPosition));
                wristPositionUpdated = false;
            }

            if(elbowPositionUpdated)
            {
                recordItem.parts[offset + 9].AddFrame(new LeapVectorStatFrame(currentTick, elbowPosition));
                elbowPositionUpdated = false;
            }

            if(armRotationUpdated)
            {
                recordItem.parts[offset + 10].AddFrame(new LeapRotationStatFrame(currentTick, armRotation));
                armRotationUpdated = false;
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
            if (stat < handStatCount)
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
                case 7:
                    armWidth = ((LeapStatFrame)frame).stat;
                    return true;
                case 8:
                    wristPosition = ((LeapVectorStatFrame)frame).stat;
                    return true;
                case 9:
                    elbowPosition = ((LeapVectorStatFrame)frame).stat;
                    return true;
                case 10:
                    armRotation = ((LeapRotationStatFrame)frame).stat;
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
            hand.Arm.PrevJoint = elbowPosition;
            hand.WristPosition = wristPosition;
            hand.Arm.NextJoint = wristPosition;
            hand.Arm.Width = armWidth;
            hand.Arm.Rotation = armRotation;
            hand.Arm.Direction = (wristPosition - elbowPosition).normalized;
            hand.Arm.Length = Vector3.Distance(elbowPosition, wristPosition);
            hand.Arm.Center = (elbowPosition + wristPosition) / 2f;
        }
    }
}
#endif