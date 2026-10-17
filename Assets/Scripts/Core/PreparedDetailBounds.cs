using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Only immutable, explicitly decorative chunks use these loading-time bounds.
    // The render mesh, shader deformation, collision and draw distance are unchanged.
    internal static class PreparedDetailBounds
    {
        internal static bool Requested =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("EMBERFALL_PERF_REVIEW")) &&
            Environment.GetEnvironmentVariable("EMBERFALL_REVIEW_TIGHT_DETAIL_BOUNDS") == "1";

        internal static bool TryCalculate(IReadOnlyList<Vector3> vertices, IReadOnlyList<Color> colors,
            Transform transform, Material[] materials, out Bounds bounds)
        {
            bounds = default;
            if (vertices == null || vertices.Count == 0 || transform == null || materials == null || materials.Length == 0)
                return false;
            float minFollow = 0, maxFollow = 0, maxSway = 0;
            float minAlpha = 0, maxAlpha = 1;
            if (colors != null && colors.Count != 0)
            {
                if (colors.Count != vertices.Count) return false;
                foreach (var color in colors)
                {
                    if (!float.IsFinite(color.a)) return false;
                    minAlpha = Mathf.Min(minAlpha, color.a); maxAlpha = Mathf.Max(maxAlpha, color.a);
                }
            }
            foreach (var material in materials)
            {
                if (material == null || material.shader == null || material.shader.name != "Emberfall/Weathered World")
                    return false;
                float follow = material.GetFloat("_TerrainFollow"), wind = material.GetFloat("_Wind"), tint = material.GetFloat("_VertexTint");
                if (!float.IsFinite(follow) || !float.IsFinite(wind) || !float.IsFinite(tint)) return false;
                minFollow = Mathf.Min(minFollow, follow); maxFollow = Mathf.Max(maxFollow, follow);
                // The shader uses an unclamped lerp; bound even nonstandard vertex alpha.
                float sway = Mathf.Max(Mathf.Abs(1 + (minAlpha - 1) * tint), Mathf.Abs(1 + (maxAlpha - 1) * tint));
                maxSway = Mathf.Max(maxSway, Mathf.Abs(wind) * sway);
            }
            var toWorld = transform.localToWorldMatrix;
            var toLocal = transform.worldToLocalMatrix;
            bool first = true;
            foreach (var vertex in vertices)
            {
                var world = toWorld.MultiplyPoint3x4(vertex);
                float height = WorldTerrain.Height(world);
                var low = toLocal.MultiplyPoint3x4(world + Vector3.up * (height * minFollow));
                var high = toLocal.MultiplyPoint3x4(world + Vector3.up * (height * maxFollow));
                if (!Finite(low) || !Finite(high)) return false;
                if (first) { bounds = new Bounds(low, Vector3.zero); first = false; }
                else bounds.Encapsulate(low);
                bounds.Encapsulate(high);
            }
            // Analytic wind envelope plus a 5 mm world-space floating-point margin.
            // Transform vectors, rather than dividing by scale: rotated/scaled parents matter.
            Vector3 padding = Abs(toLocal.MultiplyVector(Vector3.right)) * (.035f * maxSway + .005f) +
                Abs(toLocal.MultiplyVector(Vector3.up)) * .005f +
                Abs(toLocal.MultiplyVector(Vector3.forward)) * (.02f * maxSway + .005f);
            bounds.Expand(padding * 2);
            return Finite(bounds.center) && Finite(bounds.size);
        }
        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        private static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }
}
