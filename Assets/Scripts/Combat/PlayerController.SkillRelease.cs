using UnityEngine;

namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private const float SkillPreparationSeconds = .14f;
        private int pendingSkillReleaseSlot = -1, pendingSkillReleaseEpoch, skillReleaseInputFrame = -1;
        private float pendingSkillReleaseAge;
        private Vector3 pendingSkillReleaseAim, pendingSkillReleaseForward;
        private EnemyController pendingSkillReleaseTarget;
        private GameSession pendingSkillReleaseSession;
        private HeroClass pendingSkillReleaseClass;
        private bool executingPreparedSkill;
        internal bool HasPendingSkillRelease { get { return pendingSkillReleaseSlot >= 0; } }
        private bool SkillReleaseInputBlocked { get { return HasPendingSkillRelease || skillReleaseInputFrame == Time.frameCount; } }
        private float SkillPreparationProgress { get { return Mathf.Clamp01(pendingSkillReleaseAge / SkillPreparationSeconds); } }

        // Preparation is an accepted input, not a paid cast. The original resource
        // transaction and cast receipt are created once at the actual release.
        private bool BeginSkillRelease(int slot)
        {
            if (SkillReleaseInputBlocked || !CanBeginSkillTargeting(slot)) return false;
            pendingSkillReleaseSlot = slot; pendingSkillReleaseEpoch = CombatEpoch;
            pendingSkillReleaseClass = HeroClass; pendingSkillReleaseSession = session;
            pendingSkillReleaseAim = aimPoint; pendingSkillReleaseTarget = AimTarget;
            pendingSkillReleaseForward = transform.forward; pendingSkillReleaseAge = 0;
            AnimateSkillPreparation(slot, 0);
            return true;
        }

        internal void AnimateSkillPreparation(int slot, float progress)
        { if (model != null && !IsDead) model.AnimateCharge(progress, slot); }

        private void CancelPendingSkillRelease()
        {
            if (HasPendingSkillRelease) skillReleaseInputFrame = Time.frameCount;
            pendingSkillReleaseSlot = -1; pendingSkillReleaseAge = 0;
            pendingSkillReleaseSession = null; pendingSkillReleaseTarget = null;
        }
        private void OnDisable() { CancelPendingSkillRelease(); }

        // Explicit scaled-time gate; animation callbacks never own payment/damage.
        private void AdvanceSkillRelease(float deltaTime)
        {
            if (!HasPendingSkillRelease) return;
            if (session == null || session != pendingSkillReleaseSession || session.Player != this ||
                !session.HasStarted || session.CombatEffectsEnded || IsDead || CombatEpoch != pendingSkillReleaseEpoch ||
                HeroClass != pendingSkillReleaseClass || !gameObject.activeInHierarchy)
            { CancelPendingSkillRelease(); if (model != null && !IsDead) model.CancelAction(); return; }
            if (session.InputBlocked || deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            pendingSkillReleaseAge += deltaTime;
            AnimateSkillPreparation(pendingSkillReleaseSlot, SkillPreparationProgress);
            if (pendingSkillReleaseAge < SkillPreparationSeconds) return;

            int slot = pendingSkillReleaseSlot;
            Vector3 snapshotAim = pendingSkillReleaseAim, snapshotForward = pendingSkillReleaseForward;
            EnemyController snapshotTarget = pendingSkillReleaseTarget;
            // Clear before entering the real cast: reentrant damage and repeated
            // updates cannot claim the same preparation or spend its budget twice.
            CancelPendingSkillRelease();
            aimPoint = snapshotAim; AimTarget = ValidAimTarget(snapshotTarget) ? snapshotTarget : null;
            if (snapshotForward.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(snapshotForward);
            int before = nextCastId;
            executingPreparedSkill = true;
            try { CastSkill(slot); }
            finally { executingPreparedSkill = false; }
            if (nextCastId == before && model != null) model.CancelAction();
        }
    }
}
