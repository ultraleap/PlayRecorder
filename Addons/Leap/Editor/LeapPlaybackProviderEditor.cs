#if PR_LEAP
using UnityEngine;
using Leap.Unity;
using UnityEditor;

namespace PlayRecorder.Leap
{
    [CustomEditor(typeof(LeapPlaybackProvider))]
    public class LeapPlaybackProviderEditor : LeapServiceProviderEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            isVRProvider = true;

            specifyConditionalDrawing(() => {
                return serializedObject
                         .FindProperty("_temporalWarpingMode")
                           .enumValueIndex == 1;
            },
                                      "_customWarpAdjustment");

            specifyConditionalDrawing(() => {
                return serializedObject
                         .FindProperty("_deviceOffsetMode")
                           .enumValueIndex == 1;
            },
                                      "_deviceOffsetYAxis",
                                      "_deviceOffsetZAxis",
                                      "_deviceTiltXAxis");

            specifyConditionalDrawing(() => {
                return serializedObject
                         .FindProperty("_deviceOffsetMode")
                           .enumValueIndex == 2;
            },
                                      "_deviceOrigin");

            addPropertyToFoldout("_deviceOffsetMode", "Advanced Options");
            addPropertyToFoldout("_temporalWarpingMode", "Advanced Options");
            addPropertyToFoldout("_customWarpAdjustment", "Advanced Options");
            addPropertyToFoldout("_deviceOffsetYAxis", "Advanced Options");
            addPropertyToFoldout("_deviceOffsetZAxis", "Advanced Options");
            addPropertyToFoldout("_deviceTiltXAxis", "Advanced Options");
            addPropertyToFoldout("_deviceOrigin", "Advanced Options");
            addPropertyToFoldout("_preCullCamera", "Advanced Options");
            addPropertyToFoldout("_updateHandInPrecull", "Advanced Options");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("This LeapProvider will automatically apply VR specific functions when the Edit Time Pose is set to either VR mode.", MessageType.Info);
            base.OnInspectorGUI();
        }

        public override void OnSceneGUI()
        {
            LeapPlaybackProvider xrProvider = target as LeapPlaybackProvider;
            if (serializedObject.FindProperty("editTimePose").enumValueIndex == 0 || serializedObject.FindProperty("editTimePose").enumValueIndex == 1)
            {
                if (serializedObject.FindProperty("_deviceOffsetMode").enumValueIndex == 2 &&
                xrProvider.deviceOrigin != null)
                {
                    controllerOffset = xrProvider.transform.InverseTransformPoint(xrProvider.deviceOrigin.position);
                    deviceRotation = Quaternion.Inverse(xrProvider.transform.rotation) *
                                     xrProvider.deviceOrigin.rotation *
                                     Quaternion.Euler(90f, 0f, 0f);
                }
                else
                {
                    deviceRotation = Quaternion.Euler(90f, 0f, 0f) *
                                     Quaternion.Euler(xrProvider.deviceTiltXAxis, 0f, 0f);

                    controllerOffset = new Vector3(0f,
                                                   xrProvider.deviceOffsetYAxis,
                                                   xrProvider.deviceOffsetZAxis);
                }
            }
            else
            {
                deviceRotation = Quaternion.Euler(0f, 0f, 0f) *
                                     Quaternion.Euler(xrProvider.deviceTiltXAxis, 0f, 0f);

                controllerOffset = new Vector3(0f,
                                               xrProvider.deviceOffsetYAxis,
                                               xrProvider.deviceOffsetZAxis);
            }

            base.OnSceneGUI();

        }
    }
}
#endif