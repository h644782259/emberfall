using System;

namespace Emberfall
{
    public enum GraphicsQualityMode { Automatic = 0, Smooth = 1, Balanced = 2, High = 3 }

    // Device classification is independent of Unity and never changes simulation rules.
    public readonly struct GraphicsDeviceCapabilities
    {
        public readonly bool Mobile, Simulator;
        public readonly int MemoryMB, GraphicsMemoryMB, ProcessorCount, GraphicsLevel;
        public GraphicsDeviceCapabilities(bool mobile, int memoryMB, int graphicsMemoryMB, int processorCount, int graphicsLevel, bool simulator = false)
        { Mobile = mobile; Simulator = simulator; MemoryMB = memoryMB; GraphicsMemoryMB = graphicsMemoryMB; ProcessorCount = processorCount; GraphicsLevel = graphicsLevel; }
    }

    public sealed class GraphicsQualityProfile
    {
        public readonly GraphicsQualityMode Mode;
        public readonly bool Mobile, SoftShadows;
        public readonly int MaximumRenderDimension, AntiAliasing, PixelLights, CombatVisualLimit;
        public readonly float ShadowDistance, DetailDistance, LodBias, DecorativeEffectsScale;
        internal GraphicsQualityProfile(GraphicsQualityMode mode, bool mobile)
        {
            Mode = mode; Mobile = mobile;
            int tier = (int)mode - 1;
            MaximumRenderDimension = mobile ? new[] { 1600, 1920, 2560 }[tier] : 0;
            // Keep native-detail resolution; mobile MSAA bandwidth is bounded at 2x.
            AntiAliasing = !mobile && mode == GraphicsQualityMode.High ? 4 : 2;
            PixelLights = mode == GraphicsQualityMode.High ? 2 : 1;
            SoftShadows = mode == GraphicsQualityMode.High;
            ShadowDistance = mobile ? new[] { 18f, 24f, 34f }[tier] : new[] { 24f, 36f, 55f }[tier];
            // The camera can sit more than 40m from the hero in large encounters.
            // These distances only remove small decoration beyond that readable view.
            DetailDistance = new[] { 56f, 72f, 96f }[tier];
            LodBias = new[] { .8f, 1.2f, 1.75f }[tier];
            DecorativeEffectsScale = new[] { .55f, .8f, 1f }[tier];
            CombatVisualLimit = mobile ? new[] { 20, 24, 32 }[tier] : new[] { 28, 36, 48 }[tier];
        }
    }

    public static class GraphicsQualityPolicy
    {
        public static GraphicsQualityMode Sanitize(int value)
        { return value >= 0 && value <= 3 ? (GraphicsQualityMode)value : GraphicsQualityMode.Automatic; }

        public static GraphicsQualityMode Recommended(GraphicsDeviceCapabilities device)
        {
            // Unified-memory GPUs often report zero VRAM: unknown values alone must
            // not classify a modern iPhone, iPad or integrated desktop as low end.
            bool lowMemory = device.MemoryMB > 0 && device.MemoryMB < (device.Mobile ? 3800 : 6000);
            bool lowCpu = device.ProcessorCount > 0 && device.ProcessorCount < 4;
            bool lowGpu = device.GraphicsLevel > 0 && device.GraphicsLevel < 35;
            if (lowMemory || lowCpu || lowGpu) return GraphicsQualityMode.Smooth;
            // The iOS simulator reports host RAM and CPU counts, which do not
            // describe the emulated mobile GPU's rendering capacity.
            if (device.Mobile && device.Simulator) return GraphicsQualityMode.Balanced;
            bool enoughMemory = device.MemoryMB >= (device.Mobile ? 5800 : 12000);
            bool enoughCpu = device.ProcessorCount >= 6;
            bool enoughGpu = device.GraphicsLevel >= 45 &&
                (device.GraphicsMemoryMB <= 0 || device.GraphicsMemoryMB >= (device.Mobile ? 900 : 2000));
            return enoughMemory && enoughCpu && enoughGpu ? GraphicsQualityMode.High : GraphicsQualityMode.Balanced;
        }

        public static GraphicsQualityProfile Resolve(GraphicsQualityMode selection, GraphicsDeviceCapabilities device)
        {
            selection = Sanitize((int)selection);
            return new GraphicsQualityProfile(selection == GraphicsQualityMode.Automatic ? Recommended(device) : selection, device.Mobile);
        }

        public static void RenderSize(int nativeWidth, int nativeHeight, int maximumDimension, out int width, out int height)
        {
            nativeWidth = Math.Max(1, nativeWidth); nativeHeight = Math.Max(1, nativeHeight);
            double ratio = maximumDimension > 0 ? Math.Min(1, (double)maximumDimension / Math.Max(nativeWidth, nativeHeight)) : 1;
            width = Math.Max(1, (int)Math.Round(nativeWidth * ratio));
            height = Math.Max(1, (int)Math.Round(nativeHeight * ratio));
        }

        public static string Name(GraphicsQualityMode mode)
        {
            switch (mode) { case GraphicsQualityMode.Smooth: return "流畅"; case GraphicsQualityMode.Balanced: return "均衡"; case GraphicsQualityMode.High: return "高画质"; default: return "自动"; }
        }
    }
}
