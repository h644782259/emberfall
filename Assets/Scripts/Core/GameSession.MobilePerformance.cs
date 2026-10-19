using UnityEngine;
namespace Emberfall
{
    public sealed partial class GameSession
    {
        private readonly MobileThermalBudget mobileThermalBudget = new MobileThermalBudget();
        private float nextThermalSample;
        private int appliedThermalLevel = -1;
        internal int PerformanceThermalState {get;private set;}=-1;
        private readonly MobileIdleFrameBudget mobileIdleBudget = new MobileIdleFrameBudget();
        private Vector3 lastMobilePlayerPosition;
        private const string GraphicsQualityPreferenceKey = "Emberfall.GraphicsQuality";
        private GraphicsQualityMode graphicsSelection;
        private bool graphicsPending;
        private int nativeRenderWidth, nativeRenderHeight;
        public GraphicsQualityMode GraphicsQualitySelection => graphicsSelection;
        public bool GraphicsQualityChangePending => graphicsPending;
        public string GraphicsQualitySummary => "当前：" + GraphicsQualityPolicy.Name(GraphicsQualityRuntime.Current.Mode) +
            (graphicsPending ? " · 下次进入场景生效" : graphicsSelection == GraphicsQualityMode.Automatic ? " · 按设备配置" : "") +
            (mobileThermalBudget.Limited ? " · 设备温度较高，已保护帧率" :
             mobileThermalBudget.ReducedRendering ? " · 设备升温，已降低渲染精度与装饰负载" : "");

        public void SetGraphicsQuality(GraphicsQualityMode selection)
        {
            graphicsSelection = GraphicsQualityPolicy.Sanitize((int)selection);
            PlayerPrefs.SetInt(GraphicsQualityPreferenceKey, (int)graphicsSelection);
            PlayerPrefs.Save();
            graphicsPending = true;
            // Settings are drawn while paused. A programmatic switch during combat
            // waits for a scene boundary rather than uploading/changing resolution.
            if (!HasStarted || InputBlocked) ApplyGraphicsQuality();
        }
        private int IdleFrameRate(int requested)
        {
            bool eligible = HasStarted && !InDungeon && !PracticeActive && !InputBlocked && Player != null;
            bool activity = Input.touchCount > 0 || Input.anyKey || MobileControls.AttackHeld || MobileControls.Move.sqrMagnitude > .0001f;
            if (Player != null)
            {
                activity |= (Player.transform.position - lastMobilePlayerPosition).sqrMagnitude > .000001f;
                lastMobilePlayerPosition = Player.transform.position;
            }
            if (eligible) activity |= InCombat;
            return mobileIdleBudget.FrameRate(requested, eligible, activity, Time.realtimeSinceStartupAsDouble);
        }
#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern int EmberfallThermalState();
#endif
        private void ConfigureMobileRendering()
        {
            graphicsSelection = GraphicsQualityPolicy.Sanitize(PlayerPrefs.GetInt(GraphicsQualityPreferenceKey, 0));
            // Explicit soak comparisons select the complete preset before world
            // creation, without writing the player's saved graphics preference.
            string review = System.Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW");
            if (!string.IsNullOrEmpty(review) && (review.StartsWith("performance-soak-", System.StringComparison.Ordinal) || review == "performance-render-ab"))
            {
                string quality = System.Environment.GetEnvironmentVariable("EMBERFALL_REVIEW_QUALITY");
                if (quality == "Smooth") graphicsSelection = GraphicsQualityMode.Smooth;
                else if (quality == "Balanced") graphicsSelection = GraphicsQualityMode.Balanced;
            }
            nativeRenderWidth = Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.width;
            nativeRenderHeight = Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.height;
            // Display dimensions may have their native portrait orientation while
            // Screen is landscape; preserve the active screen orientation.
            if ((nativeRenderWidth > nativeRenderHeight) != (Screen.width > Screen.height))
            { int swap = nativeRenderWidth; nativeRenderWidth = nativeRenderHeight; nativeRenderHeight = swap; }
            ApplyGraphicsQuality();
        }

        private void ApplyGraphicsQuality()
        {
            GraphicsQualityRuntime.Configure(graphicsSelection);
            var profile = GraphicsQualityRuntime.Current;
            QualitySettings.antiAliasing = profile.AntiAliasing;
            ApplyThermalRendering(profile);
            QualitySettings.shadowCascades = profile.Mobile ? 1 : 2;
            QualitySettings.lodBias = profile.LodBias;
            QualitySettings.skinWeights = SkinWeights.FourBones;
            // Textures are prepared before play; dynamic mip streaming can upload
            // at an unpredictable combat frame and is deliberately avoided here.
            QualitySettings.streamingMipmapsActive = false;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            graphicsPending = false;
            PreparedWorldDetailBudget.ApplyToExistingWorlds(profile);
        }

        private void ApplyMobileRenderSize(GraphicsQualityProfile profile)
        {
            if (!profile.Mobile) return;
            int width, height;
            // Always derive from the saved native size, so increasing quality can
            // restore resolution after a previous lower preset.
            int sourceWidth = nativeRenderWidth, sourceHeight = nativeRenderHeight;
            if ((sourceWidth > sourceHeight) != (Screen.width > Screen.height))
            { int swap = sourceWidth; sourceWidth = sourceHeight; sourceHeight = swap; }
            GraphicsQualityPolicy.RenderSize(sourceWidth, sourceHeight, mobileThermalBudget.MaximumRenderDimension(profile.MaximumRenderDimension), out width, out height);
            if (width != Screen.width || height != Screen.height)
                Screen.SetResolution(width, height, Screen.fullScreenMode);
        }

        public void PrepareGraphicsForWorld(GameObject world)
        {
            if (graphicsPending) ApplyGraphicsQuality();
            PreparedWorldDetailBudget.Prepare(world, GraphicsQualityRuntime.Current);
        }
        public System.Collections.IEnumerator PrepareGraphicsForWorldAsync(GameObject world, System.Action<float> progress = null)
        {
            if (graphicsPending) ApplyGraphicsQuality();
            return PreparedWorldDetailBudget.PrepareAsync(world, GraphicsQualityRuntime.Current, progress);
        }
        // Reduce pixel work at Fair while keeping the requested cadence. The saved
        // preset stays intact; sustained cooling restores its native-clamped size.
        private void ApplyThermalRendering(GraphicsQualityProfile profile)
        {
            bool warm = profile.Mobile && mobileThermalBudget.ReducedRendering;
            QualitySettings.shadows = !profile.RealtimeShadows ? ShadowQuality.Disable :
                profile.SoftShadows && !warm ? ShadowQuality.All : ShadowQuality.HardOnly;
            QualitySettings.shadowDistance = warm ? mobileThermalBudget.ShadowDistance(profile.ShadowDistance) : profile.ShadowDistance;
            QualitySettings.shadowResolution = !profile.Mobile && profile.Mode == GraphicsQualityMode.High
                ? ShadowResolution.High : ShadowResolution.Medium;
            QualitySettings.pixelLightCount = warm ? 1 : profile.PixelLights;
            GraphicsQualityRuntime.SetThermalDetailLimit(warm ? mobileThermalBudget.DetailDistance(profile.DetailDistance) : float.MaxValue);
            PreparedWorldDetailBudget.RefreshDetailDistances();
            CombatVisualLease.SetDecorationLimit(warm ? mobileThermalBudget.DecorationLimit : int.MaxValue);
            ApplyMobileRenderSize(profile);
            appliedThermalLevel = mobileThermalBudget.ProtectionLevel;
        }

        private int ThermalFrameRate(int requested)
        {
            // Background time is not evidence of sustained cooling. Never poll the OS there.
            if (BackgroundPaused) { mobileThermalBudget.Observe(-1, 0); nextThermalSample = 0; }
            if (!BackgroundPaused && Time.realtimeSinceStartup >= nextThermalSample)
            {
                nextThermalSample = Time.realtimeSinceStartup + 2;
#if UNITY_IOS && !UNITY_EDITOR
                PerformanceThermalState=EmberfallThermalState();
                mobileThermalBudget.Observe(PerformanceThermalState, Time.realtimeSinceStartupAsDouble);
#endif
            }
            if (appliedThermalLevel != mobileThermalBudget.ProtectionLevel)
                ApplyThermalRendering(GraphicsQualityRuntime.Current);
            return mobileThermalBudget.FrameRate(IdleFrameRate(requested));
        }
    }
}
