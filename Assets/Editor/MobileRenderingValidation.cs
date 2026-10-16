using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    // Explicit native render check; does not open or save a gameplay scene.
    public static class MobileRenderingValidation
    {
        public static void Run()
        {
            const int layer = 30;
            string directory = Path.Combine(Path.GetTempPath(), "emberfall-render-efficiency");
            Directory.CreateDirectory(directory);
            var root = new GameObject("Isolated mobile rendering validation");
            var target = new RenderTexture(640, 640, 24);
            var pixels = new Texture2D(640, 640, TextureFormat.RGB24, false);
            try
            {
                var resources = root.AddComponent<WorldResources>();
                var type = typeof(WorldResources).Assembly.GetType("Emberfall.NaturalSceneryMeshLibrary");
                var apply = type.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic);
                Mesh first = null;
                for (int i = 0; i < 200; i++)
                {
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.transform.SetParent(root.transform, false);
                    apply.Invoke(null, new object[] { block, "Weathered courtyard wall", PrimitiveType.Cube, new Vector3(3, 2, .8f), resources });
                    var mesh = block.GetComponent<MeshFilter>().sharedMesh;
                    if (first == null) first = mesh;
                    else if (mesh != first) throw new Exception("Identical scenery did not reuse its native Mesh.");
                    block.SetActive(false);
                }
                var create = typeof(WorldPortal3DVisual).GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
                var portal = (WorldPortal3DVisual)create.Invoke(null, new object[] { root.transform, resources, Vector3.zero, 2f, true, "Validation portal", -1f });
                foreach (Transform child in portal.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
                var camera = new GameObject("Validation camera").AddComponent<Camera>();
                camera.transform.SetParent(root.transform, false);
                camera.transform.position = new Vector3(0, 2, -8);
                camera.transform.LookAt(new Vector3(0, 2, 0));
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.12f, .14f, .18f);
                camera.cullingMask = 1 << layer;
                camera.targetTexture = target;
                Material medium = null;
                foreach (var renderer in portal.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.sharedMaterial.shader.name == "Emberfall/Deep Travel Portal") medium = renderer.sharedMaterial;
                if (medium == null) throw new Exception("Missing portal volume.");
                var noiseType = typeof(WorldResources).Assembly.GetType("Emberfall.SkillFlowVolume");
                var noise = (Texture)noiseType.GetProperty("FlowNoise", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
                medium.SetTexture("_FlowNoise", noise);
                for (int variant = 0; variant < 2; variant++)
                {
                    if (variant == 0) medium.DisableKeyword("EMBER_PORTAL_TEXTURE_NOISE");
                    else medium.EnableKeyword("EMBER_PORTAL_TEXTURE_NOISE");
                    camera.Render();
                    var previous = RenderTexture.active;
                    try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 640, 640), 0, 0); pixels.Apply(); }
                    finally { RenderTexture.active = previous; }
                    File.WriteAllBytes(Path.Combine(directory, variant == 0 ? "portal-arithmetic.png" : "portal-texture.png"), pixels.EncodeToPNG());
                }
                foreach (var message in ShaderUtil.GetShaderMessages(medium.shader))
                    if (message.severity.ToString() == "Error") throw new Exception(message.message);
                File.WriteAllText(Path.Combine(directory, "result.txt"), "PASS: 200 identical native scenery objects share one mesh. Both portal shader variants rendered without shader errors. Screenshots require visual review; no device thermal claim.\n");
                Debug.Log("Mobile rendering validation passed: " + directory);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
