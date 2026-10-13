using System.Collections.Generic;
using UnityEngine;

namespace Emberfall
{
    // Soil loads under an imminent hit. Its mounds use the same reachable
    // footprint as damage, without a detached circle, arrow or clock.
    internal sealed class EnemyAttackTelegraph : MonoBehaviour
    {
        private readonly List<Transform> bodies = new List<Transform>();
        private readonly List<Vector3> points = new List<Vector3>();
        private readonly List<Vector3> scales = new List<Vector3>();
        private readonly List<Renderer> renderers = new List<Renderer>();
        private WorldResources resources;
        private Material earth, interruptMaterial;
        private MaterialPropertyBlock block;
        private Transform attacker, interruptCore, emitterBody;
        private Vector3 impactCenter, lastAttacker;
        private float impactRadius, progress;
        private Color earthTint;
        private int lastRevision = -1;
        private bool interruptible, circular, slime, controlWarning;
        private readonly Color danger = new Color(.34f, .18f, .075f);

        public static EnemyAttackTelegraph Circle(Vector3 center, float radius, Transform attacker, bool slime = false)
        {
            var warning = Create("Enemy windup / disturbed impact soil", slime);
            warning.attacker = attacker; warning.impactCenter = center; warning.impactRadius = radius; warning.circular = true;
            warning.RefreshCircle(); warning.Core(attacker != null ? attacker.position + attacker.forward * .9f : center);
            return warning;
        }

        private void RefreshCircle()
        {
            if (!circular || attacker == null) return;
            Vector3 from = attacker.position;
            if (lastRevision == WorldTraversal.Revision && (from - lastAttacker).sqrMagnitude < .000001f) return;
            lastRevision = WorldTraversal.Revision; lastAttacker = from;
            ClearBodies();
            int count = impactRadius > 2 ? (EffectPreferences.ReducedEffects ? 24 : 42) : 14;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.399963f;
                float distance = Mathf.Sqrt((i + .5f) / count) * Mathf.Max(.1f, impactRadius - .28f);
                Vector3 point = impactCenter + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                if (!EnemyImpactRegion.Contains(from, impactCenter, point, impactRadius)) continue;
                float size = Mathf.Lerp(.28f, .58f, (Mathf.Sin(i * 4.31f) + 1) * .5f);
                if (!FitsCircle(from, point, size * .74f)) continue;
                Add(point, new Vector3(size, slime ? .16f : .24f, size * .85f), i);
            }
            Present();
        }

        private bool FitsCircle(Vector3 from, Vector3 point, float extent)
        {
            return EnemyImpactRegion.Contains(from, impactCenter, point + Vector3.right * extent, impactRadius) &&
                EnemyImpactRegion.Contains(from, impactCenter, point - Vector3.right * extent, impactRadius) &&
                EnemyImpactRegion.Contains(from, impactCenter, point + Vector3.forward * extent, impactRadius) &&
                EnemyImpactRegion.Contains(from, impactCenter, point - Vector3.forward * extent, impactRadius);
        }

        public static EnemyAttackTelegraph Charge(Vector3 start, Vector3 end, float halfWidth)
        {
            var warning = Create("Enemy windup / churned charge soil", false);
            warning.SoilLane(start, end, halfWidth); warning.Core(start + CombatFx.Flat(end - start).normalized * .9f); return warning;
        }

        public static EnemyAttackTelegraph Melee(Vector3 start, Vector3 end, float halfWidth)
        {
            var warning = Create("Enemy windup / weapon contact soil", false);
            warning.SoilLane(start, end, halfWidth); warning.Core(start + CombatFx.Flat(end - start).normalized * .55f); return warning;
        }

        public static EnemyAttackTelegraph Fan(Vector3 muzzle, Vector3[] directions, float[] lengths, float halfWidth)
        {
            var warning = Create("Enemy windup / spirit discharge ground pressure", false);
            for (int i = 0; i < directions.Length; i++)
                if (lengths[i] > .05f) warning.SoilLane(muzzle, muzzle + directions[i] * lengths[i], halfWidth);
            warning.Core(muzzle); return warning;
        }

        public static EnemyAttackTelegraph Emitter(Vector3 muzzle)
        {
            var warning = Create("Enemy windup / gathering spectral matter", false);
            var spirit = warning.resources.Material(new Color(.42f,.32f,.45f), true, VisualSurface.Crystal);
            SkillBodyMaterialLibrary.Apply(spirit, SkillBodyMaterialLibrary.Matter.Mud, .12f);
            warning.emitterBody = EnemySkillPhysicalVfx.Body(warning.transform, "Condensing ectoplasm at the muzzle", spirit, true);
            warning.emitterBody.position = WorldTerrain.Ground(muzzle, 1.05f);
            warning.emitterBody.localScale = new Vector3(.38f,.46f,.48f);
            warning.Core(muzzle); return warning;
        }

        private static EnemyAttackTelegraph Create(string title, bool slime)
        {
            var root = new GameObject(title);
            var warning = root.AddComponent<EnemyAttackTelegraph>(); warning.slime = slime;
            warning.resources = root.AddComponent<WorldResources>(); warning.block = new MaterialPropertyBlock();
            warning.earthTint = slime ? new Color(.27f, .34f, .13f) : new Color(.23f, .16f, .095f);
            warning.earth = warning.resources.Material(warning.earthTint, false, slime ? VisualSurface.Water : VisualSurface.Stone);
            SkillBodyMaterialLibrary.Apply(warning.earth, slime ? SkillBodyMaterialLibrary.Matter.Mud : SkillBodyMaterialLibrary.Matter.Rock);
            warning.interruptMaterial = warning.resources.Material(new Color(.14f, .46f, .42f), true, VisualSurface.Crystal);
            SkillBodyMaterialLibrary.Apply(warning.interruptMaterial, SkillBodyMaterialLibrary.Matter.Ice, .18f);
            return warning;
        }

        private void SoilLane(Vector3 start, Vector3 end, float halfWidth)
        {
            Vector3 forward = CombatFx.Flat(end - start); float length = forward.magnitude;
            if (length < .01f) return; forward /= length;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            int count = Mathf.Clamp(Mathf.CeilToInt(length * (EffectPreferences.ReducedEffects ? 1.1f : 1.7f)), 8, 26);
            for (int i = 0; i < count; i++)
            {
                float along = (i + .5f) / count;
                float across = Mathf.Sin(i * 2.399963f) * Mathf.Max(0, halfWidth - .26f);
                Vector3 point = Vector3.Lerp(start, end, along) + right * across;
                if (!WorldTraversal.HasGroundPath(start, point, .12f)) continue;
                float size = Mathf.Min(halfWidth * .72f, .46f + .12f * Mathf.Sin(i * 3.13f));
                Add(point, new Vector3(size, .16f, size * 1.1f), i);
            }
        }

        private void Add(Vector3 point, Vector3 scale, int index)
        {
            Transform body = EnemySkillPhysicalVfx.Body(transform, slime ? "Loaded wet slime deposit" : "Loaded weathered soil clod", earth, slime);
            body.rotation = Quaternion.Euler(0, index * 73f, 0);
            points.Add(point); scales.Add(scale); bodies.Add(body); renderers.Add(body.GetComponent<Renderer>());
        }

        private void Core(Vector3 center)
        {
            interruptCore = EnemySkillPhysicalVfx.Body(transform, "Exposed interruptible crystal core", interruptMaterial, false);
            interruptCore.position = WorldTerrain.Ground(center, .52f); interruptCore.localScale = new Vector3(.32f, .55f, .32f);
            interruptCore.gameObject.SetActive(false);
        }

        public void SetInterruptible(bool value)
        {
            interruptible = value;
            if (interruptCore != null) interruptCore.gameObject.SetActive(value);
        }

        public void SetControlWarning(bool value) { controlWarning=value; Present(); }

        public void SetProgress(float value) { progress = Mathf.Clamp01(value); Present(); }

        private void Present()
        {
            for (int i = 0; i < bodies.Count; i++)
            {
                float load = (controlWarning ? .65f : .35f) + progress * .65f;
                bodies[i].position = WorldTerrain.Ground(points[i], -.025f + scales[i].y * load * .4f);
                bodies[i].localScale = Vector3.Scale(scales[i], new Vector3(1, load, 1));
                Color tint = Color.Lerp(earthTint, slime ? new Color(.36f, .43f, .18f) : danger, progress);
                if(controlWarning)tint=Color.Lerp(new Color(.85f,.37f,.06f),new Color(1f,.12f,.035f),progress);
                block.Clear(); block.SetColor("_Color", tint); block.SetColor("_Tint", tint);
                block.SetFloat("_Heat", controlWarning ? .65f+progress*.35f : slime ? 0 : progress * .8f);
                block.SetColor("_EmissionColor", (slime ? new Color(.18f, .24f, .025f) : new Color(.7f,.13f,.015f)) * (controlWarning ? .45f+progress*.55f : progress*.025f));
                renderers[i].SetPropertyBlock(block);
            }
            if (interruptCore != null)
            {
                interruptCore.gameObject.SetActive(interruptible);
                interruptCore.localScale = new Vector3(.32f, .43f + .19f * progress, .32f);
            }
            if (emitterBody != null) emitterBody.localScale = new Vector3(.38f,.46f,.48f) * (.6f + progress * .5f);
        }

        private void ClearBodies()
        {
            foreach (var body in bodies) if (body != null) { body.gameObject.SetActive(false); Destroy(body.gameObject); }
            bodies.Clear(); points.Clear(); scales.Clear(); renderers.Clear();
        }

        private void LateUpdate()
        {
            var game = GameSession.Instance;
            if (game != null && game.InputBlocked) return;
            RefreshCircle();
        }
    }
}
