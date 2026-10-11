using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // The healing contract remains AdvancedSkillSequence's five real health
    // mutations. This planted solid flag owns only its cloth and visual response.
    internal sealed class SkillLifeBanner : MonoBehaviour
    {
        private static readonly Dictionary<PlayerController, SkillLifeBanner> current = new Dictionary<PlayerController, SkillLifeBanner>();
        private PlayerController owner;
        private int epoch;
        private float age, pulse;
        private Mesh cloth;
        private Material clothMaterial, woodMaterial;
        private Vector3[] vertices;
        private const int Rows = 9, Columns = 15, Layer = Rows * Columns;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { current.Clear(); }

        internal static void Spawn(Transform parent, PlayerController hero)
        {
            if (parent == null || hero == null) return;
            var go = new GameObject("Life battle flag / planted healing identity");
            go.transform.SetParent(parent, false); go.transform.localPosition = new Vector3(-.65f, 0, -.3f);
            Vector3 at = go.transform.position; at.y = WorldTraversal.SurfaceHeight(at, .04f); go.transform.position = at;
            var banner = go.AddComponent<SkillLifeBanner>(); banner.owner = hero; banner.epoch = hero.CombatEpoch;
            current[hero] = banner; banner.Build();
        }

        // Returns false for other healing sources, which keep their own response.
        internal static bool Pulse(PlayerController hero)
        {
            SkillLifeBanner banner;
            if (hero == null || !current.TryGetValue(hero, out banner) || banner == null || !banner.gameObject.activeInHierarchy || banner.epoch != hero.CombatEpoch) return false;
            banner.pulse = 1; return true;
        }

        private void Build()
        {
            Shader shader = Resources.Load<Shader>("LifeBannerCloth");
            clothMaterial = new Material(shader) { name = "Life banner / gold heraldry on woven forest cloth", color = new Color(.18f, .37f, .20f) };
            woodMaterial = new Material(Resources.Load<Shader>("HeroSkillBody")) { name = "Life banner / worn timber pole", color = new Color(.35f, .25f, .15f) };
            ProceduralVisuals.ApplySurface(clothMaterial, VisualSurface.Cloth);
            SkillBodyMaterialLibrary.Apply(woodMaterial, SkillBodyMaterialLibrary.Matter.Wood);
            var pole = new GameObject("Rounded grain-bearing banner pole"); pole.transform.SetParent(transform, false);
            var poleMesh = NaturalActorMeshFactory.CurveTube("Life banner continuous tapered wood pole", new[] { Vector3.zero, Vector3.up * 1.35f, new Vector3(.025f, 2.65f, 0) }, .035f);
            pole.AddComponent<MeshFilter>().sharedMesh = poleMesh; pole.AddComponent<MeshRenderer>().sharedMaterial = woodMaterial;
            pole.AddComponent<OwnedCombatMesh>().Value = poleMesh;
            var fabric = new GameObject("Thick flexible woven battle flag"); fabric.transform.SetParent(transform, false);
            vertices = new Vector3[Layer * 2]; cloth = new Mesh { name = "Closed flexible healing banner cloth" };
            var uv = new Vector2[vertices.Length]; var triangles = new List<int>();
            for (int skin = 0; skin < 2; skin++) for (int row = 0; row < Rows; row++) for (int column = 0; column < Columns; column++)
                uv[skin * Layer + row * Columns + column] = new Vector2(column / (float)(Columns - 1), row / (float)(Rows - 1));
            for (int row = 0; row < Rows - 1; row++) for (int column = 0; column < Columns - 1; column++)
            {
                int a = row * Columns + column, b = a + 1, c = a + Columns, d = c + 1;
                triangles.AddRange(new[] { a, b, c, b, d, c, a + Layer, c + Layer, b + Layer, b + Layer, c + Layer, d + Layer });
            }
            for (int row = 0; row < Rows - 1; row++)
            {
                int a = row * Columns, b = a + Columns;
                triangles.AddRange(new[] { a, b, a + Layer, b, b + Layer, a + Layer });
                a += Columns - 1; b += Columns - 1;
                triangles.AddRange(new[] { a, a + Layer, b, b, a + Layer, b + Layer });
            }
            for (int column = 0; column < Columns - 1; column++)
            {
                int a = column, b = a + 1;
                triangles.AddRange(new[] { a, a + Layer, b, b, a + Layer, b + Layer });
                a += Layer - Columns; b += Layer - Columns;
                triangles.AddRange(new[] { a, b, a + Layer, b, b + Layer, a + Layer });
            }
            UpdateCloth(); cloth.uv = uv; cloth.triangles = triangles.ToArray(); cloth.RecalculateNormals(); cloth.RecalculateTangents(); cloth.RecalculateBounds();
            fabric.AddComponent<MeshFilter>().sharedMesh = cloth; fabric.AddComponent<MeshRenderer>().sharedMaterial = clothMaterial;
            fabric.AddComponent<OwnedCombatMesh>().Value = cloth;
        }

        private void LateUpdate()
        {
            var game = GameSession.Instance;
            if (owner == null || game == null || game.Player != owner || owner.CombatEpoch != epoch || owner.IsDead || game.CombatEffectsEnded) { gameObject.SetActive(false); Destroy(gameObject); return; }
            if (game.InputBlocked || Time.deltaTime <= 0) return;
            age += Time.deltaTime; pulse = Mathf.Max(0, pulse - Time.deltaTime * 2.7f);
            UpdateCloth(); cloth.RecalculateNormals(); cloth.RecalculateBounds();
            clothMaterial.SetColor("_EmissionColor", new Color(.07f, .19f, .045f) * pulse);
            clothMaterial.SetFloat("_Pulse", pulse);
        }

        private void UpdateCloth()
        {
            for (int skin = 0; skin < 2; skin++) for (int row = 0; row < Rows; row++) for (int column = 0; column < Columns; column++)
            {
                float x = column / (float)(Columns - 1), y = row / (float)(Rows - 1);
                float wave = (Mathf.Sin(x * 9 - age * 3.1f + y * .7f) * .06f + Mathf.Sin(x * 17 - age * 4.7f) * .018f) * x;
                float sag = -.12f * x * x + Mathf.Sin(age * 2.1f + x * 4) * .025f * x;
                vertices[skin * Layer + row * Columns + column] = new Vector3(.02f + x * .98f, 1.8f + y * .70f + sag, wave * (1 + pulse * .32f) + (skin == 0 ? .008f : -.008f));
            }
            cloth.vertices = vertices;
        }

        private void OnDisable()
        {
            SkillLifeBanner banner;
            if (!object.ReferenceEquals(owner, null) && current.TryGetValue(owner, out banner) && banner == this) current.Remove(owner);
        }
        private void OnDestroy()
        {
            OnDisable(); if (clothMaterial != null) Destroy(clothMaterial); if (woodMaterial != null) Destroy(woodMaterial);
        }
    }
}
