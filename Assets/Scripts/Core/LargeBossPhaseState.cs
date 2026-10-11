using System;

namespace Emberfall
{
    public enum LargeBossPhase { Combat, Windup, Beam, Exposed, Recovery, Finished }

    // Two optional counterplay phases, never an invulnerability or mandatory kill gate.
    public sealed class LargeBossPhaseState : IDisposable
    {
        public const float WindupSeconds = 2.2f, BeamSeconds = 14f, ExposureSeconds = 6f;
        public const float BeamTickSeconds = .65f, BeamDegreesPerSecond = 24f;
        public const float BeamHalfWidth = .55f, BeamLength = 9f, CoreDamageMultiplier = 1.35f;
        public LargeBossPhase Phase { get; private set; }
        public int PhaseNumber { get; private set; }
        public int LiveAnchorMask { get; private set; }
        public float Remaining { get; private set; }
        public float BeamAngle { get; private set; }
        public bool DamagePulse { get; private set; }
        public bool OwnsAttacks { get { return Phase != LargeBossPhase.Combat && Phase != LargeBossPhase.Finished; } }
        public bool Interruptible { get { return Phase == LargeBossPhase.Windup; } }
        public float IncomingMultiplier { get { return Phase == LargeBossPhase.Exposed ? CoreDamageMultiplier : 1f; } }
        private float nextPulse, phaseCooldown;
        private bool anchorsCommitted, followupPending;
        private readonly bool chapterFollowup, starSweepTrial;
        public float CurrentBeamLength { get { return starSweepTrial && !IsFollowup ? 14f : BeamLength; } }
        public float CurrentBeamDegreesPerSecond { get { return starSweepTrial && !IsFollowup ? 18f : BeamDegreesPerSecond; } }
        public bool IsFollowup { get; private set; }
        public LargeBossPhaseState(bool chapterFollowup = false, bool starSweepTrial = false)
        { this.chapterFollowup = chapterFollowup; this.starSweepTrial = chapterFollowup && starSweepTrial; }

        public bool TryBegin(float healthFraction, bool combatActive)
        {
            if (!combatActive || Phase != LargeBossPhase.Combat || phaseCooldown > 0 || PhaseNumber >= 2 ||
                !Finite(healthFraction) || healthFraction <= 0 || healthFraction > (PhaseNumber == 0 ? .7f : .35f)) return false;
            PhaseNumber++; followupPending = chapterFollowup; IsFollowup = false; Phase = LargeBossPhase.Windup; Remaining = WindupSeconds; BeamAngle = 0;
            LiveAnchorMask = 0; anchorsCommitted = false; DamagePulse = false; return true;
        }
        public bool CommitAnchors(int mask)
        {
            if (Phase != LargeBossPhase.Windup || anchorsCommitted || mask < 0 || mask > 7) return false;
            anchorsCommitted = true; LiveAnchorMask = mask;
            if (mask == 0) Expose(); // Unavailable safe placements must not stall the encounter.
            return true;
        }
        public bool DestroyAnchor(int phaseNumber, int index)
        {
            if (!anchorsCommitted || phaseNumber != PhaseNumber || index < 0 || index > 2 ||
                (Phase != LargeBossPhase.Windup && Phase != LargeBossPhase.Beam)) return false;
            int bit = 1 << index;
            if ((LiveAnchorMask & bit) == 0) return false;
            LiveAnchorMask &= ~bit;
            if (LiveAnchorMask == 0) Expose();
            return true;
        }
        // Host must first succeed through the existing per-cast EnemyControlPolicy.
        public bool InterruptWindup()
        { if (!Interruptible) return false; if (chapterFollowup) { followupPending=false; Phase=LargeBossPhase.Recovery; Remaining=2f; DamagePulse=false; LiveAnchorMask=0; } else Expose(); return true; }
        private void Expose()
        { followupPending = false; Phase = LargeBossPhase.Exposed; Remaining = ExposureSeconds; DamagePulse = false; LiveAnchorMask = 0; }
        public void Advance(float delta, bool combatActive, bool bossAlive)
        {
            DamagePulse = false;
            if (!bossAlive) { Dispose(); return; }
            if (Phase == LargeBossPhase.Finished || !combatActive || !Finite(delta) || delta <= 0) return;
            // No catch-up damage bursts or skipped readable windups after a long frame.
            float step = Math.Min(delta, .1f);
            if (Phase == LargeBossPhase.Combat) { phaseCooldown = Math.Max(0, phaseCooldown - step); return; }
            if (Phase == LargeBossPhase.Windup && !anchorsCommitted) return;
            Remaining = Math.Max(0, Remaining - step);
            if (Phase == LargeBossPhase.Beam)
            {
                BeamAngle = (BeamAngle + step * CurrentBeamDegreesPerSecond) % 360f;
                nextPulse -= step;
                if (Remaining > 0 && nextPulse <= 0) { DamagePulse = true; nextPulse = BeamTickSeconds; }
            }
            if (Remaining > .0001f) return;
            if (Phase == LargeBossPhase.Windup)
            { Phase = LargeBossPhase.Beam; Remaining = chapterFollowup && IsFollowup ? 4f : BeamSeconds; BeamAngle = 0; nextPulse = BeamTickSeconds; }
            else if (Phase == LargeBossPhase.Beam)
            { Phase = LargeBossPhase.Recovery; Remaining = 2f; LiveAnchorMask = 0; DamagePulse = false; }
            else if (Phase == LargeBossPhase.Recovery && followupPending)
            { followupPending = false; IsFollowup = true; Phase = LargeBossPhase.Windup; Remaining = WindupSeconds; BeamAngle = 0; LiveAnchorMask = 0; anchorsCommitted = true; }
            else { Phase = LargeBossPhase.Combat; Remaining = 0; phaseCooldown = 4f; }
        }
        public void Dispose()
        { followupPending = false; Phase = LargeBossPhase.Finished; Remaining = 0; LiveAnchorMask = 0; DamagePulse = false; }
        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
