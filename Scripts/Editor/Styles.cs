using UnityEngine;
using UnityEditor;

namespace PlayRecorder.Tools
{
    public static class Styles
    {
        public static GUIStyle textCentered = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
        public static GUIStyle textBold = new GUIStyle(EditorStyles.boldLabel);
        public static GUIStyle textRich = new GUIStyle(EditorStyles.label) { richText = true };
        public static GUIStyle textBoldRed = new GUIStyle(EditorStyles.boldLabel) { normal = new GUIStyleState() { textColor = new Color(255f * 0.8f, 0, 0) } };
        public static GUIStyle textDisabledBold = new GUIStyle(EditorStyles.boldLabel) { normal = new GUIStyleState() { textColor = Color.grey }, active = new GUIStyleState() { textColor = Color.grey }, hover = new GUIStyleState() { textColor = Color.grey } };
        
        public static GUIStyle foldoutBold = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
        public static GUIStyle foldoutIconBold = new GUIStyle(foldoutBold) { fixedHeight = EditorGUIUtility.singleLineHeight };

        public static GUIStyle miniButton = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Normal, fixedHeight = 18 };
        public static GUIStyle miniButtonLeft = new GUIStyle(EditorStyles.miniButtonLeft) { fontStyle = FontStyle.Normal, fixedHeight = 18 };
        public static GUIStyle miniButtonRight = new GUIStyle(EditorStyles.miniButtonRight) { fontStyle = FontStyle.Normal, fixedHeight = 18 };
        public static GUIStyle miniButtonMid = new GUIStyle(EditorStyles.miniButtonMid) { fontStyle = FontStyle.Normal, fixedHeight = 18 };
        public static GUIStyle miniButtonGrey = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Normal, normal = new GUIStyleState() { textColor = Color.grey }, fixedHeight = 18 };
        public static GUIStyle miniButtonBold = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold, fixedHeight = 18 };
        public static GUIStyle miniButtonBoldRed = new GUIStyle(EditorStyles.miniButton) { fontStyle = FontStyle.Bold, normal = new GUIStyleState() { textColor = new Color(255f * 0.8f, 0, 0) }, fixedHeight = 18 };

        public static GUIStyle buttonIcon = new GUIStyle(EditorStyles.miniButton) { padding = new RectOffset(1, 1, 1, 1), fixedHeight = EditorGUIUtility.singleLineHeight };

        public static GUIStyle textIcon = new GUIStyle(EditorStyles.label) { padding = new RectOffset(1, 1, 1, 1), fixedHeight = EditorGUIUtility.singleLineHeight };
        public static GUIStyle textIconBold = new GUIStyle(textIcon) { fontStyle = FontStyle.Bold };

        public static GUIStyle boxBorder = new GUIStyle(EditorStyles.helpBox);

        public static GUIStyle boxBorderText = new GUIStyle(EditorStyles.helpBox) { fontSize = 12 , richText = true};

    }

    public static class Colors
    {
        public static Color red = new Color(217f / 255f, 0, 0);

        public static Color green = new Color(0, 178f / 255f, 0);
    }
}