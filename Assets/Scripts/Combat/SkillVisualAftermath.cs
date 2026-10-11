using UnityEngine;
namespace Emberfall
{
    // Detached visual children outlive a completed field. Cancellation never
    // enters this path; ownership/epoch changes still retire the tail immediately.
    [DefaultExecutionOrder(250)]
    internal sealed class SkillVisualAftermath : MonoBehaviour
    {
        private PlayerController owner;
        private GameSession session;
        private int epoch;
        private float age, life;
        private Renderer[] surfaces;
        private MaterialPropertyBlock block;
        internal static void CompleteFieldChildren(Transform producer, PlayerController hero, bool finale = false)
        {
            if (producer == null || hero == null) return;
            for (int i = producer.childCount - 1; i >= 0; i--)
            {
                Transform child = producer.GetChild(i);
                if (child.GetComponent<ParticleSystem>() == null && child.GetComponent<ElementalFieldVisual>() == null) continue;
                child.SetParent(null, true);
                var tail = child.gameObject.AddComponent<SkillVisualAftermath>();
                tail.owner = hero; tail.session = GameSession.Instance; tail.epoch = hero.CombatEpoch;
                tail.life = SkillVisualAftermathTiming.Tail(finale);
                tail.surfaces = child.GetComponentsInChildren<Renderer>(); tail.block = new MaterialPropertyBlock();
                foreach (var particles in child.GetComponentsInChildren<ParticleSystem>())
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        private void LateUpdate()
        {
            if (owner == null || owner.IsDead || session == null || session != GameSession.Instance || session.Player != owner ||
                owner.CombatEpoch != epoch || !session.HasStarted || session.CombatEffectsEnded)
            { gameObject.SetActive(false); Destroy(gameObject); return; }
            if (session.InputBlocked || Time.deltaTime <= 0) return;
            age += Time.deltaTime;
            if (age >= life) { gameObject.SetActive(false); Destroy(gameObject); return; }
            float coverage = SkillVisualAftermathTiming.Coverage(life - age);
            foreach (var surface in surfaces) if (surface != null)
            { surface.GetPropertyBlock(block); block.SetFloat("_Opacity", coverage); surface.SetPropertyBlock(block); }
        }
    }
}
