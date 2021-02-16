﻿using UnityEngine;
using UnityEditor;


namespace PlayRecorder.Tools
{

    public static class EditorUtil
    {

        public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            r.x -= 2;
            r.width += 6;
            EditorGUI.DrawRect(r, color);
        }

        public static void DrawDividerLine()
        {
            DrawUILine(Color.grey, 1, 4);
        }

    }

}