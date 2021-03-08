using System;
using System.Text;

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

        public static string ToCSVCell(this string str)
        {
            bool mustQuote = (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            if (mustQuote)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("\"");
                foreach (char nextChar in str)
                {
                    sb.Append(nextChar);
                    if (nextChar == '"')
                        sb.Append("\"");
                }
                sb.Append("\"");
                return sb.ToString();
            }
            return str;
        }
    }
}