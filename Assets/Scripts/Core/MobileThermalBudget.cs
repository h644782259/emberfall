namespace Emberfall
{
    // This only limits rendering cadence; it never pauses simulation or input.
    public sealed class MobileIdleFrameBudget
    {
        public int FrameRate(int requested, bool idleEligible, bool activity, double now)
        {
            return requested; // Foreground idling must not reduce camera/input cadence.
        }
    }
    // OS thermal levels: nominal=0, fair=1, serious=2, critical=3.
    // Cooling must stay nominal for a full minute to avoid repeated 30/60 transitions.
    public sealed class MobileThermalBudget
    {
        public bool Limited { get; private set; }
        private double coolSince = -1;
        public void Observe(int thermalState, double now)
        {
            if (thermalState >= 2) { Limited = true; coolSince = -1; return; }
            if (!Limited) return;
            if (thermalState != 0) { coolSince = -1; return; }
            if (coolSince < 0) coolSince = now;
            if (now - coolSince >= 60) { Limited = false; coolSince = -1; }
        }
        public int FrameRate(int requested) { return Limited && requested > 30 ? 30 : requested; }
        public static void RenderSize(int width, int height, out int resultWidth, out int resultHeight)
        {
            double scale = System.Math.Min(1.0, 1920.0 / System.Math.Max(1, System.Math.Max(width, height)));
            resultWidth = System.Math.Max(1, (int)System.Math.Round(width * scale));
            resultHeight = System.Math.Max(1, (int)System.Math.Round(height * scale));
        }
    }
}
