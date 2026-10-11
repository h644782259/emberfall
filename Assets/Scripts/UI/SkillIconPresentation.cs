namespace Emberfall
{
    public static class SkillIconPresentation
    {
        public static int RasterSize(int requested) { return requested <= 24 ? 24 : requested <= 32 ? 32 : 48; }
        public static bool SideBySide(float touchWidth) { return touchWidth >= 800; }
        public static float MinimumStroke(int rasterSize) { return rasterSize <= 24 ? 3.4f : rasterSize <= 32 ? 2.6f : 1.8f; }
    }
}
