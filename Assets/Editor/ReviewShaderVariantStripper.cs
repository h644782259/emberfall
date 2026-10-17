using System;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall.Editor
{
    // Legacy A/B branches are never used by ordinary gameplay. Keep them only
    // when explicitly building a diagnostic player; preserve all production variants.
    public sealed class ReviewShaderVariantStripper : IPreprocessShaders
    {
        public int callbackOrder => 0;
        internal static bool IncludeReviews => Array.IndexOf(Environment.GetCommandLineArgs(), "-emberfallIncludeShaderReviews") >= 0;
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (IncludeReviews || shader.name != "Emberfall/Weathered World") return;
            var terrain = new ShaderKeyword("EMBER_TERRAIN_LEGACY");
            var clip = new ShaderKeyword("EMBER_WORLD_LEGACY_CLIP");
            for (int i = data.Count - 1; i >= 0; i--)
                if (data[i].shaderKeywordSet.IsEnabled(terrain) || data[i].shaderKeywordSet.IsEnabled(clip))
                    data.RemoveAt(i);
        }
    }
}
