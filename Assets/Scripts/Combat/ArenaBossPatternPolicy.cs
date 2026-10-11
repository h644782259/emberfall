namespace Emberfall
{
    /// <summary>Optional arena preference layered over ordinary boss safeguards.</summary>
    public static class ArenaBossPatternPolicy
    {
        public static BossAttackPolicy.Move Preferred(int pattern, float distance, BossAttackPolicy.Move previous, int repeated)
        {
            BossAttackPolicy.Move fallback = BossAttackPolicy.Select(distance, previous, repeated);
            if (!BossAttackPolicy.CanEngage(distance) || pattern < 1 || pattern > 3) return fallback;
            BossAttackPolicy.Move preferred = pattern == 1 ? BossAttackPolicy.Move.Slam :
                pattern == 2 ? BossAttackPolicy.Move.Fan : BossAttackPolicy.Move.Charge;
            if (preferred == BossAttackPolicy.Move.Slam && distance > BossAttackPolicy.SlamRadius) return fallback;
            if (preferred == BossAttackPolicy.Move.Charge && (distance <= 2f || distance > BossAttackPolicy.ChargeRange + 1f)) return fallback;
            if (repeated > 0 && previous == preferred) return fallback;
            return preferred;
        }
    }
}
