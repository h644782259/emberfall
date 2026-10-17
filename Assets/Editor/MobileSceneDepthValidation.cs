using System;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class MobileSceneDepthValidation
    {
        public static void RunAndExport() { Run(); IOSBuild.Export(); }
        public static void Run()
        {
            var type = typeof(GameSession).Assembly.GetType("Emberfall.MobileSceneDepth");
            const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
            var register = type.GetMethod("Register", flags);
            var before = type.GetMethod("BeforeCamera", flags);
            var after = type.GetMethod("AfterCamera", flags);
            var reset = type.GetMethod("Reset", flags);
            var force = type.GetField("ForceAlwaysForReview", flags);
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "Depth request validation";
            var camera = new GameObject("Depth request camera").AddComponent<Camera>();
            var second = new GameObject("Independent camera").AddComponent<Camera>();
            var materials = new System.Collections.Generic.List<Material>();
            int assertions = 0;
            Action<bool, string> check = (condition, reason) => { assertions++; if (!condition) throw new Exception(reason); };
            Action<bool, DepthTextureMode> render = (expected, original) =>
            {
                camera.depthTextureMode = original;
                var effectiveOriginal = camera.depthTextureMode;
                before.Invoke(null, new object[] { camera });
                check(((camera.depthTextureMode & DepthTextureMode.Depth) != 0) == expected, "Wrong depth request");
                check((camera.depthTextureMode & original) == original, "Lost externally requested camera flags");
                after.Invoke(null, new object[] { camera });
                check(camera.depthTextureMode == effectiveOriginal, "Depth request leaked into next frame: " + camera.depthTextureMode + " instead of " + effectiveOriginal);
            };
            try
            {
                reset.Invoke(null, null);
                camera.transform.position = new Vector3(0, 0, -5); camera.nearClipPlane = .1f; camera.farClipPlane = 20;
                var renderer = root.GetComponent<MeshRenderer>();
                register.Invoke(null, new object[] { renderer }); register.Invoke(null, new object[] { renderer });
                foreach (var path in new[] { "FilledSpell", "SpellEnergySurface", "SwordWave", "GroundFlameTrail", "SkillFlowVolume" })
                {
                    var shader = Resources.Load<Shader>(path); check(shader != null, "Missing shader " + path);
                    var material = new Material(shader); materials.Add(material); renderer.sharedMaterial = material;
                    render(true, DepthTextureMode.None);
                    renderer.enabled = false; render(false, DepthTextureMode.None); renderer.enabled = true;
                    root.SetActive(false); render(false, DepthTextureMode.None); root.SetActive(true);
                    renderer.forceRenderingOff = true; render(false, DepthTextureMode.None); renderer.forceRenderingOff = false;
                    root.transform.position = new Vector3(100, 0, 0); render(false, DepthTextureMode.None); root.transform.position = Vector3.zero;
                    camera.cullingMask = 0; render(false, DepthTextureMode.None); camera.cullingMask = -1;
                    render(true, DepthTextureMode.MotionVectors);
                    render(true, DepthTextureMode.Depth | DepthTextureMode.DepthNormals);
                }
                renderer.enabled = false;
                render(true, DepthTextureMode.Depth); // External requests are never removed.
                render(false, DepthTextureMode.DepthNormals);
                force.SetValue(null, true); render(true, DepthTextureMode.None); force.SetValue(null, false);
                renderer.enabled = true;
                var opaque = new Material(Shader.Find("Standard")); materials.Add(opaque); renderer.sharedMaterial = opaque;
                render(false, DepthTextureMode.None); // Pooled renderer switched to opaque material.
                renderer.sharedMaterial = materials[0]; render(true, DepthTextureMode.None);
                before.Invoke(null, new object[] { camera });
                second.cullingMask = 0; before.Invoke(null, new object[] { second });
                check(second.depthTextureMode == DepthTextureMode.None, "Camera requests leaked between cameras");
                after.Invoke(null, new object[] { second });
                check(camera.depthTextureMode == DepthTextureMode.Depth, "Other camera released active request");
                after.Invoke(null, new object[] { camera });
                UnityEngine.Object.DestroyImmediate(root); render(false, DepthTextureMode.None);
                Debug.Log("MOBILE SCENE DEPTH PASS: " + assertions + " assertions; all five shaders, pooling, camera masks/frustum, disabled/destroyed consumers, external flags and independent cameras");
            }
            finally
            {
                reset.Invoke(null, null);
                if (root != null) UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(camera.gameObject); UnityEngine.Object.DestroyImmediate(second.gameObject);
                foreach (var material in materials) UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
