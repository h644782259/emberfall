using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    public sealed partial class CombatModel
    {
        private struct CompanionMatterKey
        {
            public Material Source;
            public SkillBodyMaterialLibrary.Matter Matter;
            public bool Flame;
        }

        private struct CompanionMatterColor
        {
            public Material Material;
            public Color Tint;
        }

        private readonly Dictionary<CompanionMatterKey, Material> companionMatterPalette = new Dictionary<CompanionMatterKey, Material>();
        private readonly List<CompanionMatterColor> companionMatterColors = new List<CompanionMatterColor>();
        private readonly HashSet<Renderer> companionMatterRenderers = new HashSet<Renderer>();
        private SummonedCompanion.Kind companionMatterKind;
        private bool companionMatterEnabled, timberTurret;
        private Transform companionCannonMuzzle;
        private int matterAppearanceKey = -1;
        private float companionMaterialPhase;

        private void ConfigureCompanionMatter(SummonedCompanion.Kind form)
        {
            companionMatterEnabled = true; companionMatterKind = form;
            RefreshCompanionMatter();
        }

        private void RefreshCompanionMatter()
        {
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || companionMatterRenderers.Contains(renderer)) continue;
                string name = renderer.gameObject.name;
                if (companionMatterKind == SummonedCompanion.Kind.Treant)
                    RefineTreantNaturalPart(renderer.GetComponent<MeshFilter>(), name);
                bool flame = companionMatterKind == SummonedCompanion.Kind.Wisp && name.StartsWith("Ember combustion");
                SkillBodyMaterialLibrary.Matter matter;
                if (name.Contains("eye") || name.Contains("Eye") || name.Contains("seal") || name.Contains("knot") || name.Contains("focus crystal"))
                    matter = SkillBodyMaterialLibrary.Matter.Ice;
                else if (name.Contains("collar") || name.Contains("clasp") || name.Contains("band") || name.Contains("vane"))
                    matter = SkillBodyMaterialLibrary.Matter.Steel;
                else if (companionMatterKind == SummonedCompanion.Kind.Wolf)
                    matter = SkillBodyMaterialLibrary.Matter.Fur;
                else if (companionMatterKind == SummonedCompanion.Kind.Treant)
                    matter = name.Contains("Leaf") ? SkillBodyMaterialLibrary.Matter.Vine : SkillBodyMaterialLibrary.Matter.Wood;
                else if (name.Contains("stone") || name.Contains("coal"))
                    matter = SkillBodyMaterialLibrary.Matter.Rock;
                else if (name.Contains("timber") || name.Contains("carved"))
                    matter = SkillBodyMaterialLibrary.Matter.Wood;
                else
                    matter = SkillBodyMaterialLibrary.Matter.Steel;
                BindCompanionMatter(renderer, matter, flame, name.Contains("coal") ? .95f : matter == SkillBodyMaterialLibrary.Matter.Ice ? .18f : 0);
                // Rank adds real rear brackets to the weapon housing, rather
                // than leaving the old star crown floating over its barrel.
                if (timberTurret && name.Contains("Spirit grown crown vane"))
                {
                    Vector3 at = renderer.transform.localPosition;
                    at.y -= .48f; at.z = -.24f; renderer.transform.localPosition = at;
                }
            }
            matterAppearanceKey = companionAppearanceKey;
        }

        private void BindCompanionMatter(Renderer renderer, SkillBodyMaterialLibrary.Matter matter, bool flame, float heat)
        {
            Material source = renderer.sharedMaterial;
            if (source == null) return;
            var key = new CompanionMatterKey { Source = source, Matter = matter, Flame = flame };
            Material material;
            if (!companionMatterPalette.TryGetValue(key, out material))
            {
                Color tint = CompanionSourceTint(source);
                material = new Material(source) { name = "Companion natural matter / " + matter };
                if (flame)
                {
                    Shader fire = Resources.Load<Shader>("SpellEnergySurface");
                    if (fire != null) material.shader = fire;
                    material.SetColor("_Tint", tint); material.SetFloat("_Opacity", 1); material.SetFloat("_Heat", .6f);
                    material.renderQueue = 3000;
                    SkillBodyMaterialLibrary.Bind(material);
                }
                else { SkillBodyMaterialLibrary.Apply(material, matter, heat); material.renderQueue = 2450; }
                companionMatterPalette.Add(key, material);
                companionMatterColors.Add(new CompanionMatterColor { Material = material, Tint = tint });
            }
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = flame ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = !flame;
            companionMatterRenderers.Add(renderer);
        }

        private Color CompanionSourceTint(Material source)
        {
            // Hit flashes change the old palette in LateUpdate. New contract
            // attachments retain their authored tint even when built mid-hit.
            foreach (var pair in palette) if (pair.Value == source) return pair.Key.Color;
            return source.HasProperty("_Tint") ? source.GetColor("_Tint") : source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        }

        private void UpdateCompanionMatterTint(float flash, float opacity)
        {
            foreach (CompanionMatterColor entry in companionMatterColors)
            {
                if (entry.Material == null) continue;
                entry.Material.SetColor("_Tint", Color.Lerp(entry.Tint, new Color(1, .92f, .73f), flash));
                entry.Material.SetFloat("_Opacity", opacity);
                entry.Material.SetFloat("_Phase", companionMaterialPhase);
            }
        }

        private void ApplyCompanionMatterPose(float attack, bool advanceClock = true)
        {
            if (!companionMatterEnabled) return;
            if (advanceClock) companionMaterialPhase += Time.deltaTime;
            if (matterAppearanceKey != companionAppearanceKey) RefreshCompanionMatter();
            if (!timberTurret || body == null) return;
            // The root already turns toward the real target. The upper weapon
            // absorbs its shot along the bore; its stone footing stays loaded.
            body.localPosition = new Vector3(0, 1.05f, -attack * .08f);
            // ApplyCompanionPose contributes another -12 degrees of readiness.
            body.localRotation = Quaternion.Euler(companionPreparation * 9f + attack * 2f, 0, 0);
        }

        // Commit only the rigid weapon state needed by the shot. The normal
        // animation pass remains the sole owner of clocks and cloth sampling.
        public void CommitCompanionProjectileRelease()
        {
            if (!timberTurret) return;
            companionPreparation = 0;
            ApplyCompanionMatterPose(1, false);
            ApplyCompanionPose();
        }

        public bool TryGetCompanionProjectileSocket(out Vector3 position)
        {
            position = transform.position;
            if (!timberTurret || companionCannonMuzzle == null) return false;
            position = companionCannonMuzzle.position; return true;
        }

        private void ReleaseCompanionMatter()
        {
            foreach (Material material in companionMatterPalette.Values) if (material != null) Destroy(material);
            companionMatterPalette.Clear(); companionMatterColors.Clear(); companionMatterRenderers.Clear();
        }

        private void BuildEmberCompanionMatter()
        {
            floating = true;
            body = Part("Ember coal heart", PrimitiveType.Sphere, new Vector3(0, 1.2f, 0), new Vector3(.4f, .52f, .36f), new Color(.13f, .09f, .065f));
            body.GetComponent<MeshFilter>().sharedMesh = HeroSkillBodyAssets.Rock ?? EnemySkillPhysicalVfx.CrustMesh(1);
            companionBodyScale = body.localScale;
            Transform rigid = CompanionRigidParent();
            Mesh flame = HeroSkillBodyAssets.Spell("Flame");
            for (int i = 0; i < 3; i++)
            {
                int side = i - 1;
                Transform plume = Part("Ember combustion feather / " + i, PrimitiveType.Capsule,
                    new Vector3(side * .24f, 1.07f, -.04f + i * .035f),
                    new Vector3(.3f, i == 1 ? .85f : .62f, .29f), new Color(1, .43f, .1f), rigid);
                if (flame != null) plume.GetComponent<MeshFilter>().sharedMesh = flame;
                plume.localRotation = Quaternion.Euler(-side * 10, i * 83, -side * 38);
            }
        }

        private void BuildTimberSpiritTurret()
        {
            timberTurret = true;
            Color stone = new Color(.24f, .23f, .21f), wood = new Color(.31f, .23f, .14f), steel = new Color(.38f, .39f, .36f);
            Transform foot = Part("Turret weathered stone footing", PrimitiveType.Sphere, new Vector3(0, .15f, 0), new Vector3(1.12f, .29f, 1.08f), stone);
            foot.GetComponent<MeshFilter>().sharedMesh = EnemySkillPhysicalVfx.CrustMesh(2);
            Part("Turret carved timber pedestal", PrimitiveType.Cube, new Vector3(0, .59f, -.08f), new Vector3(.43f, .77f, .44f), wood, meshModule: "Bound spirit totem");
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Turret timber load-bearing shoe", PrimitiveType.Cube, new Vector3(side * .28f, .31f, -.07f), new Vector3(.20f, .27f, .66f), wood, meshModule: "Bound spirit totem");
                Part("Turret iron pedestal brace", PrimitiveType.Cube, new Vector3(side * .225f, .58f, -.07f), new Vector3(.055f, .48f, .30f), steel, surface: VisualSurface.Metal, meshModule: "Raised breastplate");
            }
            Part("Turret iron traverse bearing", PrimitiveType.Cylinder, new Vector3(0, .91f, -.10f), new Vector3(.57f, .11f, .55f), steel, surface: VisualSurface.Metal, meshModule: "Faceted engine housing");
            body = Joint("Turret rigid cannon cradle", new Vector3(0, 1.05f, 0));
            Part("Turret iron rear breech", PrimitiveType.Cylinder, new Vector3(0, .045f, -.18f), new Vector3(.49f, .17f, .47f), steel, body, VisualSurface.Metal, "Faceted engine housing").localRotation = Quaternion.Euler(90, 0, 0);
            Transform barrel = Part("Turret thick forged open cannon bore", PrimitiveType.Cylinder, Vector3.zero, Vector3.one, steel, body, VisualSurface.Metal);
            Mesh bore = CompanionCannonBore();
            barrel.GetComponent<MeshFilter>().sharedMesh = bore;
            barrel.gameObject.AddComponent<OwnedCombatMesh>().Value = bore;
            companionCannonMuzzle = new GameObject("Cannon physical bore outlet").transform;
            companionCannonMuzzle.SetParent(body, false);
            companionCannonMuzzle.localPosition = new Vector3(0, 0, .68f);
            for (int side = -1; side <= 1; side += 2)
            {
                Part("Turret iron cannon cheek", PrimitiveType.Cube, new Vector3(side * .28f, -.015f, -.06f), new Vector3(.12f, .38f, .42f), steel, body, VisualSurface.Metal, "Raised breastplate");
                for (int i = 0; i < 3; i++)
                    Part("Turret hammered iron rivet", PrimitiveType.Sphere, new Vector3(side * .347f, .075f - i * .09f, -.015f), Vector3.one * .065f, new Color(.22f, .23f, .21f), body, VisualSurface.Metal);
            }
            Part("Turret inset focus crystal", PrimitiveType.Sphere, new Vector3(0, .17f, -.20f), new Vector3(.23f, .25f, .18f), new Color(.19f, .51f, .43f), body, VisualSurface.Crystal, "Focus crystal");
        }

        private static Mesh CompanionCannonBore()
        {
            const int sides = 36;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            float[] distance = { 0, .055f, .10f, .40f, .60f, .68f };
            float[] radius = { .235f, .235f, .203f, .183f, .190f, .205f };
            for (int row = 0; row < distance.Length; row++)
                CannonCircle(vertices, uv, sides, radius[row], distance[row]);
            for (int row = 0; row < distance.Length - 1; row++) CannonStrip(triangles, row * (sides + 1), (row + 1) * (sides + 1), sides, false);
            int inside = vertices.Count;
            CannonCircle(vertices, uv, sides, .135f, .12f); CannonCircle(vertices, uv, sides, .145f, .68f);
            CannonStrip(triangles, inside, inside + sides + 1, sides, true);
            int mouth = vertices.Count;
            CannonCircle(vertices, uv, sides, .205f, .68f); CannonCircle(vertices, uv, sides, .145f, .68f);
            CannonStrip(triangles, mouth, mouth + sides + 1, sides, false);
            int back = vertices.Count;
            CannonCircle(vertices, uv, sides, .135f, .12f);
            int center = vertices.Count; vertices.Add(new Vector3(0, 0, .12f)); uv.Add(new Vector2(.5f, .5f));
            for (int i = 0; i < sides; i++) { triangles.Add(center); triangles.Add(back + i); triangles.Add(back + i + 1); }
            var mesh = new Mesh { name = "Thick forged cannon / open bore and recessed chamber" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds(); return mesh;
        }

        private static void CannonCircle(List<Vector3> vertices, List<Vector2> uv, int sides, float radius, float z)
        {
            for (int i = 0; i <= sides; i++)
            {
                float angle = i * Mathf.PI * 2 / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z)); uv.Add(new Vector2(i / (float)sides, z / .68f));
            }
        }

        private static void CannonStrip(List<int> triangles, int first, int second, int sides, bool inward)
        {
            for (int i = 0; i < sides; i++)
            {
                int a = first + i, b = a + 1, c = second + i, d = c + 1;
                triangles.Add(a); triangles.Add(inward ? c : b); triangles.Add(inward ? b : c);
                triangles.Add(b); triangles.Add(inward ? c : d); triangles.Add(inward ? d : c);
            }
        }
    }
}
