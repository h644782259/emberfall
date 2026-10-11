using System;
using Emberfall.Editor;
public static class BuildSizePolicyTests
{
    public static string Run()
    {
        int count = 0;
        Action<bool, string> check = (ok, message) => { count++; if (!ok) throw new Exception(message); };
        long m = BuildSizePolicy.Mebibyte, b;
        check(!BuildSizePolicy.IsSignificantGrowth(0, 500 * m), "No baseline is not growth");
        check(!BuildSizePolicy.IsSignificantGrowth(100 * m, 125 * m), "25 MiB boundary");
        check(BuildSizePolicy.IsSignificantGrowth(100 * m, 126 * m), "Both growth limits exceeded");
        check(!BuildSizePolicy.IsSignificantGrowth(1000 * m, 1100 * m), "Large bytes but small ratio");
        check(!BuildSizePolicy.IsSignificantGrowth(10 * m, 20 * m), "Large ratio but small bytes");
        check(!BuildSizePolicy.IsSignificantGrowth(500 * m, 100 * m), "Shrink");
        check(!BuildSizePolicy.IsSignificantGrowth(long.MaxValue - 1, long.MaxValue), "No arithmetic overflow");
        check(BuildSizePolicy.TryParseBudget("256", out b) && b == 256 * m, "Whole MiB");
        check(BuildSizePolicy.TryParseBudget("0.5", out b) && b == m / 2, "Fractional MiB");
        check(BuildSizePolicy.TryParseBudget("1048576", out b) && b == 1048576L * m, "Upper supported bound");
        foreach (string value in new[] { "", " ", "-1", "0", "NaN", "Infinity", "1e309", "1048577", "1,5", "garbage", null })
            check(!BuildSizePolicy.TryParseBudget(value, out b) && b == 0, "Reject invalid budget: " + value);
        return "PASS: " + count + " pure build-size policy checks (no Unity build executed).";
    }
}
