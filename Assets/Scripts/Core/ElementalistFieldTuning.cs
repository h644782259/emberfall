namespace Emberfall
{
    // User-requested coverage/pull changes. Damage coefficients, contact counts,
    // cadence, resources and cooldowns remain SkillDamageBudgets' authority.
    public static class ElementalistFieldTuning
    {
        public const float GravityRadius = 5.8f;
        // Lens quad width is 1.4 * size; the visible circular halo ends at UV radius .47.
        public const float GravityDamageRadius = GravityRadius * .7f * .47f;
        public const float GravityPullRadius = 6.4f;
        public const float GravityCollapseRadius = GravityDamageRadius;
        public static float GravityPullStrength(int rank) => rank >= 3 ? 12f : 9f;
        public const float UltimatePulseRadius = GameBalance.ArcanistPulseRadius;
        public const float UltimateFinalRadius = GameBalance.ArcanistFinaleRadius;
        public const float UltimateTailRadius = GameBalance.ArcanistPulseRadius;
        public const float UltimateVisualLife = 2.8f;
        public const float UltimateVisualTail = 1.8f;
    }
}
