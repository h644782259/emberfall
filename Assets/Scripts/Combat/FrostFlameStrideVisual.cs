using UnityEngine;
namespace Emberfall
{
    // Movement presentation only. Burn's FlameRide still owns its three real
    // damage footprints and shared tick. Other builds retain frost pulse damage.
    internal sealed class FrostFlameStrideVisual : MonoBehaviour
    {
        private const int TrailSlots = 4;
        private static readonly System.Collections.Generic.Dictionary<PlayerController, FrostFlameStrideVisual> current = new System.Collections.Generic.Dictionary<PlayerController, FrostFlameStrideVisual>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void Reset() { current.Clear(); }
        private PlayerController owner;
        private GameSession session;
        private int epoch, cursor;
        private float age, nextStamp;
        private Vector3 previous, stamped;
        private System.Func<float> remaining;
        private Transform jets;
        private readonly Transform[] trails = new Transform[TrailSlots];
        private readonly Renderer[] trailSurfaces = new Renderer[TrailSlots];
        private readonly float[] stamps = new float[TrailSlots];
        private Material flame;
        private Renderer[] jetSurfaces;
        private MaterialPropertyBlock block;
        private bool finishing;

        internal static void Spawn(PlayerController hero, System.Func<float> seconds)
        {
            if (hero == null || hero.IsDead) return;
            FrostFlameStrideVisual old;
            if (current.TryGetValue(hero, out old) && old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            var root = new GameObject("Frost flame walker / moving jets and bounded ember trail");
            if (CombatVisualLease.Attach(root, CombatVisualPriority.ActionBody) == null) return;
            var fx = root.AddComponent<FrostFlameStrideVisual>(); fx.owner = hero; fx.session = GameSession.Instance;
            current[hero] = fx; fx.epoch = hero.CombatEpoch; fx.remaining = seconds; fx.previous = fx.stamped = hero.transform.position;
            fx.Build();
        }
        private void Build()
        {
            block = new MaterialPropertyBlock();
            flame = new Material(Resources.Load<Shader>("SpellEnergySurface")) { name = "Walker / natural flame fuel" };
            SkillBodyMaterialLibrary.Bind(flame);
            jets = new GameObject("Paired backward foot flame jets").transform; jets.SetParent(transform, false);
            jetSurfaces = new Renderer[2]; Mesh mesh = HeroSkillBodyAssets.Spell("Flame");
            for (int i = 0; i < 2; i++)
            {
                var jet = Surface(jets, mesh, "Movement flame nozzle " + i, out jetSurfaces[i]);
                jet.localPosition = new Vector3(i == 0 ? -.22f : .22f, .07f, -.16f);
                jet.localRotation = Quaternion.Euler(-70, i == 0 ? -9 : 9, 0);
                jet.localScale = new Vector3(.38f, 1.85f, .46f); jet.gameObject.SetActive(false);
            }
            for (int i = 0; i < TrailSlots; i++)
            {
                trails[i] = Surface(transform, mesh, "Residual movement fuel " + i, out trailSurfaces[i]);
                trails[i].gameObject.SetActive(false); stamps[i] = -100;
            }
        }
        private Transform Surface(Transform parent, Mesh mesh, string name, out Renderer renderer)
        {
            var part = new GameObject(name); part.transform.SetParent(parent, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh; renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial = flame;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = true;
            return part.transform;
        }
        private void LateUpdate()
        {
            if (owner == null || owner.IsDead || session == null || session != GameSession.Instance || session.Player != owner || !session.HasStarted ||
                session.CombatEffectsEnded || owner.CombatEpoch != epoch)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            if (session.InputBlocked || Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            float left = remaining != null ? remaining() : 0;
            if (left <= 0 || float.IsNaN(left) || float.IsInfinity(left)) finishing = true;
            Vector3 position = owner.transform.position;
            Vector3 displacement = CombatFx.Flat(position - previous); previous = position;
            bool moving = !finishing && !owner.IsJumping && displacement.sqrMagnitude > .000025f && displacement.sqrMagnitude < 4;
            jets.SetPositionAndRotation(position, moving ? Quaternion.LookRotation(displacement) : owner.transform.rotation);
            jets.gameObject.SetActive(moving);
            if (moving)
            {
                foreach (var surface in jetSurfaces) Draw(surface, .96f, age, 1.05f);
                if (age >= nextStamp && CombatFx.Flat(position - stamped).sqrMagnitude >= .75f * .75f &&
                    CombatSight.VisualFootprint(position, position, .62f))
                {
                    // A single accepted stride placement samples the current
                    // support. Continuous jet animation never queries terrain.
                    Vector3 at = position; at.y = WorldTraversal.SurfaceHeight(at, .04f) + .012f;
                    var trail = trails[cursor]; trail.position = at;
                    trail.rotation = Quaternion.Euler(0, age * 29, 0); trail.localScale = new Vector3(.62f, .9f, .62f);
                    trail.gameObject.SetActive(true); stamps[cursor] = age;
                    cursor = (cursor + 1) % TrailSlots; stamped = position; nextStamp = age + .2f;
                }
            }
            bool visible = false;
            for (int i = 0; i < TrailSlots; i++)
            {
                float local = age - stamps[i], fuel = Mathf.Clamp01((2.2f - local) / .9f);
                bool live = local < 2.2f; trails[i].gameObject.SetActive(live);
                if (live) { visible = true; Draw(trailSurfaces[i], fuel * .9f, local, fuel * .95f); }
            }
            if (finishing && !visible) { gameObject.SetActive(false); Destroy(gameObject); }
        }
        private void Draw(Renderer surface, float opacity, float phase, float heat)
        {
            block.SetColor("_Tint", new Color(1, .36f, .065f)); block.SetFloat("_Opacity", opacity);
            block.SetFloat("_Phase", phase); block.SetFloat("_Heat", heat); block.SetFloat("_Wispy", .4f); surface.SetPropertyBlock(block);
        }
        private void OnDisable()
        {
            FrostFlameStrideVisual value;
            if (!object.ReferenceEquals(owner, null) && current.TryGetValue(owner, out value) && value == this) current.Remove(owner);
        }
        private void OnDestroy() { OnDisable(); if (flame != null) Destroy(flame); }
    }
}
