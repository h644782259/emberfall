using System;
using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Only immutable authored enemy surfaces are shared. Bones, bind poses,
    // materials and actor transforms remain owned by each individual rig.
    internal static class EnemyNaturalMeshCache
    {
        private static readonly Dictionary<string, Mesh> sources = new Dictionary<string, Mesh>();
        private static readonly Dictionary<(Mesh, Bounds), Mesh> fitted = new Dictionary<(Mesh, Bounds), Mesh>();
        private static readonly HashSet<Mesh> shared = new HashSet<Mesh>();
#if EMBERFALL_VISUAL_VALIDATION
        internal static bool Enabled = true;
#endif
        internal static Mesh Get(string key, Func<Mesh> create)
        {
#if EMBERFALL_VISUAL_VALIDATION
            if (!Enabled) return create();
#endif
            if (!sources.TryGetValue(key, out Mesh mesh))
            { using (PerformanceDiagnostics.Measure(PerformanceDiagnostics.Section.EnemyMesh)) mesh = create(); sources.Add(key, mesh); shared.Add(mesh); }
            return mesh;
        }
        internal static bool Contains(Mesh mesh) => shared.Contains(mesh);
        internal static Mesh Fit(Mesh source, Bounds bounds)
        {
            var key = (source, bounds);
            if (!fitted.TryGetValue(key, out Mesh mesh))
            {
                mesh = UnityEngine.Object.Instantiate(source);
                NaturalActorMeshFactory.FitEnvelope(mesh, bounds);
                fitted.Add(key, mesh); shared.Add(mesh);
            }
            return mesh;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            foreach (Mesh mesh in shared) if (mesh != null) UnityEngine.Object.Destroy(mesh);
            sources.Clear(); fitted.Clear(); shared.Clear();
        }
    }
}
