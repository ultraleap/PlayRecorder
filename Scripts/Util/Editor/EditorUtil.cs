using UnityEngine;
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

        public static void DrawLine(this Texture2D tex, Vector2 p1, Vector2 p2, Color col)
        {
            Vector2 t = p1;
            float frac = 1 / Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
            float ctr = 0;

            while ((int)t.x != (int)p2.x || (int)t.y != (int)p2.y)
            {
                t = Vector2.Lerp(p1, p2, ctr);
                ctr += frac;

                tex.SetPixel((int)t.x, (int)t.y-1, col);
                tex.SetPixel((int)t.x, (int)t.y, col);
            }
        }

        public static float MinAxis(this Vector2 v2)
        {
            return Mathf.Min(v2.x, v2.y);
        }

        public static float MinAxis(this Vector3 v3)
        {
            return Mathf.Min(Mathf.Min(v3.x, v3.y), v3.z);
        }

        public static float MinAxis(this Vector4 v4)
        {
            return Mathf.Min(Mathf.Min(Mathf.Min(v4.x, v4.y), v4.z), v4.w);
        }

        public static float MaxAxis(this Vector2 v2)
        {
            return Mathf.Max(v2.x, v2.y);
        }

        public static float MaxAxis(this Vector3 v3)
        {
            return Mathf.Max(Mathf.Max(v3.x, v3.y), v3.z);
        }

        public static float MaxAxis(this Vector4 v4)
        {
            return Mathf.Max(Mathf.Max(Mathf.Max(v4.x, v4.y), v4.z), v4.w);
        }

    }

}