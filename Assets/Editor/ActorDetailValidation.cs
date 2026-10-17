using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Emberfall.Editor
{
    public static class ActorDetailValidation
    {
        public static void Run()
        {
            var type = typeof(CombatModel).Assembly.GetType("Emberfall.NaturalActorMeshFactory");
            foreach (string name in new[] { "FrontShell", "Band" })
            foreach (bool variant in new[] { false, true })
            {
                var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);
                var original = (Mesh)method.Invoke(null, new object[] { "Original", variant, false });
                var mobile = (Mesh)method.Invoke(null, new object[] { "Mobile", variant, true });
                try
                {
                    Validate(original); Validate(mobile);
                    bool preserve = name == "FrontShell" && variant;
                    if (preserve ? mobile.triangles.Length != original.triangles.Length : mobile.triangles.Length >= original.triangles.Length)
                        throw new Exception("Unexpected detail budget: " + name);
                    if ((original.bounds.center - mobile.bounds.center).magnitude > .005f ||
                        (original.bounds.size - mobile.bounds.size).magnitude > .005f)
                        throw new Exception("Silhouette envelope changed: " + name);
                    Debug.Log($"ACTOR DETAIL PASS {name} variant={variant}: {original.triangles.Length / 3} -> {mobile.triangles.Length / 3} triangles");
                }
                finally { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(mobile); }
            }
        }
        private static void Validate(Mesh mesh)
        {
            var vertices = mesh.vertices;
            foreach (var vertex in vertices)
                if (!float.IsFinite(vertex.x) || !float.IsFinite(vertex.y) || !float.IsFinite(vertex.z))
                    throw new Exception("Nonfinite geometry");
            foreach (var normal in mesh.normals)
                if (!float.IsFinite(normal.sqrMagnitude) || normal.sqrMagnitude < .5f)
                    throw new Exception("Invalid normal");
            var indices = mesh.triangles;
            for (int i = 0; i < indices.Length; i += 3)
                if (Vector3.Cross(vertices[indices[i + 1]] - vertices[indices[i]], vertices[indices[i + 2]] - vertices[indices[i]]).sqrMagnitude < 1e-16f)
                    throw new Exception("Degenerate triangle");
        }
    }
}
