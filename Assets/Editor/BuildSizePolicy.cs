using System;
using System.Globalization;

namespace Emberfall.Editor
{
    // Pure policy: the editor adapter supplies the real platform/output metrics.
    public static class BuildSizePolicy
    {
        public const long Mebibyte = 1024L * 1024L;
        public static bool IsSignificantGrowth(long previous, long current)
        { return previous > 0 && current > previous && current - previous > 25L * Mebibyte && current > previous * 1.2d; }

        public static bool TryParseBudget(string text, out long bytes)
        {
            bytes = 0;
            double value;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                double.IsNaN(value) || double.IsInfinity(value) || value <= 0 || value > 1048576) return false;
            bytes = (long)Math.Ceiling(value * Mebibyte);
            return bytes > 0;
        }
    }
}
