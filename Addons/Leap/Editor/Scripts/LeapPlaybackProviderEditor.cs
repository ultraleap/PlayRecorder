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

            specifyConditionalDrawing(() =>
            {
                return serializedObject
                        .FindProperty("_purePlayback")
                            .boolValue;
            },
                                    "_providerToCopy");

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
    }
}
#endif