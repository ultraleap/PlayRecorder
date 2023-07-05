using System.Collections.Generic;
using UnityEngine;
using PlayRecorder.Tools;

namespace PlayRecorder.Statistics
{
    public static class StatisticGraph
    {

        public static Texture2D GenerateGraph(RecordMessage message, StatisticWindow.StatCache cache, int width, int height, int allMaxFrame, List<Color> lineColors, Color backgroundColor)
        {
            if (cache.maxFrame == 0)
            {
                return null;
            }
            
            for (int i = 0; i < cache.values.Length; i++)
            {
                if (cache.type == typeof(string))
                {
                    return null;
                }
                else
                {
                    Texture2D tex = new Texture2D((int)(Mathf.Abs(width) * (cache.maxFrame / (float)allMaxFrame)), Mathf.Abs(height));
                    FillPixels(tex, backgroundColor);
                    GraphLines(tex, cache, lineColors);
                    tex.Apply();
                    return tex;
                }
            }
            return null;
        }

        private static void FillPixels(Texture2D texture, Color color)
        {
            Color[] colors = texture.GetPixels();
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }
            texture.SetPixels(colors);
        }

        private static void GraphLines(Texture2D texture, StatisticWindow.StatCache cache, List<Color> lineColors)
        {
            float positive = 0, negative = 0, range;
            object item;
            for (int i = 0; i < cache.values.Length; i++)
            {
                item = cache.values[i];
                if (item.GetType() == typeof(int) ||
                    item.GetType() == typeof(float) ||
                    item.GetType() == typeof(double))
                {
                    float.TryParse(item.ToString(), out float val);
                    if (positive < val)
                    {
                        positive = val;
                    }
                    if (negative > val)
                    {
                        negative = val;
                    }
                }
                else if (item.GetType() == typeof(Vector2))
                {
                    Vector2 v2 = (Vector2)item;
                    if (positive < v2.MaxAxis())
                    {
                        positive = v2.MaxAxis();
                    }
                    if (negative > v2.MinAxis())
                    {
                        negative = v2.MinAxis();
                    }
                }
                else if (item.GetType() == typeof(Vector3))
                {
                    Vector2 v3 = (Vector3)item;
                    if (positive < v3.MaxAxis())
                    {
                        positive = v3.MaxAxis();
                    }
                    if (negative > v3.MinAxis())
                    {
                        negative = v3.MinAxis();
                    }
                }
                else if (item.GetType() == typeof(Vector4))
                {
                    Vector4 v4 = (Vector4)item;
                    if (positive < v4.MaxAxis())
                    {
                        positive = v4.MaxAxis();
                    }
                    if (negative > v4.MinAxis())
                    {
                        negative = v4.MinAxis();
                    }
                }
            }

            range = positive + Mathf.Abs(negative);
            cache.negative = negative;
            cache.positive = positive;

            if (cache.recordMessage.frames.Count == 1)
            {
                item = cache.values[0];
                // Draw the line to the end of the file.
                DrawLinesFromObject(item, item, texture, cache.recordMessage.frames[0], cache.maxFrame, cache.maxFrame, negative, range, lineColors);
            }
            else
            {
                object previous = null;
                for (int i = 0; i < cache.values.Length; i++)
                {
                    item = cache.values[i];
                    if (i == 0)
                    {
                        previous = item;
                        continue;
                    }
                    DrawLinesFromObject(item, previous, texture, cache.recordMessage.frames[i - 1], cache.recordMessage.frames[i], cache.maxFrame, negative, range, lineColors);
                    previous = item;
                }
                DrawLinesFromObject(previous, previous, texture, cache.recordMessage.frames[cache.recordMessage.frames.Count - 1], cache.maxFrame, cache.maxFrame, negative, range, lineColors);
            }
        }

        private static void DrawLinesFromObject(object item, object previousItem, Texture2D texture, float previousFrame, float currentFrame, float endFrame, float negative, float range, List<Color> lineColors)
        {
            if (item.GetType() == typeof(int) ||
                item.GetType() == typeof(float) ||
                item.GetType() == typeof(double))
            {
                // Complaining about not being able to convert a double to a float, this solves it for some reason.
                float.TryParse(item.ToString(), out float val);
                float.TryParse(previousItem.ToString(), out float valPrev);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, valPrev, val, negative, range, lineColors[0 % lineColors.Count]);
            }
            else if (item.GetType() == typeof(Vector2))
            {
                Vector2 v2Prev = (Vector2)previousItem, v2 = (Vector2)item;
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v2Prev.x, v2.x, negative, range, lineColors[0 % lineColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v2Prev.y, v2.y, negative, range, lineColors[1 % lineColors.Count]);
            }
            else if (item.GetType() == typeof(Vector3))
            {
                Vector3 v3Prev = (Vector3)previousItem, v3 = (Vector3)item;
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v3Prev.x, v3.x, negative, range, lineColors[0 % lineColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v3Prev.y, v3.y, negative, range, lineColors[1 % lineColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v3Prev.z, v3.z, negative, range, lineColors[2 % lineColors.Count]);
            }
            else if (item.GetType() == typeof(Vector4))
            {
                Vector4 v4Prev = (Vector4)previousItem, v4 = (Vector4)item;
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v4Prev.x, v4.x, negative, range, lineColors[0 % lineColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v4Prev.y, v4.y, negative, range, lineColors[1 % lineColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v4Prev.z, v4.z, negative, range, lineColors[2 % lineColors.Count]);
                DrawSingleLine(texture, previousFrame, currentFrame, endFrame, v4Prev.w, v4.w, negative, range, lineColors[3 % lineColors.Count]);
            }
        }

        private static void DrawSingleLine(Texture2D texture, float previousFrame, float currentFrame, float endFrame, float previousValue, float currentValue, float negative, float range, Color color)
        {
            texture.DrawLine(new Vector2((previousFrame / endFrame) * texture.width, ((previousValue + Mathf.Abs(negative)) / range) * texture.height),
                        new Vector2((currentFrame / endFrame) * texture.width, ((currentValue + Mathf.Abs(negative)) / range) * texture.height),
                        color);
        }
    }
}