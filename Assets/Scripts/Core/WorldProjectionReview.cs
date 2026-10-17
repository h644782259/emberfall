using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Explicit A/B override; ordinary launches use the shader platform default.
    internal static class WorldProjectionReview
    {
        private static readonly Dictionary<Material,float> saved = new Dictionary<Material,float>();
        internal static int Apply(bool enabled)
        {
            foreach (var material in Resources.FindObjectsOfTypeAll<Material>())
                if (material != null && material.shader != null && material.shader.name == "Emberfall/Weathered World" && material.HasProperty("_ProjectionFloor"))
                {
                    if (!saved.ContainsKey(material)) saved.Add(material,material.GetFloat("_ProjectionFloor"));
                    material.SetFloat("_ProjectionFloor",enabled?.003f:0);
                }
            return saved.Count;
        }
        internal static int ActiveMaterialCount => saved.Count;
        internal static void Restore()
        {
            foreach(var pair in saved) if(pair.Key!=null)pair.Key.SetFloat("_ProjectionFloor",pair.Value);
            saved.Clear();
        }
    }
}
