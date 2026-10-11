using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Every result is an owned mesh. Curved profiles carry the silhouette;
    // texture detail is secondary and never substitutes for physical volume.
    internal static class NaturalActorMeshFactory
    {
        internal static Vector4 Ring(float y, float x, float z, float shift = 0) { return new Vector4(y, x, z, shift); }

        internal static Mesh Loft(string name, Vector4[] profile, int sides = 28, float relief = .012f, string sculpt = null)
        {
            var rings = new List<Vector4>();
            for (int r = 0; r < profile.Length - 1; r++)
            {
                Vector4 a = profile[Mathf.Max(0, r - 1)], b = profile[r], c = profile[r + 1], d = profile[Mathf.Min(profile.Length - 1, r + 2)];
                for (int j = 0; j < 3; j++)
                {
                    float t = j / 3f;
                    Vector4 v = .5f * (2 * b + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t);
                    v.x = Mathf.Lerp(b.x, c.x, t); v.y = Mathf.Max(.002f, v.y); v.z = Mathf.Max(.002f, v.z); rings.Add(v);
                }
            }
            rings.Add(profile[profile.Length - 1]);
            int width = sides + 1, count = rings.Count * width;
            var vertices = new Vector3[count + 2]; var uv = new Vector2[vertices.Length]; var triangles = new List<int>();
            for (int r = 0; r < rings.Count; r++) for (int c = 0; c <= sides; c++)
            {
                Vector4 ring = rings[r]; float a = c * Mathf.PI * 2 / sides;
                float x = Mathf.Cos(a) * ring.y, z = Mathf.Sin(a) * ring.z + ring.w;
                float fold = relief * (Mathf.Sin(a * 7 + ring.x * 3.2f) * .52f + Mathf.Sin(a * 3 - ring.x * 5.1f) * .32f + Mathf.Sin(a * 11 + ring.x * 8) * .16f);
                fold *= Mathf.Clamp01(Mathf.Min(ring.y, ring.z) * 5);
                x += Mathf.Cos(a) * fold; z += Mathf.Sin(a) * fold;
                if (sculpt == "face" && Mathf.Sin(a) > 0)
                {
                    float cheek = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x) - .26f) * 8, 2) - Mathf.Pow((ring.x + .04f) * 8, 2));
                    float brow = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x) - .22f) * 8, 2) - Mathf.Pow((ring.x - .17f) * 12, 2));
                    float socket = Mathf.Exp(-Mathf.Pow((Mathf.Abs(x) - .21f) * 13, 2) - Mathf.Pow((ring.x - .075f) * 15, 2));
                    z += .038f * cheek + .032f * brow - .023f * socket;
                }
                if (sculpt == "gel")
                {
                    x += .018f * Mathf.Sin(a * 3 + ring.x * 4) * Mathf.Clamp01(1 - Mathf.Abs(ring.x) * 1.8f);
                    z += .020f * Mathf.Cos(a * 2 - ring.x * 5) * Mathf.Clamp01(1 - Mathf.Abs(ring.x) * 1.8f);
                }
                if (sculpt == "ear" || sculpt == "horn")
                { float t = Mathf.InverseLerp(profile[0].x, profile[profile.Length - 1].x, ring.x); x += (sculpt == "ear" ? .12f : .22f) * t * t; z -= .12f * t * t; }
                int i = r * width + c; vertices[i] = new Vector3(x, ring.x, z); uv[i] = new Vector2(c / (float)sides, r / (float)(rings.Count - 1));
                if (r < rings.Count - 1 && c < sides) triangles.AddRange(new[] { i, i + width, i + 1, i + 1, i + width, i + width + 1 });
            }
            vertices[count] = new Vector3(0, rings[0].x, rings[0].w); vertices[count + 1] = new Vector3(0, rings[rings.Count - 1].x, rings[rings.Count - 1].w);
            uv[count] = uv[count + 1] = new Vector2(.5f, .5f);
            for (int c = 0; c < sides; c++)
            {
                triangles.AddRange(new[] { count, c, c + 1 });
                int t = (rings.Count - 1) * width + c; triangles.AddRange(new[] { count + 1, t + 1, t });
            }
            Mesh mesh = Finish(name, vertices, uv, triangles);
            Vector3[] normals = mesh.normals;
            for (int r = 0; r < rings.Count; r++) { int a = r * width, b = a + sides; normals[a] = normals[b] = (normals[a] + normals[b]).normalized; }
            mesh.normals = normals; mesh.RecalculateTangents(); return mesh;
        }

        internal static Mesh Face(string name, bool broad = false)
        {
            return Loft(name, new[] { Ring(-.5f, .08f, .12f, .025f), Ring(-.38f, broad ? .32f : .23f, .31f, .04f), Ring(-.14f, .43f, .39f, .03f), Ring(.10f, .48f, .46f), Ring(.30f, .43f, .43f, -.01f), Ring(.44f, .29f, .30f, -.02f), Ring(.5f, .01f, .02f, -.02f) }, 32, .005f, "face");
        }

        internal static Mesh ClothTorso(string name, bool broad)
        {
            return Loft(name, new[] { Ring(-1, .42f, .42f), Ring(-.84f, .43f, .43f), Ring(-.46f, .34f, .38f), Ring(-.05f, .42f, .46f, .01f), Ring(.44f, .51f, .48f), Ring(.72f, .48f, .40f, -.025f), Ring(.87f, broad ? .40f : .36f, .34f, -.03f), Ring(1, .23f, .24f, -.025f) }, 32, broad ? .012f : .025f);
        }

        internal static Mesh Shoulder(string name)
        {
            // Low, broad saddle with a rolled lower edge; never a shoulder ball.
            return Loft(name, new[] { Ring(-.5f, .34f, .32f), Ring(-.44f, .45f, .45f), Ring(-.35f, .49f, .48f), Ring(-.17f, .50f, .47f), Ring(.18f, .48f, .42f, -.035f), Ring(.35f, .39f, .34f, -.04f), Ring(.43f, .25f, .23f, -.03f), Ring(.46f, .07f, .09f, -.03f) }, 28, .006f);
        }

        internal static Mesh FrontShell(string name, bool drape = false)
        {
            const int rows = 19, columns = 19; int plane = rows * columns;
            var v = new Vector3[plane * 2]; var uv = new Vector2[v.Length]; var tri = new List<int>();
            for (int r = 0; r < rows; r++) for (int c = 0; c < columns; c++)
            {
                float t = r / (float)(rows - 1), u = c / (float)(columns - 1) * 2 - 1;
                // Mathf.Sin(PI) is slightly negative in single precision. A
                // fractional power there made the final rim NaN, then fitting
                // its invalid bounds collapsed the entire shell's width.
                float arch = Mathf.Max(0, Mathf.Sin(t * Mathf.PI));
                float width = drape ? .45f + .04f * arch : .31f + .18f * Mathf.Pow(arch, .45f);
                float x = u * width, y = t - .5f;
                float z = .03f + (drape ? .22f : .40f) * (1 - u * u) * arch;
                if (drape) { z += Mathf.Sin(u * 12 + t * 3) * .040f * (1 - t); y += Mathf.Cos(u * 9) * .012f * (1 - t); }
                else z += .045f * Mathf.Exp(-u * u * 20) * arch;
                int i = r * columns + c; v[i] = new Vector3(x, y, z); v[i + plane] = v[i] - Vector3.forward * (drape ? .10f : .19f);
                uv[i] = uv[i + plane] = new Vector2(c / (float)(columns - 1), t);
                if (r < rows - 1 && c < columns - 1)
                {
                    int b = i + columns; tri.AddRange(new[] { i, i + 1, b, i + 1, b + 1, b, i + plane, b + plane, i + 1 + plane, i + 1 + plane, b + plane, b + 1 + plane });
                }
            }
            var edge = new List<int>(); for (int c = 0; c < columns; c++) edge.Add(c); for (int r = 1; r < rows; r++) edge.Add(r * columns + columns - 1); for (int c = columns - 2; c >= 0; c--) edge.Add((rows - 1) * columns + c); for (int r = rows - 2; r > 0; r--) edge.Add(r * columns);
            for (int i = 0; i < edge.Count; i++) { int a = edge[i], b = edge[(i + 1) % edge.Count]; tri.AddRange(new[] { a, a + plane, b, b, a + plane, b + plane }); }
            return Finish(name, v, uv, tri);
        }

        internal static Mesh Band(string name, bool bore = false)
        {
            const int sides = 36, width = sides + 1;
            var vertices = new Vector3[width * 4]; var uv = new Vector2[vertices.Length]; var triangles = new List<int>();
            for (int ring = 0; ring < 4; ring++) for (int c = 0; c <= sides; c++)
            {
                float a = c * Mathf.PI * 2 / sides, radius = ring < 2 ? .49f : bore ? .30f : .40f, h = ring % 2 == 0 ? -.5f : .5f;
                float x = Mathf.Cos(a) * radius, z = Mathf.Sin(a) * radius;
                Vector3 p = bore ? new Vector3(x, z, h) : new Vector3(x, h, z * .94f);
                int i = ring * width + c; vertices[i] = p; uv[i] = new Vector2(c / (float)sides, ring % 2);
                if (ring == 0 && c < sides)
                {
                    AddBandStrip(triangles, c, width + c, false, bore); AddBandStrip(triangles, 2 * width + c, 3 * width + c, true, bore);
                    AddBandStrip(triangles, width + c, 3 * width + c, false, bore); AddBandStrip(triangles, 2 * width + c, c, false, bore);
                }
            }
            return Finish(name, vertices, uv, triangles);
        }
        private static void AddBandStrip(List<int> tri, int a, int b, bool reverse, bool bore)
        { if (reverse != bore) tri.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); else tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }

        internal static Mesh Horn(string name, bool ear = false)
        { return Loft(name, new[] { Ring(-1, ear ? .29f : .40f, ear ? .14f : .38f), Ring(-.62f, ear ? .47f : .43f, ear ? .23f : .37f), Ring(-.13f, ear ? .38f : .32f, ear ? .19f : .29f), Ring(.46f, .19f, ear ? .10f : .17f), Ring(.85f, .075f, .055f), Ring(1, .006f, .006f) }, 24, .008f, ear ? "ear" : "horn"); }

        internal static Mesh Boot(string name, bool tall = false)
        { return Loft(name, new[] { Ring(tall ? -1 : -.5f, .42f, .49f, .01f), Ring(tall ? -.85f : -.37f, .49f, .50f), Ring(tall ? -.52f : -.13f, .45f, .42f, -.02f), Ring(.04f, .33f, .28f, -.15f), Ring(tall ? 1 : .5f, .30f, .25f, -.17f) }, 28, .008f); }

        internal static Mesh Hand(string name)
        {
            Mesh palm = Loft(name + " palm", new[] { Ring(-.29f, .40f, .24f), Ring(-.09f, .46f, .29f), Ring(.25f, .34f, .28f), Ring(.5f, .23f, .23f) }, 20, .004f);
            Mesh finger = Loft(name + " finger", new[] { Ring(-.24f, .018f, .025f), Ring(-.18f, .052f, .061f), Ring(-.03f, .059f, .062f), Ring(.04f, .05f, .06f) }, 12, .002f);
            var parts = new List<CombineInstance> { new CombineInstance { mesh = palm, transform = Matrix4x4.identity } };
            for (int i = 0; i < 4; i++) parts.Add(new CombineInstance { mesh = finger, transform = Matrix4x4.TRS(new Vector3(-.27f + .18f * i, -.26f + (i == 0 || i == 3 ? .045f : 0), .015f), Quaternion.Euler(-10, 0, (i - 1.5f) * -5), Vector3.one) });
            parts.Add(new CombineInstance { mesh = finger, transform = Matrix4x4.TRS(new Vector3(.38f, .045f, .10f), Quaternion.Euler(-26, 0, 48), new Vector3(1.1f, 1.1f, 1.1f)) });
            var result = new Mesh { name = name }; result.CombineMeshes(parts.ToArray(), true, true); result.RecalculateBounds(); Object.Destroy(palm); Object.Destroy(finger); return result;
        }

        internal static Mesh CurveTube(string name, Vector3[] path, float radius)
        {
            const int sides = 10; int width = sides + 1; var vertices = new Vector3[path.Length * width]; var uv = new Vector2[vertices.Length]; var triangles = new List<int>();
            for (int r = 0; r < path.Length; r++) for (int c = 0; c <= sides; c++)
            {
                Vector3 forward = r == path.Length - 1 ? path[r] - path[r - 1] : path[r + 1] - path[r];
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, forward.normalized); float a = c * Mathf.PI * 2 / sides;
                float taper = r == 0 || r == path.Length - 1 ? .35f : 1;
                int i = r * width + c; vertices[i] = path[r] + rotation * new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius * taper; uv[i] = new Vector2(c / (float)sides, r / (float)(path.Length - 1));
                if (r < path.Length - 1 && c < sides) triangles.AddRange(new[] { i, i + width, i + 1, i + 1, i + width, i + width + 1 });
            }
            for (int c = 1; c < sides - 1; c++) { triangles.AddRange(new[] { 0, c, c + 1 }); int b = (path.Length - 1) * width; triangles.AddRange(new[] { b, b + c + 1, b + c }); }
            return Finish(name, vertices, uv, triangles);
        }

        internal static void FitEnvelope(Mesh mesh, Bounds envelope)
        {
            Bounds current = mesh.bounds; Vector3 size = current.size, desired = envelope.size;
            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i] - current.center;
                p = new Vector3(size.x > .00001f ? p.x * desired.x / size.x : 0, size.y > .00001f ? p.y * desired.y / size.y : 0, size.z > .00001f ? p.z * desired.z / size.z : 0);
                vertices[i] = p + envelope.center;
            }
            mesh.vertices = vertices; mesh.RecalculateNormals(); WeldNormals(mesh); mesh.RecalculateTangents(); mesh.RecalculateBounds();
        }

        private static void WeldNormals(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices, normals = mesh.normals;
            var sums = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i]; var key = new Vector3Int(Mathf.RoundToInt(p.x * 100000), Mathf.RoundToInt(p.y * 100000), Mathf.RoundToInt(p.z * 100000));
                Vector3 sum; sums.TryGetValue(key, out sum); sums[key] = sum + normals[i];
            }
            for (int i = 0; i < vertices.Length; i++)
            { Vector3 p = vertices[i]; normals[i] = sums[new Vector3Int(Mathf.RoundToInt(p.x * 100000), Mathf.RoundToInt(p.y * 100000), Mathf.RoundToInt(p.z * 100000))].normalized; }
            mesh.normals = normals;
        }

        private static Mesh Finish(string name, Vector3[] vertices, Vector2[] uv, List<int> triangles)
        { var mesh = new Mesh { name = name, vertices = vertices, uv = uv, triangles = triangles.ToArray() }; mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh; }
    }
}
