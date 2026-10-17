using System;

namespace Emberfall
{
    public enum GraphicsQualityMode { Automatic = 0, Smooth = 1, Balanced = 2, High = 3 }
    public enum GraphicsDisplayClass { Unknown, Phone, Tablet }

    // Device classification is independent of Unity and never changes simulation rules.
    public readonly struct GraphicsDeviceCapabilities
    {
        public readonly bool Mobile, Simulator;
        public readonly int MemoryMB, GraphicsMemoryMB, ProcessorCount, GraphicsLevel;
        public readonly GraphicsDisplayClass DisplayClass;
        public GraphicsDeviceCapabilities(bool mobile, int memoryMB, int graphicsMemoryMB, int processorCount, int graphicsLevel, bool simulator = false, GraphicsDisplayClass displayClass = GraphicsDisplayClass.Unknown)
        { Mobile = mobile; Simulator = simulator; MemoryMB = memoryMB; GraphicsMemoryMB = graphicsMemoryMB; ProcessorCount = processorCount; GraphicsLevel = graphicsLevel; DisplayClass = displayClass; }
    }

    public sealed class GraphicsQualityProfile
    {
        public readonly GraphicsQualityMode Mode;
        public readonly bool Mobile, SoftShadows, RealtimeShadows;
        public readonly int MaximumRenderDimension, AntiAliasing, PixelLights, CombatVisualLimit;
        public readonly float ShadowDistance, DetailDistance, LodBias, DecorativeEffectsScale;
        internal GraphicsQualityProfile(GraphicsQualityMode mode, bool mobile, GraphicsDisplayClass displayClass)
        {
            Mode = mode; Mobile = mobile;
            int tier = (int)mode - 1;
            // Pixel budgets are display-specific, always clamped to native size
            // by RenderSize. Unknown displays retain the established fallback.
            MaximumRenderDimension = !mobile ? 0 : displayClass == GraphicsDisplayClass.Phone ? new[] { 960, 1440, 1920 }[tier] :
                displayClass == GraphicsDisplayClass.Tablet ? new[] { 960, 1600, 2560 }[tier] : new[] { 960, 1920, 2560 }[tier];
            // Smooth bounds fill and multisample bandwidth; input/simulation remain 60Hz.
            AntiAliasing = mobile && mode == GraphicsQualityMode.Smooth ? 0 : !mobile && mode == GraphicsQualityMode.High ? 4 : 2;
            RealtimeShadows = !mobile || mode != GraphicsQualityMode.Smooth;
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
            // RAM/API support cannot establish sustained mobile GPU headroom.
            // High remains an explicit option; automatic favors sustained play.
            // This also prevents simulator host RAM from selecting mobile High.
            if (device.Mobile) return GraphicsQualityMode.Balanced;
            bool enoughMemory = device.MemoryMB >= (device.Mobile ? 5800 : 12000);
            bool enoughCpu = device.ProcessorCount >= 6;
            bool enoughGpu = device.GraphicsLevel >= 45 &&
                (device.GraphicsMemoryMB <= 0 || device.GraphicsMemoryMB >= (device.Mobile ? 900 : 2000));
            return enoughMemory && enoughCpu && enoughGpu ? GraphicsQualityMode.High : GraphicsQualityMode.Balanced;
        }

        public static GraphicsQualityProfile Resolve(GraphicsQualityMode selection, GraphicsDeviceCapabilities device)
        {
            selection = Sanitize((int)selection);
            return new GraphicsQualityProfile(selection == GraphicsQualityMode.Automatic ? Recommended(device) : selection, device.Mobile, device.DisplayClass);
        }

        public static GraphicsDisplayClass ClassifyDisplay(string model, int nativeWidth, int nativeHeight, float dpi)
        {
            if (!string.IsNullOrEmpty(model))
            {
                if (model.StartsWith("iPad", StringComparison.OrdinalIgnoreCase)) return GraphicsDisplayClass.Tablet;
                if (model.StartsWith("iPhone", StringComparison.OrdinalIgnoreCase) || model.StartsWith("iPod", StringComparison.OrdinalIgnoreCase)) return GraphicsDisplayClass.Phone;
            }
            // Android vendors can omit or misreport DPI. Do not classify from
            // pixel count alone: dense phones can exceed tablet pixel counts.
            if (nativeWidth <= 0 || nativeHeight <= 0 || dpi < 100 || dpi > 1000 || float.IsNaN(dpi)) return GraphicsDisplayClass.Unknown;
            double diagonal = Math.Sqrt((double)nativeWidth * nativeWidth + (double)nativeHeight * nativeHeight) / dpi;
            if (diagonal < 3 || diagonal > 18) return GraphicsDisplayClass.Unknown;
            return diagonal >= 7 ? GraphicsDisplayClass.Tablet : GraphicsDisplayClass.Phone;
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
