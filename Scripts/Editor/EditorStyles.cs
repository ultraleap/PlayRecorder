using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PlayRecorder.Tools
{

    public static class Styles
    {
        public static GUIStyle textBold = new GUIStyle(EditorStyles.boldLabel);
        public static GUIStyle textBoldRed = new GUIStyle(EditorStyles.boldLabel) { normal = new GUIStyleState() { textColor = new Color(255f * 0.8f, 0, 0) } };
        public static GUIStyle textDisabledBold = new GUIStyle(EditorStyles.boldLabel) { normal = new GUIStyleState() { textColor = Color.grey }, active = new GUIStyleState() { textColor = Color.grey }, hover = new GUIStyleState() { textColor = Color.grey } };
        
        public static GUIStyle foldoutBold = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, richText = true };

        public static GUIStyle miniButton = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Normal, fixedHeight = 18 };
        public static GUIStyle miniButtonGrey = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Normal, normal = new GUIStyleState() { textColor = Color.grey }, fixedHeight = 18 };
        public static GUIStyle miniButtonBold = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold, fixedHeight = 18 };
        public static GUIStyle miniButtonBoldRed = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold, normal = new GUIStyleState() { textColor = new Color(255f * 0.8f, 0, 0) }, fixedHeight = 18 };

        public static GUIStyle buttonIcon = new GUIStyle(EditorStyles.miniButton) { padding = new RectOffset(1, 1, 1, 1), fixedHeight = 18 };

    }
    

}
