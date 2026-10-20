using UnityEngine;

namespace Emberfall
{
    public sealed partial class PlayerController
    {
        private float SkillPreparationSeconds => SkillCastingPolicy.Preparation(pendingSkillReleaseClass,pendingSkillReleaseSlot);
        private int pendingSkillReleaseSlot = -1, pendingSkillReleaseEpoch, skillReleaseInputFrame = -1;
        private float pendingSkillReleaseAge;
        private Vector3 pendingSkillReleaseAim, pendingSkillReleaseForward;
        private EnemyController pendingSkillReleaseTarget;
        private GameSession pendingSkillReleaseSession;
        private HeroClass pendingSkillReleaseClass;
        private bool executingPreparedSkill;
        internal bool HasPendingSkillRelease { get { return pendingSkillReleaseSlot >= 0; } }
        private bool SkillReleaseInputBlocked { get { return blinkFollowupPending || HasPendingSkillRelease || skillReleaseInputFrame == Time.frameCount; } }
        private float SkillPreparationProgress { get { return Mathf.Clamp01(pendingSkillReleaseAge / SkillPreparationSeconds); } }

        // Input pays once; the paid release is consumed only after preparation completes.
        private int paidSkillSlot = -1;
        internal bool PaySkillPreparation(int slot)
        {
            if (!CanBeginSkillTargeting(slot)) return false;
            int rank = session.Progression.Profile.skillRanks[slot];
            if (!CanUseMovementSkill(slot, rank)) { TraversalFailure(); return false; }
            if(!StockTargetReady(slot,rank)){session.ReportControlFailure("skill"+slot,"无目标");return false;}
            // Limited healing rank one has no defensive benefit: do not pay for an empty heal.
            if (!SkillHealingHasEffect(slot,rank))
            { session.Notify("生命已满，无需使用治疗技能。"); return false; }
            if (HeroClass==HeroClass.Vanguard && slot == 6 && skillRuntime.Remaining(slot) <= 0 && Energy >= GameBalance.SkillEnergyCost(HeroClass, slot) && !session.TrySpendHealingCharge()) return false;
            if (!skillRuntime.TryConsume(slot, rank, (ActiveRunBonuses == null ? 1f : ActiveRunBonuses.CooldownMultiplier)*(1f-stats.CooldownReduction)))
            {
                if(skillRuntime.StockPersistenceFailed){session.Notify("技能未释放："+session.Progression.LastError);return false;}
                if (skillFeedbackCooldown <= 0)
                {
                    float remaining = skillRuntime.Remaining(slot);
                    session.ReportControlFailure("skill"+slot,remaining>0?"冷却":"缺能");
                    skillFeedbackCooldown = .8f;
                }
                return false;
            }
            if(slot!=9)ultimateArmorUntil=0;
            paidSkillSlot = slot;
            StopSkillChannel();
            return true;
        }
        private bool BeginSkillRelease(int slot)
        {
            if (SkillReleaseInputBlocked || !PaySkillPreparation(slot)) return false;
            StopSkillChannel();
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
        private void OnDisable() { ClearBlinkFollowup(); paidSkillSlot = -1; CancelPendingSkillRelease(); }

        // Explicit scaled-time gate; animation callbacks never own payment/damage.
        private void AdvanceSkillRelease(float deltaTime)
        {
            if (!HasPendingSkillRelease) return;
            if (session == null || session != pendingSkillReleaseSession || session.Player != this ||
                !session.HasStarted || session.CombatEffectsEnded || IsDead || CombatEpoch != pendingSkillReleaseEpoch ||
                HeroClass != pendingSkillReleaseClass || !gameObject.activeInHierarchy)
            { paidSkillSlot = -1; CancelPendingSkillRelease(); if (model != null && !IsDead) model.CancelAction(); return; }
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
