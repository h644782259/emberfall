using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Request a depth texture only for visible consumers. Prewarming a material
    // must not add a permanent full-scene depth pass on mobile.
    internal static class MobileSceneDepth
    {
        private static readonly List<Renderer> consumers = new List<Renderer>();
        private static readonly HashSet<Renderer> registered = new HashSet<Renderer>();
        private static readonly Dictionary<Camera, DepthTextureMode> saved = new Dictionary<Camera, DepthTextureMode>();
        private static readonly Dictionary<Camera, float> savedSoftDepth = new Dictionary<Camera, float>();
        private static readonly int skipSoftDepthId = Shader.PropertyToID("_EmberSkipSoftDepth");
        internal static bool SkipSoftDepthForReview { get; private set; }
        private static bool keepSoftDepthForReview;
        internal static bool SoftDepthSimplified => SkipSoftDepthForReview ||
            (!keepSoftDepthForReview && !GraphicsQualityRuntime.Current.SoftEffectIntersections);
        private static readonly int darkCoreId = Shader.PropertyToID("_DarkCore");
        private static MaterialPropertyBlock depthProperties;
        private static readonly Plane[] planes = new Plane[6];
        internal static bool ForceAlwaysForReview;
        internal static bool LastMainCameraRequestedDepth { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Camera.onPreCull -= BeforeCamera;
            Camera.onPostRender -= AfterCamera;
            foreach (var pair in saved) if (pair.Key != null) pair.Key.depthTextureMode = pair.Value;
            saved.Clear(); savedSoftDepth.Clear(); consumers.Clear(); registered.Clear();
            Shader.SetGlobalFloat(skipSoftDepthId, 0);
            bool review = Application.isMobilePlatform &&
                !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW"));
            string softDepthOverride = System.Environment.GetEnvironmentVariable("EMBERFALL_REVIEW_SKIP_SOFT_DEPTH");
            SkipSoftDepthForReview = review && softDepthOverride == "1";
            keepSoftDepthForReview = review && softDepthOverride == "0";
            ForceAlwaysForReview = false; LastMainCameraRequestedDepth = false;
            if (!Application.isMobilePlatform) return;
            Camera.onPreCull += BeforeCamera;
            Camera.onPostRender += AfterCamera;
        }

        internal static void Track(Renderer renderer)
        {
            if (!Application.isMobilePlatform) return;
            Register(renderer);
        }

        private static void Register(Renderer renderer)
        {
            if (renderer != null && registered.Add(renderer)) consumers.Add(renderer);
        }

        private static bool ReadsDepth(Material material)
        {
            if (material == null || material.shader == null) return false;
            switch (material.shader.name)
            {
                case "Emberfall/Filled Spell Volume":
                case "Emberfall/Layered Spell Energy":
                case "Emberfall/Travelling Sword Energy": return !SoftDepthSimplified;
                // Ray-marched volumes need scene depth for actual occlusion, not just soft edges.
                case "Emberfall/Continuous Ground Flame":
                case "Emberfall/Continuous Skill Volume": return true;
                default: return false;
            }
        }

        private static bool RendererReadsDepth(Renderer renderer)
        {
            var material = renderer.sharedMaterial;
            if (!ReadsDepth(material)) return false;
            if (material.shader.name != "Emberfall/Layered Spell Energy") return true;
            float darkCore = material.GetFloat(darkCoreId);
            if (renderer.HasPropertyBlock())
            {
                if (depthProperties == null) depthProperties = new MaterialPropertyBlock();
                // A per-material block takes precedence over the whole-renderer block.
                renderer.GetPropertyBlock(depthProperties, 0);
                if (depthProperties.isEmpty) renderer.GetPropertyBlock(depthProperties);
                if (depthProperties.HasFloat(darkCoreId)) darkCore = depthProperties.GetFloat(darkCoreId);
            }
            // Match the shader branch exactly: dark cores do not sample scene depth.
            return darkCore < .5f;
        }

        private static void BeforeCamera(Camera camera)
        {
            if (camera == null) return;
            // Save external depth/depth-normal/motion-vector requests unchanged.
            // Our request lasts for this camera render, including its depth pass.
            if (saved.ContainsKey(camera)) AfterCamera(camera);
            saved.Add(camera, camera.depthTextureMode);
            savedSoftDepth.Add(camera, Shader.GetGlobalFloat(skipSoftDepthId));
            Shader.SetGlobalFloat(skipSoftDepthId, SoftDepthSimplified ? 1 : 0);
            bool needed = ForceAlwaysForReview;
            GeometryUtility.CalculateFrustumPlanes(camera, planes);
            for (int i = consumers.Count - 1; i >= 0; i--)
            {
                var renderer = consumers[i];
                if (renderer == null)
                {
                    registered.Remove(renderer); consumers.RemoveAt(i); continue;
                }
                if (needed || !renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy ||
                    (camera.cullingMask & (1 << renderer.gameObject.layer)) == 0) continue;
                if (RendererReadsDepth(renderer) && GeometryUtility.TestPlanesAABB(planes, renderer.bounds)) needed = true;
            }
            if (needed) camera.depthTextureMode |= DepthTextureMode.Depth;
            if (camera.CompareTag("MainCamera")) LastMainCameraRequestedDepth = (camera.depthTextureMode & DepthTextureMode.Depth) != 0;
        }

        private static void AfterCamera(Camera camera)
        {
            if (camera == null || !saved.TryGetValue(camera, out var original)) return;
            camera.depthTextureMode = original;
            if (savedSoftDepth.TryGetValue(camera, out var softDepth))
                Shader.SetGlobalFloat(skipSoftDepthId, softDepth);
            savedSoftDepth.Remove(camera); saved.Remove(camera);
        }
    }
}
