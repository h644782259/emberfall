using System;
using System.Globalization;

namespace Emberfall
{
    public static class CombatNumberFormat
    {
        public static string Compact(double value)
        {
            if(double.IsNaN(value)||double.IsInfinity(value))return "—";
            double magnitude=Math.Abs(value);
            double divisor=magnitude>=1e12?1e12:magnitude>=1e8?1e8:magnitude>=1e4?1e4:1;
            // Promote values that would round across the next unit boundary.
            if(divisor<1e12&&Math.Round(magnitude/divisor,2)>=10000)divisor*=10000;
            string suffix=divisor==1e12?"万亿":divisor==1e8?"亿":divisor==1e4?"万":"";
            return (value/divisor).ToString(divisor==1?"0":"0.##",CultureInfo.InvariantCulture)+suffix;
        }
    }
}
