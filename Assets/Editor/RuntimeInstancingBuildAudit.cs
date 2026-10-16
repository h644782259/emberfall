using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall.Editor
{
    // Observe the variants handed to the compiler, after other custom preprocessors.
    // This audit does not add/remove variants or change runtime rendering.
    public sealed class RuntimeInstancingBuildAudit : IPreprocessShaders
    {
        public int callbackOrder => int.MaxValue;
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if(shader.name!="Emberfall/Weathered World" && shader.name!="Emberfall/Actor Surface")return;
            var keyword = new ShaderKeyword("INSTANCING_ON");
            int instanced=0;
            foreach(var variant in data)if(variant.shaderKeywordSet.IsEnabled(keyword))instanced++;
            Debug.Log("Runtime instancing audit: " + shader.name + " / " + snippet.passName + " / " + snippet.shaderType + ": " + instanced + " instanced / " + data.Count + " variants");
        }
    }
}
