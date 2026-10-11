using System;
using Emberfall;

public static class GraphicsQualityPolicyTests
{
    public static string Run()
    {
        int checks = 0;
        Action<bool, string> check = (ok, why) => { checks++; if (!ok) throw new Exception(why); };
        var lowPhone = new GraphicsDeviceCapabilities(true, 3072, 0, 6, 50);
        var midPhone = new GraphicsDeviceCapabilities(true, 4096, 0, 6, 50);
        var unifiedPhone = new GraphicsDeviceCapabilities(true, 8192, 0, 6, 50);
        var lowCpu = new GraphicsDeviceCapabilities(false, 16000, 4096, 2, 50);
        var desktop = new GraphicsDeviceCapabilities(false, 16000, 4096, 8, 50);
        var unknown = new GraphicsDeviceCapabilities(true, 0, 0, 0, 0);
        var simulator = new GraphicsDeviceCapabilities(true, 32768, 16384, 12, 50, true);
        check(GraphicsQualityPolicy.Recommended(lowPhone) == GraphicsQualityMode.Smooth, "memory constrained phones favor lower rendering cost");
        check(GraphicsQualityPolicy.Recommended(midPhone) == GraphicsQualityMode.Balanced, "midrange phones avoid excessive native pixel cost");
        check(GraphicsQualityPolicy.Recommended(unifiedPhone) == GraphicsQualityMode.High, "zero VRAM on unified-memory phones must not demote capable devices");
        check(GraphicsQualityPolicy.Recommended(lowCpu) == GraphicsQualityMode.Smooth, "weak desktop CPUs retain a low render option");
        check(GraphicsQualityPolicy.Recommended(desktop) == GraphicsQualityMode.High, "capable desktop hardware can retain richer rendering");
        check(GraphicsQualityPolicy.Recommended(unknown) == GraphicsQualityMode.Balanced, "unknown diagnostics have a stable default");
        check(GraphicsQualityPolicy.Recommended(simulator) == GraphicsQualityMode.Balanced, "simulator host specifications must not request the high mobile GPU preset");
        check(GraphicsQualityPolicy.Resolve(GraphicsQualityMode.High, simulator).Mode == GraphicsQualityMode.High, "manual simulator quality remains under player control");
        check(GraphicsQualityPolicy.Recommended(new GraphicsDeviceCapabilities(true, 3072, 0, 2, 50, true)) == GraphicsQualityMode.Smooth, "constrained simulator hosts still retain the lower automatic preset");
        check(GraphicsQualityPolicy.Sanitize(-1) == GraphicsQualityMode.Automatic && GraphicsQualityPolicy.Sanitize(99) == GraphicsQualityMode.Automatic, "corrupted saved preference has a valid fallback");
        check(GraphicsQualityPolicy.Resolve(GraphicsQualityMode.High, lowPhone).Mode == GraphicsQualityMode.High, "explicit selection remains under player control");
        check(GraphicsQualityPolicy.Resolve(GraphicsQualityMode.Automatic, lowPhone).Mode == GraphicsQualityMode.Smooth, "automatic resolves on the actual device");
        foreach (GraphicsQualityMode mode in new[] { GraphicsQualityMode.Smooth, GraphicsQualityMode.Balanced, GraphicsQualityMode.High })
        {
            var mobile = GraphicsQualityPolicy.Resolve(mode, unifiedPhone);
            var pc = GraphicsQualityPolicy.Resolve(mode, desktop);
            check(mobile.AntiAliasing >= 2 && mobile.PixelLights >= 1, "all modes retain readable edges and contact lighting");
            check(mobile.CombatVisualLimit >= 20, "low quality never shrinks the existing phone feedback pool");
            check(mobile.DetailDistance >= 56, "detail culling cannot cut into the large encounter camera range");
            check(pc.MaximumRenderDimension == 0, "desktop windows retain their chosen resolution");
            foreach (var native in new[] { new[] { 2622, 1206 }, new[] { 1206, 2622 }, new[] { 2732, 2048 }, new[] { 1280, 720 } })
            {
                int width, height;
                GraphicsQualityPolicy.RenderSize(native[0], native[1], mobile.MaximumRenderDimension, out width, out height);
                check(width <= native[0] && height <= native[1] && Math.Max(width, height) <= mobile.MaximumRenderDimension, "rendering never upscales or exceeds profile pixel cost");
                check(Math.Abs((double)width / height - (double)native[0] / native[1]) < .003, "rendering retains portrait/landscape aspect ratio");
            }
        }
        int lowWidth, lowHeight, highWidth, highHeight;
        GraphicsQualityPolicy.RenderSize(2622, 1206, 1600, out lowWidth, out lowHeight);
        GraphicsQualityPolicy.RenderSize(2622, 1206, 2560, out highWidth, out highHeight);
        check(highWidth > lowWidth && highHeight > lowHeight, "raising quality restores resolution from native dimensions");
        GraphicsQualityPolicy.RenderSize(0, -1, 1600, out lowWidth, out lowHeight);
        check(lowWidth == 1 && lowHeight == 1, "temporarily unavailable screen dimensions remain safe");
        return "PASS: " + checks + " graphics quality policy assertions";
    }
}
