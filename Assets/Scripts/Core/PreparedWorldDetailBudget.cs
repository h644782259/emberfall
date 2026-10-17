using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Emberfall
{
    // Partition only explicitly decorative batches during scene preparation.
    // Ground, roads, cover, colliders, actors, portals and combat markers retain
    // their complete rendering and collision data at every quality setting.
    public sealed class PreparedWorldDetailBudget : MonoBehaviour
    {
        private sealed class Chunk
        {
            public MeshRenderer Renderer;
            public BoundingSphere Sphere;
            public int Band, TriangleCount;
            public float Alpha = 1;
            public bool Updating, UsingFade;
            public Material[] OpaqueMaterials, FadeMaterials;
            public readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
        }
        private sealed class ChunkGeometry
        {
            public readonly Dictionary<int, int> Remap = new Dictionary<int, int>();
            public readonly List<Vector3> Positions = new List<Vector3>(), Normals = new List<Vector3>();
            public readonly List<Vector4> Tangents = new List<Vector4>();
            public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<Color> Colors = new List<Color>();
            public readonly List<int> Indices = new List<int>();
        }
        internal static bool ForceFadeVariant { get; private set; }
        internal static void SetForceFadeVariant(bool value)
        {
            ForceFadeVariant = value;
            foreach (var world in worlds)
                foreach (var chunk in world.chunks) ApplyMaterialVariant(chunk);
        }
        private static void ApplyMaterialVariant(Chunk chunk)
        {
            bool fade = ForceFadeVariant || chunk.Alpha < 1f;
            if (chunk.Renderer == null || chunk.UsingFade == fade) return;
            chunk.Renderer.sharedMaterials = fade ? chunk.FadeMaterials : chunk.OpaqueMaterials;
            chunk.UsingFade = fade;
        }
        private static readonly HashSet<PreparedWorldDetailBudget> worlds = new HashSet<PreparedWorldDetailBudget>();
        private static readonly int fadeProperty = Shader.PropertyToID("_OcclusionFade");
        private readonly List<Chunk> chunks = new List<Chunk>();
        private readonly List<int> changing = new List<int>();
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly Dictionary<Material, Material> fadeMaterials = new Dictionary<Material, Material>();
        private CullingGroup culling;
        private Camera view;
        private float detailDistance;
        private bool prepared;
        public bool IsPrepared => prepared;
        public int PreparedRendererCount => chunks.Count;
        public int SourceTriangleCount { get; private set; }
        internal int PreparedTerrainVertexCount { get; private set; }
        internal int TightBoundsChunkCount { get; private set; }
        public int PreparedTriangleCount
        { get { int total = 0; foreach (var chunk in chunks) total += chunk.TriangleCount; return total; } }
        public float RenderDetailDistance => detailDistance;
        public int VisibleTriangleCount
        {
            get { int total = 0; foreach (var chunk in chunks) if (chunk.Renderer != null && chunk.Renderer.enabled) total += chunk.TriangleCount; return total; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { worlds.Clear(); ForceFadeVariant = false; }
        public static void ApplyToExistingWorlds(GraphicsQualityProfile profile)
        { foreach (var world in worlds) if (world != null && world.prepared) world.SetProfile(profile); }

        public static void Prepare(GameObject world, GraphicsQualityProfile profile)
        {
            var routine = PrepareAsync(world, profile);
            while (routine.MoveNext()) { }
        }
        public static IEnumerator PrepareAsync(GameObject world, GraphicsQualityProfile profile, Action<float> progress = null)
        {
            if (world == null) yield break;
            var budget = world.GetComponent<PreparedWorldDetailBudget>() ?? world.AddComponent<PreparedWorldDetailBudget>();
            if (budget.prepared) { budget.SetProfile(profile); progress?.Invoke(1); yield break; }
            var renderers = new List<MeshRenderer>();
            foreach (var renderer in world.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer != null && renderer.enabled && IsDecoration(renderer.gameObject.name)) renderers.Add(renderer);
            for (int i = 0; i < renderers.Count; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.enabled && IsDecoration(renderer.gameObject.name))
                {
                    var routine = budget.Partition(renderer);
                    while (routine.MoveNext()) yield return routine.Current;
                }
                progress?.Invoke((i + 1f) / Mathf.Max(1, renderers.Count));
            }
            budget.prepared = true;
            budget.SetProfile(profile);
            // Let native culling and the first visible geometry submission finish
            // before the scene loader enables player control.
            yield return null;
            progress?.Invoke(1);
        }

        private static bool IsDecoration(string name)
        {
            switch (name)
            {
                case "Curved solid meadow blades":
                case "Merged natural gravel":
                case "Merged woodland fern fronds":
                case "Merged forest leaf litter":
                case "Merged cathedral fractured rubble":
                case "Merged creeping ruin moss":
                case "Merged weathered timber splinters":
                case "Rounded riverbank pebbles":
                case "Curved rooted reed stems":
                case "Solid narrow reed blades": return true;
                default: return false;
            }
        }

        private IEnumerator Partition(MeshRenderer source)
        {
            var filter = source.GetComponent<MeshFilter>();
            var mesh = filter == null ? null : filter.sharedMesh;
            // Keep unfamiliar imported, unreadable and multi-material meshes intact.
            if (mesh == null || !mesh.isReadable || mesh.subMeshCount != 1 || source.GetComponent<Collider>() != null) yield break;
            var positions = mesh.vertices; var normals = mesh.normals; var tangents = mesh.tangents;
            var uv = mesh.uv; var colors = mesh.colors; var triangles = mesh.triangles;
            var groups = new Dictionary<Vector2Int, ChunkGeometry>();
            // 16m chunks limit extra draw calls, while separating distant portions
            // of a batch that formerly spanned the entire map in one renderer.
            float sx = Mathf.Max(.01f, Mathf.Abs(source.transform.lossyScale.x));
            float sz = Mathf.Max(.01f, Mathf.Abs(source.transform.lossyScale.z));
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 center = (positions[triangles[i]] + positions[triangles[i + 1]] + positions[triangles[i + 2]]) / 3;
                var key = new Vector2Int(Mathf.FloorToInt(center.x * sx / 16), Mathf.FloorToInt(center.z * sz / 16));
                if (!groups.TryGetValue(key, out var geometry)) groups.Add(key, geometry = new ChunkGeometry());
                for (int corner = 0; corner < 3; corner++)
                {
                    int original = triangles[i + corner];
                    if (!geometry.Remap.TryGetValue(original, out int index))
                    {
                        index = geometry.Positions.Count; geometry.Remap.Add(original, index); geometry.Positions.Add(positions[original]);
                        if (normals.Length == positions.Length) geometry.Normals.Add(normals[original]);
                        if (tangents.Length == positions.Length) geometry.Tangents.Add(tangents[original]);
                        if (uv.Length == positions.Length) geometry.UV.Add(uv[original]);
                        if (colors.Length == positions.Length) geometry.Colors.Add(colors[original]);
                    }
                    geometry.Indices.Add(index);
                }
                if (i > 0 && i % 12288 == 0) yield return null;
            }
            foreach (var pair in groups)
            {
                var geometry = pair.Value;
                var result = new Mesh { name = source.name + " · prepared detail chunk" };
                if (geometry.Positions.Count > 65535) result.indexFormat = IndexFormat.UInt32;
                result.SetVertices(geometry.Positions); result.SetTriangles(geometry.Indices, 0);
                if (geometry.Normals.Count > 0) result.SetNormals(geometry.Normals); else result.RecalculateNormals();
                if (geometry.Tangents.Count > 0) result.SetTangents(geometry.Tangents);
                if (geometry.UV.Count > 0) result.SetUVs(0, geometry.UV);
                if (geometry.Colors.Count > 0) result.SetColors(geometry.Colors);
                result.RecalculateBounds();
                ownedMeshes.Add(result);
                var child = new GameObject("Prepared decoration " + pair.Key.x + "," + pair.Key.y) { layer = source.gameObject.layer };
                child.transform.SetParent(source.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = result;
                var renderer = child.AddComponent<MeshRenderer>();
                var opaqueMaterials=source.sharedMaterials;
                var materials=(Material[])opaqueMaterials.Clone();
                for(int slot=0;slot<materials.Length;slot++)
                {
                    var original=materials[slot];
                    if(original==null||original.shader.name!="Emberfall/Weathered World")continue;
                    if(!fadeMaterials.TryGetValue(original,out var fading))
                    {
                        fading=new Material(original){name=original.name+" (distance fade)"};
                        fading.EnableKeyword("EMBER_WORLD_FADE");fadeMaterials.Add(original,fading);
                    }
                    materials[slot]=fading;
                }
                renderer.sharedMaterials = ForceFadeVariant ? materials : opaqueMaterials;
                renderer.shadowCastingMode = source.shadowCastingMode; renderer.receiveShadows = source.receiveShadows;
                renderer.lightProbeUsage = source.lightProbeUsage; renderer.reflectionProbeUsage = source.reflectionProbeUsage;
                // Match the original allowance for shader terrain height and wind.
                var bounds = result.bounds;
                var scale = source.transform.lossyScale;
                bounds.Expand(new Vector3(3 / sx, 8 / Mathf.Max(.01f, Mathf.Abs(scale.y)), 3 / sz));
                renderer.localBounds = bounds;
                // Keep existing distance-fade spheres; this experiment only tightens render culling.
                var worldBounds = renderer.bounds;
                if (PreparedDetailBounds.Requested && PreparedDetailBounds.TryCalculate(
                    geometry.Positions, geometry.Colors, child.transform, opaqueMaterials, out var tightBounds))
                { bounds = tightBounds; TightBoundsChunkCount++; }
                renderer.localBounds = bounds;
                chunks.Add(new Chunk { Renderer = renderer, OpaqueMaterials = opaqueMaterials, FadeMaterials = materials, UsingFade = ForceFadeVariant, Sphere = new BoundingSphere(worldBounds.center, worldBounds.extents.magnitude), TriangleCount = geometry.Indices.Count / 3 });
                // Upload occurs only while the scene loader blocks action.
                if(PreparedTerrainVertexData.Enabled && PreparedTerrainVertexData.TryBake(result,child.transform,opaqueMaterials))
                    PreparedTerrainVertexCount+=result.vertexCount;
                result.UploadMeshData(true);
                yield return null;
            }
            source.enabled = false;
            SourceTriangleCount += triangles.Length / 3;
            // This batch is world-owned and has no gameplay/support role. Once
            // all chunks are uploaded, retire its duplicate CPU/GPU allocation.
            var owner = source.GetComponentInParent<WorldResources>();
            if (owner != null && owner.ReleaseOwnedMesh(mesh)) filter.sharedMesh = null;
        }

        internal static void RefreshDetailDistances()
        {
            foreach(var world in worlds)
                if(world != null && world.prepared) world.SetDistance(GraphicsQualityRuntime.DetailDistance(GraphicsQualityRuntime.Current));
        }
        private void SetDistance(float distance)
        {
            if(Mathf.Approximately(detailDistance,distance)) return;
            detailDistance=distance;
            if(culling==null || view==null) return;
            culling.SetBoundingDistances(new[] { detailDistance - 10, detailDistance });
            for(int i=0;i<chunks.Count;i++)
            {
                var chunk=chunks[i];float range=Distance(chunk);
                chunk.Band=range<detailDistance-10?0:range<detailDistance?1:2;
                if(!chunk.Updating) { chunk.Updating=true;changing.Add(i); }
            }
        }
        private void SetProfile(GraphicsQualityProfile profile)
        {
            detailDistance = GraphicsQualityRuntime.DetailDistance(profile);
            if (culling != null) culling.Dispose();
            view = Camera.main;
            if (view == null || chunks.Count == 0) { culling = null; return; }
            var spheres = new BoundingSphere[chunks.Count];
            for (int i = 0; i < chunks.Count; i++) spheres[i] = chunks[i].Sphere;
            culling = new CullingGroup { targetCamera = view };
            culling.SetBoundingSpheres(spheres); culling.SetBoundingSphereCount(spheres.Length);
            culling.SetDistanceReferencePoint(view.transform);
            culling.SetBoundingDistances(new[] { detailDistance - 10, detailDistance });
            culling.onStateChanged = OnCullingChanged;
            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];
                float distance = Distance(chunk);
                chunk.Band = distance < detailDistance - 10 ? 0 : distance < detailDistance ? 1 : 2;
                if (!chunk.Updating) { chunk.Updating = true; changing.Add(i); }
            }
        }
        private float Distance(Chunk chunk)
        { return Mathf.Max(0, Vector3.Distance(view.transform.position, chunk.Sphere.position) - chunk.Sphere.radius); }
        private void OnCullingChanged(CullingGroupEvent change)
        {
            if (change.index < 0 || change.index >= chunks.Count) return;
            var chunk = chunks[change.index]; chunk.Band = change.currentDistance;
            if (!chunk.Updating) { chunk.Updating = true; changing.Add(change.index); }
        }
        private void Update()
        {
            if (!prepared || view == null || GameSession.Instance != null && GameSession.Instance.BackgroundPaused) return;
            // Near stable chunks and fully distant chunks have zero script work.
            for (int n = changing.Count - 1; n >= 0; n--)
            {
                var chunk = chunks[changing[n]];
                float target = chunk.Band == 0 ? 1 : chunk.Band >= 2 ? 0 : Mathf.Clamp01((detailDistance - Distance(chunk)) / 10);
                chunk.Alpha = Mathf.MoveTowards(chunk.Alpha, target, Time.unscaledDeltaTime * 4);
                bool visible = chunk.Alpha > .002f;
                chunk.Renderer.enabled = visible;
                // Opaque chunks have no discard instruction; use the dither variant
                // only while fading. Switch once at each boundary, not every frame.
                ApplyMaterialVariant(chunk);
                if (visible)
                { chunk.Properties.SetFloat(fadeProperty, chunk.Alpha); chunk.Renderer.SetPropertyBlock(chunk.Properties); }
                if (chunk.Band != 1 && Mathf.Abs(chunk.Alpha - target) < .001f)
                { chunk.Updating = false; changing.RemoveAt(n); }
            }
        }
        private void OnEnable() { worlds.Add(this); if (culling != null) culling.enabled = true; }
        private void OnDisable() { worlds.Remove(this); if (culling != null) culling.enabled = false; }
        private void OnDestroy()
        { worlds.Remove(this); if (culling != null) culling.Dispose(); foreach (var mesh in ownedMeshes) if (mesh != null) Destroy(mesh); foreach(var material in fadeMaterials.Values)if(material!=null)Destroy(material); }
    }
}
