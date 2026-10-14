using UnityEngine;
namespace Emberfall
{
    // Moving foot jets only. FlameRide owns all ground fire presentation and damage.
    internal sealed class FrostFlameStrideVisual : MonoBehaviour
    {
        private static readonly System.Collections.Generic.Dictionary<PlayerController, FrostFlameStrideVisual> current = new System.Collections.Generic.Dictionary<PlayerController, FrostFlameStrideVisual>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] private static void Reset() { current.Clear(); }
        private PlayerController owner;
        private GameSession session;
        private int epoch;
        private float age;
        private Vector3 previous;
        private System.Func<float> remaining;
        private Transform jets;
        private Material flame;
        private Renderer[] jetSurfaces;
        private MaterialPropertyBlock block;
        private bool finishing;

        internal static void Spawn(PlayerController hero, System.Func<float> seconds)
        {
            if (hero == null || hero.IsDead) return;
            FrostFlameStrideVisual old;
            if (current.TryGetValue(hero, out old) && old != null) { old.gameObject.SetActive(false); Destroy(old.gameObject); }
            var root = new GameObject("Frost flame walker / moving foot jets");
            if (CombatVisualLease.Attach(root, CombatVisualPriority.ActionBody) == null) return;
            var fx = root.AddComponent<FrostFlameStrideVisual>(); fx.owner = hero; fx.session = GameSession.Instance;
            current[hero] = fx; fx.epoch = hero.CombatEpoch; fx.remaining = seconds; fx.previous = hero.transform.position;
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
            }
            // FlameRide owns the continuous ground trail and its retirement.
            if (finishing) { gameObject.SetActive(false); Destroy(gameObject); }
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
