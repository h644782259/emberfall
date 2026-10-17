using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Emberfall.Editor
{
    // Strip only unused Deferred passes, never lighting/shadow/instancing keywords.
    public sealed class ForwardShaderBuildPolicy : IPreprocessBuildWithReport, IProcessSceneWithReport, IPreprocessShaders
    {
        private static bool retainDeferred = true;
        public int callbackOrder => 10;
        public void OnPreprocessBuild(BuildReport report)
        {
            retainDeferred = false;
            var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            foreach (var tier in new[] { GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3 })
            {
                var path = EditorGraphicsSettings.GetTierSettings(group, tier).renderingPath;
                if (path != RenderingPath.Forward && path != RenderingPath.VertexLit) retainDeferred = true;
            }
        }
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera.renderingPath != RenderingPath.UsePlayerSettings && camera.renderingPath != RenderingPath.Forward && camera.renderingPath != RenderingPath.VertexLit)
                        retainDeferred = true;
        }
        internal static bool ShouldStrip(string shader, PassType pass) => !retainDeferred && pass == PassType.Deferred &&
            (shader == "Emberfall/Weathered World" || shader == "Emberfall/Actor Surface");
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (!ShouldStrip(shader.name, snippet.passType)) return;
            int removed = data.Count;
            data.Clear();
            if (removed > 0) Debug.Log($"Forward-only build: removed {removed} unused Deferred {snippet.shaderType} variants from {shader.name}");
        }
    }
}
