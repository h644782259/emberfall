using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // World render geometry only. The original transform, bounds, flat support
    // datum and traversal footprint remain authoritative; no colliders are added.
    internal static class NaturalSceneryMeshLibrary
    {
        internal enum Kind { None, Stone, Timber, Column, Log, Rock }
        internal struct Topology
        {
            internal Vector3[] Vertices;
            internal Vector2[] UV;
            internal int[] Triangles;
        }

        internal static void Apply(GameObject go, string name, PrimitiveType type, Vector3 scale, WorldResources resources)
        {
            if (go == null || resources == null) return;
            var filter = go.GetComponent<MeshFilter>(); var renderer = go.GetComponent<Renderer>();
            if (filter == null || filter.sharedMesh == null || renderer == null) return;
            // The Rock factory installs its original boulder after Primitive.
            // Its second explicit hook replaces that final mesh, exactly once.
            if (name == "Weathered rock" && filter.sharedMesh.name != "Emberfall weathered boulder") return;
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_EmissionColor"))
            {
                Color emission = renderer.sharedMaterial.GetColor("_EmissionColor");
                if (emission.r + emission.g + emission.b > .035f) return;
            }
            Kind kind = Classify(name, type, scale); if (kind == Kind.None) return;
            Topology data = Build(kind, scale, StableSeed(name));
            FitEnvelope(ref data, filter.sharedMesh.bounds);
            var mesh = new Mesh { name = "Natural closed scenery / " + name, vertices = data.Vertices, uv = data.UV, triangles = data.Triangles };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); resources.Own(mesh); filter.sharedMesh = mesh;
        }

        internal static Kind Classify(string name, PrimitiveType type, Vector3 scale)
        {
            if (string.IsNullOrEmpty(name)) return Kind.None;
            string n = name.ToLowerInvariant();
            if (n.StartsWith("npc ") || n.Contains("merchant") || n.Contains("smith") || n.Contains("anvil") || n.Contains("apron") || n.Contains("exchange") || n.Contains("chart") || n.Contains("potion") || n.Contains("provision stock")) return Kind.None;
            if (n.Contains("sign") || n.Contains("label") || n.Contains("marker") || n.Contains("emblem") || n.Contains("rune") || n.Contains("inset") || n.Contains("inlay") || n.Contains("glow") || n.Contains("lit doorway") || n.Contains("loot") || n.Contains("star") || n.Contains("seal") || n.Contains("reed")) return Kind.None;
            if (n.Contains("roof module") || n.Contains("projecting eave") || n.Contains("ridge cap") || n.Contains("chimney cap") || n.Contains("tent")) return Kind.None;
            // The bridge factory supplies an actual solid .048m board. Its
            // support plane stays fixed while only the exposed rim is rounded.
            if (type == PrimitiveType.Cube && name == "Timber crossing plank") return Kind.Timber;
            if ((type == PrimitiveType.Cube && scale.y <= .12f) || (type == PrimitiveType.Cylinder && scale.y <= .10f)) return Kind.None;
            if (n.Contains("rock") || n.Contains("pebble") || n.Contains("strata")) return type == PrimitiveType.Cube || type == PrimitiveType.Sphere ? Kind.Rock : Kind.None;
            if (type == PrimitiveType.Cylinder)
            {
                if (n.Contains("firewood") || n.Contains("log") || n.Contains("stump")) return Kind.Log;
                if (n.Contains("column") || n.Contains("pilaster") || n.Contains("pier") || n.Contains("capital") || n.Contains("band")) return Kind.Column;
                return Kind.None;
            }
            if (type != PrimitiveType.Cube) return Kind.None;
            if (n.Contains("timber") || n.Contains("wood") || n.Contains("coffer") || n.Contains("shelf") || n.Contains("storage") || n.Contains("mine perimeter support") || n.Contains("gantry") || n.Contains("hoist")) return Kind.Timber;
            if (n.Contains("wall") || n.Contains("lintel") || n.Contains("partition") || n.Contains("barricade") || n.Contains("barrier") || n.Contains("retaining block") || n.Contains("river low block") || n.Contains("river high block") || n.Contains("workshop") || n.Contains("arcade") || n.Contains("column base") || n.Contains("capital") || n.Contains("voussoir") || n.Contains("chimney") || n.Contains("step") || n.Contains("footing") || n.Contains("bench") || n.Contains("cover")) return Kind.Stone;
            return Kind.None;
        }

        internal static Topology Build(Kind kind, Vector3 scale, int seed)
        {
            if (kind == Kind.Column || kind == Kind.Log) return BuildColumn(kind == Kind.Log, seed);
            return BuildBox(kind == Kind.Timber, kind == Kind.Rock, scale, seed);
        }

        private static Topology BuildBox(bool timber, bool rock, Vector3 scale, int seed)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            var shared = new Dictionary<Vector3Int, int>();
            Vector3 size = new Vector3(Mathf.Max(.001f, Mathf.Abs(scale.x)), Mathf.Max(.001f, Mathf.Abs(scale.y)), Mathf.Max(.001f, Mathf.Abs(scale.z)));
            float minimum = Mathf.Min(size.x, Mathf.Min(size.y, size.z)), radius = Mathf.Min(timber ? .014f : .065f, minimum * (timber ? .12f : .11f));
            Vector3 bevel = new Vector3(radius / size.x, radius / size.y, radius / size.z);
            // Edge samples represent the actual curved radius even on a very
            // long beam, instead of interpolating one huge diagonal face.
            float[] xs = Samples(bevel.x), ys = Samples(bevel.y), zs = Samples(bevel.z);
            for (int face = 0; face < 6; face++)
            {
                int axis = face / 2, sign = face % 2 == 0 ? 1 : -1;
                float[] us = axis == 0 ? zs : xs, vs = axis == 1 ? zs : ys;
                int[,] ids = new int[vs.Length, us.Length];
                for (int row = 0; row < vs.Length; row++) for (int column = 0; column < us.Length; column++)
                {
                    float u = us[column], v = vs[row]; Vector3 p;
                    if (axis == 0) p = new Vector3(sign * .5f, v, u);
                    else if (axis == 1) p = new Vector3(u, sign * .5f, v);
                    else p = new Vector3(u, v, sign * .5f);
                    Vector3 q = rock ? NaturalBoulder(p, seed) : RoundedWornBox(p, size, bevel, radius, timber, seed);
                    var key = new Vector3Int(Mathf.RoundToInt(p.x * 1000000), Mathf.RoundToInt(p.y * 1000000), Mathf.RoundToInt(p.z * 1000000));
                    int id;
                    if (!shared.TryGetValue(key, out id)) { id = vertices.Count; shared.Add(key, id); vertices.Add(q); uv.Add(new Vector2(u + .5f, v + .5f)); }
                    ids[row, column] = id;
                }
                // u/v orientation is explicit: x faces are z/y, y faces x/z,
                // z faces x/y. Every neighbouring face shares the same indices.
                bool reverse = axis == 2 ? sign < 0 : sign > 0;
                for (int row = 0; row < vs.Length - 1; row++) for (int column = 0; column < us.Length - 1; column++)
                {
                    int a = ids[row, column], b = ids[row, column + 1], c = ids[row + 1, column], d = ids[row + 1, column + 1];
                    if (reverse) triangles.AddRange(new[] { a, c, b, b, c, d }); else triangles.AddRange(new[] { a, b, c, b, d, c });
                }
            }
            var data = new Topology { Vertices = vertices.ToArray(), UV = uv.ToArray(), Triangles = triangles.ToArray() };
            FitEnvelope(ref data, new Bounds(Vector3.zero, Vector3.one)); return data;
        }

        private static float[] Samples(float bevel)
        { float b = Mathf.Clamp(bevel, .001f, .13f); return new[] { -.5f, -.5f + b * .2928932f, -.5f + b, -.23f, 0, .23f, .5f - b, .5f - b * .2928932f, .5f }; }

        private static Vector3 RoundedWornBox(Vector3 p, Vector3 size, Vector3 bevel, float radius, bool timber, int seed)
        {
            Vector3 inner = new Vector3(Mathf.Clamp(p.x, -.5f + bevel.x, .5f - bevel.x), Mathf.Clamp(p.y, -.5f + bevel.y, .5f - bevel.y), Mathf.Clamp(p.z, -.5f + bevel.z, .5f - bevel.z));
            Vector3 delta = Vector3.Scale(p - inner, size);
            Vector3 rounded = inner + new Vector3(delta.normalized.x * radius / size.x, delta.normalized.y * radius / size.y, delta.normalized.z * radius / size.z);
            // Upper and lower support planes remain exactly planar. Side
            // erosion is shallow, deterministic, and wholly inside the box.
            if (p.y >= .5f - .00001f || p.y <= -.5f + .00001f) return rounded;
            Vector3 world = Vector3.Scale(p, size); float phase = seed * .00073f;
            float grain = .5f + .5f * Mathf.Sin(world.y * 17.1f + world.x * 2.7f + world.z * 6.3f + phase);
            float cut = timber ? (.25f + .75f * grain) * .0035f : (.30f + .70f * grain) * .007f;
            if (!timber) cut += Mathf.Pow(.5f + .5f * Mathf.Cos(world.y * 14.3f + phase), 18) * .006f;
            float side = Mathf.Clamp01((Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.z)) - .35f) / .15f);
            rounded.x *= 1 - cut * side / Mathf.Max(.02f, size.x); rounded.z *= 1 - cut * side / Mathf.Max(.02f, size.z); return rounded;
        }

        private static Vector3 NaturalBoulder(Vector3 p, int seed)
        {
            Vector3 d = p.normalized; float phase = seed * .00037f;
            float swell = .91f + .055f * Mathf.Sin(d.x * 5.3f + d.z * 3.7f + phase) + .035f * Mathf.Sin(d.y * 7.9f - d.z * 5.2f - phase);
            Vector3 v = d * (.5f * swell);
            v.x += .012f * Mathf.Sin(d.y * 5 + d.z * 3); v.z += .011f * Mathf.Sin(d.x * 4 - d.y * 3);
            // Continuous low-frequency erosion preserves a solid volume with
            // irregular shoulders, rather than exposed triangular spikes.
            return v;
        }

        private static Topology BuildColumn(bool timber, int seed)
        {
            const int sides = 32; float[] heights = { -1, -.985f, -.95f, -.88f, -.63f, -.38f, -.13f, .13f, .38f, .63f, .88f, .95f, .985f, 1 };
            var vertices = new Vector3[heights.Length * sides + 2]; var uv = new Vector2[vertices.Length]; var triangles = new List<int>();
            for (int row = 0; row < heights.Length; row++) for (int c = 0; c < sides; c++)
            {
                float a = c * Mathf.PI * 2 / sides, y = heights[row];
                float edge = Mathf.Clamp01((1 - Mathf.Abs(y)) / .05f);
                float flute = timber ? Mathf.Sin(a * 7 + y * 3 + seed * .00013f) * .005f : -.009f * (.5f + .5f * Mathf.Cos(a * 12));
                float radius = .474f + edge * .020f + flute * edge;
                int i = row * sides + c; vertices[i] = new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius); uv[i] = new Vector2(c / (float)sides, (y + 1) * .5f);
                if (row < heights.Length - 1)
                { int next = row * sides + (c + 1) % sides, above = i + sides, aboveNext = next + sides; triangles.AddRange(new[] { i, above, next, next, above, aboveNext }); }
            }
            int bottom = vertices.Length - 2, top = vertices.Length - 1;
            vertices[bottom] = new Vector3(0, -1, 0); vertices[top] = new Vector3(0, 1, 0); uv[bottom] = uv[top] = new Vector2(.5f, .5f);
            for (int c = 0; c < sides; c++) { int next = (c + 1) % sides, offset = (heights.Length - 1) * sides; triangles.AddRange(new[] { bottom, c, next, top, offset + next, offset + c }); }
            var data = new Topology { Vertices = vertices, UV = uv, Triangles = triangles.ToArray() }; FitEnvelope(ref data, new Bounds(Vector3.zero, new Vector3(1, 2, 1))); return data;
        }

        internal static void FitEnvelope(ref Topology data, Bounds envelope)
        {
            Vector3 low = data.Vertices[0], high = low;
            foreach (Vector3 v in data.Vertices) { low = Vector3.Min(low, v); high = Vector3.Max(high, v); }
            Vector3 centre = (low + high) * .5f, size = high - low, target = envelope.size;
            for (int i = 0; i < data.Vertices.Length; i++)
            { Vector3 p = data.Vertices[i] - centre; data.Vertices[i] = new Vector3(p.x * target.x / size.x, p.y * target.y / size.y, p.z * target.z / size.z) + envelope.center; }
        }

        private static int StableSeed(string name)
        { unchecked { int value = 173; foreach (char c in name) value = value * 31 + c; return value & 65535; } }
    }
}
