using UnityEngine;

namespace Emberfall
{
    public static class GraphicsQualityRuntime
    {
        private static GraphicsQualityProfile current;
        private static float thermalDetailLimit = float.MaxValue;
        internal static float DetailDistance(GraphicsQualityProfile profile)
        { return profile.Mobile ? Mathf.Min(profile.DetailDistance, thermalDetailLimit) : profile.DetailDistance; }
        internal static void SetThermalDetailLimit(float distance) { thermalDetailLimit = distance; }

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
            SystemInfo.graphicsDeviceName.IndexOf("simulator", System.StringComparison.OrdinalIgnoreCase) >= 0,
            GraphicsQualityPolicy.ClassifyDisplay(SystemInfo.deviceModel, Display.main.systemWidth, Display.main.systemHeight, Screen.dpi)); }
        internal static void Configure(GraphicsQualityMode selection)
        { current = GraphicsQualityPolicy.Resolve(selection, CaptureDevice()); CombatVisualBudget.ConfiguredLimit = current.CombatVisualLimit; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { thermalDetailLimit = float.MaxValue; current = null; CombatVisualBudget.ConfiguredLimit = 0; }
    }
}
