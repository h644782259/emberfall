using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    // Checks the optional review path with native Unity materials and cameras.
    public static class SoftDepthReviewValidation
    {
        public static void RunAll()
        { MobileSceneDepthValidation.Run(); Run(); RunProfileSwitching(); SceneDepthRenderValidation.Run(); }

        public static void Run()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var type = typeof(GameSession).Assembly.GetType("Emberfall.MobileSceneDepth");
            var mode = type.GetField("<SkipSoftDepthForReview>k__BackingField", flags);
            var reads = type.GetMethod("ReadsDepth", flags);
            var before = type.GetMethod("BeforeCamera", flags);
            var after = type.GetMethod("AfterCamera", flags);
            var register = type.GetMethod("Register", flags);
            bool previousMode = (bool)mode.GetValue(null);
            float previousGlobal = Shader.GetGlobalFloat("_EmberSkipSoftDepth");
            var cameraObject = new GameObject("Soft depth validation camera");
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
            camera.transform.position = new Vector3(0, 0, -5);
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer = body.GetComponent<Renderer>();
            register.Invoke(null, new object[] { renderer });
            int checks = 0;
            try
            {
                foreach (string name in new[] { "FilledSpell", "SpellEnergySurface", "SwordWave", "GroundFlameTrail", "SkillFlowVolume" })
                {
                    var shader = Resources.Load<Shader>(name);
                    if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                        throw new Exception("Shader failed: " + name);
                    var material = new Material(shader);
                    renderer.sharedMaterial = material;
                    try
                    {
                        foreach (bool skip in new[] { false, true })
                        {
                            mode.SetValue(null, skip);
                            bool expected = !skip || name == "GroundFlameTrail" || name == "SkillFlowVolume";
                            if ((bool)reads.Invoke(null, new object[] { material }) != expected)
                                throw new Exception("Depth consumer policy: " + name);
                            camera.depthTextureMode = DepthTextureMode.None;
                            before.Invoke(null, new object[] { camera });
                            if (((camera.depthTextureMode & DepthTextureMode.Depth) != 0) != expected ||
                                Shader.GetGlobalFloat("_EmberSkipSoftDepth") != (skip ? 1 : 0))
                                throw new Exception("Camera depth/global policy: " + name);
                            after.Invoke(null, new object[] { camera });
                            if (camera.depthTextureMode != DepthTextureMode.None ||
                                Shader.GetGlobalFloat("_EmberSkipSoftDepth") != previousGlobal)
                                throw new Exception("Camera state not restored");
                            // Independent depth requests must survive both review modes.
                            camera.depthTextureMode = DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
                            before.Invoke(null, new object[] { camera });
                            after.Invoke(null, new object[] { camera });
                            if (camera.depthTextureMode != (DepthTextureMode.Depth | DepthTextureMode.MotionVectors))
                                throw new Exception("External depth request lost");
                            checks++;
                        }
                        if (name != "GroundFlameTrail" && name != "SkillFlowVolume")
                            ValidateOpaqueOcclusion(camera, renderer, material, name);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(material); }
                }
                Debug.Log("SOFT DEPTH REVIEW PASS: " + checks + " native material/camera cases; five shaders supported. Thermal benefit unverified.");
            }
            finally
            {
                after.Invoke(null, new object[] { camera });
                mode.SetValue(null, previousMode);
                Shader.SetGlobalFloat("_EmberSkipSoftDepth", previousGlobal);
                UnityEngine.Object.DestroyImmediate(body);
                UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }
        private static void RunProfileSwitching()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var depth = typeof(GameSession).Assembly.GetType("Emberfall.MobileSceneDepth");
            var reads = depth.GetMethod("ReadsDepth", flags);
            var current = typeof(GraphicsQualityRuntime).GetField("current", flags);
            var original = current.GetValue(null);
            var effect = new Material(Resources.Load<Shader>("FilledSpell"));
            var volume = new Material(Resources.Load<Shader>("SkillFlowVolume"));
            try
            {
                foreach (bool mobile in new[] { true, false })
                foreach (var mode in new[] { GraphicsQualityMode.High, GraphicsQualityMode.Smooth, GraphicsQualityMode.Balanced, GraphicsQualityMode.Smooth })
                {
                    current.SetValue(null, GraphicsQualityPolicy.Resolve(mode, new GraphicsDeviceCapabilities(mobile, 4096, 0, 6, 50)));
                    bool expected = !mobile || mode != GraphicsQualityMode.Smooth;
                    if ((bool)reads.Invoke(null, new object[] { effect }) != expected ||
                        !(bool)reads.Invoke(null, new object[] { volume }))
                        throw new Exception("Runtime quality transition lost effect/volume depth policy: " + mode);
                }
                Debug.Log("SOFT DEPTH PROFILE PASS: mobile and desktop quality switches update immediately; volume depth retained");
            }
            finally
            {
                current.SetValue(null, original);
                UnityEngine.Object.DestroyImmediate(effect); UnityEngine.Object.DestroyImmediate(volume);
            }
        }
        private static void ValidateOpaqueOcclusion(Camera camera, Renderer renderer, Material material, string name)
        {
            var target = new RenderTexture(128, 128, 24);
            var pixels = new Texture2D(128, 128, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var oldGlobal = Shader.GetGlobalFloat("_EmberSkipSoftDepth");
            var oldDepth = Shader.GetGlobalTexture("_CameraDepthTexture");
            var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.transform.position = new Vector3(0, 0, -2);
            blocker.transform.localScale = Vector3.one * 2;
            var opaque = new Material(Shader.Find("Unlit/Color")) { color = Color.green };
            blocker.GetComponent<Renderer>().sharedMaterial = opaque;
            camera.targetTexture = target; camera.depthTextureMode = DepthTextureMode.None;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            material.SetFloat("_Opacity", 1);
            if (material.HasProperty("_Heat")) material.SetFloat("_Heat", 0);
            try
            {
                Shader.SetGlobalFloat("_EmberSkipSoftDepth", 1);
                blocker.SetActive(false);
                renderer.enabled = false; var empty = Capture(camera, pixels);
                renderer.enabled = true;
                Shader.SetGlobalTexture("_CameraDepthTexture", Texture2D.blackTexture);
                var black = Capture(camera, pixels);
                Shader.SetGlobalTexture("_CameraDepthTexture", Texture2D.whiteTexture);
                var white = Capture(camera, pixels);
                if (Difference(black, empty) < .00001) throw new Exception("Review effect invisible: " + name);
                if (Difference(black, white) > .00001) throw new Exception("Review sampled stale scene depth: " + name);
                blocker.SetActive(true); var behind = Capture(camera, pixels);
                renderer.enabled = false; var opaqueOnly = Capture(camera, pixels);
                if (Difference(behind, opaqueOnly) > .00001) throw new Exception("Effect visible through opaque wall: " + name);
                Debug.Log("SOFT DEPTH RENDER PASS " + name + ": visible without scene texture; opaque occlusion retained");
            }
            finally
            {
                renderer.enabled = true; camera.targetTexture = null;
                Shader.SetGlobalFloat("_EmberSkipSoftDepth", oldGlobal);
                Shader.SetGlobalTexture("_CameraDepthTexture", oldDepth);
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(blocker); UnityEngine.Object.DestroyImmediate(opaque);
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
        private static Color32[] Capture(Camera camera, Texture2D pixels)
        {
            camera.Render(); RenderTexture.active = camera.targetTexture;
            pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply(); return pixels.GetPixels32();
        }
        private static double Difference(Color32[] a, Color32[] b)
        {
            long sum = 0;
            for (int i = 0; i < a.Length; i++) sum += Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b);
            return sum / (a.Length * 3.0 * 255);
        }
    }
}
