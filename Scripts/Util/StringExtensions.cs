using System;

namespace PlayRecorder
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source?.IndexOf(toCheck, comp) >= 0;
        }

        public static string FormatType(this string source)
        {
            int lastIndex = source.LastIndexOf('.');
            
            if (lastIndex == -1)
                return source;

            return source.Substring(lastIndex+1);
        }
    }
}