using UnityEngine;

namespace Emberfall
{
    // Shared textured stone and a smooth, asymmetric wet gel silhouette.
    // Bodies are visual only; controller contact and traversal own all damage.
    internal static class EnemySkillPhysicalVfx
    {
        private static Mesh gel;
        private static readonly Mesh[] crusts = new Mesh[3];
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (gel != null) Object.Destroy(gel); gel = null;
            for (int i = 0; i < crusts.Length; i++) { if (crusts[i] != null) Object.Destroy(crusts[i]); crusts[i] = null; }
        }

        internal static Transform Body(Transform parent, string title, Material material, bool slime)
        {
            int variant = parent.childCount % crusts.Length;
            var part = new GameObject(title); part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = slime ? GelMesh : CrustMesh(variant);
            var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; renderer.receiveShadows = true;
            return part.transform;
        }

        internal static Mesh GelMesh
        {
            get
            {
                if (gel != null) return gel;
                gel = NaturalBody(true, 31); gel.name = "Folded wet clay and viscous slime mass";
                return gel;
            }
        }

        internal static Mesh CrustMesh(int variant)
        {
            variant = Mathf.Abs(variant) % crusts.Length;
            if (crusts[variant] == null)
            {
                crusts[variant] = NaturalBody(false, 57 + variant * 19);
                crusts[variant].name = "Eroded irregular basalt crust / " + variant;
            }
            return crusts[variant];
        }

        private static float Noise(Vector3 point)
        {
            return (Mathf.PerlinNoise(point.x, point.y) + Mathf.PerlinNoise(point.y, point.z) + Mathf.PerlinNoise(point.z, point.x)) / 3f;
        }

        private static Mesh NaturalBody(bool wet, int seed)
        {
            // The existing 24 by 18 smooth sphere is only a topology template;
            // its silhouette becomes a compressed, weathered, asymmetric lump.
            Mesh mesh = Object.Instantiate(ProceduralVisuals.Shape(PrimitiveType.Sphere));
            Vector3[] vertices = mesh.vertices;
            Vector3 seedOffset = new Vector3(seed * .13f, seed * .071f, seed * .19f);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i], normal = p.normalized;
                float broad = Noise(normal * 2.6f + seedOffset);
                float erosion = Noise(normal * 7.3f + seedOffset * 2f);
                float contour = 1 + (broad - .5f) * (wet ? .38f : .52f) + (erosion - .5f) * (wet ? .08f : .15f);
                float lowerLoad = Mathf.Clamp01(-normal.y * 1.5f);
                p.x *= contour * (1.04f + lowerLoad * .08f + normal.z * .06f);
                p.z *= contour * (.93f + normal.x * .085f);
                p.y *= wet ? .83f : .89f;
                // Uneven folds in wet matter and compressed rock layers are
                // embedded in the surface rather than outlined with lines.
                float fold = Mathf.Sin(p.y * (wet ? 23f : 16f) + broad * 11f + p.z * 3f);
                float depression = Mathf.Exp(-((normal.x - .23f) * (normal.x - .23f) + (normal.z + .18f) * (normal.z + .18f)) * 12f);
                p += normal * (fold * (wet ? .021f : .012f) - depression * Mathf.Max(0, normal.y) * (wet ? .065f : .027f));
                p.x += normal.y * normal.z * (wet ? .052f : .027f);
                vertices[i] = p;
            }
            mesh.vertices = vertices; mesh.RecalculateNormals();
            // Weld duplicate UV seam normals; otherwise a wet fold acquires an
            // artificial sharp zipper despite being one continuous surface.
            Vector3[] normals = mesh.normals;
            const int rowSize = 25;
            for (int row = 0; row < 19; row++)
            {
                int first = row * rowSize, last = first + rowSize - 1;
                Vector3 joined = (normals[first] + normals[last]).normalized;
                normals[first] = normals[last] = joined;
                if (row == 0 || row == 18)
                {
                    Vector3 pole = Vector3.zero;
                    for (int j = first; j <= last; j++) pole += normals[j];
                    pole.Normalize();
                    for (int j = first; j <= last; j++) normals[j] = pole;
                }
            }
            mesh.normals = normals; mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }
    }

    // Molten soil rises from the collision-clipped current sweep corridor.
    // Released fragments use gravity and contact; the floor body stays grounded.
    internal sealed class EnemySweepPhysicalVfx : MonoBehaviour
    {
        private Transform[] bodies;
        private Renderer[] renderers;
        private Transform core;
        private Material material;
        private Color baseTint;
        private MaterialPropertyBlock block;
        private float radius, debrisClock;
        private bool wasLive;

        internal static EnemySweepPhysicalVfx Create(Transform parent, float radius)
        {
            var root = new GameObject("Large boss / sweeping molten ground rupture"); root.transform.SetParent(parent, false);
            var fx = root.AddComponent<EnemySweepPhysicalVfx>(); fx.radius = radius;
            var resources = root.AddComponent<WorldResources>();
            fx.baseTint = new Color(.18f, .125f, .085f);
            fx.material = resources.Material(fx.baseTint, false, VisualSurface.Stone); fx.block = new MaterialPropertyBlock();
            SkillBodyMaterialLibrary.Apply(fx.material, SkillBodyMaterialLibrary.Matter.Rock);
            int count = EffectPreferences.ReducedEffects ? 22 : 32;
            fx.bodies = new Transform[count]; fx.renderers = new Renderer[count];
            for (int i = 0; i < count; i++)
            {
                fx.bodies[i] = EnemySkillPhysicalVfx.Body(root.transform, "Terrain-bound molten basalt", fx.material, false);
                fx.renderers[i] = fx.bodies[i].GetComponent<Renderer>();
            }
            Material interrupt = resources.Material(new Color(.14f, .46f, .42f), true, VisualSurface.Crystal);
            SkillBodyMaterialLibrary.Apply(interrupt, SkillBodyMaterialLibrary.Matter.Ice, .18f);
            fx.core = EnemySkillPhysicalVfx.Body(root.transform, "Exposed sweep interrupt crystal", interrupt, false); return fx;
        }

        internal void Draw(Vector3 from, Vector3 to, float progress, bool live, bool interruptible)
        {
            float length = CombatFx.Flat(to - from).magnitude;
            Vector3 forward = length > .01f ? CombatFx.Flat(to - from) / length : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            float spacing = length / Mathf.Max(1, bodies.Length - 1);
            for (int i = 0; i < bodies.Length; i++)
            {
                float t = i / (float)(bodies.Length - 1);
                Vector3 point = Vector3.Lerp(from, to, t) + right * Mathf.Sin(i * 2.399963f) * radius * .24f;
                float rise = live ? .38f + .06f * Mathf.Sin(Time.time * 12 - i * .7f) : .1f + .19f * progress;
                bodies[i].position = WorldTerrain.Ground(point, rise * .2f);
                // Elongated clods follow the actual sweep corridor. Keep only
                // small local irregularity, so turning the sweep cannot rotate
                // the long rock axis across its collision-clipped footprint.
                bodies[i].rotation = Quaternion.LookRotation(forward, Vector3.up) *
                    Quaternion.Euler(i % 3 * 8, (i % 4 - 1.5f) * 9, i % 4 * 7);
                // The body excludes player-radius compensation in the damage capsule.
                float width = Mathf.Max(.28f, radius - .4f) * 1.25f;
                bodies[i].localScale = new Vector3(width, rise * 2f, Mathf.Max(.62f, spacing * 1.75f));
                Color tint = live ? new Color(.24f, .155f, .085f) : Color.Lerp(baseTint, new Color(.29f, .18f, .09f), progress);
                block.Clear(); block.SetColor("_Color", tint); block.SetColor("_Tint", tint);
                block.SetFloat("_Heat", live ? 1 : progress * .7f);
                block.SetColor("_EmissionColor", new Color(.7f, .13f, .018f) * (live ? .035f : progress * .018f));
                renderers[i].SetPropertyBlock(block);
            }
            core.gameObject.SetActive(!live && interruptible); core.position = WorldTerrain.Ground(from, .52f); core.localScale = new Vector3(.38f, .62f, .38f);
            if (live)
            {
                debrisClock -= Time.deltaTime;
                if (debrisClock <= 0)
                {
                    debrisClock = EffectPreferences.ReducedEffects ? .65f : .36f;
                    Vector3 point = Vector3.Lerp(from, to, .35f + Mathf.Repeat(Time.time * .43f, .6f));
                    PhysicalSkillVfx.Impact(WorldTerrain.Ground(point), forward, new Color(.36f, .23f, .12f), .38f);
                }
            }
            else debrisClock = 0;
            if (live && !wasLive) PhysicalSkillVfx.Impact(WorldTerrain.Ground(from), forward, new Color(.36f, .23f, .12f), radius);
            wasLive = live;
        }
    }
}
