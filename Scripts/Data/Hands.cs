using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlayRecorder.Hands
{

    [System.Serializable]
    public class PalmFrame : RecordFrame
    {
        // Local Position
        public Vector3 position;
        // Local Rotation
        public Quaternion rotation;

        public PalmFrame(int tick, Vector3 position, Quaternion rotation) : base(tick)
        {
            this.position = position;
            this.rotation = rotation;
        }
    }

    [System.Serializable]
    public class FingerFrame : RecordFrame
    {
        // Metacarpal -> Proximal -> Intermediate -> Distal
        // Thumb only uses last 3

        // Local Positions
        public Vector3 metaPos, proxPos, interPos, distPos;
        // Local Rotations
        public Quaternion metaRot, proxRot, interRot, distRot;

        public FingerFrame(int tick, Vector3 metaPos, Vector3 proxPos, Vector3 interPos, Vector3 distPos,
            Quaternion metaRot, Quaternion proxRot, Quaternion interRot, Quaternion distRot) : base(tick)
        {
            this.metaPos = metaPos;
            this.metaRot = metaRot;

            this.proxPos = proxPos;
            this.proxRot = proxRot;

            this.interPos = interPos;
            this.interRot = interRot;

            this.distPos = distPos;
            this.distRot = distRot;
        }
    }

    [System.Serializable]
    public class HandPart : RecordPart
    {
        [System.Serializable]
        public enum HandPartID
        {
            Palm = 0,
            Thumb = 1,
            Index = 2,
            Middle = 3,
            Ring = 4,
            Pinky = 5
        }

        public HandPartID handPart;

        public HandPart(HandPartID handPart)
        {
            this.handPart = handPart;
        }
    }

    [System.Serializable]
    public class HandItem : RecordItem
    {
        [System.Serializable]
        public enum HandID
        {
            Left = 0,
            Right = 1
        }

        public HandID handID;

        public HandItem(string descriptor, string type, bool active, HandID hand) : base(descriptor, type, active)
        {
            handID = hand;
        }
    }

    public class PalmCache
    {
        public Transform transform;

        public bool updated;

        public Vector3 localPosition;
        public Quaternion localRotation;

        public PalmCache(Transform transform)
        {
            this.transform = transform;
            updated = true;
            localPosition = transform.localPosition;
            localRotation = transform.localRotation;
        }

        public virtual void Update(float rotationThreshold)
        {
            localPosition = transform.localPosition;
            if ((transform.localRotation.eulerAngles - localRotation.eulerAngles).sqrMagnitude > rotationThreshold)
            {
                localRotation = transform.localRotation;
                updated = true;
            }
        }

        public virtual void PlayUpdate(PalmFrame frame)
        {
            transform.localPosition = frame.position;
            transform.localRotation = frame.rotation;
        }
    }

    public class FingerCache
    {
        public Transform meta, prox, inter, dist;

        public bool updated;

        public Vector3 metaPos, proxPos, interPos, distPos;
        public Quaternion metaRot, proxRot, interRot, distRot;

        public FingerCache(Transform meta, Transform prox, Transform inter, Transform dist)
        {
            this.meta = meta;
            this.prox = prox;
            this.inter = inter;
            this.dist = dist;
            updated = true;
        }

        public virtual void Update(float rotationThreshold)
        {
            if (meta != null)
            {
                metaPos = meta.localPosition;
                if ((meta.localRotation.eulerAngles - metaRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    metaRot = meta.localRotation;
                    updated = true;
                }
            }
            if (prox != null)
            {
                proxPos = prox.localPosition;
                if ((prox.localRotation.eulerAngles - proxRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    proxRot = prox.localRotation;
                    updated = true;
                }
            }
            if (inter != null)
            {
                interPos = inter.localPosition;
                if ((inter.localRotation.eulerAngles - interRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    interRot = inter.localRotation;
                    updated = true;
                }
            }
            if (dist != null)
            {
                distPos = dist.localPosition;
                if ((dist.localRotation.eulerAngles - distRot.eulerAngles).sqrMagnitude > rotationThreshold)
                {
                    distRot = dist.localRotation;
                    updated = true;
                }
            }
        }

        public virtual void PlayUpdate(FingerFrame frame)
        {
            if (meta != null)
            {
                meta.localPosition = frame.metaPos;
                meta.localRotation = frame.metaRot;
            }
            if (prox != null)
            {
                prox.localPosition = frame.proxPos;
                prox.localRotation = frame.proxRot;
            }
            if (inter != null)
            {
                inter.localPosition = frame.interPos;
                inter.localRotation = frame.interRot;
            }
            if (dist != null)
            {
                dist.localPosition = frame.distPos;
                dist.localRotation = frame.distRot;
            }
        }
    }

}