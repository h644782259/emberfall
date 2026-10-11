using UnityEngine;
namespace Emberfall
{
    // Contact-time material bodies, without an independent damage path.
    // Chunks leave the ground under impulse, fall, collide, bounce and settle.
    internal sealed class SkillImpactDetail : MonoBehaviour
    {
        private GameSession session;
        private PlayerController player;
        private EnemyController source;
        private int epoch;
        private float age, life, emitLife, emitClock;
        private bool slime, chargeBody;
        private Transform[] fragments;
        private Renderer[] renderers;
        private Vector3[] positions, velocities, spins;
        private float[] radii;
        private int[] supportRevision;
        private Material material;
        private Color tint;
        private MaterialPropertyBlock block;
        private int nextFragment;
        private Vector3 previousSource;

        private static SkillImpactDetail Create(EnemyController enemy, Vector3 at, float lifetime, int count, bool slime)
        {
            var game = GameSession.Instance;
            if (game == null || game.Player == null || enemy == null) return null;
            var root = new GameObject(slime ? "Enemy contact / wet slime mass" : "Enemy contact / displaced terrain");
            if (CombatVisualLease.Attach(root, CombatVisualPriority.RealContact) == null) return null;
            var fx = root.AddComponent<SkillImpactDetail>();
            fx.session = game; fx.player = game.Player; fx.epoch = game.Player.CombatEpoch; fx.source = enemy;
            fx.life = lifetime; fx.slime = slime; fx.block = new MaterialPropertyBlock();
            var resources = root.AddComponent<WorldResources>();
            fx.tint = slime ? new Color(.29f,.38f,.14f) : new Color(.34f,.265f,.18f);
            fx.material = resources.Material(fx.tint, false, slime ? VisualSurface.Water : VisualSurface.Stone);
            SkillBodyMaterialLibrary.Apply(fx.material, slime ? SkillBodyMaterialLibrary.Matter.Mud : SkillBodyMaterialLibrary.Matter.Rock);
            fx.fragments = new Transform[count]; fx.renderers = new Renderer[count];
            fx.positions = new Vector3[count]; fx.velocities = new Vector3[count]; fx.spins = new Vector3[count];
            fx.radii = new float[count]; fx.supportRevision = new int[count];
            for (int i = 0; i < count; i++)
            {
                fx.fragments[i] = EnemySkillPhysicalVfx.Body(root.transform, slime ? "Viscous droplet" : "Weathered impact rubble", fx.material, slime);
                fx.renderers[i] = fx.fragments[i].GetComponent<Renderer>(); fx.fragments[i].gameObject.SetActive(false);
            }
            return fx;
        }

        internal static void EnemyRelease(EnemyController enemy, Vector3 at, float radius, bool slam, bool slime)
        {
            int count = EffectPreferences.ReducedEffects ? (slam ? 9 : 5) : slam ? 18 : slime ? 10 : 6;
            var fx = Create(enemy, at, slam ? 1.65f : 1.25f, count, slime);
            if (fx == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.399963f;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 point = at + outward * radius * Mathf.Sqrt((i + .2f) / count) * .88f;
                if (!EnemyImpactRegion.Contains(enemy.transform.position, at, point, radius)) continue;
                float size = (slime ? .13f : slam ? .21f : .12f) + .055f * (1 + Mathf.Sin(i * 7.1f));
                fx.Launch(i, WorldTerrain.Ground(point, size), outward * (slam ? 2.1f : slime ? 1.8f : 1.2f) + Vector3.up * (slam ? 3.1f + .8f * Mathf.Sin(i) : slime ? 2.6f : 1.7f), size);
            }
            PhysicalSkillVfx.Impact(WorldTerrain.Ground(at), enemy.transform.forward, slime ? new Color(.29f,.38f,.14f) : new Color(.34f,.265f,.18f), Mathf.Min(radius,.9f), slime);
        }

        internal static void ChargeRelease(EnemyController enemy, float duration)
        {
            var fx = Create(enemy, enemy.transform.position, Mathf.Max(.12f,duration) + .7f, EffectPreferences.ReducedEffects ? 6 : 10, false);
            if (fx == null) return;
            fx.chargeBody = true; fx.emitLife = duration; fx.previousSource = enemy.transform.position;
            PhysicalSkillVfx.Impact(enemy.transform.position, enemy.transform.forward, new Color(.52f,.35f,.19f), .6f);
        }

        private void Launch(int i, Vector3 position, Vector3 velocity, float radius)
        {
            positions[i] = position; velocities[i] = velocity; radii[i] = radius;
            supportRevision[i] = -1;
            spins[i] = new Vector3(117 + i * 13, 73 + i * 9, -91 - i * 17);
            fragments[i].position = position; fragments[i].rotation = Quaternion.Euler(i * 29, i * 73, i * 19);
            fragments[i].localScale = slime ? new Vector3(radius * 1.9f, radius * 2.4f, radius * 1.6f) : Vector3.one * radius * 2;
            fragments[i].gameObject.SetActive(true);
        }

        private void Update()
        {
            if (session == null || session != GameSession.Instance || player == null || session.Player != player || player.CombatEpoch != epoch || session.CombatEffectsEnded)
            { Destroy(gameObject); return; }
            if (session.InputBlocked) return;
            float dt = Time.deltaTime; age += dt;
            if (age >= life) { Destroy(gameObject); return; }
            if (chargeBody && age < emitLife && source != null && !source.IsDead && !source.IsStunned && source.enabled)
            {
                Vector3 displacement = CombatFx.Flat(source.transform.position - previousSource);
                emitClock += displacement.magnitude;
                if (emitClock >= (EffectPreferences.ReducedEffects ? .8f : .45f))
                {
                    emitClock = 0;
                    Vector3 forward = displacement.sqrMagnitude > .00001f ? displacement.normalized : source.transform.forward;
                    Vector3 side = Vector3.Cross(Vector3.up, forward) * (nextFragment % 2 == 0 ? 1 : -1);
                    Launch(nextFragment, WorldTerrain.Ground(source.transform.position - forward * .35f + side * .38f, .16f), side * 1.3f - forward * .8f + Vector3.up * 2f, .13f);
                    nextFragment = (nextFragment + 1) % fragments.Length;
                }
                previousSource = source.transform.position;
            }
            for (int i = 0; i < fragments.Length; i++)
            {
                if (!fragments[i].gameObject.activeSelf) continue;
                bool contact = PhysicalSkillMotion.AdvanceSupported(ref positions[i], ref velocities[i], ref supportRevision[i], radii[i], dt, slime ? .08f : .28f, slime ? .9f : .65f);
                fragments[i].position = positions[i];
                if (velocities[i].sqrMagnitude > .06f) fragments[i].Rotate(spins[i] * dt, Space.Self);
                if (contact) spins[i] *= slime ? .25f : .62f;
                if (slime)
                {
                    float squash = contact ? .5f : Mathf.Clamp01(velocities[i].magnitude * .045f);
                    fragments[i].localScale = new Vector3(radii[i] * (1.9f + squash), radii[i] * (2.4f - squash * 2), radii[i] * (1.6f + squash));
                }
                float fade = Mathf.Clamp01((life - age) / .28f);
                block.Clear(); block.SetColor("_Color", tint); block.SetColor("_Tint", tint);
                block.SetFloat("_OcclusionFade", fade); block.SetFloat("_Opacity", fade);
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
