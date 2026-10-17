using System;
using System.Reflection;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class DecorationDepthValidation
    {
        public static void RunAndExport() { Run(); IOSBuild.Export(); }
        public static void Run()
        {
            var type = typeof(PreparedWorldDetailBudget);
            var chunkType = type.GetNestedType("Chunk", BindingFlags.NonPublic);
            var apply = type.GetMethod("ApplyMaterialVariant", BindingFlags.NonPublic | BindingFlags.Static);
            var force = type.GetMethod("SetForceFadeVariant", BindingFlags.NonPublic | BindingFlags.Static);
            var root = new GameObject("Decoration depth validation");
            var opaque = new Material(Shader.Find("Emberfall/Weathered World"));
            var fade = new Material(opaque); fade.EnableKeyword("EMBER_WORLD_FADE");
            try
            {
                var renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = opaque;
                var chunk = Activator.CreateInstance(chunkType);
                chunkType.GetField("Renderer").SetValue(chunk, renderer);
                chunkType.GetField("OpaqueMaterials").SetValue(chunk, new[] { opaque });
                chunkType.GetField("FadeMaterials").SetValue(chunk, new[] { fade });
                foreach (float alpha in new[] { 1f, .75f, .25f, 0f, .5f, 1f })
                {
                    chunkType.GetField("Alpha").SetValue(chunk, alpha); apply.Invoke(null, new[] { chunk });
                    if (renderer.sharedMaterial != (alpha < 1 ? fade : opaque)) throw new Exception("Wrong fade transition");
                }
                force.Invoke(null, new object[] { true }); apply.Invoke(null, new[] { chunk });
                if (renderer.sharedMaterial != fade) throw new Exception("A/B baseline not restored");
                force.Invoke(null, new object[] { false }); apply.Invoke(null, new[] { chunk });
                if (renderer.sharedMaterial != opaque || opaque.IsKeywordEnabled("EMBER_WORLD_FADE")) throw new Exception("Opaque material polluted");
                Debug.Log("DECORATION DEPTH PASS: opaque/fade transitions, A/B restoration, shared material isolation");
            }
            finally
            {
                force.Invoke(null, new object[] { false });
                UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(opaque); UnityEngine.Object.DestroyImmediate(fade);
            }
        }
    }
}
