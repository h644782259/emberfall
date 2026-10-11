using UnityEngine;

namespace Emberfall
{
    public static class GraphicsQualityRuntime
    {
        private static GraphicsQualityProfile current;
        public static GraphicsQualityProfile Current
        {
            get
            {
                if (current == null)
                { current = GraphicsQualityPolicy.Resolve(GraphicsQualityMode.Balanced, CaptureDevice()); CombatVisualBudget.ConfiguredLimit = current.CombatVisualLimit; }
                return current;
            }
        }
        internal static GraphicsDeviceCapabilities CaptureDevice()
        { return new GraphicsDeviceCapabilities(Application.isMobilePlatform, SystemInfo.systemMemorySize, SystemInfo.graphicsMemorySize, SystemInfo.processorCount, SystemInfo.graphicsShaderLevel,
            SystemInfo.graphicsDeviceName.IndexOf("simulator", System.StringComparison.OrdinalIgnoreCase) >= 0); }
        internal static void Configure(GraphicsQualityMode selection)
        { current = GraphicsQualityPolicy.Resolve(selection, CaptureDevice()); CombatVisualBudget.ConfiguredLimit = current.CombatVisualLimit; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { current = null; CombatVisualBudget.ConfiguredLimit = 0; }
    }
}
