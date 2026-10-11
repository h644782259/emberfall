using System.Collections.Generic;
using UnityEngine;
namespace Emberfall
{
    // Four bowed, closed forged plates and raised gold edging. The open crown
    // reads from behind and above, without hiding the actor inside a glowing ball.
    internal sealed class VanguardWardVisual : MonoBehaviour
    {
        private static Mesh shell, edging;
        private static Material steel, gold;
        private Renderer plate, rim;
        private MaterialPropertyBlock block;
        private float age;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        { if (shell != null) Destroy(shell); if (edging != null) Destroy(edging); if (steel != null) Destroy(steel); if (gold != null) Destroy(gold); shell = edging = null; steel = gold = null; }
        internal static void Prewarm()
        {
            if (shell != null) return;
            shell = PlateMesh(false); edging = PlateMesh(true);
            steel = new Material(Resources.Load<Shader>("HeroSkillBody")) { name = "Saint ward / engraved blue steel" };
            gold = new Material(steel) { name = "Saint ward / raised warm brass" };
            SkillBodyMaterialLibrary.Apply(steel, SkillBodyMaterialLibrary.Matter.Steel);
            SkillBodyMaterialLibrary.Apply(gold, SkillBodyMaterialLibrary.Matter.Steel);
        }
        internal static void Attach(Transform parent)
        {
            Prewarm();
            var root = new GameObject("Saint counter ward / four readable forged faces"); root.transform.SetParent(parent, false);
            if (CombatVisualLease.Attach(root, CombatVisualPriority.ActionBody) == null) return;
            var ward = root.AddComponent<VanguardWardVisual>(); ward.block = new MaterialPropertyBlock();
            ward.plate = Surface(root.transform, shell, steel, "Closed curved steel shields");
            ward.rim = Surface(root.transform, edging, gold, "Raised heraldic gold border"); ward.Sample();
        }
        private static Renderer Surface(Transform parent, Mesh mesh, Material material, string name)
        {
            var part = new GameObject(name); part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = true; return renderer;
        }
        private static Mesh PlateMesh(bool border)
        {
            const int columns = 16, rows = 6;
            var vertices = new List<Vector3>(); var triangles = new List<int>(); var uv = new List<Vector2>();
            for (int panel = 0; panel < 4; panel++)
            {
                int start = vertices.Count;
                for (int skin = 0; skin < 2; skin++) for (int y = 0; y <= rows; y++) for (int x = 0; x <= columns; x++)
                {
                    float u = x / (float)columns, v = y / (float)rows, angle = panel * Mathf.PI * .5f + (u - .5f) * 1.38f;
                    float radius = .82f + Mathf.Sin(v * Mathf.PI) * .12f + (skin == 0 ? .035f : -.035f) + (border ? .015f : 0);
                    float height = .45f + v * 1.18f + Mathf.Sin(u * Mathf.PI) * .16f;
                    vertices.Add(new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius)); uv.Add(new Vector2(u, v));
                }
                int layer = (columns + 1) * (rows + 1);
                for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
                {
                    if (border && y > 0 && y < rows - 1 && x > 0 && x < columns - 1) continue;
                    int a = start + y * (columns + 1) + x, b = a + 1, c = a + columns + 1, d = c + 1;
                    triangles.AddRange(new[] { a, c, b, b, c, d, a + layer, b + layer, c + layer, b + layer, d + layer, c + layer });
                }
                // Raised edging is a solid frame; close its inner opening too.
                if (border)
                {
                    for (int y = 1; y < rows - 1; y++) for (int edge = 0; edge < 2; edge++)
                    {
                        int a = start + y * (columns + 1) + (edge == 0 ? 1 : columns - 1), b = a + columns + 1;
                        if (edge == 0) triangles.AddRange(new[] { a, b, a + layer, b, b + layer, a + layer });
                        else triangles.AddRange(new[] { a, a + layer, b, b, a + layer, b + layer });
                    }
                    for (int x = 1; x < columns - 1; x++) for (int edge = 0; edge < 2; edge++)
                    {
                        int a = start + (edge == 0 ? 1 : rows - 1) * (columns + 1) + x, b = a + 1;
                        if (edge == 0) triangles.AddRange(new[] { a, a + layer, b, b, a + layer, b + layer });
                        else triangles.AddRange(new[] { a, b, a + layer, b, b + layer, a + layer });
                    }
                }
                for (int y = 0; y < rows; y++) for (int edge = 0; edge < 2; edge++)
                {
                    int a = start + y * (columns + 1) + edge * columns, b = a + columns + 1;
                    if (edge == 0) triangles.AddRange(new[] { a, a + layer, b, b, a + layer, b + layer });
                    else triangles.AddRange(new[] { a, b, a + layer, b, b + layer, a + layer });
                }
                for (int x = 0; x < columns; x++) for (int edge = 0; edge < 2; edge++)
                {
                    int a = start + edge * rows * (columns + 1) + x, b = a + 1;
                    if (edge == 0) triangles.AddRange(new[] { a, b, a + layer, b, b + layer, a + layer });
                    else triangles.AddRange(new[] { a, a + layer, b, b, a + layer, b + layer });
                }
            }
            // Existing triangles were assembled with an inward traversal.
            // Reverse once so the closed skins and all cap normals face outward.
            for (int i = 0; i < triangles.Count; i += 3)
            { int b = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = b; }
            var mesh = new Mesh { name = border ? "Four continuous heraldic borders" : "Four smooth closed bowed shield plates" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        private void LateUpdate()
        { var game = GameSession.Instance; if (game == null || game.InputBlocked || Time.deltaTime <= 0) return; age += Time.deltaTime; Sample(); }
        private void Sample()
        {
            float arrival = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .18f));
            transform.localScale = Vector3.one * (.88f + .12f * arrival);
            block.SetColor("_Tint", new Color(.17f, .27f, .39f)); block.SetFloat("_Surface", 1); block.SetFloat("_Opacity", .82f);
            block.SetFloat("_CoreLight", .08f); block.SetFloat("_Engraving", 1); block.SetFloat("_Phase", age); plate.SetPropertyBlock(block);
            block.SetColor("_Tint", new Color(.92f, .63f, .16f)); block.SetFloat("_Opacity", 1); block.SetFloat("_CoreLight", .25f + .12f * Mathf.Sin(age * 3)); rim.SetPropertyBlock(block);
        }
    }
}
