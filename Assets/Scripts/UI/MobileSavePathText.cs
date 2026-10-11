using System;
using System.Globalization;
using System.Text;

namespace Emberfall
{
    /// <summary>Display-only line breaks; callers keep the original path for copy/open.</summary>
    public static class MobileSavePathText
    {
        public static string Wrap(string value, float width, Func<string, float> measure)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (measure == null) throw new ArgumentNullException(nameof(measure));
            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            var result = new StringBuilder();
            var line = new StringBuilder();
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(value);
            while (elements.MoveNext())
            {
                string element = elements.GetTextElement();
                if (element == "\n" || element == "\r" || element == "\r\n")
                { result.Append(line).Append(element); line.Clear(); continue; }
                if (line.Length > 0 && measure(line.ToString() + element) > width)
                { result.Append(line).Append('\n'); line.Clear(); }
                // A single glyph wider than the viewport remains intact. The
                // production viewport is much wider than any normal path glyph.
                line.Append(element);
            }
            return result.Append(line).ToString();
        }
    }
}
