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
        public int ProtectionLevel { get; private set; }
        public bool ReducedRendering => ProtectionLevel > 0;
        public bool Limited => ProtectionLevel >= 2;
        // Fair heat must remove real work even when Balanced already uses
        // one light and hard shadows. These affect ornaments only, not gameplay.
        public float ShadowDistance(float requested) => ReducedRendering ? System.Math.Min(requested, 18f) : requested;
        public float DetailDistance(float requested) => ReducedRendering ? System.Math.Min(requested, 48f) : requested;
        public int DecorationLimit => ReducedRendering ? 6 : int.MaxValue;
        private double coolSince = -1;
        public void Observe(int thermalState, double now)
        {
            if (thermalState >= 1)
            {
                ProtectionLevel = System.Math.Max(ProtectionLevel, System.Math.Min(thermalState, 3));
                coolSince = -1;
                return;
            }
            if (!ReducedRendering) return;
            if (thermalState != 0) { coolSince = -1; return; }
            if (coolSince < 0) coolSince = now;
            if (now - coolSince >= 60) { ProtectionLevel = 0; coolSince = -1; }
        }
        public int FrameRate(int requested) { return System.Math.Min(requested, ProtectionLevel >= 3 ? 15 : Limited ? 30 : requested); }
        public static void RenderSize(int width, int height, out int resultWidth, out int resultHeight)
        {
            double scale = System.Math.Min(1.0, 1920.0 / System.Math.Max(1, System.Math.Max(width, height)));
            resultWidth = System.Math.Max(1, (int)System.Math.Round(width * scale));
            resultHeight = System.Math.Max(1, (int)System.Math.Round(height * scale));
        }
    }
}
